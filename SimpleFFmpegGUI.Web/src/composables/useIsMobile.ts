import { computed } from 'vue'
import { MOBILE_BREAKPOINT, useUiStore } from '@/stores/ui'

/**
 * 响应窗口宽度的窄屏检测。
 * 默认断点 680px（取自 `@/stores/ui` 的模块级常量 `MOBILE_BREAKPOINT`），与各处手机端媒体查询
 * （@media (max-width: 680px)）保持一致。
 * 用于需要在模板中用不同控件（如 el-radio-group ↔ el-select）区分手机/桌面时。
 *
 * 宽度本身由 useUiStore 统一维护（全局只有一个 resize 监听）；本 composable 只是按
 * 调用方要的断点换算成布尔值，因此不再各自注册/注销 resize 监听。
 */
export function useIsMobile(breakpoint = MOBILE_BREAKPOINT) {
  const ui = useUiStore()
  // 用 <= 与 CSS 的 @media (max-width: 680px) 语义一致（680 也算窄屏），避免正好 680px 时
  // 模板分支（桌面表格）与 CSS（手机扁平化/横向滚动）不一致
  return { isMobile: computed(() => ui.windowWidth <= breakpoint) }
}
