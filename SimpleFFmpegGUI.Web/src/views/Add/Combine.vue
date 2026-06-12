<template>
  <div>
    <el-form label-width="100px">
      <h2>输入和输出</h2>
      <el-form-item label="视频">
        <file-select :file="video" @update:file="(v: string) => { video = v; output = v }" class="right24" />
      </el-form-item>
      <el-form-item label="音频">
        <file-select :file="audio" @update:file="(v: string) => audio = v" class="right24" />
      </el-form-item>
      <el-form-item label="输出">
        <el-input placeholder="输出文件名" style="width: 300px; display: block" v-model="output" />
        <a class="gray">输出文件名在处理时会自动重命名为首个不存在重复文件的文件名</a>
      </el-form-item>
    </el-form>
    <h2>参数</h2>
    <CodeArguments :type="1" ref="args" />
    <AddToTaskButtons :addFunc="addTask" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, loadArgs } from '../../common'
import * as net from '../../net'
import CodeArguments from '../../components/CodeArguments.vue'
import AddToTaskButtons from '../../components/AddToTaskButtons.vue'
import FileSelect from '../../components/FileSelect.vue'

const args = ref<any>(null)
const video = ref('')
const audio = ref('')
const output = ref('')

function addTask(start: boolean) {
  if (video.value === '' || audio.value === '') {
    showError('请选择输入文件')
    return
  }
  net.postAddCombineTask({
    inputs: [{ filePath: video.value }, { filePath: audio.value }],
    output: output.value,
    parameter: args.value?.getArgs(),
    start
  })
    .then(() => {
      video.value = ''
      audio.value = ''
      output.value = ''
      showSuccess('已加入队列')
    })
    .catch(showError)
}

onMounted(() => {
  const inputOutput = loadArgs(args.value)
  if (inputOutput.inputs) {
    video.value = inputOutput.inputs[0]?.filePath ?? ''
    audio.value = inputOutput.inputs[1]?.filePath ?? ''
  }
  if (inputOutput.output) output.value = inputOutput.output
})
</script>
