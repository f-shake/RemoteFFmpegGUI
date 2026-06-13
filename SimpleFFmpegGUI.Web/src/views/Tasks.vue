<template>
  <div class="page-container-wide">
    <!-- 控制栏 -->
    <div class="tasks-toolbar">
      <div class="toolbar-left">
        <el-button v-if="selection.length > 0" type="danger" class="right12" @click="cancelTasks">取消</el-button>
        <el-button v-if="selection.length > 0" class="right12" @click="resetTasks">重置</el-button>
      </div>
      <div class="toolbar-right">
        <template v-if="!isProcessing">
          <a class="right12" v-if="hasSchedule">已计划开始时间</a>
          <el-date-picker v-if="!isProcessing" class="right12" v-model="scheduleTime"
            value-format="yyyy-MM-dd[T]HH:mm:ss[Z]" placeholder="计划开始时间" type="datetime" :disabled="hasSchedule" />
          <el-button v-if="!isProcessing && !hasSchedule" :disabled="!scheduleTime" type="primary"
            @click="schedule" class="right12">设置计划</el-button>
          <el-button v-if="!isProcessing && hasSchedule" type="primary" @click="cancelSchedule"
            class="right12">取消计划</el-button>
          <el-button v-if="!isProcessing" type="primary" @click="start">开始队列</el-button>
        </template>
        <template v-else>
          <el-popconfirm title="真的要取消任务吗？" @confirm="cancel" class="right12">
            <template #reference><el-button type="danger">停止</el-button></template>
          </el-popconfirm>
          <el-button class="right12" v-if="isProcessing && !isPaused" type="warning" @click="pause">暂停</el-button>
          <el-button class="right12" v-if="isProcessing && isPaused" type="primary" @click="resume">继续</el-button>
        </template>
      </div>
    </div>

    <!-- 任务表格 -->
    <el-card shadow="never" class="table-card">
      <el-table ref="table" :data="list" @selection-change="handleSelectionChange" size="small">
        <el-table-column type="expand">
          <template #default="props">
            <el-form label-position="left" label-width="120px" class="expand-form">
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
              <el-form-item label="FFmpeg参数" class="mono">{{ props.row.fFmpegArguments }}</el-form-item>
              <el-form-item label="信息" class="s">{{ props.row.message }}</el-form-item>
              <el-form-item label="参数">
                <CodeArgumentsDescription :type="props.row.type" :args="props.row.parameters" />
              </el-form-item>
            </el-form>
          </template>
        </el-table-column>
        <el-table-column type="selection" width="48" />
        <el-table-column label="类型" width="90">
          <template #default="scope">
            <el-tag :type="scope.row.type === 4 ? 'warning' : 'info'" size="small" effect="plain">
              {{ scope.row.typeText }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="80">
          <template #default="scope">
            <el-tag
              v-if="scope.row.status === 1" type="info" size="small" effect="plain">待处理</el-tag>
            <el-tag
              v-if="scope.row.status === 2" type="warning" size="small" effect="dark">进行中</el-tag>
            <el-tag
              v-if="scope.row.status === 3" type="success" size="small" effect="plain">完成</el-tag>
            <el-tag
              v-if="scope.row.status === 4" type="danger" size="small" effect="plain">错误</el-tag>
            <el-tag
              v-if="scope.row.status === 5" type="info" size="small" effect="plain">取消</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="输入" min-width="320">
          <template #default="scope">
            <span class="ellipsis-text">{{ scope.row.inputText }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="210" class-name="ops-col">
          <template #default="scope">
            <div class="ops-btns">
              <el-button @click="resetTask(scope.row)" type="text" size="small"
                :disabled="scope.row.status === 1 || scope.row.status === 2">重置</el-button>
              <el-popconfirm v-if="scope.row.status === 2" title="真的要取消任务吗？任务会终止"
                @confirm="cancelTask(scope.row)">
                <template #reference><el-button type="text" size="small">取消</el-button></template>
              </el-popconfirm>
              <el-popconfirm title="真的要删除任务吗？" @confirm="deleteTask(scope.row)">
                <template #reference><el-button type="text" size="small">删除</el-button></template>
              </el-popconfirm>
            </div>
          </template>
        </el-table-column>
        <el-table-column align="right">
          <template #header><el-button type="text" @click="fillData">刷新</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 分页 -->
    <div class="tasks-pagination">
      <el-pagination
        @size-change="fillData" @current-change="fillData"
        layout="sizes, prev, pager, next"
        :page-sizes="[10, 20, 50, 100]"
        v-model:page-size="countPerPage" v-model:current-page="page"
        :total="totalCount" background
      />
      <el-radio-group v-model="statusFilter" @change="fillData">
        <el-radio-button :value="0"><b>全部</b></el-radio-button>
        <el-radio-button :value="1">排队中</el-radio-button>
        <el-radio-button :value="2">进行中</el-radio-button>
        <el-radio-button :value="3">已完成</el-radio-button>
        <el-radio-button :value="4">错误</el-radio-button>
        <el-radio-button :value="5">取消</el-radio-button>
      </el-radio-group>
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
const statusFilter = ref<number>(0)
const scheduleTime = ref('')
const hasSchedule = ref(false)

function handleSelectionChange(val: any[]) {
  selection.value = val
}

function getSelectionIds(): number[] {
  return selection.value.map(p => p.id)
}

function refreshQueueStatus() {
  net.getQueueStatus()
    .then((r) => {
      isProcessing.value = r.data.isProcessing ?? false
      isPaused.value = r.data.isPaused ?? false
    })
    .catch(() => {})
}

function start() {
  net.postStartQueue()
    .then(() => {
      isProcessing.value = true
      isPaused.value = false
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function pause() {
  net.postPauseQueue()
    .then(() => {
      isPaused.value = true
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function resume() {
  net.postResumeQueue()
    .then(() => {
      isPaused.value = false
      setTimeout(fillData, 500)
    })
    .catch(showError)
}

function cancel() {
  net.postCancelQueue()
    .then(() => {
      isProcessing.value = false
      isPaused.value = false
      setTimeout(fillData, 1500)
    })
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
  const s = statusFilter.value === 0 ? null : statusFilter.value
  return net.getTaskList(s, page.value, countPerPage.value)
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
  refreshQueueStatus()
  net.getQueueScheduleTime()
    .then((r) => {
      const time = r.data
      if (time != null && time !== '') {
        scheduleTime.value = time
        hasSchedule.value = true
      }
    })
    .catch(showError)
    .finally(() => {
      closeLoading()
      // 定时轮询队列状态
      setInterval(() => {
        refreshQueueStatus()
        if (isProcessing.value) {
          fillData()
        }
      }, 3000)
    })
})
</script>

<style scoped>
@import '../assets/page.css';

/* 控制栏 */
.tasks-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}
.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 4px;
}

/* 表格卡片 */
.table-card {
  border-radius: var(--radius-lg) !important;
  overflow: hidden;
}

/* 展开详情 */
.expand-form {
  padding: 8px 0;
}
.expand-form :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-weight: 500;
}

/* 状态徽标微调 */
.el-tag--small {
  font-weight: 500;
}

/* 分页 */
.tasks-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 12px;
  padding-bottom: 16px;
}

/* 展开的 cell 换行 */
.el-table .cell { white-space: pre-line; word-wrap: break-word; }
.cell .el-button { margin-right: 6px; }
.ops-btns { display: flex; align-items: center; gap: 2px; flex-wrap: nowrap; }
.ops-btns .el-popconfirm { display: inline-flex; }
.ops-btns .el-button { flex-shrink: 0; }
.ellipsis-text {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: middle;
}

@media (max-width: 640px) {
  .tasks-toolbar {
    flex-direction: column;
    align-items: stretch;
  }
  .tasks-pagination {
    flex-direction: column;
    align-items: stretch;
  }
  .tasks-pagination .el-radio-group {
    display: flex;
    overflow-x: auto;
  }
}
</style>
