/**
 * 前端部署基址。由后端在托管 index.html 时注入 <base href="{PathBase}/"> 提供，
 * 前端只在 getBase()/getBasePath() 这两处读取 —— 因此 /ffmpeg 之类的部署前缀只需在
 * 后端 appsettings 的 PathBase 配置一次，改前缀无需改前端代码。
 */

export function getBase(): string {
  const href = document.querySelector('base')?.getAttribute('href')
  if (href) return href.endsWith('/') ? href : href + '/'
  // 无 <base>（dev / 裸部署）时退回构建基址（相对 './' 归一为 '/'）
  let base = import.meta.env.BASE_URL || '/'
  if (base === './' || base === '.') base = '/'
  return base.endsWith('/') ? base : base + '/'
}

/** 用于 cookie path（去掉尾斜杠）。 */
export function getBasePath(): string {
  const base = getBase()
  return base === '/' ? '/' : base.slice(0, -1)
}
