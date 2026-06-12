<template>
  <div>
    <el-form label-width="100px">
      <h2>输入和输出</h2>
      <el-form-item label="视频1">
        <file-select :file="video1" @update:file="(v: string) => video1 = v" class="right24" />
      </el-form-item>
      <el-form-item label="视频2">
        <file-select :file="video2" @update:file="(v: string) => video2 = v" class="right24" />
      </el-form-item>
    </el-form>
    <AddToTaskButtons :addFunc="addTask" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, loadArgs } from '../../common'
import * as net from '../../net'
import AddToTaskButtons from '../../components/AddToTaskButtons.vue'
import FileSelect from '../../components/FileSelect.vue'

const video1 = ref('')
const video2 = ref('')

function addTask(start: boolean) {
  if (video1.value === '' || video2.value === '') {
    showError('请选择输入文件')
    return
  }
  net.postAddCompareTask({
    inputs: [{ filePath: video1.value }, { filePath: video2.value }],
    start
  })
    .then(() => {
      video1.value = ''
      video2.value = ''
      showSuccess('已加入队列')
    })
    .catch(showError)
}

onMounted(() => {
  const inputOutput = loadArgs(null)
  if (inputOutput.inputs) {
    video1.value = inputOutput.inputs[0]?.filePath ?? ''
    video2.value = inputOutput.inputs[1]?.filePath ?? ''
  }
})
</script>
