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
  background-color: var(--bg-card);
  padding: 16px 0;
  margin-top: auto;
  border-top: 1px solid var(--border-color);
}
.in {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  flex-wrap: wrap;
  padding-right: 8px;
}

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
  .out {
    padding: 12px 12px;
  }
  .in {
    gap: 8px;
    padding-right: 0;
  }
  .action-btn {
    flex: 1;
    min-width: 0;
  }
}
</style>
