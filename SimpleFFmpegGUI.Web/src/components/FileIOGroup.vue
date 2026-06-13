<template>
  <el-form label-position="top">
    <el-form-item label="输入文件">
      <el-collapse v-model="activeInput" style="width: 100%">
        <el-collapse-item :name="index" v-for="(value, index) in inputFiles" :key="index">
          <template #title>
            <span class="file-title">
              文件{{ index + 1 }}
              <span v-if="value.name" class="file-name">{{ value.name }}</span>
              <span v-if="value.enableFrom" class="file-clip">从{{ value.from }}s</span>
              <span v-if="value.enableTo" class="file-clip">到{{ value.to }}s</span>
              <span v-if="value.enableDuration" class="file-clip">经{{ value.duration }}s</span>
            </span>
          </template>
          <div style="padding: 8px 0">
            <file-select @update:file="(f: string) => updateFile(f, index)" :file="value.name" class="file-input" />
            <div v-if="showClip" class="clip-section">
              <time-input :enabled="value.enableFrom" label="开始" @update:enabled="(v: boolean) => value.enableFrom = v"
                @update:time="(v: number) => value.from = v" :time="value.from" />
              <time-input :enabled="value.enableTo" label="结束" @update:enabled="(v: boolean) => value.enableTo = v"
                @update:time="(v: number) => value.to = v" :time="value.to" />
            </div>
            <div v-if="showMore" class="more-section">
              <el-checkbox v-model="value.image2">输入为图片序列</el-checkbox>
              <span v-show="value.image2" class="left12">输入帧率：</span>
              <el-input-number v-show="value.image2" v-model="value.framerate" size="small" :precision="3" :min="1" :max="120" />
            </div>
            <div v-if="showMore" class="more-section">
              <span>其他参数：</span>
              <el-input v-model="value.extra" class="width240" />
            </div>
          </div>
        </el-collapse-item>
      </el-collapse>
      <div class="file-actions">
        <el-button @click="addFile" circle><el-icon><Plus /></el-icon></el-button>
        <el-button @click="removeFile" circle v-if="inputFiles.length > min"><el-icon><Close /></el-icon></el-button>
      </div>
    </el-form-item>

    <el-form-item label="输出文件名">
      <el-input placeholder="留空则自动生成" v-model="outputFile"
        :disabled="inputFiles.length > 1 && !singleOutput"
        @change="(value: string) => emit('update:output', value)" class="output-input" />
      <div v-if="inputFiles.length > 1 && !singleOutput" class="gray top12">
        输入多个文件时，输出文件名为首个不重复的原文件名
      </div>
    </el-form-item>
  </el-form>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, Close } from '@element-plus/icons-vue'
import { showError } from '../common'
import FileSelect from './FileSelect.vue'
import TimeInput from './TimeInput.vue'

const props = withDefaults(defineProps<{
  inputs?: any[]
  output?: string
  min?: number
  showClip?: boolean
  showMore?: boolean
  singleOutput?: boolean
}>(), {
  inputs: () => [],
  output: '',
  min: 1,
  showClip: true,
  showMore: false,
  singleOutput: false
})

const emit = defineEmits<{
  'update:inputs': [value: any[]]
  'update:output': [value: string]
}>()

const activeInput = ref([0])
const outputFile = ref(props.output)

function getNewFile() {
  return {
    name: '', enableFrom: false, enableTo: false, enableDuration: false,
    from: 0, to: 0, duration: 0, image2: false, framerate: 30, extra: ''
  }
}

const inputFiles = ref<any[]>(
  props.inputs.length > 0
    ? props.inputs.map((f: any) => ({
        name: f.filePath, enableFrom: f.from != null, from: f.from ?? 0,
        enableTo: f.to != null, to: f.to ?? 0, enableDuration: f.duration != null,
        duration: f.duration ?? 0, image2: f.image2 ?? false,
        framerate: f.framerate ?? 30, extra: f.extra ?? ''
      }))
    : [getNewFile()]
)

for (let i = 1; i < props.min; i++) {
  inputFiles.value.push(getNewFile())
  activeInput.value.push(i)
}

watch(() => props.output, (val) => { outputFile.value = val })

function addFile() {
  inputFiles.value.push(getNewFile())
  activeInput.value = [inputFiles.value.length - 1]
  emit('update:inputs', getArgs())
}

function updateFile(file: string, index: number) {
  inputFiles.value[index].name = file
  emit('update:inputs', getArgs())
}

function removeFile() {
  inputFiles.value.splice(-1)
  emit('update:inputs', getArgs())
}

function getArgs() {
  const inputs: any[] = []
  inputFiles.value
    .filter(p => p.name !== '')
    .forEach(file => {
      if (file.to && file.from && file.to <= file.from) {
        showError('结束时间需要小于开始时间')
        return
      }
      inputs.push({
        filePath: file.name,
        from: file.enableFrom ? file.from : null,
        to: file.enableTo ? file.to : null,
        duration: file.enableDuration ? file.duration : null,
        image2: file.image2,
        framerate: file.image2 ? file.framerate : null,
        extra: file.extra
      })
    })
  return inputs
}
</script>

<style scoped>
.file-title {
  font-size: 13px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.file-name { color: var(--el-color-primary); margin-left: 4px; }
.file-clip { color: #999; margin-left: 4px; font-size: 12px; }
.file-input { width: 100%; }
.clip-section, .more-section { margin-top: 12px; }
.file-actions { margin-top: 12px; display: flex; gap: 8px; }
.output-input { max-width: 400px; }
@media (max-width: 640px) {
  .output-input { max-width: 100%; }
}
</style>
