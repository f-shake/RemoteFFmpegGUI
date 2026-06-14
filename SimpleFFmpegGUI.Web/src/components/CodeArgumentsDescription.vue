<template>
  <div v-if="args" class="c-args">
    <!-- 视频 -->
    <div v-if="showVideoSection" class="c-card">
      <div class="c-card-header">
        <el-icon><VideoCamera /></el-icon>
        <span>视频</span>
      </div>
      <div class="c-grid">
        <span class="c-label">策略</span>
        <span class="c-val">{{ ['重新编码', '复制', '不导出'][args.video?.strategy ?? 1] }}</span>
        <template v-if="showVideo">
          <span class="c-label">编码</span>
          <span class="c-val">{{ (args.video?.codec ?? args.video?.code) || '自动' }}</span>
          <span class="c-label">预设</span>
          <span class="c-val">{{ args.video?.preset }}</span>
          <span class="c-label">CRF</span>
          <span class="c-val" :class="{ 'c-val--nil': args.video?.crf == null }">{{ args.video?.crf ?? '未定义' }}</span>
          <span class="c-label">二次编码</span>
          <span class="c-val">{{ args.video?.twoPass ? '是' : '否' }}</span>
          <span class="c-label">帧率</span>
          <span v-if="args.video?.fps != null" class="c-val">{{ args.video.fps }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">平均码率</span>
          <span v-if="args.video?.averageBitrate != null" class="c-val">{{ args.video.averageBitrate }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">最高码率</span>
          <span v-if="args.video?.maxBitrate != null" class="c-val">{{ args.video.maxBitrate }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">缓冲倍率</span>
          <span v-if="args.video?.maxBitrateBuffer != null" class="c-val">{{ args.video.maxBitrateBuffer }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">分辨率</span>
          <span v-if="args.video?.size" class="c-val">{{ args.video.size }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">画面比例</span>
          <span v-if="args.video?.aspectRatio" class="c-val">{{ args.video.aspectRatio }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">像素格式</span>
          <span v-if="args.video?.pixelFormat" class="c-val">{{ args.video.pixelFormat }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
        </template>
      </div>
    </div>

    <!-- 音频 -->
    <div v-if="showAudioSection" class="c-card">
      <div class="c-card-header">
        <el-icon><Headset /></el-icon>
        <span>音频</span>
      </div>
      <div class="c-grid audio-grid">
        <span class="c-label">策略</span>
        <span class="c-val">{{ ['重新编码', '复制', '不导出'][args.audio?.strategy ?? 1] }}</span>
        <template v-if="showAudio">
          <span class="c-label">编码</span>
          <span class="c-val">{{ (args.audio?.codec ?? args.audio?.code) || '自动' }}</span>
          <span class="c-label">码率</span>
          <span v-if="args.audio?.bitrate != null" class="c-val">{{ args.audio.bitrate }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
          <span class="c-label">采样率</span>
          <span v-if="args.audio?.samplingRate != null" class="c-val">{{ args.audio.samplingRate }}</span>
          <span v-else class="c-val c-val--nil">未定义</span>
        </template>
      </div>
    </div>

    <!-- 容器 -->
    <div v-if="showContainerSection && args.format" class="c-card c-card--thin">
      <div class="c-grid">
        <span class="c-label">容器</span>
        <span class="c-val">{{ args.format }}</span>
      </div>
    </div>

    <!-- 合并参数 -->
    <div v-if="hasCombine" class="c-card c-card--thin">
      <div class="c-grid">
        <span class="c-label">合并</span>
        <span class="c-val">{{ combineShortest ? '裁剪到最短媒体的长度' : '最后部分静帧或黑屏' }}</span>
      </div>
    </div>

    <!-- 拼接参数 -->
    <div v-if="type === 4 && args.concat" class="c-card c-card--thin">
      <div class="c-grid">
        <span class="c-label">拼接</span>
        <span class="c-val">{{ args.concat.type === 0 ? '通过 ts 中转' : '使用 concat 格式' }}</span>
      </div>
    </div>

    <!-- 其他参数 -->
    <div v-if="showOtherSection && (args.extra || args.processedOptions)" class="c-card c-card--thin">
      <div class="c-grid">
        <span v-if="args.extra" class="c-label">额外参数</span>
        <span v-if="args.extra" class="c-val c-val--mono">{{ args.extra }}</span>
        <span class="c-label">同步时间</span>
        <span class="c-val">{{ args.processedOptions?.syncModifiedTime ? '是' : '否' }}</span>
        <span class="c-label">删除输入</span>
        <span class="c-val">{{ args.processedOptions?.deleteInputFiles ? '是' : '否' }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  args: any
  type: number
}>()

const showVideo = computed(() => props.args?.video?.strategy === 0)
const showAudio = computed(() => props.args?.audio?.strategy === 0)
const showVideoSection = computed(() => props.type === 0 || (props.type === 4 && props.args?.concat?.type !== 1))
const showAudioSection = computed(() => props.type === 0 || (props.type === 4 && props.args?.concat?.type !== 1))
const showContainerSection = computed(() => props.type <= 2)
const showOtherSection = computed(() => props.type === 0 || props.type === 1 || props.type === 99)
const hasCombine = computed(() => props.type === 1)
const combineShortest = computed(() => {
  if (props.args?.mux?.shortest != null) return props.args.mux.shortest
  if (props.args?.combine?.shortest != null) return props.args.combine.shortest
  return false
})
</script>

<style scoped>
.c-args {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 2px 0;
}

.c-card {
  background: var(--el-fill-color-lighter);
  border-radius: 6px;
  padding: 8px 12px;
  border: 1px solid var(--border-color);
  margin: 0 8px;
}

.c-card-header {
  display: flex;
  align-items: center;
  gap: 5px;
  font-weight: 600;
  font-size: 12px;
  color: var(--text-primary);
  margin-bottom: 4px;
  padding-bottom: 4px;
  border-bottom: 1px solid var(--border-color);
}

.c-card-header .el-icon {
  font-size: 14px;
  color: var(--el-color-primary);
}

.c-card--thin {
  padding: 6px 12px;
}
.c-card--thin .c-grid {
  gap: 2px 12px;
  grid-template-columns: auto 1fr;
}

.c-grid {
  display: grid;
  gap: 2px 12px;
  font-size: 12px;
  line-height: 1.6;
}

/* 视频默认 3 列（6 cells/row），音频 2 列（4 cells/row） */
.c-card:not(.c-card--thin) .c-grid {
  grid-template-columns: auto 1fr auto 1fr auto 1fr;
}
.c-card:not(.c-card--thin) .c-grid.audio-grid {
  grid-template-columns: auto 1fr auto 1fr;
}

@media (max-width: 640px) {
  .c-card:not(.c-card--thin) .c-grid {
    grid-template-columns: auto 1fr;
  }
}

.c-label {
  color: var(--text-secondary);
  white-space: nowrap;
  text-align: right;
}

.c-val {
  color: var(--text-primary);
  word-break: break-all;
  min-width: 0;
}

.c-val--nil {
  color: var(--text-disabled);
  font-style: italic;
}

.c-val--mono {
  font-family: var(--font-mono);
  font-size: 11px;
}
</style>
