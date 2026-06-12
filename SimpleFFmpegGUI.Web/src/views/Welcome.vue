<template>
  <div>
    <el-row :gutter="12">
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <template #header><span>新建任务</span></template>
          <div v-for="type in types" :key="type.Name">
            <el-button type="text" @click="jump('add/' + type.Route)">{{ type.Description }}</el-button>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <template #header><span>工具</span></template>
          <div>
            <el-button type="text" @click="jump('info')">查询媒体信息</el-button>
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <template #header><span>信息</span></template>
          <div><el-button type="text" @click="jump('tasks')">任务列表</el-button></div>
          <div><el-button type="text" @click="jump('preset')">预设</el-button></div>
          <div><el-button type="text" @click="jump('log')">日志</el-button></div>
        </el-card>
      </el-col>
    </el-row>
    <el-link href="https://github.com/f-shake/RemoteFFmpegGUI" target="_blank">项目Github</el-link>
  </div>
</template>

<script setup lang="ts">
import { TaskType, jump } from '../common'
const types = TaskType.Types
</script>
