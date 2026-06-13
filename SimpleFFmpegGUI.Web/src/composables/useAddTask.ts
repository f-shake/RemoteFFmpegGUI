import { ref } from 'vue'
import { showError, showSuccess } from '@/utils/ui'
import * as net from '@/api'

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

  function addTask(start: boolean, data: any) {
    apiCall(data)
      .then(() => {
        resetForm?.()
        showSuccess('已加入队列')
        if (start) net.postStartQueue().catch(showError)
      })
      .catch(showError)
  }

  return { args, addTask }
}
