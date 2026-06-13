<template>
  <div class="add-task-page">
    <el-card shadow="never" class="section-card">
      <template #header>
        <div class="section-title">
          <el-icon><Setting /></el-icon>
          <span>参数</span>
        </div>
      </template>
      <CodeArguments ref="args" :type="99" />
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

const args = ref<any>(null)

function addTask(start: boolean) {
  const taskArgs = args.value?.getArgs()
  net.postAddCustomTask({
    input: null,
    output: null,
    parameter: taskArgs,
    start
  })
    .then(() => showSuccess('已加入队列'))
    .catch(showError)
}

onMounted(() => {
  loadArgs(args.value)
})
</script>

<style scoped>
@import './AddCommon.css';
</style>
