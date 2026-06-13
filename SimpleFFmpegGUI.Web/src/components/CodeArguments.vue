<template>
  <el-form :label-width="labelWidth" :label-position="labelPosition" class="code-args-form">
    <h3 v-if="showPresets">预设</h3>
    <div v-if="showPresets">
      <el-form-item label="选择和更新">
        <div class="flex-row">
          <el-select @change="selectPreset" placeholder="加载预设" v-model="preset" class="preset-select">
            <el-option v-for="p in presets" :key="p.id" :label="p.name" :value="p.id" />
          </el-select>
          <el-button :disabled="preset == null" @click="updatePreset">更新</el-button>
        </div>
      </el-form-item>
      <el-form-item label="新增">
        <div class="flex-row">
          <el-input v-model="newPresetName" class="preset-name-input" />
          <el-button :disabled="!newPresetName?.trim()" @click="savePreset">
            保存或更新"{{ newPresetName }}"
          </el-button>
        </div>
      </el-form-item>
    </div>
    <div v-if="type === 1">
      <h3>合并参数</h3>
      <el-form-item label="音频比视频长">
        <el-switch v-model="code.combine.shortest" active-text="裁剪到最短的媒体" inactive-text="最后部分静帧或黑屏" />
      </el-form-item>
    </div>
    <div v-if="showFormats">
      <h3>容器</h3>
      <el-form-item label="指定输出容器" class="format-form-item">
        <div>
          <div class="flex-row" style="flex-wrap: nowrap;">
            <el-switch v-model="code.enableFormat" />
            <el-select v-show="code.enableFormat" v-model="code.format" placeholder="指定容器格式" class="format-select">
              <el-option v-for="item in formats" :key="item.extension" :label="item.extension" :value="item.name" />
            </el-select>
          </div>
          <div v-show="code.enableFormat" class="help-text">指定输出容器后，输出时会根据格式修改文件扩展名</div>
        </div>
      </el-form-item>
    </div>
    <div v-if="showVideosAndAudios">
      <h3>视频编码</h3>
      <el-form-item label="重编码">
        <el-switch v-model="code.enableVideo" />
        <span v-show="!code.enableVideo" class="right12 gray">不导出视频</span>
        <el-switch v-show="!code.enableVideo" v-model="code.disableVideo" />
      </el-form-item>
      <div v-show="code.enableVideo">
        <el-form-item label="编码">
          <el-select v-model="code.video.code">
            <el-option v-for="c in videoCodes" :key="c" :label="c" :value="c" />
          </el-select>
        </el-form-item>
        <el-form-item label="速度预设" class="preset-slider-item">
          <el-slider :max="8" :show-tooltip="false" v-model="code.video.preset" :marks="speedPresets" />
        </el-form-item>
        <el-form-item label="CRF">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableCrf" />
            <el-slider v-show="code.video.enableCrf" :max="40" :min="10" show-input :step="1" v-model="code.video.crf" />
          </div>
        </el-form-item>
        <el-form-item label="二次编码">
          <el-switch v-model="code.video.twoPass" />
        </el-form-item>
        <el-form-item label="平均码率">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableBitrate" />
            <el-slider v-show="code.video.enableBitrate" :max="200" :min="0.1" show-input :step="0.1" v-model="code.video.bitrate" />
          </div>
        </el-form-item>
        <el-form-item label="最大码率">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableMaxBitrate" />
            <el-slider v-show="code.video.enableMaxBitrate" :max="500" :min="0.1" show-input :step="0.1" v-model="code.video.maxBitrate" />
          </div>
        </el-form-item>
        <el-form-item label="缓冲倍率" v-show="code.video.enableMaxBitrate">
          <div class="switch-slider-row">
            <span class="label-placeholder"></span>
            <el-slider :max="10" :min="1" show-input show-stops :step="0.5" v-model="code.video.maxBitrateBuffer" />
          </div>
        </el-form-item>
        <el-form-item label="帧率">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableFps" />
            <div v-show="code.video.enableFps" class="flex-row flex-wrap">
              <el-input-number v-model="code.video.fps" :precision="3" :min="1" :max="120" class="fps-input" />
              <el-button type="text" @click="code.video.fps = f" v-for="f in fpses" :key="f">{{ f }}帧</el-button>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="分辨率">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableSize" />
            <div v-show="code.video.enableSize" class="flex-row flex-wrap">
              <el-input class="size-input" placeholder="示例：640:480" v-model="code.video.size" />
              <el-button v-for="(v, k) in sizes" :key="k" type="text" @click="code.video.size = v">{{ k }}</el-button>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="画面比例">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enableAspectRatio" />
            <div v-show="code.video.enableAspectRatio" class="flex-row flex-wrap">
              <el-input class="size-input" placeholder="示例：4:3" v-model="code.video.aspectRatio" />
              <el-button v-for="i in aspectRatios" :key="i" type="text" @click="code.video.aspectRatio = i">{{ i }}</el-button>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="像素格式">
          <div class="switch-slider-row">
            <el-switch v-model="code.video.enablePixelFormat" />
            <div v-show="code.video.enablePixelFormat" class="flex-row flex-wrap">
              <el-input class="size-input" v-model="code.video.pixelFormat" />
              <el-button v-for="p in pixelFormats" :key="p" type="text" @click="code.video.pixelFormat = p">{{ p }}</el-button>
            </div>
          </div>
        </el-form-item>
      </div>
    </div>
    <div v-if="showVideosAndAudios">
      <h3>音频编码</h3>
      <el-form-item label="重编码">
        <el-switch v-model="code.enableAudio" />
        <span v-show="!code.enableAudio" class="right12 gray">不导出音频</span>
        <el-switch v-show="!code.enableAudio" v-model="code.disableAudio" />
      </el-form-item>
      <div v-show="code.enableAudio">
        <el-form-item label="编码">
          <el-select v-model="code.audio.code">
            <el-option v-for="c in audioCodes" :key="c" :label="c" :value="c" />
          </el-select>
        </el-form-item>
        <el-form-item label="码率">
          <div class="switch-slider-row">
            <el-switch v-model="code.audio.enableBitrate" />
            <el-slider v-show="code.audio.enableBitrate" :max="320" :min="32" :show-tooltip="false" :step="32"
              v-model="code.audio.bitrate" :marks="audioBitrates" />
          </div>
        </el-form-item>
        <el-form-item label="采样率">
          <div class="switch-slider-row">
            <el-switch v-model="code.audio.enableSample" />
            <el-select v-model="code.audio.sample" v-show="code.audio.enableSample">
              <el-option v-for="c in audioSamples" :key="c" :label="c" :value="c" />
            </el-select>
          </div>
        </el-form-item>
      </div>
    </div>
    <div>
      <h3>{{ type === 99 ? '参数' : '其他参数' }}</h3>
      <el-form-item label="额外参数">
        <el-input v-model="code.extra" type="textarea" autosize spellcheck="false" autocorrect="off"
          placeholder="请输入ffmpeg的输出参数" />
      </el-form-item>
      <el-form-item label="同步文件时间">
        <el-switch v-model="code.processedOptions.syncModifiedTime" />
        <span class="gray left12">将输出文件的修改时间设置为最后一个输入文件的修改时间</span>
      </el-form-item>
      <el-form-item label="删除输入文件">
        <el-switch v-model="code.processedOptions.deleteInputFiles" />
        <span class="gray left12">处理完成后，删除所有输入文件</span>
      </el-form-item>
    </div>
  </el-form>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { showError, showSuccess } from '../common'
import * as net from '../net'

const props = withDefaults(defineProps<{
  type?: number
  showPresets?: boolean
}>(), {
  type: 0,
  showPresets: true
})

defineExpose({ getArgs, updateFromArgs })

const speedPresets = {
  0: '最慢', 1: '更慢', 2: '慢', 3: '平衡',
  4: '快', 5: '更快', 6: '很快', 7: '超快', 8: '极快'
}
const audioBitrates: Record<number, string> = {
  32: '32', 64: '64', 96: '96', 128: '128', 192: '192', 256: '256', 320: '320'
}
const videoCodes = ['自动', 'H264', 'H265', 'VP9', 'AV1 (aom)', 'AV1 (SVT)']
const audioCodes = ['自动', 'AAC', 'OPUS']
const audioSamples = [8000, 16000, 32000, 44100, 48000, 96000]
const aspectRatios = ['4:3', '16:9', '2.35']
const fpses = [10, 24, 25, 29.97, 30, 59.94, 60]
const sizes: Record<string, string> = {
  '480P': '-1:480', '720P': '-1:720', '1080P': '-1:1080',
  '1440P': '-1:1440', '2160P': '-1:2160'
}
const pixelFormats = ['yuv420p', 'yuvj420p', 'yuv422p', 'yuvj422p', 'rgb24', 'gray', 'yuv420p10le']
const formats = ref<any[]>([])
const presets = ref<any[]>([])
const preset = ref<any>(null)
const newPresetName = ref('新预设')

const code = reactive({
  enableVideo: true,
  enableAudio: true,
  enableFormat: true,
  format: 'mp4',
  video: {
    code: 'H265', preset: 3, crf: 23, enableCrf: true, twoPass: false,
    size: '1920:1080', enableSize: false, bitrate: 6, enableBitrate: false,
    maxBitrate: 24, maxBitrateBuffer: 2, enableMaxBitrate: false,
    fps: 30, enableFps: false, enableAspectRatio: false, aspectRatio: '16:9',
    enablePixelFormat: false, pixelFormat: ''
  },
  audio: {
    code: 'AAC', enableBitrate: true, bitrate: 128, enableSample: false, sample: 48000
  },
  combine: { shortest: false },
  disableVideo: false,
  disableAudio: false,
  extra: '',
  processedOptions: { syncModifiedTime: false, deleteInputFiles: false }
})

const showFormats = computed(() => [0, 1, 2, 4].includes(props.type))
const showVideosAndAudios = computed(() => [0].includes(props.type))

const labelWidth = ref('100px')
const labelPosition = ref<'top' | 'left' | 'right'>('right')
onMounted(() => {
  function updateWidth() {
    if (window.innerWidth < 640) {
      labelPosition.value = 'top'
      labelWidth.value = '100%'
    } else {
      labelPosition.value = 'right'
      labelWidth.value = '100px'
    }
  }
  updateWidth()
  window.addEventListener('resize', updateWidth)
})

function fillPresetsAnd(action: (id: number) => void) {
  net.getPresets(props.type)
    .then((r) => {
      presets.value = r.data
      action(r.data)
    })
    .catch(showError)
}

function fillPresets() {
  fillPresetsAnd(() => {})
}

function selectPreset(presetId: number) {
  const p = presets.value.find((p: any) => p.id === presetId)
  if (p) updateFromArgs(p.arguments ?? p.parameters)
}

function updatePreset() {
  const args = getArgs()
  if (args == null) return
  const name = (presets.value.find((p: any) => p.id === preset.value) as any)?.name
  net.postAddOrUpdatePreset(name, props.type, args)
    .then((r) => {
      showSuccess('更新预设成功')
      fillPresetsAnd(() => { preset.value = r.data })
    })
    .catch(showError)
}

function savePreset() {
  const args = getArgs()
  if (args == null) return
  net.postAddOrUpdatePreset(newPresetName.value, props.type, args)
    .then((r) => {
      showSuccess('新建或更新预设成功')
      fillPresetsAnd(() => { preset.value = r.data as any })
    })
    .catch(showError)
}

function getArgs() {
  const v = code.video
  const videoArg = code.enableVideo
    ? {
        code: v.code, preset: v.preset,
        crf: v.enableCrf ? v.crf : null, twoPass: v.twoPass,
        size: v.enableSize ? v.size : null,
        fps: v.enableFps ? v.fps : null,
        averageBitrate: v.enableBitrate ? v.bitrate : null,
        maxBitrate: v.enableMaxBitrate ? v.maxBitrate : null,
        maxBitrateBuffer: v.enableMaxBitrate ? v.maxBitrateBuffer : null,
        aspectRatio: v.enableAspectRatio ? v.aspectRatio : null,
        pixelFormat: v.enablePixelFormat ? v.pixelFormat : null
      }
    : null
  const a = code.audio
  const audioArg = code.enableAudio
    ? {
        code: a.code,
        bitrate: a.enableBitrate ? a.bitrate : null,
        samplingRate: a.enableSample ? a.sample : null
      }
    : null
  const arg = {
    video: videoArg,
    audio: audioArg,
    input: null,
    combine: props.type === 1 ? { shortest: code.combine.shortest } : null,
    extra: code.extra,
    processedOptions: code.processedOptions,
    disableVideo: videoArg == null && code.disableVideo,
    disableAudio: audioArg == null && code.disableAudio,
    format: code.enableFormat ? code.format : null
  }
  if (arg.disableVideo && arg.disableAudio) {
    showError('不可同时禁用视频和音频')
    return null
  }
  return arg
}

function updateFromArgs(args: any) {
  const video = args.video
  const audio = args.audio
  const combine = args.combine
  if (video != null && !args.disableVideo) {
    code.enableVideo = true
    const uiV = code.video
    uiV.code = video.code
    uiV.preset = video.preset
    uiV.enableCrf = video.crf != null; if (video.crf != null) uiV.crf = video.crf
    uiV.twoPass = video.twoPass
    uiV.enableSize = video.size != null; if (video.size != null) uiV.size = video.size
    uiV.enableFps = video.fps != null; if (video.fps != null) uiV.fps = video.fps
    uiV.enableBitrate = video.averageBitrate != null; if (video.averageBitrate != null) uiV.bitrate = video.averageBitrate
    uiV.enableMaxBitrate = video.maxBitrate != null
    if (video.maxBitrate != null) { uiV.maxBitrate = video.maxBitrate; uiV.maxBitrateBuffer = video.maxBitrateBuffer }
    uiV.enableAspectRatio = !!video.aspectRatio; if (video.aspectRatio) uiV.aspectRatio = video.aspectRatio
    uiV.enablePixelFormat = !!video.pixelFormat; if (video.pixelFormat) uiV.pixelFormat = video.pixelFormat
  } else {
    code.enableVideo = false
  }
  if (audio != null && !args.disableAudio) {
    const uiA = code.audio
    uiA.code = audio.code
    uiA.enableBitrate = audio.bitrate != null; if (audio.bitrate != null) uiA.bitrate = audio.bitrate
    uiA.enableSample = audio.samplingRate != null; if (audio.samplingRate != null) uiA.sample = audio.samplingRate
  } else {
    code.enableAudio = false
  }
  if (props.type === 1 && combine != null) {
    code.combine = { shortest: combine.shortest }
  }
  code.disableVideo = args.disableVideo
  code.disableAudio = args.disableAudio
  code.enableFormat = args.format != null
  code.format = args.format
  code.extra = args.extra ?? args.Extra
  code.processedOptions = args.processedOptions
}

onMounted(() => {
  fillPresets()
  net.getFormats()
    .then((r) => { formats.value = r.data })
    .catch(showError)
})
</script>

<style scoped>
div[role="slider"] { min-width: 200px; max-width: 400px; }
.el-select { min-width: 160px; }

.flex-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}
.flex-wrap {
  flex-wrap: wrap;
}
.switch-slider-row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}
.switch-slider-row .el-slider {
  flex: 1;
}
.preset-select { min-width: 200px; flex: 1; }
.preset-name-input { width: 180px; }
.format-select { min-width: 160px; }
.fps-input { width: 100px; }
.size-input { width: 200px; }
.preset-slider-item {
  padding-bottom: 20px;
}
.help-text {
  margin-top: 8px;
  color: #999;
  font-size: 13px;
  line-height: 1.4;
}
.format-form-item :deep(.el-form-item__content) {
  align-items: flex-start;
}
.code-args-form .el-form-item { margin-bottom: 20px !important; }
</style>

<style>
@media (max-width: 640px) {
  .code-args-form .el-form-item__content > * { max-width: 100%; }
  .code-args-form .el-slider { width: 100% !important; min-width: 0 !important; }
  .code-args-form .el-input-number, .code-args-form .el-select { min-width: 0 !important; }
  .code-args-form .el-slider .el-input-number { width: 130px !important; }
  .code-args-form .el-slider .el-slider__input { margin-left: 0 !important; }
  .code-args-form .switch-slider-row .el-slider { min-width: 0 !important; width: auto !important; flex: none !important; }
  .code-args-form .el-input, .code-args-form .el-textarea { width: 100% !important; }
  .size-input { width: 100% !important; }
  .fps-input { width: 100% !important; }

  /* 预设行：select 和按钮保持同行，左右分布 */
  .code-args-form .flex-row { width: 100%; }
  .code-args-form .preset-select { flex: 1; min-width: 80px !important; }
  .code-args-form .preset-name-input { flex: 1; min-width: 80px !important; }

  /* 隐藏预设文字按钮节省空间 */
  .code-args-form .el-form-item .el-button--text { display: none; }
}
.code-args-form .el-form-item { margin-bottom: 20px !important; }
</style>
