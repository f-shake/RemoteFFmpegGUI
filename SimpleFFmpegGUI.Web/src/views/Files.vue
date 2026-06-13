<template>
  <div class="page-container">
    <!-- FTP 状态 -->
    <el-card shadow="never" class="section-card" v-if="status != null">
      <template #header>
        <div class="section-title">
          <el-icon><Connection /></el-icon>
          <span>FTP 服务</span>
        </div>
      </template>
      <el-row :gutter="24">
        <el-col :xs="24" :sm="12">
          <div class="ftp-item">
            <div class="ftp-info">
              <span class="ftp-label">输入文件夹</span>
              <el-tag :type="status.inputOn ? 'success' : 'info'" size="small" effect="plain">
                {{ status.inputOn ? '已启动，端口 ' + status.inputPort : '未启动' }}
              </el-tag>
            </div>
            <el-button
              :type="status.inputOn ? 'danger' : 'primary'"
              size="small"
              round
              @click="post(true, !status.inputOn)"
            >{{ status.inputOn ? '关闭' : '开启' }}</el-button>
          </div>
        </el-col>
        <el-col :xs="24" :sm="12">
          <div class="ftp-item">
            <div class="ftp-info">
              <span class="ftp-label">输出文件夹</span>
              <el-tag :type="status.outputOn ? 'success' : 'info'" size="small" effect="plain">
                {{ status.outputOn ? '已启动，端口 ' + status.outputPort : '未启动' }}
              </el-tag>
            </div>
            <el-button
              :type="status.outputOn ? 'danger' : 'primary'"
              size="small"
              round
              @click="post(false, !status.outputOn)"
            >{{ status.outputOn ? '关闭' : '开启' }}</el-button>
          </div>
        </el-col>
      </el-row>
    </el-card>

    <!-- 上传文件 -->
    <el-card shadow="never" class="section-card top24">
      <template #header>
        <div class="section-title">
          <el-icon><Upload /></el-icon>
          <span>上传输入文件</span>
        </div>
      </template>
      <p class="section-desc">仅支持小文件，大文件请通过其他方式上传。</p>
      <el-upload
        :action="getUploadUrl()"
        :auto-upload="false"
        :headers="getHeader()"
        ref="uploadRef"
        drag
        class="upload-area"
      >
        <el-icon class="upload-icon"><UploadFilled /></el-icon>
        <div class="upload-text">拖拽文件到此处，或点击选择</div>
        <div class="upload-actions">
          <el-button type="primary" round class="upload-btn">浏览文件</el-button>
          <el-button type="success" round class="upload-btn" @click.stop="doUpload">上传到服务器</el-button>
        </div>
      </el-upload>
    </el-card>

    <!-- 输出文件下载 -->
    <el-card shadow="never" class="section-card top24" v-if="files != null">
      <template #header>
        <div class="section-title">
          <el-icon><Download /></el-icon>
          <span>输出文件下载</span>
        </div>
      </template>
      <el-table ref="table" :data="files" size="small">
        <el-table-column prop="name" label="文件名" min-width="140" />
        <el-table-column prop="lengthText" label="大小" width="100" />
        <el-table-column prop="lastWriteTime" label="修改时间" width="180">
          <template #default="scope">{{ formatDateTime(scope.row.lastWriteTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="70">
          <template #default="scope">
            <el-button type="text" size="small" @click="download(scope.row)">下载</el-button>
          </template>
        </el-table-column>
        <el-table-column align="right">
          <template #header><el-button type="text" @click="fillData">刷新</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, formatDateTime, showLoading, closeLoading } from '../common'
import * as net from '../net'

const status = ref<any>(null)
const files = ref<any[]>([])
const uploadRef = ref<any>(null)

function getHeader() { return net.getHeader() }
function getUploadUrl() { return net.getUploadUrl() }

function download(file: any) {
  net.download(file.name)
}

function doUpload() {
  uploadRef.value?.submit()
}

function getStatus() {
  return net.getFtpStatus()
    .then((r) => { status.value = r.data })
    .catch(showError)
}

function post(input: boolean, on: boolean) {
  net.postFtp(input, on).then(getStatus).catch(showError)
}

function fillData() {
  showLoading()
  return net.getMediaDetails()
    .then((response) => { files.value = response.data })
    .catch(showError)
    .finally(closeLoading)
}

onMounted(() => {
  showLoading()
  fillData()
  getStatus().finally(closeLoading)
})
</script>

<style scoped>
@import '../assets/page.css';

.section-card {
  margin-bottom: 0;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
  color: var(--text-primary);
}

.section-title .el-icon {
  color: var(--el-color-primary);
}

.section-desc {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--text-secondary);
}

/* FTP 项目 */
.ftp-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 0;
  flex-wrap: wrap;
}
.ftp-info {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}
.ftp-label {
  font-weight: 600;
  font-size: 14px;
  color: var(--text-primary);
}

/* 上传区域 */
.upload-area {
  width: 100%;
}
.upload-area :deep(.el-upload-dragger) {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 24px;
  width: 100%;
  border-radius: var(--radius-md);
  border: 2px dashed var(--border-color);
  transition: border-color var(--transition-fast), background var(--transition-fast);
}
.upload-area :deep(.el-upload-dragger:hover) {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}
.upload-area :deep(.el-upload-dragger.is-dragover) {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}
.upload-icon {
  font-size: 40px;
  color: var(--text-secondary);
}
.upload-text {
  font-size: 14px;
  color: var(--text-secondary);
}
.upload-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  justify-content: center;
  margin-top: 8px;
}

.top24 {
  margin-top: 16px;
}

@media (max-width: 640px) {
  .ftp-item {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
