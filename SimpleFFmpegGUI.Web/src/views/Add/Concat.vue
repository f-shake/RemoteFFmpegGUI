<template>
  <div class="add-task-page">
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><FolderOpened /></el-icon>
          <span>输入和输出</span>
        </div>
      </template>
      <FileIOGroup :inputs="files" :output="output" :min="2" ref="io" :showClip="false" :singleOutput="true" />
    </el-card>

    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Setting /></el-icon>
          <span>参数</span>
        </div>
      </template>
      <CodeArguments ref="args" :type="4" />
    </el-card>

    <AddToTaskButtons :addFunc="addTask" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, loadArgs } from '../../common'
import * as net from '../../net'
import CodeArguments from '../../components/CodeArguments.vue'
import AddToTaskButtons from '../../components/AddToTaskButtons.vue'
import FileIOGroup from '../../components/FileIOGroup.vue'

const io = ref<any>(null)
const args = ref<any>(null)
const files = ref<any[]>([{ filePath: '', from: null, to: null, duration: null }])
const output = ref('')

function getNewFile() {
  return { filePath: '', from: null, to: null, duration: null }
}

function addTask(start: boolean) {
  const ioResult = io.value
  files.value = ioResult.getArgs()
  output.value = ioResult.outputFile
  if (files.value.filter((p: any) => p.filePath !== '').length === 0) {
    showError('请选择输入文件')
    return
  }
  const taskArgs = args.value?.getArgs()
  if (taskArgs == null) return
  net.postAddConcatTask({
    inputs: files.value,
    output: output.value,
    parameter: taskArgs
  })
    .then(() => {
      files.value = [getNewFile(), getNewFile()]
      output.value = ''
      showSuccess('已加入队列')
      if (start) net.postStartQueue().catch(showError)
    })
    .catch(showError)
}

onMounted(() => {
  const inputOutput = loadArgs(args.value)
  if (inputOutput.inputs) files.value = inputOutput.inputs
  if (inputOutput.output) output.value = inputOutput.output
})
</script>

<style scoped>
@import './AddCommon.css';
</style>
