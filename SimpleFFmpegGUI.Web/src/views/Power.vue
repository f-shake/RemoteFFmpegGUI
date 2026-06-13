<template>
  <div class="page-container">
    <!-- 关机 -->
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><CircleCloseFilled /></el-icon>
          <span>立即关机</span>
        </div>
      </template>
      <p class="section-desc">立即关闭服务器计算机</p>
      <div class="action-row">
        <el-popconfirm title="是否立即关机？" @confirm="shutdown">
          <template #reference><el-button type="danger" round>立即关机</el-button></template>
        </el-popconfirm>
        <el-button @click="abortShutdown" round>终止关机</el-button>
      </div>
    </el-card>

    <!-- 队列结束关机 -->
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Timer /></el-icon>
          <span>队列结束后关机</span>
        </div>
      </template>
      <div class="switch-row">
        <span class="switch-label">完成当前队列后自动关机</span>
        <el-switch v-model="shutdownQueue" @change="setShutdownQueue" />
      </div>
    </el-card>

    <!-- 进程优先级 -->
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Sort /></el-icon>
          <span>进程优先级</span>
        </div>
      </template>
      <p class="section-desc">FFmpeg 进程的默认优先级</p>
      <div class="slider-row">
        <span class="slider-value">{{ processPriorities[defaultProcessPriority] }}</span>
        <el-slider
          class="priority-slider"
          @change="updateDefaultProcessPriority"
          :max="4"
          :show-tooltip="false"
          v-model="defaultProcessPriority"
          :marks="processPriorities"
        />
      </div>
    </el-card>

    <!-- CPU 占用率 -->
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Cpu /></el-icon>
          <span>CPU 占用率</span>
        </div>
      </template>
      <div class="cpu-grid">
        <div v-for="item in cpuCoreUsages" :key="item.id" class="cpu-item">
          <el-progress
            :percentage="item.usage"
            :width="56"
            type="circle"
            :stroke-width="5"
            :color="cpuColors"
          />
          <span class="cpu-label">CPU {{ item.cpuIndex }}-{{ item.coreIndex }}</span>
        </div>
        <div v-if="cpuCoreUsages.length === 0" class="cpu-empty">暂无数据</div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, showLoading, closeLoading } from '@/utils/ui'
import * as net from '@/api'

const shutdownQueue = ref(false)
const cpuCoreUsages = ref<any[]>([])
const defaultProcessPriority = ref(2)
const cpuColors = [
  { color: '#67c23a', percentage: 50 },
  { color: '#e6a23c', percentage: 80 },
  { color: '#f56c6c', percentage: 100 }
]
const processPriorities = {
  0: '空闲',
  1: '低于正常',
  2: '正常',
  3: '高于正常',
  4: '高'
}

function shutdown() {
  net.postShutdown()
    .then(() => showSuccess('发送关机命令成功，计算机将在 3 分钟后关机。'))
    .catch(() => showError('发送关机命令失败'))
}

function abortShutdown() {
  net.postAbortShutdown()
    .then(() => showSuccess('发送终止关机命令成功'))
    .catch(() => showError('发送终止关机命令失败'))
}

function setShutdownQueue(value: boolean) {
  net.postShutdownQueue(value)
    .catch(() => showError('设置队列结束后自动关机失败'))
}

function updateShutdownQueue() {
  net.getShutdownQueue().then((v) => {
    if (v.data === true) shutdownQueue.value = true
    else if (v.data === false) shutdownQueue.value = false
    else showError('获取是否在队列结束后自动关机的状态失败')
    closeLoading()
  })
}

function updateDefaultProcessPriority(priority: number) {
  net.postDefaultProcessPriority(priority)
    .catch(() => showError('修改默认进程优先级失败'))
}

function loadCpuCoreUsage() {
  net.getCpuCoreUsage().then((r) => {
    cpuCoreUsages.value = []
    r.data.forEach((p: any) => {
      if (p.cpuIndex >= 0) {
        p.id = p.cpuIndex * 1000 + p.coreIndex
        p.usage = Math.round(p.usage * 100)
        if (p.usage > 100) p.usage = 100
        if (p.usage < 0) p.usage = 0
        cpuCoreUsages.value.push(p)
      }
    })
  })
}

function loadDefaultProcessPriority() {
  net.getDefaultProcessPriority()
    .then((response) => { defaultProcessPriority.value = response.data })
    .catch(() => showError('加载默认进程优先级失败'))
}

onMounted(() => {
  showLoading()
  updateShutdownQueue()
  loadDefaultProcessPriority()
  loadCpuCoreUsage()
  setInterval(loadCpuCoreUsage, 5000)
})
</script>

<style scoped>
@import '../assets/page.css';

.section-card {
  margin-bottom: 16px;
}

.section-desc {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--text-secondary);
}

.action-row {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.switch-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}
.switch-label {
  font-size: 14px;
  color: var(--text-regular);
}

.slider-row {
  display: flex;
  align-items: flex-start;
  gap: 16px;
}
.slider-value {
  flex-shrink: 0;
  width: 80px;
  font-size: 13px;
  color: var(--el-color-primary);
  font-weight: 600;
  padding-top: 2px;
}
.priority-slider {
  flex: 1;
  min-width: 0;
}

.cpu-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 20px;
}
.cpu-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
}
.cpu-label {
  font-size: 11px;
  color: var(--text-secondary);
  font-family: var(--font-mono);
}
.cpu-empty {
  font-size: 13px;
  color: var(--text-disabled);
  padding: 16px;
}

@media (max-width: 640px) {
  .slider-row {
    flex-direction: column;
    gap: 8px;
  }
  .slider-value {
    width: auto;
  }
}
</style>
