import { argKey, inputKey, outputKey } from '@/constants/encoding'
import axios from 'axios'
import { showError, showSuccess } from './ui'
import { TaskType } from '@/models/TaskType'
import router from '@/router'

// 基准目录缓存，用于路径显示
let inputDir: string | null = null
let outputDir: string | null = null

function getApiUrl(controller: string): string {
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
    const r = await axios.get(getApiUrl('File/Dirs'))
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

export function jump(url: string): void {
  router.push('/' + url).catch(() => {})
}

export function loadArgs(argsComponent: any): any {
  if (argsComponent) {
    const raw = localStorage.getItem(argKey)
    if (raw != null) {
      try {
        const args = JSON.parse(raw)
        argsComponent.updateFromArgs(args)
        showSuccess('已加载参数')
      } catch (error) {
        showError('加载参数失败：' + error)
        console.log('错误参数为', raw)
      } finally {
        localStorage.removeItem(argKey)
      }
    }
  }
  const result: any = {}
  if (localStorage.getItem(outputKey) != null) {
    result.output = localStorage.getItem(outputKey)
  }
  if (localStorage.getItem(inputKey) != null) {
    try {
      result.inputs = JSON.parse(localStorage.getItem(inputKey) as string)
    } catch {
      // 忽略无效输入
    }
  }
  localStorage.removeItem(argKey)
  localStorage.removeItem(outputKey)
  localStorage.removeItem(inputKey)
  return result
}

export function jumpByArgs(args: any, input: any[], output: string, type: number): void {
  if (args != null) {
    localStorage.setItem(argKey, JSON.stringify(args))
  }
  localStorage.setItem(inputKey, JSON.stringify(input))
  localStorage.setItem(outputKey, output ?? '')
  jump('add/' + TaskType.GetByID(type).Route)
}
