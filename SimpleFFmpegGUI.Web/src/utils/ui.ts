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
  ElNotification({
    title: '错误',
    // 后端对内部错误统一返回空 body 的 500（防泄露），展示兜底文案避免空白弹窗
    message: r.response ? r.response.data || '服务器内部错误' : r,
    type: 'error'
  })
}

export function showSuccess(msg: string): void {
  ElNotification({ title: '成功', message: msg, type: 'success' })
}
