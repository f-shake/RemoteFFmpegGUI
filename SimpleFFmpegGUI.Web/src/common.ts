import { ElNotification, ElLoading } from 'element-plus'
import type { LoadingInstance } from 'element-plus/es/components/loading/src/loading'
import { argKey, inputKey, outputKey } from './parameters'
import axios from 'axios'

let loadingInstance: LoadingInstance | null = null

// 基准目录缓存，用于路径显示
let inputDir: string | null = null
let outputDir: string | null = null

function getUrl(controller: string): string {
  if (process.env.NODE_ENV === 'production') {
    return `api/${controller}`
  }
  return `http://localhost:5001/${controller}`
}

/**
 * 从 API 加载 InputDir/OutputDir 到缓存
 */
export async function loadDirs(): Promise<void> {
  if (inputDir && outputDir) return
  try {
    const r = await axios.get(getUrl('File/Dirs'))
    inputDir = (r.data.inputDir ?? '').replace(/\\/g, '/').replace(/\/$/, '').toLowerCase()
    outputDir = (r.data.outputDir ?? '').replace(/\\/g, '/').replace(/\/$/, '').toLowerCase()
  } catch {
    // 静默
  }
}

/**
 * 去除 InputDir/OutputDir 前缀，只显示相对路径
 */
export function displayPath(path: string | null | undefined): string {
  if (!path) return ''
  const normalized = path.replace(/\\/g, '/')
  for (const dir of [inputDir, outputDir]) {
    if (dir && normalized.toLowerCase().startsWith(dir)) {
      const relative = normalized.substring(dir.length).replace(/^\//, '')
      return relative || path
    }
  }
  return path
}

export function showLoading(): void {
  loadingInstance = ElLoading.service({})
}

export function closeLoading(): void {
  loadingInstance?.close()
}

export function formatDateTime(
  time: Date | string,
  includeDate = true,
  includeTime = true,
  includeYear = true
): string {
  if (typeof time === 'string') {
    time = new Date(time)
  }
  const strDate = includeYear
    ? time.getFullYear().toString().padStart(4, '0') + '-' +
      (time.getMonth() + 1).toString().padStart(2, '0') + '-' +
      time.getDate().toString().padStart(2, '0')
    : (time.getMonth() + 1).toString() + '-' +
      time.getDate().toString().padStart(2, '0')

  const strTime = time.getHours().toString().padStart(2, '0') + ':' +
    time.getMinutes().toString().padStart(2, '0') + ':' +
    time.getSeconds().toString().padStart(2, '0')
  if (includeDate && includeTime) return strDate + ' ' + strTime
  if (includeDate) return strDate
  return strTime
}

export function formatDoubleTimeSpan(seconds: number, includeMs = false): string {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds / 60) % 60)
  const s = seconds - m * 60 - h * 3600
  if (includeMs) {
    return (h > 0 ? String(h) + ':' : '') +
      String(m).padStart(2, '0') + ':' +
      String(Math.floor(s)).padStart(2, '0') + '.' +
      String(s - Math.floor(s)).substring(2, 4)
  }
  return String(h).padStart(2, '0') + ':' +
    String(m).padStart(2, '0') + ':' +
    String(Math.floor(s)).padStart(2, '0')
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

export function jump(url: string): void {
  window.location.href = import.meta.env.BASE_URL + '#/' + url
}

export function loadArgs(argsComponent: any): any {
  if (argsComponent && localStorage.getItem(argKey) != null) {
    const args = JSON.parse(localStorage.getItem(argKey) as string)
    try {
      argsComponent.updateFromArgs(args)
      showSuccess('已加载参数')
    } catch (error) {
      showError('加载参数失败：' + error)
      console.log('错误参数为', args)
      throw error
    } finally {
      localStorage.removeItem(argKey)
    }
  }
  const result: any = {}
  if (localStorage.getItem(outputKey) != null) {
    result.output = localStorage.getItem(outputKey)
  }
  if (localStorage.getItem(inputKey) != null) {
    result.inputs = JSON.parse(localStorage.getItem(inputKey) as string)
  }
  localStorage.removeItem(argKey)
  localStorage.removeItem(outputKey)
  localStorage.removeItem(inputKey)
  return result
}

export function getTaskTypeDescription(type: number): string {
  return TaskType.GetByID(type).Description
}

export function jumpByArgs(args: any, input: any[], output: string, type: number): void {
  localStorage.setItem(argKey, JSON.stringify(args))
  localStorage.setItem(inputKey, JSON.stringify(input))
  localStorage.setItem(outputKey, output)
  jump('add/' + TaskType.GetByID(type).Route)
}

export class TaskType {
  public static Types = [
    new TaskType(0, 'code', 'Code', '转码'),
    new TaskType(4, 'concat', 'Concat', '拼接'),
    new TaskType(1, 'combine', 'Combine', '合并音视频'),
    new TaskType(2, 'compare', 'Compare', '视频对比'),
    new TaskType(3, 'custom', 'Custom', '自定义')
  ]
  public static GetByID(id: number): TaskType {
    const types = this.Types.filter(p => p.Id === id)
    if (types.length === 0) {
      throw new Error('未知类型：' + id)
    }
    return types[0]
  }
  constructor(
    public Id: number,
    public Route: string,
    public Name: string,
    public Description: string
  ) {}
}

export { getUrl }
