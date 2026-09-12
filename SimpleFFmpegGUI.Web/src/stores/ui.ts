import { ref } from 'vue'
import { defineStore } from 'pinia'

/**
 * 窄屏断点：与各处媒体查询 @media (max-width: 680px) 及原 useIsMobile 语义一致（680 也算窄屏）。
 * 导出供 `useIsMobile()` 当默认断点用。组件里那些与 `windowWidth` 比较的 `680` 仍写成字面量
 * （CSS 媒体查询无法引用 TS 常量，两者必须同值），**改断点时要把 CSS 一起改**。
 */
export const MOBILE_BREAKPOINT = 680

/**
 * 全局 UI 状态：主题、侧栏折叠、窗口宽度。
 *
 * 进 pinia 的判据只有一条：**有两个以上互不相邻的组件要读它、或者它要跨页面存活**。
 * 这三样都符合（顶栏读主题、侧栏读折叠、各页面的窄屏分支读宽度），所以集中到这里；
 * 页面私有的数据（任务列表、预设、日志、文件列表…）继续留在各自组件里，不搬。
 */
export const useUiStore = defineStore('ui', () => {
  const windowWidth = ref(window.innerWidth)

  // 最近一次"自动判定"的折叠结果：只在跨越断点时才改写 menuCollapse，
  // 否则窗口宽度在同一侧的每次变化都会把用户手动点的折叠/展开覆盖掉
  // （原先 App.vue 里 getStatus 每 3 秒触发一次，桌面点「折叠菜单」后会被自己弹开）
  const autoCollapse = ref(window.innerWidth <= MOBILE_BREAKPOINT)
  // 初值与自动判定结果同源，避免首帧闪一下展开态
  const menuCollapse = ref(autoCollapse.value)

  const themeMode = ref(localStorage.getItem('theme') || 'auto')

  function applyTheme(mode: string = themeMode.value) {
    if (mode === 'dark') {
      document.documentElement.classList.add('dark')
    } else if (mode === 'light') {
      document.documentElement.classList.remove('dark')
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      document.documentElement.classList.toggle('dark', prefersDark)
    }
  }

  function setTheme(mode: string) {
    themeMode.value = mode
    localStorage.setItem('theme', mode)
    applyTheme(mode)
  }

  function onResize() {
    windowWidth.value = window.innerWidth
    // 用 <= 与 CSS 的 @media (max-width: 680px) 语义一致，避免正好 680px 时
    // 模板分支（桌面表格）与 CSS（手机扁平化）不一致
    const shouldCollapse = window.innerWidth <= MOBILE_BREAKPOINT
    if (shouldCollapse === autoCollapse.value) {
      return
    }
    autoCollapse.value = shouldCollapse
    menuCollapse.value = shouldCollapse
  }

  // 监听全局只注册一次：store 是单例，setup 只在首次 useUiStore() 时执行。
  // 此前 App.vue（且从不移除）、useIsMobile、CodeArguments 各挂了一份 resize 监听。
  window.addEventListener('resize', onResize)
  window
    .matchMedia('(prefers-color-scheme: dark)')
    .addEventListener('change', () => {
      if (themeMode.value === 'auto') {
        applyTheme('auto')
      }
    })

  // 主题须在首帧前应用：setup 在挂载之前执行，与原先 App.vue 模块顶层的调用时机等价
  applyTheme(themeMode.value)

  // 只暴露实际有消费方的状态：windowWidth（StatusBar/useIsMobile）、menuCollapse、themeMode。
  // autoCollapse 与 applyTheme 仅本 store 内部使用，不导出，避免出现"没人用但要维护"的接口
  return {
    windowWidth,
    menuCollapse,
    themeMode,
    setTheme,
  }
})
