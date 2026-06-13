<template>
  <div class="add-task-page">
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><FolderOpened /></el-icon>
          <span>输入和输出</span>
        </div>
      </template>
      <FileIOGroup :inputs="files" :output="output" :showMore="true" ref="io" />
    </el-card>

    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Setting /></el-icon>
          <span>编码参数</span>
        </div>
      </template>
      <CodeArguments ref="args" :type="0" />
    </el-card>

    <AddToTaskButtons :addFunc="addTask" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { loadArgs } from '@/utils/navigation'
import * as net from '@/api'
import { useAddTask } from '@/composables/useAddTask'
import CodeArguments from '@/components/CodeArguments.vue'
import AddToTaskButtons from '@/components/AddToTaskButtons.vue'
import FileIOGroup from '@/components/FileIOGroup.vue'

const io = ref<any>(null)
const { args, addTask: submitTask } = useAddTask(net.postAddCodeTask, resetForm)
const files = ref<any[]>([{ filePath: '', from: null, to: null, duration: null }])
const output = ref('')

function getNewFile() {
  return { filePath: '', from: null, to: null, duration: null }
}

function resetForm() {
  files.value = [getNewFile()]
  output.value = ''
}

function addTask(start: boolean) {
  const ioResult = io.value
  files.value = ioResult.getArgs()
  output.value = ioResult.outputFile
  if (files.value.filter((p: any) => p.filePath !== '').length === 0) {
    // 错误提示在 getArgs 中已处理
    return
  }
  const taskArgs = args.value?.getArgs()
  if (taskArgs == null) return
  submitTask(start, {
    inputs: files.value,
    output: output.value,
    parameter: taskArgs
  })
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
