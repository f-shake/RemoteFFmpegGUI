<template>
  <div class="task-detail">
    <div class="c-card c-card--thin">
      <div class="c-card-header">
        <el-icon><InfoFilled /></el-icon>
        <span>任务详情</span>
      </div>
      <div class="c-grid">
        <span class="c-label">输入</span>
        <span class="c-val">
          <div v-for="file in (task.displayInputs ?? task.inputs)" :key="file.filePath">
            <div>{{ file.displayPath ?? file.filePath }}</div>
            <div v-if="file.image2" class="c-hint">图像序列，帧率为 {{ file.framerate }}</div>
            <div v-if="file.extra" class="c-hint">额外参数：{{ file.extra }}</div>
            <div v-if="file.from || file.to || file.duration" class="c-hint">
              <span v-if="file.from">开始 {{ file.from }}s</span>
              <span v-if="file.to" class="left12">结束 {{ file.to }}s</span>
              <span v-if="file.duration" class="left12">经过 {{ file.duration }}s</span>
            </div>
          </div>
        </span>
        <span class="c-label">输出</span>
        <span class="c-val" :class="{ 'c-val--nil': !task.output }">{{ task.output || '自动生成' }}</span>
        <span class="c-label">创建时间</span>
        <span class="c-val">{{ task.createTime }}</span>
        <span class="c-label" v-if="task.startTime">开始时间</span>
        <span class="c-val" v-if="task.startTime">{{ task.startTime }}</span>
        <span class="c-label" v-if="task.finishTime">结束时间</span>
        <span class="c-val" v-if="task.finishTime">{{ task.finishTime }}</span>
      </div>
    </div>

    <div v-if="task.fFmpegArguments" class="c-card">
      <div class="c-card-header">
        <el-icon><Operation /></el-icon>
        <span>FFmpeg 参数</span>
      </div>
      <pre class="c-ffmpeg">{{ task.fFmpegArguments }}</pre>
    </div>

    <div v-if="task.message" class="c-card">
      <div class="c-card-header">
        <el-icon><ChatDotSquare /></el-icon>
        <span>信息</span>
      </div>
      <pre class="c-ffmpeg">{{ task.message }}</pre>
    </div>

    <CodeArgumentsDescription v-if="task.parameters" :type="task.type" :args="task.parameters" />
  </div>
</template>

<script setup lang="ts">
import { InfoFilled, Operation, ChatDotSquare } from '@element-plus/icons-vue'
import CodeArgumentsDescription from '@/components/CodeArgumentsDescription.vue'

defineProps<{ task: any }>()
</script>

<style scoped>
/* 任务详情卡片风格（与 CodeArgumentsDescription 一致） */
.task-detail {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 2px 0;
}
.task-detail :deep(.c-card) {
  background: var(--el-fill-color-lighter);
  border-radius: 6px;
  padding: 8px 12px;
  border: 1px solid var(--border-color);
  margin: 0 8px;
}
.task-detail :deep(.c-card-header) {
  display: flex;
  align-items: center;
  gap: 5px;
  font-weight: 600;
  font-size: 12px;
  color: var(--text-primary);
  margin-bottom: 4px;
  padding-bottom: 4px;
  border-bottom: 1px solid var(--border-color);
}
.task-detail :deep(.c-card-header .el-icon) {
  font-size: 14px;
  color: var(--el-color-primary);
}
.task-detail :deep(.c-grid) {
  display: grid;
  gap: 2px 12px;
  font-size: 12px;
  line-height: 1.6;
}
.task-detail :deep(.c-card:not(.c-card--thin) .c-grid) {
  grid-template-columns: auto 1fr auto 1fr auto 1fr;
}
.task-detail :deep(.c-card:not(.c-card--thin) .c-grid.audio-grid) {
  grid-template-columns: auto 1fr auto 1fr;
}
.task-detail :deep(.c-card--thin .c-grid) {
  grid-template-columns: auto 1fr;
}
@media (max-width: 640px) {
  .task-detail :deep(.c-card:not(.c-card--thin) .c-grid) {
    grid-template-columns: auto 1fr;
  }
}
.task-detail :deep(.c-label) {
  color: var(--text-secondary);
  white-space: nowrap;
  text-align: right;
}
.task-detail :deep(.c-val) {
  color: var(--text-primary);
  word-break: break-all;
  min-width: 0;
}
.task-detail :deep(.c-val--nil) {
  color: var(--text-disabled);
  font-style: italic;
}
.task-detail :deep(.c-hint) {
  color: var(--text-secondary);
  font-size: 11px;
  margin-top: 2px;
}
.c-ffmpeg {
  margin: 0;
  font-family: var(--font-mono);
  font-size: 11px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
  color: var(--text-regular);
}
</style>
