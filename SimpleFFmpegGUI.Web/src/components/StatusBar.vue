<template>
  <div class="status-bar" :class="barClass" v-if="status != null && status.isProcessing">
    <template v-if="status.hasDetail">
      <!-- === 宽屏（桌面）=== -->
      <div v-if="windowWidth > 768" class="bar-inner">
        <div class="bar-snapshot" v-show="snapshotSrc !== ''">
          <div class="snapshot-placeholder">
            <img :src="snapshotSrc" @click="clickSnapshot" class="snapshot-img" />
          </div>
        </div>
        <div class="bar-info">
          <div class="bar-stats">
            <div class="stat-item">
              <span class="stat-label">码率</span>
              <span class="stat-value">{{ status.bitrate }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">速度</span>
              <span class="stat-value">{{ status.fps }}FPS {{ status.speed }}X</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">进度</span>
              <span class="stat-value">{{ status.frame }}帧 {{ formatDoubleTimeSpan(status.time, true) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">已用</span>
              <span class="stat-value">{{ formatDoubleTimeSpan(status.progress.duration) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">剩余</span>
              <span class="stat-value">{{ formatDoubleTimeSpan(status.progress.lastTime) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">预计完成</span>
              <span class="stat-value">{{ formatDateTime(finishTime(), true, true, false) }}</span>
            </div>
          </div>
          <div class="bar-progress-row">
            <span class="bar-task-name one-line">
              <b>{{ status.isPaused ? '暂停中' : '运行中' }}：</b>{{ status.progress.name }}
            </span>
            <div class="bar-progress-wrap">
              <el-progress
                :text-inside="true" :stroke-width="18"
                :percentage="status.progress.isIndeterminate ? 100 : (status.progress.percent * 100)"
                :color="status.isPaused ? '#909399' : (status.task?.status === 4 ? '#F56C6C' : undefined)"
                :format="status.progress.isIndeterminate ? (() => '进度未知') : ((p: number) => p.toFixed(1) + '%')"
              />
            </div>
            <el-popconfirm title="真的要取消任务吗？" @confirm="cancel">
              <template #reference>
                <el-button text class="bar-cancel-btn">取消</el-button>
              </template>
            </el-popconfirm>
          </div>
        </div>
      </div>

      <!-- === 窄屏（手机）=== -->
      <div v-else class="bar-compact">
        <div class="bar-compact-inner">
          <div class="bar-snapshot bar-snapshot-mobile" v-show="snapshotSrc !== ''">
            <div class="snapshot-placeholder">
              <img :src="snapshotSrc" @click="clickSnapshot" class="snapshot-img" />
            </div>
          </div>
          <div class="bar-compact-body">
            <div class="bar-compact-info">
              <span class="one-line bar-task-name"><b>{{ status.isPaused ? '暂停中' : '运行中' }}：</b>{{ status.progress.name }}</span>
              <span class="bar-compact-stats">
                {{ status.fps }}FPS<template v-if="windowWidth > 300"> / {{ formatDoubleTimeSpan(status.time, true) }}</template><template v-if="windowWidth > 360"> / {{ status.speed }}X</template><template v-if="windowWidth > 440"> / {{ status.bitrate }}</template>
              </span>
            </div>
            <div class="bar-compact-row2">
              <div class="bar-compact-progress">
                <el-progress
                  :text-inside="true" :stroke-width="18"
                  :percentage="status.progress.isIndeterminate ? 100 : (status.progress.percent * 100)"
                  :color="status.isPaused ? '#909399' : (status.task?.status === 4 ? '#F56C6C' : undefined)"
                  :format="status.progress.isIndeterminate ? (() => '') : ((p: number) => p.toFixed(1) + '%')"
                />
              </div>
              <el-popconfirm title="真的要取消任务吗？" @confirm="cancel">
                <template #reference>
                  <el-button text class="bar-cancel-btn">取消</el-button>
                </template>
              </el-popconfirm>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- === 无详细信息 === -->
    <div v-else class="bar-simple">
      <div class="bar-simple-inner">
        <el-icon class="is-loading bar-loading"><Loading /></el-icon>
        <span class="one-line bar-output">{{ status.lastOutput || '处理中...' }}</span>
        <el-popconfirm title="真的要取消任务吗？" @confirm="cancel">
          <template #reference>
            <el-button text class="bar-cancel-btn">取消</el-button>
          </template>
        </el-popconfirm>
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

const barClass = computed(() => ({
  paused: props.status?.isPaused,
  error: props.status?.task?.status === 4,
}))

const props = defineProps<{
  status: any
  windowWidth: number
}>()

const snapshotSrc = ref('')
const lastSnapshotTime = ref(1e10)
const lastSnapshotFile = ref('')

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
  if (props.status == null) return

  if (props.status?.hasDetail && props.status.task != null && props.status.task.inputs.length >= 1) {
    const isDesktop = props.windowWidth > 768
    const needRefresh = isDesktop && !props.status.isPaused
    const needInitial = snapshotSrc.value === ''

    if (needRefresh || needInitial) {
      if (
        !needInitial &&
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
/* ==============================================================
   StatusBar — 融入式底部状态栏
   ============================================================== */
.status-bar {
  background: var(--el-color-primary-light-8);
  font-size: 14px;
  line-height: 1.4;
  transition: background 0.3s;
}
.status-bar.paused {
  background: var(--el-color-warning-light-8);
}
.status-bar.error {
  background: var(--el-color-danger-light-8);
}

.bar-inner {
  display: flex;
  align-items: stretch;
  gap: 12px;
  padding: 8px 16px;
}

/* 缩略图 */
.bar-snapshot {
  flex-shrink: 0;
  width: 120px;
}
.snapshot-placeholder {
  width: 120px;
  height: 68px;
  background: var(--border-color-light);
  border-radius: var(--radius-sm);
  overflow: hidden;
}
.snapshot-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  cursor: pointer;
  transition: transform var(--transition-fast);
}
.snapshot-img:hover {
  transform: scale(1.05);
}

/* 统计信息区 */
.bar-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}
.bar-stats + .bar-progress-row {
  margin-top: 0;
}
.bar-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 2px 8px;
}
.stat-item {
  display: flex;
  align-items: center;
  gap: 4px;
}
.stat-label {
  color: var(--text-secondary);
  font-weight: 500;
  white-space: nowrap;
}
.stat-value {
  color: var(--text-primary);
  white-space: nowrap;
}

/* 进度条行 */
.bar-progress-row {
  display: flex;
  align-items: center;
  gap: 12px;
  justify-content: space-between;
}
.bar-task-name {
  flex-shrink: 0;
  max-width: 420px;
  color: var(--text-regular);
}
.bar-progress-wrap {
  flex: 1;
  min-width: 0;
}
.bar-progress-wrap .el-progress { margin: 0; }

/* 取消按钮 */
.bar-cancel-btn {
  flex-shrink: 0;
  color: var(--el-color-danger);
  font-size: 12px;
  padding: 4px 8px;
}
.bar-cancel-btn:hover {
  background: rgba(245, 108, 108, 0.1);
  border-radius: var(--radius-xs);
}

/* ---- 精简模式 ---- */
.bar-compact {
  padding: 6px 16px;
}
.bar-compact-inner {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 10px;
}
.bar-snapshot-mobile {
  flex-shrink: 0;
  width: 100px;
}
.bar-snapshot-mobile .snapshot-placeholder {
  width: 100%;
  height: 100%;
  min-height: 56px;
  aspect-ratio: 16 / 9;
  border-radius: var(--radius-sm);
  overflow: hidden;
}
.bar-compact-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
  justify-content: center;
}
.bar-compact-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.bar-compact-stats {
  color: var(--text-secondary);
}
.bar-compact-row2 {
  display: flex;
  align-items: center;
  gap: 8px;
}
.bar-compact-progress {
  flex: 1;
  min-width: 0;
}

/* ---- 简单模式（无进度详情） ---- */
.bar-simple {
  padding: 4px 16px;
}
.bar-simple-inner {
  display: flex;
  align-items: center;
  gap: 8px;
}
.bar-loading {
  font-size: 18px;
  color: var(--el-color-primary);
  flex-shrink: 0;
}
.bar-output {
  flex: 1;
  color: var(--text-regular);
  font-size: 12px;
}
</style>
