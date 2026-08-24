<template>
  <div class="mobile-pager">
    <button class="pager-nav" :disabled="page <= 1" @click="go(page - 1)" aria-label="上一页">
      <el-icon><ArrowLeft /></el-icon>
    </button>
    <button
      v-for="p in windowPages"
      :key="p"
      class="pager-num"
      :class="{ 'is-current': p === page }"
      @click="go(p)"
    >{{ p }}</button>
    <button class="pager-nav" :disabled="page >= totalPages" @click="go(page + 1)" aria-label="下一页">
      <el-icon><ArrowRight /></el-icon>
    </button>
    <span class="pager-total">共 {{ totalPages }} 页</span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ArrowLeft, ArrowRight } from '@element-plus/icons-vue'

const props = defineProps<{ page: number; total: number; pageSize: number }>()
const emit = defineEmits<{ change: [page: number] }>()

const totalPages = computed(() =>
  props.total <= 0 ? 0 : Math.ceil(props.total / props.pageSize)
)

// 以当前页为中心的 3 个页码窗口（边界钳制）
const windowPages = computed(() => {
  if (totalPages.value <= 0) return [] // 空结果显示空数组，避免渲染孤立的"1"按钮
  const total = Math.max(1, totalPages.value)
  const list: number[] = []
  let start = Math.max(1, props.page - 1)
  let end = start + 2
  if (end > total) {
    end = total
    start = Math.max(1, end - 2)
  }
  for (let i = start; i <= end; i++) list.push(i)
  return list
})

function go(p: number) {
  if (p < 1 || p > totalPages.value || p === props.page) return // 点当前页不发重复请求
  emit('change', p)
}
</script>

<style scoped>
/* 仿 el-pagination background 模式的紧凑分页器（手机端：仅 3 个页码） */
.mobile-pager {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 6px;
}
.pager-num {
  min-width: 30px;
  height: 30px;
  padding: 0 4px;
  border: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
  border-radius: var(--el-border-radius-base);
  font-size: 13px;
  color: var(--text-regular);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.pager-num:hover:not(.is-current) {
  color: var(--el-color-primary);
}
.pager-num.is-current {
  background: var(--el-color-primary);
  border-color: var(--el-color-primary);
  color: #fff;
  font-weight: 600;
}
.pager-nav {
  min-width: 30px;
  height: 30px;
  border: none;
  background: none;
  color: var(--text-regular);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.pager-nav:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.pager-nav:hover:not(:disabled) {
  color: var(--el-color-primary);
}
.pager-total {
  margin-left: 8px;
  font-size: 12px;
  color: var(--text-secondary);
  white-space: nowrap;
}
</style>
