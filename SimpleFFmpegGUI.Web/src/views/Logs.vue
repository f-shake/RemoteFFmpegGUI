<template>
  <div>
    <div>
      <a class="right24">时间范围：</a>
      <el-date-picker @change="fillData" v-model="timeRange" type="datetimerange" range-separator="至"
        start-placeholder="开始日期" end-placeholder="结束日期" align="right" />
    </div>
    <div class="gray top12" v-if="taskName">任务：{{ taskName }}</div>
    <el-table ref="table" :data="list" size="small">
      <el-table-column type="expand">
        <template #default="props">
          <div class="pre-wrap">{{ props.row.message }}</div>
        </template>
      </el-table-column>
      <el-table-column prop="timeText" label="时间" width="180" />
      <el-table-column label="类型" width="80">
        <template #default="scope">
          <span v-if="scope.row.type === 'E'" style="color: red">错误</span>
          <span style="color: orange" v-if="scope.row.type === 'W'">警告</span>
          <span v-if="scope.row.type === 'I'">信息</span>
          <span style="color: gray" v-if="scope.row.type === 'O'">输出</span>
        </template>
      </el-table-column>
      <el-table-column label="信息" min-width="180">
        <template #default="scope">
          <div class="single-line">{{ scope.row.message }}</div>
        </template>
      </el-table-column>
      <el-table-column align="right">
        <template #header><el-button type="text" @click="fillData">刷新</el-button></template>
      </el-table-column>
    </el-table>
    <div>
      <div class="top12">
        <el-pagination
          style="float: left" @size-change="fillData" @current-change="fillData"
          layout="sizes,prev, pager, next"
          :page-sizes="[10, 20, 50, 100, 200, 500, 1000]"
          v-model:page-size="countPerPage" v-model:current-page="page" :total="totalCount"
        />
        <el-radio-group v-model="typeFilter" size="mini" @change="fillData" style="float: right">
          <el-radio-button :label="null"><b>全部</b></el-radio-button>
          <el-radio-button label="E">错误</el-radio-button>
          <el-radio-button label="W">警告</el-radio-button>
          <el-radio-button label="I">信息</el-radio-button>
          <el-radio-button label="O">输出</el-radio-button>
        </el-radio-group>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { showError, formatDateTime, showLoading, closeLoading, TaskType, displayPath } from '../common'
import * as net from '../net'

const route = useRoute()
const list = ref<any[]>([])
const totalCount = ref(0)
const page = ref(1)
const countPerPage = ref(100)
const typeFilter = ref<string | null>(null)
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
  return net.getLogs(typeFilter.value, taskId, from, to, (page.value - 1) * countPerPage.value, countPerPage.value)
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
.el-table .cell { white-space: pre-line; word-wrap: break-word; }
.cell .el-button { margin-right: 6px; }
</style>
