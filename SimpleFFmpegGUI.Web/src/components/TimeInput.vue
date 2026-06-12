<template>
  <div>
    <el-row :gutter="12">
      <el-col :sm="24" :md="11" class="top12">
        <el-input
          maxlength="13"
          placeholder="时间格式：12:34:56.123"
          v-model="str"
          class="time-text"
        >
          <template #prepend>{{ label }}</template>
        </el-input>
      </el-col>
      <el-col :span="1">
        <el-checkbox style="margin-top: 15px" v-model="isEnabled" />
      </el-col>
      <el-col :xs="22" :sm="22" :md="10" class="top12 left12">
        <el-input-number
          :disabled="!isEnabled"
          v-model="h"
          :min="0"
          :max="100"
          size="small"
          :controls="false"
          class="time"
        />
        <a class="time-colon">:</a>
        <el-input-number
          :disabled="!isEnabled"
          v-model="m"
          :min="0"
          :controls="false"
          :max="59"
          size="small"
          class="time"
        />
        <a class="time-colon"> :</a>
        <el-input-number
          :disabled="!isEnabled"
          v-model="s"
          :min="0"
          :controls="false"
          :precision="3"
          :max="59.999"
          size="small"
          class="time"
        />
      </el-col>
    </el-row>
    <div v-if="error != null" style="color: red">{{ error }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const props = defineProps<{
  enabled: boolean
  label: string
  time: number
}>()

const emit = defineEmits<{
  'update:enabled': [value: boolean]
  'update:time': [value: number]
}>()

const h = ref(0)
const m = ref(0)
const s = ref(0)
const str = ref('')
const isEnabled = ref(props.enabled)
const error = ref('')

if (props.time) {
  h.value = Math.floor(props.time / 3600)
  m.value = Math.floor((props.time / 60) % 60)
  s.value = props.time - m.value * 60 - h.value * 3600
}

watch(() => props.enabled, (val) => { isEnabled.value = val })
watch(isEnabled, (val) => { emit('update:enabled', val) })
watch(str, () => parseTime())
watch(h, () => updateTime())
watch(m, () => updateTime())
watch(s, () => updateTime())

function updateTime() {
  emit('update:time', h.value * 3600 + m.value * 60 + s.value)
}

function parseTime() {
  const parts = str.value.replace('：', ':').split(':')
  if (parts.length === 1 || parts.length > 3) {
    error.value = '解析失败，无法识别时间部分'
    return
  }
  const strS = parts[parts.length - 1]
  const strM = parts[parts.length - 2]
  const strH = parts.length === 3 ? parts[parts.length - 3] : '0'
  const parsedH = Number.parseInt(strH)
  const parsedM = Number.parseInt(strM)
  const parsedS = Number.parseFloat(strS)
  if (Number.isNaN(parsedH) || Number.isNaN(parsedM) || Number.isNaN(parsedS)) {
    error.value = '解析失败，无法转为数字'
    return
  }
  error.value = ''
  h.value = parsedH
  m.value = parsedM
  s.value = parsedS
  isEnabled.value = true
}
</script>

<style scoped>
.time { width: 72px; }
.time-colon { margin-left: 6px; margin-right: 6px; }
.time-text { max-width: 320px; }
</style>
