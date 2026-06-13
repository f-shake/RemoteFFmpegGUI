<template>
  <div class="add-task-page">
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><FolderOpened /></el-icon>
          <span>输入和输出</span>
        </div>
      </template>
      <el-form label-position="top">
        <el-form-item label="视频">
          <file-select :file="video" @update:file="(v: string) => { video = v; output = v }" style="width:100%" />
        </el-form-item>
        <el-form-item label="音频">
          <file-select :file="audio" @update:file="(v: string) => audio = v" style="width:100%" />
        </el-form-item>
        <el-form-item label="输出文件名">
          <el-input placeholder="留空则自动生成" v-model="output" class="output-input" />
          <div class="gray top12">输出文件名在处理时会自动重命名为首个不存在重复文件的文件名</div>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Setting /></el-icon>
          <span>参数</span>
        </div>
      </template>
      <CodeArguments :type="1" ref="args" />
    </el-card>

    <AddToTaskButtons :addFunc="addTask" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError } from '@/utils/ui'
import { loadArgs } from '@/utils/navigation'
import * as net from '@/api'
import { useAddTask } from '@/composables/useAddTask'
import CodeArguments from '@/components/CodeArguments.vue'
import AddToTaskButtons from '@/components/AddToTaskButtons.vue'
import FileSelect from '@/components/FileSelect.vue'

const { args, addTask: submitTask } = useAddTask(
  (data: any) => net.postAddCombineTask(data),
  () => { video.value = ''; audio.value = ''; output.value = '' }
)
const video = ref('')
const audio = ref('')
const output = ref('')

function addTask(start: boolean) {
  if (video.value === '' || audio.value === '') {
    showError('请选择输入文件')
    return
  }
  submitTask(start, {
    inputs: [{ filePath: video.value }, { filePath: audio.value }],
    output: output.value,
    parameter: args.value?.getArgs()
  })
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

<style scoped>
@import './AddCommon.css';
.output-input { max-width: 400px; }
@media (max-width: 640px) { .output-input { max-width: 100%; } }
</style>
