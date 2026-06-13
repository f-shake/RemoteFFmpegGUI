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
