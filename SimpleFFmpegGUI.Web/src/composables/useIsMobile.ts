import { ref, onBeforeUnmount, onMounted } from 'vue'

/**
 * 响应窗口宽度的窄屏检测。
 * 默认断点 680px，与各处手机端媒体查询（@media (max-width: 680px)）保持一致。
 * 用于需要在模板中用不同控件（如 el-radio-group ↔ el-select）区分手机/桌面时。
 */
export function useIsMobile(breakpoint = 680) {
  // 用 <= 与 CSS 的 @media (max-width: 680px) 语义一致（680 也算窄屏），避免正好 680px 时
  // 模板分支（桌面表格）与 CSS（手机扁平化/横向滚动）不一致
  const isMobile = ref(window.innerWidth <= breakpoint)

  function onResize() {
    isMobile.value = window.innerWidth <= breakpoint
  }

  onMounted(() => window.addEventListener('resize', onResize))
  onBeforeUnmount(() => window.removeEventListener('resize', onResize))

  return { isMobile }
}
