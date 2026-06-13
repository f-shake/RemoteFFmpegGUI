<template>
  <div class="welcome">
    <!-- Hero 区域 -->
    <div class="hero">
      <div class="hero-bg"></div>
      <div class="hero-content">
        <div class="hero-icon-wrap">
          <el-icon :size="56" class="hero-icon"><VideoCameraFilled /></el-icon>
        </div>
        <h1 class="hero-title">远程 FFmpeg 工具箱</h1>
        <p class="hero-desc">基于 FFmpeg 的高性能视频处理工具 — 转码、拼接、合并、媒体信息查询</p>
        <div class="hero-actions">
          <el-button type="primary" size="large" round @click="jump('add/' + types[0].Route)">
            <el-icon><Plus /></el-icon> 新建任务
          </el-button>
          <el-button size="large" plain round @click="jump('tasks')">
            <el-icon><Document /></el-icon> 任务列表
          </el-button>
        </div>
      </div>
    </div>

    <!-- 快捷卡片 -->
    <el-row :gutter="20" class="card-grid">
      <!-- 新建任务组 -->
      <el-col :xs="24" :sm="12" :md="8" v-for="group in cardGroups" :key="group.title">
        <el-card class="group-card" shadow="hover">
          <template #header>
            <div class="card-header">
              <el-icon :size="18" class="card-header-icon"><component :is="group.icon" /></el-icon>
              <span>{{ group.title }}</span>
              <el-icon class="card-header-arrow"><ArrowRight /></el-icon>
            </div>
          </template>
          <div class="card-body">
            <div
              v-for="item in group.items"
              :key="item.label"
              class="card-item"
              @click="item.action"
            >
              <el-icon v-if="item.icon" :size="16" class="card-item-icon"><component :is="item.icon" /></el-icon>
              <span>{{ item.label }}</span>
              <el-icon class="card-item-arrow"><ArrowRight /></el-icon>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 底部 -->
    <div class="footer-area">
      <el-link href="https://github.com/f-shake/RemoteFFmpegGUI" target="_blank" :underline="false" class="footer-link">
        <el-icon><Share /></el-icon> GitHub 仓库
      </el-link>
      <span class="footer-version">v2.0</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { jump } from '@/utils/navigation'
import { TaskType } from '@/models/TaskType'
import {
  VideoCameraFilled, DocumentAdd, Plus, ArrowRight,
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
      { label: '查询媒体信息', icon: 'Search', action: () => jump('info') },
      { label: '预设管理', icon: 'CopyDocument', action: () => jump('preset') },
      { label: '文件服务', icon: 'FolderOpened', action: () => jump('file') },
    ]
  },
  {
    title: '信息',
    icon: 'TakeawayBox',
    items: [
      { label: '任务列表', icon: 'Document', action: () => jump('tasks') },
      { label: '日志', icon: 'TakeawayBox', action: () => jump('log') },
      { label: '电源管理', icon: 'Setting', action: () => jump('power') },
    ]
  }
]
</script>

<style scoped>
.welcome {
  min-height: 100%;
  position: relative;
  background: var(--bg-card);
}

/* ==============================================================
   Hero 区域
   ============================================================== */
.hero {
  position: relative;
  overflow: hidden;
  padding: 60px 24px 48px;
  text-align: center;
}
.hero-bg {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, var(--el-color-primary-light-9) 0%, var(--bg-card) 100%);
  opacity: 0.5;
}
html.dark .hero-bg {
  background: var(--bg-card);
  opacity: 1;
}
html.dark .hero-bg::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 200px;
  background: radial-gradient(ellipse at 50% 0, rgba(255, 255, 255, 0.03) 0%, transparent 70%);
  pointer-events: none;
}
.hero-content {
  position: relative;
  z-index: 1;
}

.hero-icon-wrap {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 88px;
  height: 88px;
  border-radius: 24px;
  background: linear-gradient(135deg, var(--el-color-primary), var(--el-color-primary-light-3));
  color: #fff;
  margin-bottom: 20px;
  box-shadow: 0 8px 32px rgba(51, 112, 255, 0.3);
  transition: transform var(--transition-normal), box-shadow var(--transition-normal);
}
.hero-icon-wrap:hover {
  transform: translateY(-2px) scale(1.03);
  box-shadow: 0 12px 40px rgba(51, 112, 255, 0.4);
}

.hero-title {
  font-size: 30px;
  font-weight: 800;
  margin: 0 0 10px;
  color: var(--text-primary);
  letter-spacing: -0.3px;
}
.hero-desc {
  font-size: 15px;
  color: var(--text-secondary);
  margin: 0 0 28px;
  max-width: 500px;
  margin-left: auto;
  margin-right: auto;
  line-height: var(--lh-relaxed);
}

.hero-actions {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  flex-wrap: wrap;
}

/* ==============================================================
   卡片网格
   ============================================================== */
.card-grid {
  padding: 0 24px 32px;
  max-width: 1100px;
  margin: 0 auto;
}

.group-card {
  margin-bottom: 20px;
  border-radius: var(--radius-lg) !important;
  cursor: default;
  transition: transform var(--transition-normal), box-shadow var(--transition-normal);
}
.group-card:hover {
  transform: translateY(-2px);
}

.card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
  color: var(--text-primary);
}
.card-header-icon {
  color: var(--el-color-primary);
}
.card-header-arrow {
  margin-left: auto;
  color: var(--text-disabled);
  font-size: 14px;
  transition: transform var(--transition-fast);
}
.group-card:hover .card-header-arrow {
  transform: translateX(3px);
  color: var(--el-color-primary);
}

.card-body {
  display: flex;
  flex-direction: column;
}
.card-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 4px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: background var(--transition-fast), padding-left var(--transition-fast);
  color: var(--text-regular);
  font-size: 14px;
}
.card-item:hover {
  background: var(--el-color-primary-light-9);
  padding-left: 8px;
  color: var(--el-color-primary);
}
html.dark .card-item:hover {
  background: rgba(255, 255, 255, 0.06);
}
.card-item-icon {
  color: var(--text-secondary);
  flex-shrink: 0;
}
.card-item:hover .card-item-icon {
  color: var(--el-color-primary);
}
.card-item-arrow {
  margin-left: auto;
  color: transparent;
  font-size: 13px;
  transition: color var(--transition-fast);
}
.card-item:hover .card-item-arrow {
  color: var(--el-color-primary-light-3);
}

.card-item + .card-item {
  border-top: 1px solid var(--border-color-light);
}

/* ==============================================================
   底部
   ============================================================== */
.footer-area {
  text-align: center;
  padding: 24px 16px 32px;
  border-top: 1px solid var(--border-color-light);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  flex-wrap: wrap;
}
.footer-link {
  font-size: 13px;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  gap: 4px;
}
.footer-link:hover {
  color: var(--el-color-primary);
}
.footer-version {
  font-size: 12px;
  color: var(--text-disabled);
  font-family: var(--font-mono);
}

/* ==============================================================
   响应式
   ============================================================== */
@media (max-width: 640px) {
  .hero {
    padding: 40px 16px 32px;
  }
  .hero-title {
    font-size: 24px;
  }
  .hero-desc {
    font-size: 14px;
  }
  .hero-icon-wrap {
    width: 72px;
    height: 72px;
    border-radius: 20px;
  }
  .hero-icon-wrap .el-icon {
    font-size: 40px !important;
  }
  .card-grid {
    padding: 0 12px 24px;
  }
}
</style>
