import { ElNotification, ElLoading } from 'element-plus'
import type { LoadingInstance } from 'element-plus/es/components/loading/src/loading'

let loadingInstance: LoadingInstance | null = null

export function showLoading(): void {
  loadingInstance = ElLoading.service({})
}

export function closeLoading(): void {
  loadingInstance?.close()
}

export function showError(r: any): void {
  console.log(r)
  ElNotification({
    title: '错误',
    message: r.response ? r.response.data : r,
    type: 'error'
  })
}

export function showSuccess(msg: string): void {
  ElNotification({ title: '成功', message: msg, type: 'success' })
}
