<template>
  <div>
    <el-form-item label="输入">
      <el-collapse v-model="activeInput">
        <el-collapse-item :name="index" v-for="(value, index) in inputFiles" :key="index">
          <template #title>
            <a>
              文件{{ index + 1 }}
              {{ value.name }}
              {{ value.enableFrom ? `从${value.from}s` : '' }}
              {{ value.enableTo ? `到${value.to}s` : '' }}
              {{ value.enableDuration ? `经过${value.duration}s` : '' }}
            </a>
          </template>
          <div class="top12">
            <file-select
              style="margin-left: 7px"
              ref="files"
              @update:file="(f: string) => updateFile(f, index)"
              :file="value.name"
              class="right24"
            />
          </div>
          <div v-if="showClip">
            <time-input
              :enabled="value.enableFrom"
              label="开始"
              @update:enabled="(v: boolean) => value.enableFrom = v"
              @update:time="(v: number) => value.from = v"
              :time="value.from"
            />
            <time-input
              :enabled="value.enableTo"
              label="结束"
              @update:enabled="(v: boolean) => value.enableTo = v"
              @update:time="(v: number) => value.to = v"
              :time="value.to"
            />
          </div>
          <div class="top12" v-if="showMore">
            <el-checkbox v-model="value.image2">输入为图片序列</el-checkbox>
            <a class="left24" v-show="value.image2">输入帧率：</a>
            <el-input-number
              v-show="value.image2"
              v-model="value.framerate"
              size="small"
              :precision="3"
              :min="1"
              :max="120"
            />
          </div>
          <div class="top12" v-if="showMore">
            <a>其他参数：</a>
            <el-input v-model="value.extra" class="width240" size="small" />
          </div>
        </el-collapse-item>
      </el-collapse>
      <div class="top12">
        <el-button @click="addFile" circle class="right12"><el-icon><Plus /></el-icon></el-button>
        <el-button @click="removeFile" circle v-if="inputFiles.length > min"><el-icon><Close /></el-icon></el-button>
      </div>
    </el-form-item>
    <el-form-item label="输出">
      <el-input
        placeholder="输出文件名"
        style="width: 300px; display: block"
        v-model="outputFile"
        :disabled="inputFiles.length > 1 && !singleOutput"
        @change="(value: string) => emit('update:output', value)"
      />
      <a v-if="inputFiles.length > 1 && !singleOutput" class="gray">
        输入多个文件时，输出文件名为首个不重复的原文件名
      </a>
    </el-form-item>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { Plus, Close } from '@element-plus/icons-vue'
import * as net from '../net'
import { showError } from '../common'
import FileSelect from './FileSelect.vue'
import TimeInput from './TimeInput.vue'

const props = defineProps<{
  inputs: any[]
  output: string
  min: number
  showClip: boolean
  showMore: boolean
  singleOutput: boolean
}>()

const emit = defineEmits<{
  'update:inputs': [value: any[]]
  'update:output': [value: string]
}>()

const activeInput = ref([0])
const outputFile = ref(props.output)

function getNewFile() {
  return {
    name: '',
    enableFrom: false,
    enableTo: false,
    enableDuration: false,
    from: 0,
    to: 0,
    duration: 0,
    image2: false,
    framerate: 30,
    extra: ''
  }
}

const inputFiles = ref<any[]>(
  props.inputs.length > 0
    ? props.inputs.map((f: any) => ({
        name: f.filePath,
        enableFrom: f.from != null,
        from: f.from ?? 0,
        enableTo: f.to != null,
        to: f.to ?? 0,
        enableDuration: f.duration != null,
        duration: f.duration ?? 0,
        image2: f.image2 ?? false,
        framerate: f.framerate ?? 30,
        extra: f.extra ?? ''
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
