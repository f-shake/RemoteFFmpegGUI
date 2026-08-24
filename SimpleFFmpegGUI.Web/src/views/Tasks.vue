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
            value-format="YYYY-MM-DD[T]HH:mm:ss" placeholder="计划开始时间" type="datetime" :disabled="hasSchedule" />
          <el-button v-if="!isProcessing && !hasSchedule" :disabled="!scheduleTime" type="primary"
            @click="schedule" class="right12">设置计划</el-button>
          <el-button v-if="!isProcessing && hasSchedule" @click="cancelSchedule"
            class="right12">取消计划</el-button>
          <el-button v-if="!isProcessing" type="primary" :disabled="!hasPending" @click="start">开始队列</el-button>
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

    <!-- 任务表格（桌面） -->
    <el-card v-if="!isMobile" shadow="never" class="table-card">
      <el-table ref="table" :data="list" @selection-change="handleSelectionChange" size="small">
        <el-table-column type="expand">
          <template #default="props">
            <TaskDetail :task="props.row" />
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
        <el-table-column label="操作" width="260" class-name="ops-col">
          <template #default="scope">
            <div class="ops-btns">
              <el-button @click="remakeTask(scope.row)" text size="small" title="以本任务参数重新创建新任务">复制</el-button>
              <el-button @click="resetTask(scope.row)" text size="small"
                :disabled="scope.row.status === 1 || scope.row.status === 2">重置</el-button>
              <el-popconfirm v-if="scope.row.status === 2" title="真的要取消任务吗？任务会终止"
                @confirm="cancelTask(scope.row)">
                <template #reference><el-button text size="small">取消</el-button></template>
              </el-popconfirm>
              <el-popconfirm title="真的要删除任务吗？" @confirm="deleteTask(scope.row)">
                <template #reference><el-button text size="small">删除</el-button></template>
              </el-popconfirm>
            </div>
          </template>
        </el-table-column>
        <el-table-column align="right">
          <template #header><el-button text @click="fillData">刷新</el-button></template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 任务卡片（手机端，每个任务独立渲染） -->
    <div v-else class="task-cards">
      <div v-for="row in list" :key="row.id" class="task-card">
        <div class="task-card-head">
          <el-tag :type="row.type === 4 ? 'warning' : 'info'" size="small" effect="plain">{{ row.typeText }}</el-tag>
          <el-tag :type="statusMeta(row.status).type" :effect="statusMeta(row.status).effect" size="small">
            {{ statusMeta(row.status).text }}
          </el-tag>
          <button class="task-card-toggle" :class="{ 'is-open': expandedId === row.id }"
            @click="toggleDetail(row)" type="button" aria-label="展开/收起详情">
            <el-icon><ArrowRight /></el-icon>
          </button>
        </div>
        <div class="task-card-body" :title="row.inputText" @click="toggleDetail(row)">{{ row.inputText }}</div>
        <el-collapse-transition>
          <TaskDetail v-if="expandedId === row.id" :task="row" class="task-card-detail" />
        </el-collapse-transition>
        <div class="task-card-ops">
          <el-button @click="remakeTask(row)" text size="small" title="以本任务参数重新创建新任务">复制</el-button>
          <el-button @click="resetTask(row)" text size="small" :disabled="row.status === 1 || row.status === 2">重置</el-button>
          <el-popconfirm v-if="row.status === 2" title="真的要取消任务吗？任务会终止" @confirm="cancelTask(row)">
            <template #reference><el-button text size="small">取消</el-button></template>
          </el-popconfirm>
          <el-popconfirm title="真的要删除任务吗？" @confirm="deleteTask(row)">
            <template #reference><el-button text size="small">删除</el-button></template>
          </el-popconfirm>
        </div>
      </div>
      <el-empty v-if="list.length === 0" description="暂无数据" />
    </div>

    <!-- 分页 + 分类筛选 -->
    <div class="tasks-pagination">
      <!-- 桌面：条数 + 页码 + 分类 radio -->
      <template v-if="!isMobile">
        <el-pagination
          @size-change="fillData" @current-change="fillData"
          layout="sizes, prev, pager, next"
          :page-sizes="[10, 20, 50, 100]"
          v-model:page-size="countPerPage" v-model:current-page="page"
          :total="totalCount" background
        />
        <el-radio-group v-model="statusFilter" @change="onStatusFilterChange">
          <el-radio-button :value="0"><b>全部</b></el-radio-button>
          <el-radio-button :value="1">排队中</el-radio-button>
          <el-radio-button :value="2">进行中</el-radio-button>
          <el-radio-button :value="3">已完成</el-radio-button>
          <el-radio-button :value="4">错误</el-radio-button>
          <el-radio-button :value="5">取消</el-radio-button>
        </el-radio-group>
      </template>

      <!-- 手机：第一行页码(prev pager next)；第二行 1/3 条数 + 2/3 分类 select -->
      <template v-else>
        <MobilePager :page="page" :total="totalCount" :page-size="countPerPage" @change="onPagerChange" />
        <div class="mobile-filter-row">
          <el-select v-model="countPerPage" @change="onMobileSizeChange" class="page-size-select">
            <el-option v-for="s in [10, 20, 50, 100]" :key="s" :label="s + '条/页'" :value="s" />
          </el-select>
          <el-select v-model="statusFilter" @change="onStatusFilterChange" class="status-select">
            <el-option label="全部" :value="0" />
            <el-option label="排队中" :value="1" />
            <el-option label="进行中" :value="2" />
            <el-option label="已完成" :value="3" />
            <el-option label="错误" :value="4" />
            <el-option label="取消" :value="5" />
          </el-select>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, showLoading, closeLoading } from '@/utils/ui'
import { getTaskTypeDescription } from '@/models/TaskType'
import { displayPath, jumpByArgs } from '@/utils/navigation'
import { ArrowRight } from '@element-plus/icons-vue'
import * as net from '@/api'
import TaskDetail from '@/components/TaskDetail.vue'
import MobilePager from '@/components/MobilePager.vue'
import { useIsMobile } from '@/composables/useIsMobile'

const { isMobile } = useIsMobile()
const list = ref<any[]>([])
const isProcessing = ref(false)
const isPaused = ref(false)
// 是否存在待执行（排队中）任务——用于控制"开始队列"按钮置灰
const hasPending = ref(true)
const totalCount = ref(0)
const selection = ref<any[]>([])
const page = ref(1)
const countPerPage = ref(20)
const statusFilter = ref<number>(0)
const scheduleTime = ref('')
const hasSchedule = ref(false)
// 手机端卡片展开详情的当前任务 id（单开，详情复用桌面展开的 TaskDetail）
const expandedId = ref<number | null>(null)
function toggleDetail(row: any) {
  expandedId.value = expandedId.value === row.id ? null : row.id
}

// 手机端切换每页条数时回到第一页
function onMobileSizeChange() {
  page.value = 1
  fillData()
}

// 切换状态筛选时回到第一页（避免 page 超出筛选后的 totalPages）
function onStatusFilterChange() {
  page.value = 1
  fillData()
}

// 手机端切换页码
function onPagerChange(p: number) {
  page.value = p
  fillData()
}

// 手机端卡片的状态徽标
function statusMeta(status: number) {
  switch (status) {
    case 1: return { text: '待处理', type: 'info', effect: 'plain' }
    case 2: return { text: '进行中', type: 'warning', effect: 'dark' }
    case 3: return { text: '完成', type: 'success', effect: 'plain' }
    case 4: return { text: '错误', type: 'danger', effect: 'plain' }
    case 5: return { text: '取消', type: 'info', effect: 'plain' }
    default: return { text: '未知', type: 'info', effect: 'plain' }
  }
}

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

function refreshHasPending() {
  net.getQueueHasPending()
    .then((r) => {
      hasPending.value = r.data === true
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

function remakeTask(item: any) {
  // 以任务当前参数为基准，重新进入对应类型的新建任务界面（参数/输入/输出预填，可在其上修改后重提）。
  // 任务存的是解析后的绝对路径；转成相对路径再回填，既让 FileSelect 下拉（option value=relativePath）
  // 能匹配显示，也避免重提时后端拒绝对路径。
  const inputs = (item.inputs ?? []).map((f: any) => ({ ...f, filePath: displayPath(f.filePath) }))
  jumpByArgs(item.parameters, inputs, displayPath(item.output), item.type)
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
  refreshHasPending()
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
        refreshHasPending()
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
  border-radius: var(--radius-lg);
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

/* 手机端第二行：1/3 条数 + 2/3 分类（mobile-filter-row 仅移动端模板渲染） */
.mobile-filter-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  min-width: 0;
}
.mobile-filter-row :deep(.el-select) {
  min-width: 0;
}
.mobile-filter-row .page-size-select {
  flex: 1 1 0;
}
.mobile-filter-row .status-select {
  flex: 2 1 0;
}

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


/* 标签去背景（仅 plain 样式） */
.el-table .el-tag--plain {
  background: transparent !important;
  border: none;
}
.el-table .el-tag--plain:hover {
  background: transparent !important;
}
@media (max-width: 640px) {
  .tasks-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
    padding: 8px 12px;
  }
  .toolbar-left {
    flex-direction: column;
    align-items: stretch;
    width: 100%;
    gap: 8px;
  }
  .toolbar-left .el-button {
    width: 100%;
    margin-left: 0 !important;
  }
  /* 右侧：日期选择器独占一行，操作按钮下面一行左右 1:1 分布；去掉 right12 边距使左右对齐，
     行间用 row-gap 留出 margin */
  .toolbar-right {
    flex-wrap: wrap;
    width: 100%;
    column-gap: 6px;
    row-gap: 8px;
  }
  .toolbar-right :deep(.el-date-editor) {
    flex: 1 1 100%;
    width: 100%;
    margin: 0 !important;
  }
  .toolbar-right > .el-button {
    flex: 1 1 auto;
    min-width: 0;
    margin: 0 !important;
  }
  .toolbar-right :deep(.el-popconfirm) {
    flex: 1 1 auto;
    min-width: 0;
    display: flex;
    margin: 0 !important;
  }
  .toolbar-right :deep(.el-popconfirm .el-button) {
    flex: 1;
    width: auto;
    margin: 0 !important;
  }
  .toolbar-right a {
    display: none;
  }
  .tasks-pagination {
    flex-direction: column;
    align-items: stretch;
    padding: 8px 12px 16px;
    box-sizing: border-box;
    gap: 8px;
  }
}

/* 手机端任务卡片（<640px 时替代表格，每个任务独立渲染，无边框无圆角、左右顶到两侧） */
.task-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 0 0 16px;
}
.task-card {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  background: var(--bg-card);
  border: none;
  border-radius: 0;
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.task-card-head {
  display: flex;
  align-items: center;
  gap: 8px;
}
.task-card-head .el-tag { margin: 0; }
.task-card-body {
  color: var(--text-regular);
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  cursor: pointer;
}
/* 右上角展开/收起详情按钮 */
.task-card-toggle {
  margin-left: auto;
  border: none;
  background: none;
  padding: 2px;
  color: var(--text-secondary);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
}
.task-card-toggle .el-icon {
  transition: transform 0.2s ease;
}
.task-card-toggle.is-open .el-icon {
  transform: rotate(90deg);
}
/* 卡片内详情去掉自身 8px 外边距，铺满卡片内容宽度（提高特异度，确保胜过 TaskDetail 自身的 margin 规则） */
.task-card .task-card-detail :deep(.c-card) {
  margin: 0;
}
.task-card-ops {
  display: flex;
  align-items: center;
  gap: 4px;
  border-top: 1px solid var(--border-color);
  padding-top: 8px;
}
.task-card-ops .el-popconfirm { display: inline-flex; }
.task-card-ops .el-button { flex-shrink: 0; }
</style>
