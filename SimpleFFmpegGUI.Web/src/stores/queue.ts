import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as net from '@/api'
import * as realtime from '@/utils/realtime'

/**
 * 队列状态（跨组件共享的**单一数据源**）：顶栏、底部状态栏、任务页都从这里读。
 *
 * 取数方式：优先服务端推送（SignalR），连不上/断开后由 utils/realtime.ts 永久降级为 HTTP 轮询，
 * 两条通道都汇入这里的 `applyStatus()`——组件只认这一份状态，不必知道数据是推来的还是问来的。
 *
 * 改造前同一个 `GET api/Queue` 被 App.vue 与任务页各轮询一次，任务页还额外自己轮询
 * HasPending/任务列表——状态的多个生产者正是"请求翻倍 + 定时器泄漏"的根源。
 */
export const useQueueStore = defineStore('queue', () => {
  /** 队列状态快照，形状与推送的 `queueStatus` 一致（裁掉了状态栏不读的大字段） */
  const status = ref<any>(null)
  /** 是否存在排队中任务——控制任务页「开始队列」按钮的置灰 */
  const hasPending = ref(true)
  const scheduleTime = ref('')
  const hasSchedule = ref(false)
  /** 实时连接状态（绿=已连接、黄闪=重试中、蓝=轮询兜底、红=失败、灰=首次连接中） */
  const connection = ref<realtime.RealtimeState>('connecting')
  /** 任务清单版本号：变化即代表"清单可能变了"，任务页 watch 它去抖刷新列表 */
  const tasksVersion = ref(0)

  /** 已应用的状态序号：只接受序号更大的状态（见 applyStatus） */
  let lastSeq = 0
  /** 是否已经从服务端拿到过 hasPending：用于区分"首次赋值"与"真的变化"（见 setHasPending） */
  let hasPendingKnown = false
  /** 事件退订函数（startRealtime/stopRealtime 成对使用） */
  let unsubscribe: (() => void) | null = null

  const isProcessing = computed(() => status.value?.isProcessing === true)
  const isPaused = computed(() => status.value?.isPaused === true)

  /**
   * 应用一份状态。
   *
   * 只在序号更大时接受：重连与降级切换的瞬间推送与轮询可能并存，
   * 必须挡住"已经过时的数据把新数据盖回去"。
   */
  function applyStatus(next: any) {
    if (next == null) {
      return
    }

    const seq: number = typeof next.seq === 'number' ? next.seq : 0
    if (seq > 0 && seq <= lastSeq) {
      return
    }

    const previous = status.value
    if (seq > 0) {
      lastSeq = seq
    }

    status.value = next
    // 任务清单只在任务开始/结束时才会变（进度变化不算）。轮询兜底时没有"清单变了"的事件，
    // 就用这两个信号推断，免得每 3 秒白刷一次任务列表；首次拿到状态（previous 为 null）不算变化，
    // 任务页自己会先拉一次
    if (previous != null && (previous.isProcessing !== next.isProcessing || previous.task?.id !== next.task?.id)) {
      tasksVersion.value++
    }
  }

  /** 应用计划开始时间的**对称**更新：服务端在队列开跑时会清空它，本地也必须能清掉 */
  function applySchedule(time: string | null | undefined) {
    if (time != null && time !== '') {
      scheduleTime.value = time
      hasSchedule.value = true
    } else {
      scheduleTime.value = ''
      hasSchedule.value = false
    }
  }

  /**
   * 更新"是否有排队中任务"。它变化本身就说明任务清单变了（新增/消耗/取消了一个待处理任务），
   * 因此这里让 tasksVersion 自增——降级轮询时没有事件，只看 isProcessing/task.id 会漏掉
   * "空闲期间别的客户端加/取消了任务"这类变化。
   * 第一次拿到值不算变化（初值是为了让按钮先可用），否则每次进任务页都会白刷一次列表。
   */
  function setHasPending(value: boolean) {
    const changed = hasPendingKnown && hasPending.value !== value
    hasPending.value = value
    hasPendingKnown = true
    if (changed) {
      tasksVersion.value++
    }
  }

  /**
   * 主动取一次合并状态（进任务页时立刻要一份当前状态，不必等下一次推送/轮询）。
   * 失败抛给调用方（任务页要据此提示并收掉加载遮罩）。
   */
  async function refreshState() {
    const r = await net.getQueueState()
    const state = r.data
    applyStatus(state?.status)
    setHasPending(state?.hasPending === true)
    applySchedule(state?.scheduleTime)
  }

  /** 建立实时通道并接线（根组件挂载时调用一次，幂等） */
  function startRealtime() {
    if (unsubscribe !== null) {
      return
    }

    unsubscribe = realtime.subscribe({
      onStatus: applyStatus,
      onTasksChanged: (payload) => {
        hasPending.value = payload.hasPending
        tasksVersion.value++
      },
      onHasPending: setHasPending,
      onSchedule: applySchedule,
      onState: (value) => {
        connection.value = value
      },
    })
    void realtime.connect()
  }

  /** 断开实时通道（根组件卸载时调用） */
  function stopRealtime() {
    if (unsubscribe !== null) {
      unsubscribe()
      unsubscribe = null
    }
    void realtime.close()
  }

  /**
   * 当前已应用的状态序号。命令发出前先记下它（`applyOptimisticCommand` 的第二个参数），
   * 用于判断"命令返回时是否已经有更新的真实状态到达"。
   */
  function currentSeq(): number {
    return lastSeq
  }

  /**
   * 队列命令的**乐观更新**：点开始/暂停/继续/停止后立刻反馈，不等下一次推送。
   *
   * 为什么必须有：`StartQueue` 是"发射后不管"（QueueService.StartQueue → RunQueueAsync().ContinueWith，
   * 控制器在真正跑起来之前就返回了），`CancelAsync` 也只是置标志、等当前任务收尾，
   * 所以"命令返回后立刻 GET 一次状态"很可能仍拿到旧值——改造前靠本地直接置位来即时反馈，这里保留同样的语义。
   *
   * `issuedSeq` 是命令发出前的状态序号，**必须传**：服务端的推送往往比 HTTP 响应先到
   * （点"开始队列"后队列已经在跑、任务甚至已经结束，推送都发了，而 POST 的响应才刚回到浏览器），
   * 这时若还按老状态去"乐观"，就会把已经更新的真实状态盖回旧值，而且之后不会再有推送来纠正它
   * ——状态栏就会一直停在"处理中"。因此：期间收到过更新的状态就放弃这次预测。
   */
  function applyOptimisticCommand(command: 'start' | 'pause' | 'resume' | 'cancel', issuedSeq: number) {
    if (lastSeq > issuedSeq) {
      return
    }

    const current = status.value ?? {}
    if (command === 'start') {
      status.value = { ...current, isProcessing: true, isPaused: false }
      realtime.hintProcessing(true)
    } else if (command === 'pause') {
      status.value = { ...current, isPaused: true }
    } else if (command === 'resume') {
      status.value = { ...current, isPaused: false }
    } else {
      status.value = { ...current, isProcessing: false, isPaused: false }
      // 这里**不能**按乐观值降成空闲节奏：`CancelAsync` 只是"置标志、等当前任务收尾"，
      // 命令返回时任务其实还在跑（乐观值只是给界面看的）。保持快节奏才能尽快拿到真相，
      // 而且真停了的话下一次状态就会是"未处理"，节奏自然降下来
      realtime.hintProcessing(true)
    }
  }

  function setSchedule(time: string) {
    scheduleTime.value = time
    hasSchedule.value = true
  }

  function clearSchedule() {
    scheduleTime.value = ''
    hasSchedule.value = false
  }

  return {
    status,
    hasPending,
    scheduleTime,
    hasSchedule,
    connection,
    tasksVersion,
    isProcessing,
    isPaused,
    refreshState,
    startRealtime,
    stopRealtime,
    currentSeq,
    applyOptimisticCommand,
    setSchedule,
    clearSchedule,
  }
})
