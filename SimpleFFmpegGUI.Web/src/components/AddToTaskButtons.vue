<template>
  <div class="out">
    <div class="in">
      <el-button type="primary" @click="add(false)" class="action-btn">加入队列</el-button>
      <el-button @click="add(true)" class="action-btn">加入队列并立即开始</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  addFunc: (start: boolean) => void
}>()

function add(start: boolean) {
  props.addFunc(start)
}
</script>

<style scoped>
.out {
  position: sticky;
  bottom: 0;
  z-index: 1100;
  background-color: white;
  padding: 16px 0;
  border-top: 1px solid var(--el-border-color-light, #e4e7ed);
}
html.dark .out { background-color: #1a1a2e; }
.in { display: flex; justify-content: flex-end; gap: 8px; flex-wrap: wrap; }

.action-btn {
  min-width: 160px;
  border-radius: 6px;
  font-weight: 500;
  transition: all 0.2s ease;
}
.action-btn:focus,
.action-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.12);
}

@media (max-width: 640px) {
  .out { padding: 12px 12px; }
  .in { flex-wrap: wrap; gap: 8px; }
  .action-btn { flex: 1; min-width: 0; }
}
</style>
