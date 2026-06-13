<template>
  <div class="page-container">
    <!-- 查询区 -->
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Search /></el-icon>
          <span>媒体信息查询</span>
        </div>
      </template>
      <div class="query-bar">
        <file-select :file="file" @update:file="(v: string) => file = v" class="query-file" />
        <el-button type="primary" @click="query" :disabled="file === ''" round>查询</el-button>
      </div>
    </el-card>

    <!-- 基本信息 -->
    <el-card v-if="info != null" shadow="never" class="section-card info-card">
      <template #header>
        <div class="section-title">
          <el-icon><InfoFilled /></el-icon>
          <span>基本信息</span>
        </div>
      </template>
      <el-descriptions :column="2" border size="small" class="info-descriptions">
        <el-descriptions-item label="长度" width="80px">{{ formatDoubleTimeSpan(info.duration, true) }}</el-descriptions-item>
        <el-descriptions-item label="格式">{{ info.format }}</el-descriptions-item>
        <el-descriptions-item label="码率">{{ (info.overallBitRate / 1024 / 1024).toFixed(2) }} Mbps</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 视频流 -->
    <el-card v-for="s in info?.videos ?? []" :key="s.index" shadow="never" class="section-card info-card">
      <template #header>
        <div class="section-title">
          <el-icon><VideoCamera /></el-icon>
          <span>视频流 #{{ s.index }}</span>
        </div>
      </template>
      <el-descriptions :column="2" border size="small" class="info-descriptions">
        <el-descriptions-item label="编码">{{ s.format }}</el-descriptions-item>
        <el-descriptions-item label="编码预设">{{ s.format_Profile }}</el-descriptions-item>
        <el-descriptions-item label="码率">{{ s.bitRate == null ? '' : (s.bitRate / 1024 / 1024).toFixed(2) }} Mbps</el-descriptions-item>
        <el-descriptions-item label="帧率">{{ s.frameRate.toFixed(3) }} FPS</el-descriptions-item>
        <el-descriptions-item label="分辨率">{{ s.width }} × {{ s.height }}</el-descriptions-item>
        <el-descriptions-item label="比例">{{ s.displayAspectRatio }}</el-descriptions-item>
        <el-descriptions-item label="像素格式">{{ s.colorSpace }} {{ s.chromaSubsampling }}</el-descriptions-item>
        <el-descriptions-item label="色彩深度">{{ s.bitDepth }}</el-descriptions-item>
        <el-descriptions-item label="旋转">{{ s.rotation }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 音频流 -->
    <el-card v-for="s in info?.audios ?? []" :key="s.index" shadow="never" class="section-card info-card">
      <template #header>
        <div class="section-title">
          <el-icon><Headset /></el-icon>
          <span>音频流 #{{ s.index }}</span>
        </div>
      </template>
      <el-descriptions :column="2" border size="small" class="info-descriptions">
        <el-descriptions-item label="编码">{{ s.format }}</el-descriptions-item>
        <el-descriptions-item label="码率">{{ (s.bitRate / 1024).toFixed(0) }} Kbps</el-descriptions-item>
        <el-descriptions-item label="声道数">{{ s.channels }}</el-descriptions-item>
        <el-descriptions-item label="声道布局">{{ s.channelLayout }}</el-descriptions-item>
        <el-descriptions-item label="采样率">{{ s.samplingRate }} Hz</el-descriptions-item>
        <el-descriptions-item label="默认">{{ s.default }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 字幕 -->
    <el-card v-for="s in info?.texts ?? []" :key="s.index" shadow="never" class="section-card info-card">
      <template #header>
        <div class="section-title">
          <el-icon><ChatLineSquare /></el-icon>
          <span>字幕 #{{ s.index }}</span>
        </div>
      </template>
      <el-descriptions :column="2" border size="small" class="info-descriptions">
        <el-descriptions-item label="编码">{{ s.format }}</el-descriptions-item>
        <el-descriptions-item label="语言">{{ s.language }}</el-descriptions-item>
        <el-descriptions-item label="标题">{{ s.title }}</el-descriptions-item>
        <el-descriptions-item label="默认">{{ s.default }}</el-descriptions-item>
      </el-descriptions>
    </el-card>

    <!-- 详细信息 (JSON 树) -->
    <el-card v-if="info?.raw" shadow="never" class="section-card info-card">
      <template #header>
        <div class="section-title">
          <el-icon><Document /></el-icon>
          <span>详细信息</span>
        </div>
      </template>
      <json-tree :data="parsedRaw" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { showError, formatDoubleTimeSpan, showLoading, closeLoading } from '../common'
import * as net from '../net'
import FileSelect from '../components/FileSelect.vue'
import JsonTree from '../components/JsonTree.vue'

const file = ref('')
const info = ref<any>(null)
const parsedRaw = computed(() => {
  if (!info.value?.raw) return null
  if (typeof info.value.raw === 'string') {
    try { return JSON.parse(info.value.raw) } catch { return info.value.raw }
  }
  return info.value.raw
})

function query() {
  showLoading()
  net.getMediaInfo(file.value)
    .then((response) => { info.value = response.data })
    .catch(showError)
    .finally(closeLoading)
}
</script>

<style scoped>
@import './Add/AddCommon.css';

.page-container {
  max-width: 960px;
  margin: 0 auto;
  padding: 16px 16px 0;
}

.query-bar {
  display: flex;
  align-items: center;
  gap: 12px;
}
.query-file {
  flex: 1;
}

.info-card {
  margin-bottom: 16px;
}

/* descriptions 微调 */
.info-descriptions {
  --el-descriptions-table-border-color: var(--border-color);
}
.info-descriptions :deep(.el-descriptions__title) {
  font-weight: 600;
}
.info-descriptions :deep(.el-descriptions__label) {
  font-weight: 500;
  color: var(--text-secondary);
  background: var(--bg-page);
  white-space: nowrap;
}

@media (max-width: 640px) {
  .page-container {
    padding: 0;
  }
  .query-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .query-bar .el-button {
    width: 100%;
  }
}
</style>
