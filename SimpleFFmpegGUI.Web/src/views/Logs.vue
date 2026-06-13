<template>
  <div class="page-container-wide logs-page">
    <div class="gray" v-if="taskName" style="margin-bottom: 12px;">任务：{{ taskName }}</div>

    <!-- 日志表格 -->
    <el-card shadow="never" class="table-card top12">
      <el-table ref="table" :data="list" size="small">
        <el-table-column type="expand">
          <template #default="props">
            <div class="expand-message">{{ props.row.message }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="timeText" label="时间" width="160" />
        <el-table-column label="类型" width="70">
          <template #default="scope">
            <el-tag v-if="scope.row.type === 'E'" type="danger" size="small" effect="plain">错误</el-tag>
            <el-tag v-if="scope.row.type === 'W'" type="warning" size="small" effect="plain">警告</el-tag>
            <el-tag v-if="scope.row.type === 'I'" type="primary" size="small" effect="plain">信息</el-tag>
            <el-tag v-if="scope.row.type === 'O'" type="info" size="small" effect="plain">输出</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="信息" min-width="200">
          <template #default="scope">
            <div class="single-line">{{ scope.row.message }}</div>
          </template>
        </el-table-column>
        <el-table-column align="right">
          <template #header><el-button text @click="fillData">刷新</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 分页 + 类型筛选 -->
    <div class="logs-pagination">
      <el-pagination
        @size-change="fillData" @current-change="fillData"
        layout="sizes, prev, pager, next"
        :page-sizes="[10, 20, 50, 100, 200, 500, 1000]"
        v-model:page-size="countPerPage" v-model:current-page="page"
        :total="totalCount" background
      />
      <div class="filter-bar">
        <span class="filter-label">时间范围：</span>
        <el-date-picker
          @change="fillData" v-model="timeRange" type="datetimerange"
          range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期"
          align="right" class="filter-date"
        />
        <el-radio-group v-model="typeFilter" @change="fillData">
          <el-radio-button :value="0"><b>全部</b></el-radio-button>
          <el-radio-button value="E">错误</el-radio-button>
          <el-radio-button value="W">警告</el-radio-button>
          <el-radio-button value="I">信息</el-radio-button>
          <el-radio-button value="O">输出</el-radio-button>
        </el-radio-group>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { showError, showLoading, closeLoading } from '@/utils/ui'
import { formatDateTime } from '@/utils/format'
import { displayPath } from '@/utils/navigation'
import { TaskType } from '@/models/TaskType'
import * as net from '@/api'

const route = useRoute()
const list = ref<any[]>([])
const totalCount = ref(0)
const page = ref(1)
const countPerPage = ref(100)
const typeFilter = ref<string | number>(0)
const timeRange = ref<any[]>([])
const taskName = ref('')

if (route.query.id) {
  net.getTask(Number.parseInt(route.query.id as string)).then((r) => {
    taskName.value = TaskType.GetByID(r.data.type).Description + '（'
    if (r.data.inputs && r.data.inputs.length > 0) {
      taskName.value += displayPath(r.data.inputs[0].filePath)
      if (r.data.inputs.length > 1) taskName.value += ' 等'
    } else {
      taskName.value = '未知'
    }
    taskName.value += '）'
  })
}

function fillData() {
  showLoading()
  const from = timeRange.value && timeRange.value.length === 2 ? (timeRange.value[0] as Date).toJSON() : null
  const to = timeRange.value && timeRange.value.length === 2 ? (timeRange.value[1] as Date).toJSON() : null
  const taskId = route.query.id ? Number.parseInt(route.query.id as string) : 0
  const type = typeFilter.value === 0 ? null : typeFilter.value as string
  return net.getLogs(type, taskId, from, to, (page.value - 1) * countPerPage.value, countPerPage.value)
    .then((response) => {
      totalCount.value = response.data.totalCount
      response.data.list.forEach((element: any) => {
        element.timeText = formatDateTime(new Date(element.time))
      })
      list.value = response.data.list
    })
    .catch(showError)
    .finally(closeLoading)
}

onMounted(() => {
  showLoading()
  fillData()
})
</script>

<style scoped>
@import '../assets/page.css';

.table-card {
  border-radius: var(--radius-lg) !important;
  overflow: hidden;
}

.expand-message {
  white-space: pre-wrap;
  font-size: 12px;
  line-height: var(--lh-relaxed);
  color: var(--text-regular);
  background: var(--bg-page);
  padding: 12px;
  border-radius: var(--radius-sm);
  font-family: var(--font-mono);
}

.logs-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 12px;
  padding-bottom: 16px;
}
.filter-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.filter-label {
  font-size: 13px;
  color: var(--text-secondary);
  white-space: nowrap;
}
.filter-date {
  max-width: 280px;
}

@media (max-width: 640px) {
  .logs-pagination {
    flex-direction: column;
    align-items: stretch;
  }
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .filter-date {
    width: 100%;
    max-width: 100%;
  }
  .logs-page :deep(.filter-date .el-range-editor) {
    width: 100% !important;
    min-width: 0 !important;
  }
  .logs-page :deep(.filter-date .el-range-input) {
    min-width: 0 !important;
    width: 0 !important;
    flex: 1 1 0 !important;
  }
}
</style>
