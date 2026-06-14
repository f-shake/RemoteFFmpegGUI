<template>
  <div class="out">
    <div class="in">
      <div class="left-group">
        <el-button text @click="viewArgs" class="view-btn">查看参数</el-button>
      </div>
      <div class="right-group">
        <el-button type="primary" @click="add(false)" class="action-btn">加入队列</el-button>
        <el-button @click="add(true)" class="action-btn">加入队列并立即开始</el-button>
      </div>
    </div>

    <el-dialog v-model="dialogVisible" title="ffmpeg 输出参数" width="80%" top="5vh">
      <pre class="args-output">{{ argsText }}</pre>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { showError } from '@/utils/ui'
import * as net from '@/api'

const props = defineProps<{
  addFunc: (start: boolean) => void
  getArgs?: () => any
}>()

const dialogVisible = ref(false)
const argsText = ref('')

function add(start: boolean) {
  props.addFunc(start)
}

async function viewArgs() {
  if (!props.getArgs) return
  const parameters = props.getArgs()
  if (!parameters) return
  try {
    const res = await net.previewArguments(parameters)
    argsText.value = res.data
    dialogVisible.value = true
  } catch (e) {
    showError(e)
  }
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
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding-right: 8px;
}
.left-group {
  display: flex;
  gap: 8px;
  padding-left: 8px;
}
.right-group {
  display: flex;
  gap: 12px;
  justify-content: flex-end;
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
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.view-btn {
  font-size: 13px;
}

.args-output {
  background: var(--el-fill-color-lighter);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 16px;
  font-family: var(--font-mono);
  font-size: 12px;
  line-height: 1.6;
  overflow-x: auto;
  white-space: pre-wrap;
  word-break: break-all;
  margin: 0;
  max-height: 60vh;
}

@media (max-width: 640px) {
  .out {
    padding: 12px 12px;
  }
  .in {
    padding-right: 0;
  }
  .right-group {
    flex: 1;
    gap: 8px;
  }
  .action-btn {
    flex: 1;
    min-width: 0;
  }
  .left-group {
    display: none;
  }
}
</style>
