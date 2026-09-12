import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import * as net from '@/api'

/**
 * 队列状态（跨组件共享的**单一数据源**）：顶栏、底部状态栏、任务页都从这里读。
 *
 * 改造前同一个 `GET api/Queue` 被 App.vue 与任务页各轮询一次，任务页还额外自己
 * 轮询 HasPending/任务列表——状态的多个生产者正是"请求翻倍 + 定时器泄漏"的根源。
 * 收敛到这里之后，取数方式可以变（轮询 → SignalR 推送 + 断线轮询兜底），
 * 读它的组件一行都不用改。
 */
export const useQueueStore = defineStore('queue', () => {
  /** 队列状态快照，形状与 `GET api/Queue` 的响应一致（后续推送用的是无多余字段的同形结构） */
  const status = ref<any>(null)
  /** 取状态失败：顶栏「获取状态失败」提示用它（后续与实时连接的降级状态合并） */
  const netError = ref(false)
  /** 是否存在排队中任务——控制任务页「开始队列」按钮的置灰 */
  const hasPending = ref(true)
  const scheduleTime = ref('')
  const hasSchedule = ref(false)

  const isProcessing = computed(() => status.value?.isProcessing === true)
  const isPaused = computed(() => status.value?.isPaused === true)

  async function refreshStatus() {
    try {
      const r = await net.getQueueStatus()
      status.value = r.data
      netError.value = false
    } catch {
      netError.value = true
    }
  }

  async function refreshHasPending() {
    try {
      const r = await net.getQueueHasPending()
      hasPending.value = r.data === true
    } catch {
      // 与改造前一致：这个探测失败不打扰用户，按钮保持上一次的可用状态
    }
  }

  /** 取计划时间。失败交给调用方处理（原先在任务页 .catch(showError)） */
  async function refreshSchedule() {
    const r = await net.getQueueScheduleTime()
    const time = r.data
    if (time != null && time !== '') {
      scheduleTime.value = time
      hasSchedule.value = true
    } else {
      // 服务端在队列真正开跑时会把计划时间清空（QueueService.RunQueueAsync 开头会把 scheduleTime 置空），
      // 所以这里必须**对称地**清理本地状态：否则"计划执行过之后"再进任务页，
      // 「已计划开始时间」标签与被锁死的日期选择器会一直残留到刷新整页
      scheduleTime.value = ''
      hasSchedule.value = false
    }
  }

  /**
   * 队列命令的**乐观更新**：点开始/暂停/继续/停止后立刻反馈，不等下一次刷新。
   *
   * 为什么必须有：`StartQueue` 是"发射后不管"（QueueService.StartQueue → RunQueueAsync().ContinueWith，
   * 控制器在真正跑起来之前就返回了），`CancelAsync` 也只是置标志、等当前任务收尾，
   * 所以"命令返回后立刻 GET 一次状态"很可能仍拿到旧值——改造前靠本地直接置位来即时反馈，这里保留同样的语义。
   * 代价与改造前一致：真实状态到达（下一次刷新/推送）即纠正，短暂不一致是允许的。
   */
  function applyOptimisticCommand(command: 'start' | 'pause' | 'resume' | 'cancel') {
    const current = status.value ?? {}
    if (command === 'start') {
      status.value = { ...current, isProcessing: true, isPaused: false }
    } else if (command === 'pause') {
      status.value = { ...current, isPaused: true }
    } else if (command === 'resume') {
      status.value = { ...current, isPaused: false }
    } else {
      status.value = { ...current, isProcessing: false, isPaused: false }
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
    netError,
    hasPending,
    scheduleTime,
    hasSchedule,
    isProcessing,
    isPaused,
    refreshStatus,
    refreshHasPending,
    refreshSchedule,
    applyOptimisticCommand,
    setSchedule,
    clearSchedule,
  }
})
