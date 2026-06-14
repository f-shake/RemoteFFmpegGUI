<template>
  <el-select
    filterable
    v-model="selectedFile"
    @change="selectChanged"
    :placeholder="files == null ? '请选择文件' : '请选择文件（共' + files.length + '个）'"
  >
    <el-option v-for="item in files" :key="item.path" :label="item.relativePath" :value="item.relativePath" />
  </el-select>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import * as net from '@/api'
import { showError } from '@/utils/ui'

const props = defineProps<{ file: string }>()
const emit = defineEmits<{ 'update:file': [value: string] }>()

const files = ref<any[] | null>(null)
const selectedFile = ref(props.file)

watch(() => props.file, (val) => { selectedFile.value = val })

function selectChanged(e: string) {
  emit('update:file', e)
}

onMounted(() => {
  net.getMediaNames()
    .then((response) => { files.value = response.data })
    .catch(showError)
})
</script>

<style scoped>
.el-select-dropdown__item {
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  padding-left: 28px;
  padding-right: 28px;
}
</style>
