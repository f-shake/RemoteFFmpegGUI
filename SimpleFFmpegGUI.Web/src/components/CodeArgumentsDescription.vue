<template>
  <div v-if="args">
    <el-descriptions title="视频" v-if="type === 0 || (type === 4 && args.concat?.type !== 1)">
      <el-descriptions-item label="策略">{{
        args.video == null ? (args.disableVideo ? '不导出' : '不重新编码') : '重新编码'
      }}</el-descriptions-item>
      <el-descriptions-item label="编码" v-if="showVideo">{{
        args.video?.code ? args.video.code : '自动'
      }}</el-descriptions-item>
      <el-descriptions-item label="速度预设" v-if="showVideo">{{ args.video?.preset }}</el-descriptions-item>
      <el-descriptions-item label="CRF" v-if="showVideo">{{ args.video?.crf ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="二次编码" v-if="showVideo">{{ args.video?.twoPass ? '是' : '否' }}</el-descriptions-item>
      <el-descriptions-item label="帧率" v-if="showVideo">{{ args.video?.fps ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="平均码率" v-if="showVideo">{{ args.video?.averageBitrate ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="最高码率" v-if="showVideo">{{ args.video?.maxBitrate ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="最高码率缓冲倍率" v-if="showVideo">{{ args.video?.maxBitrateBuffer ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="分辨率" v-if="showVideo">{{ args.video?.size ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="画面比例" v-if="showVideo">{{ args.video?.aspectRatio ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="像素格式" v-if="showVideo">{{ args.video?.pixelFormat ?? '未定义' }}</el-descriptions-item>
    </el-descriptions>
    <el-descriptions title="音频" v-if="type === 0 || (type === 4 && args.concat?.type !== 1)">
      <el-descriptions-item label="策略">{{
        args.audio == null ? (args.disableAudio ? '不导出' : '不重新编码') : '重新编码'
      }}</el-descriptions-item>
      <el-descriptions-item label="编码" v-if="showAudio">{{
        args.audio?.code ? args.audio.code : '自动'
      }}</el-descriptions-item>
      <el-descriptions-item label="码率" v-if="showAudio">{{ args.audio?.bitrate ?? '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="采样率" v-if="showAudio">{{ args.audio?.samplingRate ?? '未定义' }}</el-descriptions-item>
    </el-descriptions>
    <el-descriptions title="容器" v-if="type <= 2">
      <el-descriptions-item label="格式">{{ args.format ?? '未定义' }}</el-descriptions-item>
    </el-descriptions>
    <el-descriptions title="合并参数" v-if="type === 1">
      <el-descriptions-item label="格式">{{
        args.combine ? (args.combine.shortest ? '裁剪到最短的媒体' : '最后部分静帧或黑屏') : '未定义'
      }}</el-descriptions-item>
    </el-descriptions>
    <el-descriptions title="拼接参数" v-if="type === 4 && args.concat">
      <el-descriptions-item label="格式">
        <a v-if="args.concat.type === 0">通过ts中转</a>
        <a v-if="args.concat.type === 1">使用concat格式</a>
      </el-descriptions-item>
    </el-descriptions>
    <el-descriptions title="其他参数" v-if="type === 0 || type === 1 || type === 99">
      <el-descriptions-item label="参数">{{ args.extra || '未定义' }}</el-descriptions-item>
      <el-descriptions-item label="同步文件时间">{{ args.processedOptions?.syncModifiedTime ? '是' : '否' }}</el-descriptions-item>
      <el-descriptions-item label="完成后删除输入文件">{{ args.processedOptions?.deleteInputFiles ? '是' : '否' }}</el-descriptions-item>
    </el-descriptions>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  args: any
  type: number
}>()

const speedPresets: Record<number, string> = {
  0: '最慢', 1: '更慢', 2: '慢', 3: '平衡',
  4: '快', 5: '更快', 6: '很快', 7: '超快', 8: '极快'
}

const showVideo = computed(() => props.args?.video && props.args?.disableVideo === false)
const showAudio = computed(() => props.args?.audio && props.args?.disableAudio === false)
</script>

<style scoped>
.el-descriptions { margin-top: 18px; }
</style>
