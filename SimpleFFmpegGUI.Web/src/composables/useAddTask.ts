import { ref } from 'vue'
import { showError, showSuccess } from '@/utils/ui'
import * as net from '@/api'
import { useQueueStore } from '@/stores/queue'

/**
 * Add 任务页面的公共逻辑
 * @param apiCall  创建任务的 API 函数，接收请求体数据
 * @param resetForm 成功后重置表单的回调
 */
export function useAddTask(
  apiCall: (data: any) => Promise<any>,
  resetForm?: () => void,
) {
  const args = ref<any>(null)
  // 在 setup 期间取 store（这几个 Add 页面都在 setup 里调用本 composable）
  const queue = useQueueStore()

  function addTask(start: boolean, data: any) {
    apiCall(data)
      .then(() => {
        resetForm?.()
        showSuccess('已加入队列')
        if (start) {
          // 与任务页的「开始队列」保持一致：走 store 的乐观更新，让顶栏/底部状态栏立刻反映"已在跑"，
          // 而不是等下一次推送/轮询才发现。
          // 顺序必须是"先 POST、成功后再乐观"（与 Tasks.vue 相同）：反过来的话 POST 失败时
          // 也会先亮出"运行中"，要等下一次状态到达才纠正；
          // 同时把"发起前的状态序号"一并传下去，避免把已经先到的推送结果盖回旧值
          const issuedSeq = queue.currentSeq()
          net.postStartQueue()
            .then(() => queue.applyOptimisticCommand('start', issuedSeq))
            .catch(showError)
        }
      })
      .catch(showError)
  }

  return { args, addTask }
}
