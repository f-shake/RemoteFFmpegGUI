<template>
  <div class="out">
    <div class="in">
      <el-button-group>
        <el-button type="primary" @click="add(false)">加入队列</el-button>
        <el-button @click="add(true)">加入队列并立即开始</el-button>
      </el-button-group>
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
  bottom: -20px;
  width: 100%;
  height: 48px;
  z-index: 1100;
  background-color: white;
}
html.dark .out { background-color: #1a1a2e; }
.in { float: right; margin-top: 8px; }
</style>
