<template>
  <div class="page-container-wide">
    <!-- 操作栏 -->
    <div class="presets-toolbar">
      <div class="toolbar-left">
        <el-popconfirm title="真的要清空所有预设吗？此操作不可撤销！" @confirm="clearPresets">
          <template #reference><el-button size="default">清空</el-button></template>
        </el-popconfirm>
      </div>
      <div class="toolbar-right">
        <el-upload
          :headers="net.getHeader()"
          class="right12"
          :action="net.getImportPresetsUrl()"
          :on-success="fillData"
          accept="application/json"
          :show-file-list="false"
        >
          <el-button size="default">导入</el-button>
        </el-upload>
        <el-button @click="exportPresets">导出</el-button>
      </div>
    </div>

    <!-- 预设表格 -->
    <el-card shadow="never" class="table-card">
      <el-table ref="table" :data="list">
        <el-table-column type="expand">
          <template #default="props">
            <code-arguments-description :args="props.row.parameters" :type="props.row.type" />
          </template>
        </el-table-column>
        <el-table-column prop="name" label="预设名" min-width="120" />
        <el-table-column label="类型" width="100">
          <template #default="scope">
            <el-tag size="small" effect="plain">{{ scope.row.typeText }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" class-name="ops-col">
          <template #default="scope">
            <div class="ops-btns">
              <el-button text size="small" @click="remake(scope.row)">新建任务</el-button>
              <el-button text size="small" @click="edit(scope.row)">编辑</el-button>
              <el-popconfirm title="真的要删除预设吗？" @confirm="deletePreset(scope.row)">
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

    <!-- 编辑对话框 -->
    <el-dialog title="编辑预设" v-model="dialogVisible" width="80%" destroy-on-close>
      <CodeArguments ref="args" :type="type" :showPresets="false" />
      <template #footer>
        <el-button type="primary" @click="savePreset" :loading="saving">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { showError, showSuccess, showLoading, closeLoading } from '@/utils/ui'
import { getTaskTypeDescription } from '@/models/TaskType'
import { jumpByArgs } from '@/utils/navigation'
import * as net from '@/api'
import CodeArguments from '@/components/CodeArguments.vue'
import CodeArgumentsDescription from '@/components/CodeArgumentsDescription.vue'

const list = ref<any[]>([])
const dialogVisible = ref(false)
const editingPreset = ref<any>(null)
const type = ref(0)
const saving = ref(false)
const args = ref<any>(null)


function remake(item: any) {
  jumpByArgs(item.parameters ?? item.arguments, item.inputs, item.output, item.type)
}

function savePreset() {
  saving.value = true
  const item = editingPreset.value
  net.postAddOrUpdatePreset(item.name, item.type, args.value?.getArgs())
    .then(() => {
      showSuccess('保存成功')
      dialogVisible.value = false
      fillData()
    })
    .catch(showError)
    .finally(() => { saving.value = false })
}

function deletePreset(item: any) {
  net.postDeletePreset(item.id).then(fillData).catch(showError)
}

function edit(item: any) {
  editingPreset.value = item
  type.value = item.type
  dialogVisible.value = true
}

function clearPresets() {
  net.postClearPresets().then(fillData).catch(showError)
}

function exportPresets() {
  net.downloadExportPresetsUrl()
}

function fillData() {
  showLoading()
  return net.getPresets()
    .then((response) => {
      response.data.forEach((element: any) => {
        element.typeText = getTaskTypeDescription(element.type)
      })
      list.value = response.data
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

.presets-toolbar {
  display: flex;
  justify-content: space-between;
  margin: 4px 0 12px;
  align-items: center;
}
.toolbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.table-card {
  border-radius: var(--radius-lg) !important;
  overflow: hidden;
}

.ops-btns { display: flex; align-items: center; gap: 2px; flex-wrap: nowrap; }
.ops-btns .el-popconfirm { display: inline-flex; }
.ops-btns .el-button { flex-shrink: 0; }

@media (max-width: 640px) {
  .presets-toolbar {
    flex-wrap: wrap;
    gap: 8px;
    margin: 8px 0 8px;
  }
  .toolbar-left {
    order: 2;
  }
  .toolbar-right {
    width: 100%;
    justify-content: flex-end;
  }
}
</style>
