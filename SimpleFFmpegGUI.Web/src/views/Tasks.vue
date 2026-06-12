<template>
  <div>
    <div>
      <span style="float: right">
        <a class="right12" v-if="!isProcessing && hasSchedule">已计划开始时间</a>
        <el-date-picker v-if="!isProcessing" class="right12" v-model="scheduleTime"
          value-format="yyyy-MM-dd[T]HH:mm:ss[Z]" placeholder="计划开始时间" type="datetime" :disabled="hasSchedule" />
        <el-button v-if="!isProcessing && !hasSchedule" :disabled="!scheduleTime" type="primary"
          @click="schedule" class="right24">设置计划</el-button>
        <el-button v-if="!isProcessing && hasSchedule" type="primary" @click="cancelSchedule"
          class="right24">取消计划</el-button>
        <el-button v-if="!isProcessing" type="primary" @click="start">开始队列</el-button>
        <el-popconfirm v-if="isProcessing" title="真的要取消任务吗？" @confirm="cancel" class="right12">
          <template #reference><el-button type="danger">停止</el-button></template>
        </el-popconfirm>
        <el-button class="right12" v-if="isProcessing && !isPaused" type="warning" @click="pause">暂停</el-button>
        <el-button class="right12" v-if="isProcessing && isPaused" type="warning" @click="resume">继续</el-button>
      </span>
      <span style="float: left">
        <el-button v-if="selection.length > 0" type="danger" class="right12" @click="cancelTasks">取消</el-button>
        <el-button v-if="selection.length > 0" class="right12" @click="resetTasks">重置</el-button>
      </span>
    </div>
    <el-table ref="table" :data="list" @selection-change="handleSelectionChange">
      <el-table-column type="expand">
        <template #default="props">
          <el-form label-position="left" label-width="120px" class="pre-wrap">
            <el-form-item label="输入">
              <div v-for="file in (props.row.displayInputs ?? props.row.inputs)" :key="file.filePath">
                <a class="right24">
                  {{ file.displayPath ?? file.filePath }}
                  <br v-if="file.image2" />{{ file.image2 ? '图像序列，帧率为' + file.framerate : '' }}
                  <br v-if="file.extra" />{{ file.extra ? '额外参数：' + file.extra : '' }}
                </a>
                <a v-if="file.from" class="right12">开始：{{ file.from }}s</a>
                <a v-if="file.to" class="right12">结束：{{ file.to }}s</a>
                <a v-if="file.duration" class="right12">经过：{{ file.duration }}s</a>
              </div>
            </el-form-item>
            <el-form-item label="输出">{{ props.row.output }}</el-form-item>
            <el-form-item label="创建时间">{{ props.row.createTime }}</el-form-item>
            <el-form-item label="开始时间">{{ props.row.startTime }}</el-form-item>
            <el-form-item label="结束时间">{{ props.row.finishTime }}</el-form-item>
            <el-form-item label="FFmpeg参数">{{ props.row.fFmpegArguments }}</el-form-item>
            <el-form-item label="信息" class="s">{{ props.row.message }}</el-form-item>
            <el-form-item label="参数">
              <CodeArgumentsDescription :type="props.row.type" :args="props.row.parameters" />
            </el-form-item>
          </el-form>
        </template>
      </el-table-column>
      <el-table-column type="selection" width="55" />
      <el-table-column prop="typeText" label="类型" width="90" />
      <el-table-column label="状态" width="80">
        <template #default="scope">
          <span v-if="scope.row.status === 1">待处理</span>
          <span style="color: orange; font-weight: bold" v-if="scope.row.status === 2">进行中</span>
          <span style="color: green" v-if="scope.row.status === 3">完成</span>
          <span style="color: red" v-if="scope.row.status === 4">错误</span>
          <span style="color: gray" v-if="scope.row.status === 5">取消</span>
        </template>
      </el-table-column>
      <el-table-column label="输入" min-width="360">
        <template #default="scope">
          <span class="ellipsis-text">{{ scope.row.inputText }}</span>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180">
        <template #default="scope">
          <el-button @click="resetTask(scope.row)" type="text" size="small"
            :disabled="scope.row.status === 1 || scope.row.status === 2">重置</el-button>
          <el-popconfirm v-if="scope.row.status === 2" title="真的要取消任务吗？任务会终止"
            style="margin-left: 10px; margin-right: 10px" @confirm="cancelTask(scope.row)">
            <template #reference><el-button type="text" size="small">取消</el-button></template>
          </el-popconfirm>
          <el-popconfirm title="真的要删除任务吗？" @confirm="deleteTask(scope.row)">
            <template #reference><el-button type="text" size="small">删除</el-button></template>
          </el-popconfirm>
        </template>
      </el-table-column>
      <el-table-column align="right">
        <template #header><el-button type="text" @click="fillData">刷新</el-button></template>
      </el-table-column>
    </el-table>
    <div>
      <div class="top12">
        <el-pagination style="float: left" @size-change="fillData" @current-change="fillData"
          layout="sizes,prev, pager, next" :page-sizes="[10, 20, 50, 100]"
          v-model:page-size="countPerPage" v-model:current-page="page" :total="totalCount" />
        <el-radio-group v-model="statusFilter" size="mini" @change="fillData" style="float: right">
          <el-radio-button :label="null"><b>全部</b></el-radio-button>
          <el-radio-button :label="1">排队中</el-radio-button>
          <el-radio-button :label="2">进行中</el-radio-button>
          <el-radio-button :label="3">已完成</el-radio-button>
          <el-radio-button :label="4">错误</el-radio-button>
          <el-radio-button :label="5">取消</el-radio-button>
        </el-radio-group>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  showError, showSuccess, getTaskTypeDescription,
  showLoading, closeLoading, displayPath
} from '../common'
import * as net from '../net'
import CodeArgumentsDescription from '../components/CodeArgumentsDescription.vue'

const list = ref<any[]>([])
const isProcessing = ref(false)
const isPaused = ref(false)
const totalCount = ref(0)
const selection = ref<any[]>([])
const page = ref(1)
const countPerPage = ref(20)
const statusFilter = ref<number | null>(null)
const scheduleTime = ref('')
const hasSchedule = ref(false)

function handleSelectionChange(val: any[]) {
  selection.value = val
}

function getSelectionIds(): number[] {
  return selection.value.map(p => p.id)
}

function start() {
  net.postStartQueue().catch(showError)
}

function pause() {
  net.postPauseQueue().catch(showError)
}

function resume() {
  net.postResumeQueue().catch(showError)
}

function cancel() {
  net.postCancelQueue()
    .then(() => setTimeout(fillData, 500))
    .catch(showError)
}

function schedule() {
  if (!scheduleTime.value) {
    showError('请选择计划时间')
    return
  }
  net.postSchedule(scheduleTime.value)
    .then(() => {
      showSuccess('设置成功')
      hasSchedule.value = true
    })
    .catch(showError)
}

function cancelSchedule() {
  net.postCancelSchedule()
    .then(() => {
      hasSchedule.value = false
      scheduleTime.value = ''
    })
    .catch(showError)
}

function resetTask(item: any) {
  net.postResetTask(item.id)
    .then(() => {
      showSuccess('重置成功')
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function resetTasks() {
  net.postResetTasks(getSelectionIds())
    .then(() => {
      showSuccess('重置成功')
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function deleteTask(item: any) {
  net.postDeleteTask(item.id)
    .then(() => {
      showSuccess('删除成功')
      setTimeout(fillData, 500)
    })
    .catch(showError)
}


function cancelTask(item: any) {
  net.postCancelTask(item.id)
    .then(() => {
      showSuccess('取消成功')
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function cancelTasks() {
  net.postCancelTasks(getSelectionIds())
    .then(() => {
      showSuccess('取消成功')
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function fillData() {
  return net.getTaskList(statusFilter.value, page.value, countPerPage.value)
    .then((response) => {
      totalCount.value = response.data.totalCount
      response.data.list.forEach((element: any) => {
        element.typeText = getTaskTypeDescription(element.type)
        element.inputText = element.inputs == null
          ? '未知'
          : element.inputs.length === 1
            ? displayPath(element.inputs[0].filePath)
            : displayPath(element.inputs[0].filePath) + ' 等'
        element.output = displayPath(element.output)
        element.displayInputs = (element.inputs ?? []).map((f: any) => ({
          ...f,
          displayPath: displayPath(f.filePath)
        }))
      })
      list.value = response.data.list
    })
    .catch(showError)
}

onMounted(() => {
  showLoading()
  fillData()
  net.getQueueScheduleTime()
    .then((r) => {
      const time = r.data
      if (time != null && time !== '') {
        scheduleTime.value = time
        hasSchedule.value = true
      }
    })
    .catch(showError)
    .finally(closeLoading)
})
</script>

<style scoped>
.el-table .cell { white-space: pre-line; word-wrap: break-word; }
.cell .el-button { margin-right: 6px; }
.ellipsis-text {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
}
</style>
