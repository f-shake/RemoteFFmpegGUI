<template>
  <div class="json-tree">
    <!-- null / undefined -->
    <span v-if="data === null || data === undefined" class="json-null">无</span>

    <!-- primitive values -->
    <span v-else-if="isPrimitive(data)" :class="valueTypeClass(data)">
      {{ displayText(data) }}
    </span>

    <!-- empty array -->
    <span v-else-if="Array.isArray(data) && data.length === 0" class="json-bracket">[]</span>

    <!-- non-empty array -->
    <el-collapse v-else-if="Array.isArray(data)" v-model="activeIndices">
      <el-collapse-item v-for="(item, index) in data" :key="index" :name="index">
        <template #title>
          <span class="collapse-title">[{{ index }}]</span>
        </template>
        <json-tree :data="item" />
      </el-collapse-item>
    </el-collapse>

    <!-- empty object -->
    <span v-else-if="Object.keys(data).length === 0" class="json-bracket">{}</span>

    <!-- non-empty object -->
    <template v-else>
      <template v-for="(value, key) in data" :key="key">
        <div v-if="isPrimitive(value)" class="kv-row">
          <span class="json-key">{{ key }}:</span>
          <span class="json-value" :class="valueTypeClass(value)">{{ displayText(value) }}</span>
        </div>
      </template>
      <el-collapse v-if="complexKeys.length > 0" v-model="activeKeys">
        <el-collapse-item v-for="k in complexKeys" :key="k" :name="k">
          <template #title>
            <span class="collapse-title">{{ k }}</span>
          </template>
          <json-tree :data="data[k]" />
        </el-collapse-item>
      </el-collapse>
    </template>
  </div>
</template>

<script lang="ts">
export default { name: 'JsonTree' }
</script>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = defineProps<{ data: any }>()

const activeIndices = ref<(string | number)[]>([])
const activeKeys = ref<string[]>([])

watch(
  () => props.data,
  (data) => {
    if (Array.isArray(data)) {
      activeIndices.value = data.map((_, i) => i)
    }
    if (data && typeof data === 'object' && !Array.isArray(data)) {
      activeKeys.value = Object.keys(data).filter((k) => {
        const v = data[k]
        return v !== null && v !== undefined && typeof v === 'object'
      })
    }
  },
  { immediate: true }
)

function isPrimitive(v: any): boolean {
  return v === null || v === undefined || typeof v !== 'object'
}

function valueTypeClass(v: any): string {
  if (v === null || v === undefined) return 'json-null'
  if (typeof v === 'string') return 'json-string'
  if (typeof v === 'number') return 'json-number'
  if (typeof v === 'boolean') return 'json-bool'
  return ''
}

function displayText(v: any): string {
  if (v === null) return 'null'
  if (v === undefined) return 'undefined'
  if (typeof v === 'string') return v
  return String(v)
}

const complexKeys = computed(() => {
  if (!props.data || typeof props.data !== 'object' || Array.isArray(props.data)) return []
  return Object.keys(props.data).filter((k) => {
    const v = props.data[k]
    return v !== null && v !== undefined && typeof v === 'object'
  })
})
</script>

<style scoped>
.json-tree {
  font-size: 13px;
  line-height: 1.7;
}

/* key name */
.json-key {
  color: var(--el-color-primary);
  font-weight: 500;
  flex-shrink: 0;
}

/* value by type */
.json-string {
  color: var(--el-color-success);
  word-break: break-all;
}
.json-number {
  color: var(--el-color-warning);
}
.json-bool {
  color: var(--el-color-danger);
}
.json-null {
  color: var(--el-text-color-secondary);
}
.json-bracket {
  color: var(--el-text-color-secondary);
  font-style: italic;
}

/* key-value row */
.kv-row {
  display: flex;
  gap: 8px;
  padding: 2px 8px;
  border-radius: 4px;
}
.kv-row:hover {
  background-color: var(--el-fill-color-light);
}

/* collapse title area */
.collapse-title {
  font-weight: 500;
  font-size: 13px;
  color: var(--el-color-primary);
  user-select: none;
}

/* nested collapse — remove redundant borders to keep flat look */
.json-tree .el-collapse {
  border-top: none;
}
.json-tree .el-collapse-item:last-child {
  border-bottom: none;
}
.json-tree .el-collapse-item .el-collapse-item__header {
  padding-left: 8px;
  height: 36px;
}
.json-tree .el-collapse-item .el-collapse-item__wrap {
  background: transparent;
}
.json-tree .el-collapse-item .el-collapse-item__content {
  padding: 4px 8px 8px 20px;
}

/* limit depth visual indicator — left border for nested content */
.json-tree .el-collapse-item .json-tree {
  border-left: 1px solid var(--el-border-color-light);
  padding-left: 8px;
}
</style>
