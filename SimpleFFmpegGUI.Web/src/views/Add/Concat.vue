<template>
  <div>
    <el-form label-width="100px">
      <h2>输入和输出</h2>
      <FileIOGroup :inputs="files" :output="output" :min="2" ref="io" :showClip="false" :singleOutput="true" />
      <h2>参数</h2>
    </el-form>
    <CodeArguments ref="args" :type="4" />
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
    parameter: taskArgs,
    start
  })
    .then(() => {
      files.value = [getNewFile(), getNewFile()]
      output.value = ''
      showSuccess('已加入队列')
    })
    .catch(showError)
}

onMounted(() => {
  const inputOutput = loadArgs(args.value)
  if (inputOutput.inputs) files.value = inputOutput.inputs
  if (inputOutput.output) output.value = inputOutput.output
})
</script>

<style>
.el-collapse-item__header { height: 32px !important; }
</style>
