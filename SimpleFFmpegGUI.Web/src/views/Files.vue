<template>
  <div class="page-container">
    <div v-if="status != null">
      <h2>FTP</h2>
      <el-row>
        <el-col :span="12">
          <h3>输入文件夹</h3>
          <a>{{ status.inputOn ? '已启动，端口号' + status.inputPort : '未启动' }}</a>
          <el-button style="margin-left: 24px" :type="status.inputOn ? 'danger' : 'primary'"
            @click="post(true, !status.inputOn)">{{ status.inputOn ? '关闭' : '开启' }}</el-button>
        </el-col>
        <el-col :span="12">
          <h3>输出文件夹</h3>
          <a>{{ status.outputOn ? '已启动，端口号' + status.outputPort : '未启动' }}</a>
          <el-button style="margin-left: 24px" :type="status.outputOn ? 'danger' : 'primary'"
            @click="post(false, !status.outputOn)">{{ status.outputOn ? '关闭' : '开启' }}</el-button>
        </el-col>
      </el-row>
    </div>
    <div class="top24">
      <h2>上传输入文件</h2>
      <a>仅支持小文件，大文件请通过其他方式上传</a>
      <el-upload class="top12" :action="getUploadUrl()" :auto-upload="false" :headers="getHeader()" ref="uploadRef">
        <template #trigger>
          <el-button type="primary" class="right12">浏览文件</el-button>
        </template>
        <el-button type="success" @click="doUpload">上传到服务器</el-button>
      </el-upload>
    </div>
    <div v-if="files != null" class="top24">
      <h2>输出文件下载</h2>
      <el-table ref="table" :data="files">
        <el-table-column prop="name" label="文件名" min-width="120" />
        <el-table-column prop="lengthText" label="大小" width="120" />
        <el-table-column prop="lastWriteTime" label="修改时间" width="200">
          <template #default="scope">{{ formatDateTime(scope.row.lastWriteTime) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="50">
          <template #default="scope">
            <el-button type="text" size="small" @click="download(scope.row)">下载</el-button>
          </template>
        </el-table-column>
        <el-table-column align="right">
          <template #header><el-button type="text" @click="fillData">刷新</el-button></template>
        </el-table-column>
      </el-table>
    </div>
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
</style>
