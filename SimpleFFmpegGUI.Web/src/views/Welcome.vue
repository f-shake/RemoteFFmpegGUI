<template>
  <div class="welcome">
    <div class="hero">
      <div class="hero-icon">
        <el-icon :size="48"><VideoCamera /></el-icon>
      </div>
      <h1 class="hero-title">远程 FFmpeg 工具箱</h1>
      <p class="hero-desc">基于 FFmpeg 的视频转码、拼接、媒体信息查询工具</p>
    </div>

    <el-row :gutter="16" class="card-row">
      <el-col :xs="24" :sm="12" :md="8" v-for="group in cardGroups" :key="group.title">
        <el-card class="group-card" shadow="hover">
          <template #header>
            <div class="card-header">
              <el-icon :size="18"><component :is="group.icon" /></el-icon>
              <span>{{ group.title }}</span>
            </div>
          </template>
          <div v-for="item in group.items" :key="item.label" class="card-item">
            <el-button text @click="item.action">
              <el-icon :size="14" v-if="item.icon" style="margin-right: 4px"><component :is="item.icon" /></el-icon>
              {{ item.label }}
            </el-button>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <div class="footer-link">
      <el-link href="https://github.com/f-shake/RemoteFFmpegGUI" target="_blank" :underline="false">
        <el-icon><Share /></el-icon> GitHub
      </el-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { jump, TaskType } from '../common'
import {
  VideoCamera, DocumentAdd, CirclePlus, Plus,
  Search, Document, CopyDocument, FolderOpened, Setting, TakeawayBox, Share
} from '@element-plus/icons-vue'

const types = TaskType.Types

const cardGroups = [
  {
    title: '新建任务',
    icon: 'DocumentAdd',
    items: types.map((t: any) => ({
      label: t.Description,
      icon: 'Plus',
      action: () => jump('add/' + t.Route)
    }))
  },
  {
    title: '工具',
    icon: 'Setting',
    items: [
      { label: '查询媒体信息', icon: 'Search', action: () => jump('info') }
    ]
  },
  {
    title: '信息',
    icon: 'TakeawayBox',
    items: [
      { label: '任务列表', icon: 'Document', action: () => jump('tasks') },
      { label: '预设管理', icon: 'CopyDocument', action: () => jump('preset') },
      { label: '日志', icon: 'TakeawayBox', action: () => jump('log') },
      { label: '文件服务', icon: 'FolderOpened', action: () => jump('file') },
      { label: '电源管理', icon: 'Setting', action: () => jump('power') }
    ]
  }
]
</script>

<style scoped>
.welcome {
  max-width: 960px;
  margin: 0 auto;
  padding: 32px 16px;
}

.hero {
  text-align: center;
  margin-bottom: 40px;
}

.hero-icon {
  margin-bottom: 16px;
  color: var(--el-color-primary);
}

.hero-title {
  font-size: 28px;
  margin: 0 0 8px 0;
  color: var(--el-text-color-primary);
}

.hero-desc {
  font-size: 14px;
  color: var(--el-text-color-secondary);
  margin: 0;
}

.card-row {
  margin-bottom: 32px;
}

.group-card {
  margin-bottom: 16px;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
}

.card-item {
  padding: 4px 0;
}

.card-item .el-button {
  padding-left: 0;
}

.footer-link {
  text-align: center;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-light);
}
</style>
