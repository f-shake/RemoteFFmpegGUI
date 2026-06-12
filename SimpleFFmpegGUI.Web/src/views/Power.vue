<template>
  <div>
    <div class="top24">
      <h2>立即关机</h2>
      <el-popconfirm title="是否立即关机？" @confirm="shutdown" class="right12">
        <template #reference><el-button type="danger">立即关机</el-button></template>
      </el-popconfirm>
      <el-button @click="abortShutdown">终止关机</el-button>
    </div>
    <div class="top24">
      <h2>队列结束后关机</h2>
      <a class="right12">是否在完成当前队列后自动关机</a>
      <el-switch v-model="shutdownQueue" @change="setShutdownQueue" />
    </div>
    <div class="top24">
      <h2>进程优先级</h2>
      <a>默认进程优先级</a>
      <el-slider style="width: 240px" class="left12" @change="updateDefaultProcessPriority" :max="4" :show-tooltip="false"
        v-model="defaultProcessPriority" :marks="processPriorities" />
    </div>
    <div class="top24">
      <h2>CPU占用率</h2>
      <div v-for="item in cpuCoreUsages" :key="item.id" style="display: inline-block" class="bottom12 right12">
        <el-progress :percentage="item.usage" :width="48" type="circle" :color="colors" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, showLoading, closeLoading } from '../common'
import * as net from '../net'

const shutdownQueue = ref(false)
const cpuCoreUsages = ref<any[]>([])
const defaultProcessPriority = ref(2)
const colors = [
  { color: 'green', percentage: 50 },
  { color: 'orange', percentage: 80 },
  { color: 'red', percentage: 100 }
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
    .then(() => showSuccess('发送关机命令成功，计算机将在3分钟后关机。'))
    .catch(() => showError('发送关机命令失败'))
}

function abortShutdown() {
  net.postAbortShutdown()
    .then(() => showSuccess('发送终止关机命令成功'))
    .catch(() => showError('发送终止关机命令失败'))
}

function setShutdownQueue(value: boolean) {
  net.postShutdownQueue(value)
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
