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
      <!-- 桌面：条数 + 页码 + 时间/类型 -->
      <el-pagination
        v-if="!isMobile"
        @size-change="fillData" @current-change="fillData"
        layout="sizes, prev, pager, next"
        :page-sizes="[10, 20, 50, 100, 200, 500, 1000]"
        v-model:page-size="countPerPage" v-model:current-page="page"
        :total="totalCount" background
      />
      <!-- 手机：第一行仅页码（少量），条数随 filter 行 -->
      <MobilePager
        v-else
        :page="page" :total="totalCount" :page-size="countPerPage" @change="onPagerChange"
      />
      <div class="filter-bar">
        <span class="filter-label">时间范围：</span>
        <el-date-picker
          @change="onFilterChange" v-model="timeRange" type="datetimerange"
          range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期"
          align="right" class="filter-date"
        />
        <el-select v-if="isMobile" v-model="typeFilter" @change="onFilterChange" class="filter-select">
          <el-option label="全部" :value="0" />
          <el-option label="错误" value="E" />
          <el-option label="警告" value="W" />
          <el-option label="信息" value="I" />
          <el-option label="输出" value="O" />
        </el-select>
        <el-radio-group v-else v-model="typeFilter" @change="onFilterChange">
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
import { useIsMobile } from '@/composables/useIsMobile'
import MobilePager from '@/components/MobilePager.vue'

const { isMobile } = useIsMobile()
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

// 手机端切换页码
function onPagerChange(p: number) {
  page.value = p
  fillData()
}

// 切换时间范围/类型筛选时回到第一页（避免 page 超出筛选后的 totalPages）
function onFilterChange() {
  page.value = 1
  fillData()
}

onMounted(() => {
  showLoading()
  fillData()
})
</script>

<style scoped>
@import '../assets/page.css';

.table-card {
  border-radius: var(--radius-lg);
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

@media (max-width: 680px) {
  .logs-pagination {
    flex-direction: column;
    align-items: stretch;
    padding: 8px 12px;
    box-sizing: border-box;
    min-width: 0;
    gap: 8px;
  }
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
    min-width: 0;
    width: 100%;
    max-width: 100%;
  }
  /* 时间范围：border-box 让 100% 宽度包含内边距，避免 content-box 把盒子撑到超出容器宽度 */
  .logs-page :deep(.filter-date) {
    box-sizing: border-box;
    width: 100% !important;
    min-width: 0 !important;
    max-width: 100% !important;
    font-size: 13px;
  }
  .logs-page :deep(.filter-date .el-range-input) {
    min-width: 0 !important;
    width: 0 !important;
    flex: 1 1 0 !important;
    font-size: 13px;
  }
  .logs-page :deep(.filter-date .el-range-separator) {
    flex-shrink: 0;
    padding: 0 4px;
  }
  .logs-page :deep(.filter-date .el-range__icon),
  .logs-page :deep(.filter-date .el-range__close-icon) {
    flex-shrink: 0;
  }
  .filter-select,
  .logs-page :deep(.filter-select) {
    width: 100%;
  }
}
</style>
