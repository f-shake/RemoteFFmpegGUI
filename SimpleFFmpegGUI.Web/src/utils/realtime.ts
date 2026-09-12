import Cookies from 'js-cookie'
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr'
import type { HubConnection } from '@microsoft/signalr'
import * as net from '@/api'
import { getUrl } from '@/api'

/**
 * 实时推送的**单例连接**（模块级，不放 pinia：这里没有需要被模板响应式的状态，只有事件与定时器；
 * 连接状态由订阅方（queue store）保存给界面用）。
 *
 * 两条取数通道，只有一条在工作：
 * - WebSocket（SignalR）：连上就只靠推送，服务端空闲时不发消息（靠 SignalR 自己的 ping 保活）；
 * - HTTP 轮询兜底：重连序列（2/4/8 秒）用尽后**永久**转入轮询，只有**刷新页面**才会重新尝试 WebSocket。
 *
 * 页面不可见时暂停轮询，切回来立即补一次。
 *
 * 对外只有 subscribe（订阅事件）、connect/close 与 hintProcessing（把界面的乐观判断告诉轮询节奏）。
 */

/** 实时连接状态：绿点=已连接、黄闪=重试中、蓝点=轮询兜底、红点=HTTP 也失败、灰点=首次连接中 */
export type RealtimeState = 'connecting' | 'ws' | 'retrying' | 'http' | 'failed'

export interface RealtimeListener {
  /** 收到一份队列状态（推送或 HTTP 兜底，两者同形，带自增序号） */
  onStatus(status: any): void
  /** 任务清单发生了变化（只有推送通道会发） */
  onTasksChanged(payload: { reason: string; hasPending: boolean }): void
  /** 是否有排队中任务（推送载荷或轮询结果都会更新） */
  onHasPending(hasPending: boolean): void
  /** 计划开始时间变化（null 表示没有计划） */
  onSchedule(scheduleTime: string | null): void
  /** 连接状态变化 */
  onState(state: RealtimeState): void
}

/** 重连序列：与"黄点闪烁约 14 秒"的手工验收步骤对应（2+4+8 秒） */
const RECONNECT_DELAYS = [2000, 4000, 8000]
/** 降级轮询间隔：处理中 3 秒、空闲 15 秒 */
const POLL_INTERVAL_PROCESSING = 3000
const POLL_INTERVAL_IDLE = 15000

const listeners = new Set<RealtimeListener>()

/**
 * 是否把收到的消息打到浏览器控制台。
 * <para>
 * 开发环境默认打开；生产构建默认关闭，但可以用 URL 参数覆盖：`?realtime=1` 打开、`?realtime=0` 关闭。
 * 排查"经反代后圆点一直是蓝色"这类静默降级时，用它确认浏览器到底有没有收到推送。
 * </para>
 */
let logging = import.meta.env.DEV
try {
  const flag = new URLSearchParams(window.location.search).get('realtime')
  if (flag === '1') {
    logging = true
  } else if (flag === '0') {
    logging = false
  }
} catch {
  // 拿不到 location 就按默认值走，不影响功能
}

/** 打印一条收到的消息（带时间戳，便于和操作时间对照） */
function logReceived(method: string, payload: unknown) {
  if (!logging) {
    return
  }

  console.log(`[realtime] ${new Date().toLocaleTimeString()} ← ${method}`, payload)
}

let connection: HubConnection | null = null
let state: RealtimeState = 'connecting'
/** 已进入轮询兜底：进入后不再自动重连 */
let fallbackStarted = false
let pollTimer: number | null = null
/** 上一次轮询还没回来：网络慢时不再叠一次请求 */
let polling = false
let visibilityBound = false
let stopped = false
/** 最近一份状态里"是否处理中"：决定轮询间隔 */
let processing = false

/**
 * 订阅事件。返回退订函数；订阅时会立刻同步一次当前连接状态，避免订阅方停在初始值上。
 */
export function subscribe(listener: RealtimeListener): () => void {
  listeners.add(listener)
  listener.onState(state)
  return () => {
    listeners.delete(listener)
  }
}

/** 建立连接（幂等）。由根组件在挂载时调用一次 */
export async function connect(): Promise<void> {
  if (connection !== null || fallbackStarted) {
    return
  }

  stopped = false
  const hub = new HubConnectionBuilder()
    // 显式解析成绝对地址：PROD 下 getUrl 返回相对路径（跟随后端注入的 <base> 部署基址），
    // DEV 下它本身就是绝对地址——两种部署形态都不需要另写拼接逻辑
    .withUrl(new URL(getUrl('queue-hub'), document.baseURI).toString(), {
      // 只走 WebSocket：浏览器发 WebSocket 无法自定义请求头，凭据只能靠 Cookie（服务端优先读它）
      // 或 access_token 查询参数（这里由 accessTokenFactory 追加）
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets,
      accessTokenFactory: () => Cookies.get('token') ?? '',
    })
    // 重连用尽后由 onclose 转入轮询兜底，不做传输层回退（回退到 LongPolling 会一直占着 HTTP 连接）
    .withAutomaticReconnect(RECONNECT_DELAYS)
    .configureLogging(LogLevel.Warning)
    .build()
  connection = hub

  hub.on('queueStatus', (status) => {
    logReceived('queueStatus', status)
    notifyStatus(status)
  })
  hub.on('tasksChanged', (payload) => {
    logReceived('tasksChanged', payload)
    const event = { reason: payload?.reason ?? '', hasPending: payload?.hasPending === true }
    listeners.forEach((l) => l.onTasksChanged(event))
  })
  hub.on('scheduleChanged', (payload) => {
    logReceived('scheduleChanged', payload)
    notifySchedule(payload?.scheduleTime ?? null)
  })
  hub.onreconnecting((error) => {
    logReceived('(reconnecting)', error?.message ?? '')
    notifyState('retrying')
  })
  hub.onreconnected((connectionId) => {
    logReceived('(reconnected)', connectionId ?? '')
    notifyState('ws')
    // 断线期间的变化不会再补发，重连后先主动要一次全量
    void fetchState()
  })
  hub.onclose((error) => {
    logReceived('(closed)', error?.message ?? '')
    // 重连序列用尽（或被服务端断开）：永久转入 HTTP 轮询。这里不再尝试 WebSocket——
    // 只有刷新页面才会重新连，避免"时通时不通"的抖动
    if (hub === connection) {
      startFallback()
    }
  })

  notifyState('connecting')
  try {
    await hub.start()
    notifyState('ws')
    // 连上先要一次全量：否则最多要等 1 秒的采样才有状态可显示
    await fetchState()
  } catch {
    // 首次连接就失败：withAutomaticReconnect 只对"连上之后再断开"生效，这里直接进轮询。
    // 与 onclose 一样要判"是不是当前这条连接"：若 start() 在飞期间发生过 close()+connect()，
    // 迟到的失败回调会把新连接之外的轮询也启动起来，变成"WS 连着还在轮询"
    if (hub === connection) {
      startFallback()
    }
  }
}

/** 关闭连接与轮询（根组件卸载时调用） */
export async function close(): Promise<void> {
  stopped = true
  if (pollTimer !== null) {
    clearTimeout(pollTimer)
    pollTimer = null
  }

  if (visibilityBound) {
    document.removeEventListener('visibilitychange', onVisibilityChange)
    visibilityBound = false
  }

  const hub = connection
  connection = null
  if (hub !== null) {
    try {
      await hub.stop()
    } catch {
      // 关闭失败无需处理：连接此时已经不可用
    }
  }

  // 允许之后再次 connect（HMR、重新挂载）；连接状态也一并复位，
  // 否则新订阅者会先看到上一轮的颜色（subscribe 会立刻同步一次当前状态）
  fallbackStarted = false
  state = 'connecting'
}

function notifyState(next: RealtimeState) {
  if (state === next) {
    return
  }

  logReceived('(state)', next)
  state = next
  listeners.forEach((l) => l.onState(next))
}

function notifyStatus(status: any) {
  if (status == null) {
    return
  }

  processing = status.isProcessing === true
  listeners.forEach((l) => l.onStatus(status))
}

function notifySchedule(scheduleTime: string | null) {
  listeners.forEach((l) => l.onSchedule(scheduleTime))
}

/**
 * 取一次合并状态（连上/重连后补全量，以及轮询兜底）。
 * 返回是否成功，供调用方决定"轮询中"还是"失败"。
 */
async function fetchState(): Promise<boolean> {
  try {
    const r = await net.getQueueState()
    const data = r.data
    notifyStatus(data?.status)
    listeners.forEach((l) => l.onHasPending(data?.hasPending === true))
    notifySchedule(data?.scheduleTime ?? null)
    return true
  } catch {
    return false
  }
}

function startFallback() {
  if (stopped || fallbackStarted) {
    return
  }

  fallbackStarted = true
  notifyState('http')
  if (!visibilityBound) {
    document.addEventListener('visibilitychange', onVisibilityChange)
    visibilityBound = true
  }

  void pollOnce()
}

function onVisibilityChange() {
  if (document.hidden) {
    // 不可见时暂停轮询（浏览器本来也会节流定时器，挂着只是徒增噪声）
    if (pollTimer !== null) {
      clearTimeout(pollTimer)
      pollTimer = null
    }
  } else if (pollTimer === null) {
    // 切回来立即补一次，不等下一个周期
    void pollOnce()
  }
}

/**
 * 界面自己做了一次"乐观判断"（例如点了「开始队列」，store 里立刻置成"处理中"）。
 * 轮询的间隔只看本模块自己收到的状态，因此这里显式告知一次：空闲节奏是 15 秒，
 * 不告知的话降级期间点开始会按 15 秒的节奏更新进度，用户要等十几秒才看到变化。
 * WS 通道下只是更新一个内部变量，无副作用。
 */
export function hintProcessing(isProcessingNow: boolean) {
  processing = isProcessingNow
  if (!fallbackStarted || stopped || document.hidden) {
    return
  }

  if (pollTimer !== null) {
    clearTimeout(pollTimer)
    pollTimer = null
  }

  scheduleNextPoll()
}

async function pollOnce() {
  if (pollTimer !== null) {
    clearTimeout(pollTimer)
    pollTimer = null
  }

  try {
    await pollOnceCore()
  } finally {
    // 必须放 finally：订阅方抛异常也不能让轮询停摆（可见状态下没有别的路径会再排下一次）
    scheduleNextPoll()
  }
}

/** 取一次数据：失败也返回（由调用方决定成败状态），下次轮询照排 */
async function pollOnceCore() {
  if (stopped || polling || document.hidden) {
    return
  }

  polling = true
  try {
    // 失败也继续轮询：断网恢复后要能自己回到"轮询中"（圆点由红转蓝）
    notifyState((await fetchState()) ? 'http' : 'failed')
  } finally {
    polling = false
  }
}

function scheduleNextPoll() {
  // 不可见时不排下一个（回到可见时由 visibilitychange 立即补一次），避免后台空转
  if (stopped || document.hidden || pollTimer !== null) {
    return
  }

  pollTimer = window.setTimeout(() => {
    pollTimer = null
    void pollOnce()
  }, processing ? POLL_INTERVAL_PROCESSING : POLL_INTERVAL_IDLE)
}
