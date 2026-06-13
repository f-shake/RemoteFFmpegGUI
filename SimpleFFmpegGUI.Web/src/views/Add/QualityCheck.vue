<template>
  <div class="add-task-page">
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><FolderOpened /></el-icon>
          <span>输入</span>
        </div>
      </template>
      <el-form label-position="top">
        <el-form-item label="视频 1">
          <file-select :file="video1" @update:file="(v: string) => video1 = v" style="width:100%" />
        </el-form-item>
        <el-form-item label="视频 2">
          <file-select :file="video2" @update:file="(v: string) => video2 = v" style="width:100%" />
        </el-form-item>
      </el-form>
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
import AddToTaskButtons from '@/components/AddToTaskButtons.vue'
import FileSelect from '@/components/FileSelect.vue'

const video1 = ref('')
const video2 = ref('')

const { addTask: submitTask } = useAddTask(
  (data: any) => net.postAddCompareTask(data),
  () => { video1.value = ''; video2.value = '' }
)

function addTask(start: boolean) {
  if (video1.value === '' || video2.value === '') {
    showError('请选择输入文件')
    return
  }
  submitTask(start, {
    inputs: [{ filePath: video1.value }, { filePath: video2.value }]
  })
}

onMounted(() => {
  const inputOutput = loadArgs(null)
  if (inputOutput.inputs) {
    video1.value = inputOutput.inputs[0]?.filePath ?? ''
    video2.value = inputOutput.inputs[1]?.filePath ?? ''
  }
})
</script>

<style scoped>
@import '@/assets/AddCommon.css';
</style>
