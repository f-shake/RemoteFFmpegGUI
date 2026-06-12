<template>
  <div class="status-bar" v-if="status != null && status.isProcessing">
    <div v-if="status.hasDetail">
      <div v-if="windowWidth > 768">
        <el-row>
          <el-col style="width: 108px; height: 64px">
            <div style="background-color: #7dd07d; width: 100%; height: 100%">
              <img width="108" height="64" style="cursor: pointer" :src="snapshotSrc" v-show="snapshotSrc !== ''"
                @click="clickSnapshot" />
            </div>
          </el-col>
          <el-col style="width: calc(100% - 120px); padding-left: 12px">
            <el-row>
              <el-col :span="7">
                <el-row><b>码率：</b>{{ status.bitrate }}</el-row>
                <el-row><b>已用：</b>{{ formatDoubleTimeSpan(status.progress.duration) }}</el-row>
              </el-col>
              <el-col :span="7">
                <el-row><b>速度：</b>{{ status.fps }}FPS{{ ' ' }}{{ status.speed }}X</el-row>
                <el-row><b>剩余：</b>{{ formatDoubleTimeSpan(status.progress.lastTime) }}</el-row>
              </el-col>
              <el-col :span="7">
                <el-row><b>进度：</b>{{ status.frame }}帧 {{ formatDoubleTimeSpan(status.time, true) }}</el-row>
                <el-row><b>预计：</b>{{ formatDateTime(finishTime(), true, true, false) }}</el-row>
              </el-col>
              <el-col :span="3">
                <el-popconfirm title="真的要取消任务吗？" @confirm="cancel">
                  <template #reference>
                    <el-button type="text" style="color: red" size="large">取消</el-button>
                  </template>
                </el-popconfirm>
              </el-col>
            </el-row>
            <el-row class="right24">
              <el-col :span="8" class="one-line"><b>任务：</b>{{ status.isPaused ? '暂停中' : status.progress.name }}</el-col>
              <el-col :span="16">
                <el-progress :text-inside="true" :stroke-width="20"
                  v-if="status.progress.isIndeterminate" :percentage="100"
                  style="margin-right: 24px; margin-top: 4px" :show-text="true"
                  :format="() => '进度未知'" />
                <el-progress :text-inside="true" :stroke-width="20"
                  :percentage="status.progress.percent * 100"
                  v-else
                  style="margin-right: 24px; margin-top: 4px"
                  :format="(p: number) => p.toFixed(2) + '%'" />
              </el-col>
            </el-row>
          </el-col>
        </el-row>
      </div>
      <div v-else>
        <el-row>
          <el-col :span="12">
            <el-row><b>码率：</b>{{ status.bitrate }}</el-row>
            <el-row><b>速度：</b>{{ status.fps }}FPS{{ ' ' }}{{ status.speed }}X</el-row>
            <el-row><b>进度：</b>{{ status.frame }}帧 {{ formatDoubleTimeSpan(status.time, true) }}</el-row>
          </el-col>
          <el-col :span="12">
            <el-row><b>已用：</b>{{ formatDoubleTimeSpan(status.progress.duration) }}</el-row>
            <el-row><b>剩余：</b>{{ formatDoubleTimeSpan(status.progress.lastTime) }}</el-row>
            <el-row><b>预计：</b>{{ formatDateTime(finishTime(), true, true, false) }}</el-row>
          </el-col>
        </el-row>
        <el-row class="single-line">
          <b>任务：</b>{{ status.isPaused ? '暂停中' : status.progress.name }}
        </el-row>
        <el-row>
          <el-col :span="20">
            <el-progress :text-inside="true" :stroke-width="20"
              v-if="status.progress.isIndeterminate" :percentage="100"
              style="margin-right: 24px; margin-top: 4px" :show-text="true"
              :format="() => '进度未知'" />
            <el-progress :text-inside="true" :stroke-width="20"
              v-else :percentage="status.progress.percent * 100"
              style="margin-top: 10px"
              :format="(p: number) => p.toFixed(2) + '%'" />
          </el-col>
          <el-col :span="4">
            <el-popconfirm title="真的要取消任务吗？" @confirm="cancel">
              <template #reference>
                <el-button style="width: 75%; color: red" type="text" size="large">取消</el-button>
              </template>
            </el-popconfirm>
          </el-col>
        </el-row>
      </div>
    </div>
    <div v-else style="height: 60px">
      <el-icon v-if="windowWidth > 988" class="is-loading" style="float: left; font-size: 2em; margin-top: 12px"><Loading /></el-icon>
      <el-popconfirm title="真的要取消任务吗？" style="float: right; margin-right: 36px; margin-top: 8px" @confirm="cancel">
        <template #reference>
          <el-button type="text" style="color: red" size="large">取消</el-button>
        </template>
      </el-popconfirm>
      <div v-if="windowWidth > 988" style="margin-left: 48px; text-align: center; padding-top: 16px">
        {{ status.lastOutput }}
      </div>
      <div v-else style="text-align: center; transform: translate(0, -4px)">
        {{ status.lastOutput }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { ElMessageBox } from 'element-plus'
import { Loading } from '@element-plus/icons-vue'
import * as net from '../net'
import { showError, formatDateTime, formatDoubleTimeSpan } from '../common'

const props = defineProps<{
  status: any
  windowWidth: number
}>()

const snapshotSrc = ref('')
const lastSnapshotTime = ref(1e10)
const lastSnapshotFile = ref('')

const progressColor = computed(() => {
  return props.status?.isPaused ? '#777777' : '#50a0fc'
})

function finishTime(): Date {
  return new Date(props.status.progress.finishTime)
}

function cancel() {
  net.postCancelQueue().catch(showError)
}

function clickSnapshot() {
  ElMessageBox.alert(
    `<img src="${snapshotSrc.value}" style="width:100%">`,
    '缩略图',
    { dangerouslyUseHTMLString: true }
  )
}

function updateSnapshot() {
  if (props.status == null || props.status.isPaused) return
  if (props.status?.hasDetail && props.status.task != null && props.windowWidth > 768) {
    if (props.status.task.inputs.length >= 1) {
      if (
        props.status.task.inputs[0].filePath === lastSnapshotFile.value &&
        Math.abs(props.status.time - lastSnapshotTime.value) < 1
      ) {
        return
      }
      net.getSnapshot(props.status.task.inputs[0].filePath, props.status.time)
        .then((r) => {
          lastSnapshotFile.value = props.status.task.inputs[0].filePath
          lastSnapshotTime.value = props.status.time
          const reader = new window.FileReader()
          reader.readAsDataURL(r.data)
          reader.onload = () => {
            snapshotSrc.value = reader.result as string
          }
        })
        .catch(() => {
          snapshotSrc.value = ''
          lastSnapshotFile.value = ''
        })
    }
  } else {
    snapshotSrc.value = ''
    lastSnapshotFile.value = ''
  }
}

onMounted(() => {
  setInterval(updateSnapshot, 10 * 1000)
  setTimeout(updateSnapshot, 1000)
})
</script>

<style scoped>
.status-bar {
  background-color: lightgreen;
  padding-left: 24px;
  padding-top: 12px;
  padding-bottom: 8px;
  margin-left: -30px;
  margin-right: -30px;
  margin-top: -12px;
}
</style>
