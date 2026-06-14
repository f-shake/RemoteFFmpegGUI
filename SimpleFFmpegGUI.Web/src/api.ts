import axios from 'axios'
import type { AxiosResponse } from 'axios'
import Cookies from 'js-cookie'

function getUrl(controller: string): string {
  if (process.env.NODE_ENV === 'production') {
    return `api/${controller}`
  }
  return `http://localhost:5001/${controller}`
}

// ===== Task =====

export function getTaskList(status: number | null | undefined, page: number, pageSize: number): Promise<AxiosResponse<any>> {
  let url = `Task?Page=${page}&PageSize=${pageSize}`
  if (status != null) {
    url += `&Status=${status}`
  }
  return axios.get(getUrl(url))
}

export function getTask(id: number): Promise<AxiosResponse<any>> {
  return axios.get(getUrl(`Task/${id}`))
}

export function postAddCodeTask(item: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Transcode'), item)
}

export function postAddConcatTask(item: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Concat'), item)
}

export function postAddCombineTask(item: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Mux'), item)
}

export function postAddCompareTask(item: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/QualityCheck'), item)
}

export function postAddCustomTask(item: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Custom'), item)
}

export function postCancelTask(id: number): Promise<AxiosResponse<any>> {
  return axios.post(getUrl(`Task/${id}/Cancel`))
}

export function postCancelTasks(ids: number[]): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Batch/Cancel'), ids)
}

export function postDeleteTask(id: number): Promise<AxiosResponse<any>> {
  return axios.post(getUrl(`Task/${id}/Delete`))
}

export function postDeleteTasks(ids: number[]): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Batch/Delete'), ids)
}

export function postResetTask(id: number): Promise<AxiosResponse<any>> {
  return axios.post(getUrl(`Task/${id}/Reset`))
}

export function postResetTasks(ids: number[]): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Task/Batch/Reset'), ids)
}

export function getFormats(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Task/Formats'))
}

export function previewArguments(parameters: any): Promise<AxiosResponse<string>> {
  return axios.post(getUrl('Task/PreviewArguments'), parameters)
}

// ===== Queue =====

export function getQueueStatus(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Queue'))
}

export function getQueueScheduleTime(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Queue/Schedule'))
}

export function postStartQueue(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/Start'))
}

export function postPauseQueue(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/Pause'))
}

export function postResumeQueue(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/Resume'))
}

export function postCancelQueue(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/Cancel'))
}

export function postSchedule(time: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/Schedule'), { time })
}

export function postCancelSchedule(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Queue/CancelSchedule'))
}

// ===== Preset =====

export function getPresets(type: number | null = null): Promise<AxiosResponse<any>> {
  let url = 'Preset'
  if (type !== null) {
    url += `?type=${type}`
  }
  return axios.get(getUrl(url))
}

export function postAddOrUpdatePreset(name: string, type: number, args: any): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Preset'), { name, type, parameters: args })
}

export function postDeletePreset(id: number): Promise<AxiosResponse<any>> {
  return axios.post(getUrl(`Preset/${id}/Delete`))
}

export function postClearPresets(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Preset/Clear'))
}

export function getImportPresetsUrl(): string {
  return getUrl('Preset/Import')
}

export function downloadExportPresetsUrl(): void {
  axios.get(getUrl('Preset/Export'), { responseType: 'arraybuffer' }).then(r => {
    const blob = new Blob([r.data])
    const link = document.createElement('a')
    link.href = window.URL.createObjectURL(blob)
    link.download = '远程 FFmpeg工具箱 预设.json'
    link.click()
  })
}

// ===== File =====

export function getMediaNames(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('File/List/Input'))
}

export function getMediaDetails(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('File/List/Output'))
}

export function getUploadUrl(): string {
  return getUrl('File/Upload')
}

export function download(name: string): void {
  axios.get(getUrl(`File/Download/${encodeURI(name)}`), {
    responseType: 'blob',
    headers: getHeader()
  }).then((r) => {
    const blob = new Blob([r.data])
    const link = document.createElement('a')
    link.href = window.URL.createObjectURL(blob)
    link.download = name
    link.click()
  }).catch(() => {})
}

export function getDirs(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('File/Dirs'))
}

export function getFtpStatus(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('File/Ftp'))
}

export function postFtp(input: boolean, on: boolean): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('File/' + (input ? 'Ftp/Input' : 'Ftp/Output') + '/' + (on ? 'On' : 'Off')))
}

// ===== MediaInfo =====

export function getMediaInfo(name: string): Promise<AxiosResponse<any>> {
  return axios.get(getUrl(`MediaInfo/${encodeURI(name)}`))
}

export function getSnapshot(videoPath: string, seconds: number): Promise<AxiosResponse<any>> {
  return axios.get(
    getUrl(`MediaInfo/Snapshot?videoPath=${encodeURI(videoPath)}&seconds=${seconds}`),
    { responseType: 'blob' }
  )
}

// ===== Power =====

export function getCpuCoreUsage(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Power/Cpu'))
}

export function getShutdownQueue(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Power/ShutdownQueue'))
}

export function postShutdownQueue(on: boolean): Promise<AxiosResponse<any>> {
  const form = new FormData()
  form.append('on', String(on))
  return axios.post(getUrl('Power/ShutdownQueue'), form)
}

export function postShutdown(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Power/Shutdown'))
}

export function postAbortShutdown(): Promise<AxiosResponse<any>> {
  return axios.post(getUrl('Power/AbortShutdown'))
}

// ===== Config =====

export function getDefaultProcessPriority(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Config/ProcessPriority'))
}

export function postDefaultProcessPriority(priority: number): Promise<AxiosResponse<any>> {
  return axios.post(getUrl(`Config/ProcessPriority?priority=${priority}`))
}

// ===== Log =====

export function getLogs(
  type: string | null,
  taskId: number,
  from: string | null,
  to: string | null,
  skip: number,
  take: number
): Promise<AxiosResponse<any>> {
  let url = `Log?skip=${skip}&take=${take}`
  if (type) url += `&type=${type}`
  if (from) url += `&from=${from}`
  if (to) url += `&to=${to}`
  if (taskId !== 0) url += `&taskId=${taskId}`
  return axios.get(getUrl(url))
}

// ===== Token =====

export function getNeedToken(): Promise<AxiosResponse<any>> {
  return axios.get(getUrl('Token/Need'))
}

export function getCheckToken(token: string): Promise<AxiosResponse<any>> {
  return axios.get(getUrl(`Token/Check/${token}`))
}

// ===== Auth Helpers =====

export function setHeader(): void {
  axios.defaults.headers.common['Authorization'] = `Bearer ${Cookies.get('token')}`
}

export function getHeader(): Record<string, string> {
  const token = Cookies.get('token')
  if (token == null) return {}
  return { Authorization: `Bearer ${token}` }
}
