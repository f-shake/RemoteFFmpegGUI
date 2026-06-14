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

    <AddToTaskButtons :addFunc="addTask" :getArgs="getArgs" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { loadArgs } from '@/utils/navigation'
import * as net from '@/api'
import { useAddTask } from '@/composables/useAddTask'
import CodeArguments from '@/components/CodeArguments.vue'
import AddToTaskButtons from '@/components/AddToTaskButtons.vue'

const { args, addTask: submitTask } = useAddTask(
  (data: any) => net.postAddCustomTask(data),
)

function addTask(start: boolean) {
  const taskArgs = args.value?.getArgs()
  submitTask(start, {
    input: null,
    output: null,
    parameter: taskArgs
  })
}

function getArgs() {
  return args.value?.getArgs()
}

onMounted(() => {
  loadArgs(args.value)
})
</script>

<style scoped>
@import '@/assets/AddCommon.css';
</style>
