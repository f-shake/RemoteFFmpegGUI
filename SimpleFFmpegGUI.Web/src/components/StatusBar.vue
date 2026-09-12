<template>
  <div class="status-bar" :class="barClass" v-if="status != null && status.isProcessing">
    <template v-if="status.hasDetail">
      <!-- === 宽屏（桌面）=== -->
      <div v-if="windowWidth > 680" class="bar-inner">
        <div class="bar-snapshot" v-show="snapshotSrc !== ''">
          <div class="snapshot-placeholder">
            <el-image :src="snapshotSrc" :preview-src-list="[snapshotSrc]" preview-z-index="9999" fit="cover" class="snapshot-img" />
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
              <span class="stat-label">预计</span>
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
              <el-image :src="snapshotSrc" :preview-src-list="[snapshotSrc]" preview-z-index="9999" fit="cover" class="snapshot-img" />
            </div>
          </div>
          <!-- 点击此处（缩略图与取消按钮除外）弹出详细进度表单 -->
          <div class="bar-compact-body" @click="detailVisible = true" title="点击查看详细进度">
            <div class="bar-compact-info">
              <span class="one-line bar-task-name"><b>{{ status.isPaused ? '暂停中' : '运行中' }}：</b>{{ status.progress.name }}</span>
              <span class="bar-compact-stats"><template v-if="windowWidth >= 320"><span class="stat-key">已用</span> {{ formatDoubleTimeSpan(status.progress.duration) }} / </template><span class="stat-key">剩余</span> {{ formatDoubleTimeSpan(status.progress.lastTime) }}<template v-if="windowWidth >= 400"> / {{ status.fps }}FPS</template><template v-if="windowWidth > 500"> / {{ status.bitrate }}</template></span>
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
                  <el-button text class="bar-cancel-btn" @click.stop>取消</el-button>
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

    <!-- === 窄屏点击下方进度区域（缩略图与取消按钮除外）弹出的详细进度表单 ===
         只在窄屏形态下挂载：它替代的是窄屏统计区域，桌面形态下没有入口；
         同时避免拉宽后弹窗留在桌面布局上、以及关闭状态仍在后台跟着状态轮询重渲染 -->
    <el-dialog v-if="status.hasDetail && windowWidth <= 680" v-model="detailVisible" title="详细进度" :width="detailWidth"
      append-to-body>
      <el-form label-width="88px" size="small" class="detail-form">
        <el-form-item label="任务名称">{{ status.progress.name }}</el-form-item>
        <el-form-item label="状态">{{ status.isPaused ? '已暂停' : '运行中' }}</el-form-item>
        <el-form-item label="进度">{{ progressText }}</el-form-item>
        <el-form-item label="已用时间">{{ formatDoubleTimeSpan(status.progress.duration) }}</el-form-item>
        <el-form-item label="剩余时间">{{ formatDoubleTimeSpan(status.progress.lastTime) }}</el-form-item>
        <el-form-item label="预计">{{ formatDateTime(finishTime(), true, true, false) }}</el-form-item>
        <el-form-item label="编码速度">{{ status.fps }}FPS {{ status.speed }}X</el-form-item>
        <el-form-item label="码率">{{ status.bitrate }}</el-form-item>
        <el-form-item label="已编帧数">{{ status.frame }} 帧</el-form-item>
        <el-form-item label="已编码时长">{{ formatDoubleTimeSpan(status.time, true) }}</el-form-item>
        <el-form-item label="已写出大小">{{ status.size }}</el-form-item>
        <el-form-item label="输出文件">{{ displayPath(status.task?.output) }}</el-form-item>
      </el-form>
      <div class="detail-raw">{{ status.lastOutput }}</div>
    </el-dialog>
  </div>

</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { Loading } from '@element-plus/icons-vue'
import * as net from '@/api'
import { showError } from '@/utils/ui'
import { formatDateTime, formatDoubleTimeSpan } from '@/utils/format'
import { displayPath } from '@/utils/navigation'
import { useQueueStore } from '@/stores/queue'
import { useUiStore } from '@/stores/ui'

// 队列状态与窗口宽度都来自 pinia（不再由 App.vue 用 props 往下传）：这里保留 status / windowWidth
// 两个名字，模板与其余逻辑一行都不用改
const { status } = storeToRefs(useQueueStore())
const { windowWidth } = storeToRefs(useUiStore())
// 本组件里所有与 windowWidth 比较的 680 都必须与 CSS 的 @media (max-width: 680px)
// 以及 stores/ui.ts 的 MOBILE_BREAKPOINT 同值（CSS 无法引用 TS 常量，改断点时要一起改）

const barClass = computed(() => ({
  paused: status.value?.isPaused,
  error: status.value?.task?.status === 4,
}))

const snapshotSrc = ref('')
const lastSnapshotTime = ref(1e10)
const lastSnapshotFile = ref('')
// 窄屏点击进度区域弹出的详细进度表单
const detailVisible = ref(false)
// 拉宽到桌面形态时关掉这个窄屏专用弹窗：弹窗挂载条件里已带宽度判断（拉宽即卸载），
// 这里把状态一并复位，否则缩回窄屏时它会自己又弹出来
watch(() => windowWidth.value > 680, (isDesktop) => {
  if (isDesktop) {
    detailVisible.value = false
  }
})
// 弹窗宽度跟随窗口并留出边距，最大 520px；下限 280px 是给极窄窗口（< 312px）兜底
// （windowWidth 现在来自 useUiStore，初值即真实宽度，不再是改造前那个初值为 0、靠 resizeMenu 纠正的 props）
const detailWidth = computed(() => `${Math.min(Math.max(windowWidth.value - 32, 280), 520)}px`)
// 进度百分比文案：进度无法计算时显示"未知"，与桌面分支的占位文案一致
const progressText = computed(() =>
  status.value?.progress?.isIndeterminate
    ? '未知'
    : (status.value.progress.percent * 100).toFixed(1) + '%'
)
function finishTime(): Date {
  return new Date(status.value.progress.finishTime)
}

function cancel() {
  net.postCancelQueue().catch(showError)
}

function updateSnapshot() {
  if (status.value == null) return

  if (status.value?.hasDetail && status.value.task != null && status.value.task.inputs.length >= 1) {
    const isDesktop = windowWidth.value > 680
    const needRefresh = isDesktop && !status.value.isPaused
    const needInitial = snapshotSrc.value === ''

    if (needRefresh || needInitial) {
      if (
        !needInitial &&
        status.value.task.inputs[0].filePath === lastSnapshotFile.value &&
        Math.abs(status.value.time - lastSnapshotTime.value) < 1
      ) {
        return
      }
      net.getSnapshot(status.value.task.inputs[0].filePath, status.value.time)
        .then((r) => {
          lastSnapshotFile.value = status.value.task.inputs[0].filePath
          lastSnapshotTime.value = status.value.time
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

// 两个定时器的句柄都要留给 onBeforeUnmount 清理：本组件由 App.vue 的
// v-if="status != null && status.isProcessing" 控制挂载，**每跑一次队列就会重新挂载一次**，
// 而闭包里的 status 仍跟着 store 更新——不清理的话，跑过 N 次队列就有 N 个定时器各自去拉快照
// （JPEG 几十 KB，是全站最贵的请求），且永不停止
let snapshotTimer: number | null = null
let firstSnapshotTimer: number | null = null

onMounted(() => {
  snapshotTimer = setInterval(updateSnapshot, 10 * 1000)
  firstSnapshotTimer = setTimeout(updateSnapshot, 1000)
})

onBeforeUnmount(() => {
  if (snapshotTimer !== null) {
    clearInterval(snapshotTimer)
    snapshotTimer = null
  }
  if (firstSnapshotTimer !== null) {
    clearTimeout(firstSnapshotTimer)
    firstSnapshotTimer = null
  }
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
html.dark .status-bar.paused {
  background: var(--bg-elevated);
}
.status-bar.error {
  background: var(--el-color-danger-light-8);
}

.bar-inner {
  display: flex;
  align-items: stretch;
  gap: 12px;
  padding: 8px 16px 6px;
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
  padding: 6px 16px 4px;
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
  /* 点击弹详细进度表单 */
  cursor: pointer;
}
.bar-compact-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.bar-compact-stats {
  color: var(--text-secondary);
}
/* "已用/剩余"标签：比数值小一号，与数值统一按基线对齐（标签后紧跟一个空格） */
.stat-key {
  font-size: 0.85em;
  vertical-align: baseline;
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
  padding: 4px 16px 3px;
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

/* ---- 详细进度表单（窄屏点击弹出） ---- */
.detail-form :deep(.el-form-item) {
  margin-bottom: 6px;
}
.detail-form :deep(.el-form-item__label) {
  color: var(--text-secondary);
}
.detail-form :deep(.el-form-item__content) {
  word-break: break-all;
}
.detail-raw {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--border-color);
  font-family: monospace;
  font-size: 12px;
  line-height: 1.5;
  color: var(--text-secondary);
  word-break: break-all;
}

</style>
