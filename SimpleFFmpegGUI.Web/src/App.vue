<template>
  <el-container style="height: 100%">
    <el-header class="app-header">
      <div class="header-inner">
        <div class="header-left">
          <el-icon :size="24" color="var(--el-color-primary)"><VideoCamera /></el-icon>
          <h2 class="header-title">远程 FFmpeg 工具箱</h2>
        </div>
        <div class="header-right">
          <a v-if="netError" class="header-error">获取状态失败</a>
          <el-button text v-show="logged" @click="logout">已登录</el-button>
          <el-dropdown @command="setTheme" size="small">
            <el-button text size="small">
              <el-icon><Monitor v-if="themeMode === 'auto'" /><Sunny v-else-if="themeMode === 'light'" /><Moon v-else /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="auto" :class="{ active: themeMode === 'auto' }">
                  <el-icon><Monitor /></el-icon> 跟随系统
                </el-dropdown-item>
                <el-dropdown-item command="light" :class="{ active: themeMode === 'light' }">
                  <el-icon><Sunny /></el-icon> 亮色
                </el-dropdown-item>
                <el-dropdown-item command="dark" :class="{ active: themeMode === 'dark' }">
                  <el-icon><Moon /></el-icon> 暗色
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </el-header>
    <el-container class="center" style="overflow: auto">
      <el-aside class="app-aside" :class="{ collapsed: menuCollapse }" v-if="route.path != '/login'">
        <el-menu router :default-active="route.path" :collapse="menuCollapse" :collapse-transition="false">
          <el-menu-item index="/">
            <el-icon><HomeFilled /></el-icon>
            <template #title>欢迎</template>
          </el-menu-item>
          <el-sub-menu index="new">
            <template #title>
              <el-icon><DocumentAdd /></el-icon>
              <span>新建任务</span>
            </template>
            <el-menu-item
              v-for="type in types"
              :key="type.Name"
              :index="'/add/' + type.Route"
            >
              <el-icon><CirclePlus /></el-icon>
              <span>{{ type.Description }}</span>
            </el-menu-item>
          </el-sub-menu>
          <el-menu-item
            v-for="value in menus"
            :key="value[0]"
            :index="value[0]"
          >
            <el-icon><component :is="value[1]" /></el-icon>
            <template #title>{{ value[2] }}</template>
          </el-menu-item>
        </el-menu>
        <div class="menu-toggle" @click="menuCollapse = !menuCollapse">
          <el-icon :size="18">
            <Fold v-if="!menuCollapse" />
            <Expand v-else />
          </el-icon>
          <span v-show="!menuCollapse" class="toggle-label">折叠菜单</span>
        </div>
      </el-aside>
      <el-main>
        <router-view :status="status" @statusChanged="delayGetStatus" />
      </el-main>
    </el-container>
    <el-footer class="footer" style="height: auto; z-index: 1000">
      <StatusBar :status="status" :window-width="windowWidth" />
    </el-footer>
  </el-container>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessageBox } from 'element-plus'
import {
  Fold, Expand, HomeFilled, DocumentAdd, CirclePlus, VideoCamera,
  Search, Document, CopyDocument, FolderOpened, Setting, TakeawayBox,
  Monitor, Sunny, Moon
} from '@element-plus/icons-vue'
import Cookies from 'js-cookie'
import { jump, showError, TaskType, loadDirs } from './common'
import * as net from './net'
import StatusBar from './components/StatusBar.vue'

const route = useRoute()

const activeMenu = ref('/')
const menus = [
  ['/info', 'Search', '媒体信息查询'],
  ['/tasks', 'Document', '任务列表'],
  ['/preset', 'CopyDocument', '预设'],
  ['/file', 'FolderOpened', '文件服务'],
  ['/power', 'Setting', '电源管理'],
  ['/log', 'TakeawayBox', '日志']
]
const types = TaskType.Types
const status = ref<any>(null)
const netError = ref(false)
const menuCollapse = ref(false)
const windowWidth = ref(0)
const logged = ref(false)
const themeMode = ref(localStorage.getItem('theme') || 'auto')

function applyTheme(mode: string) {
  if (mode === 'dark') {
    document.documentElement.classList.add('dark')
  } else if (mode === 'light') {
    document.documentElement.classList.remove('dark')
  } else {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    document.documentElement.classList.toggle('dark', prefersDark)
  }
}

function setTheme(mode: string) {
  themeMode.value = mode
  localStorage.setItem('theme', mode)
  applyTheme(mode)
}

applyTheme(themeMode.value)

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
  if (themeMode.value === 'auto') {
    applyTheme('auto')
  }
})

watch(
  () => route.path,
  (path) => { activeMenu.value = path }
)

onMounted(() => {
  nextTick(() => {
    resizeMenu()
    setInterval(getStatus, 2000)
  })
})

net.getNeedToken().then((r) => {
  if (r.data == true) {
    if (Cookies.get('token')) {
      net.getCheckToken(Cookies.get('token') as string).then((r) => {
        if (!r.data) {
          jump('login')
        } else {
          net.setHeader()
          logged.value = true
        }
      })
    } else {
      jump('login')
    }
  }
})
net.setHeader()
loadDirs()
getStatus()
window.addEventListener('resize', resizeMenu)

function resizeMenu() {
  windowWidth.value = window.innerWidth
  menuCollapse.value = window.innerWidth < 500
}

function delayGetStatus() {
  setTimeout(getStatus, 500)
}

function logout() {
  ElMessageBox.confirm('是否注销？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    Cookies.remove('token')
    location.reload()
  })
}

function getStatus() {
  net.getQueueStatus()
    .then((response) => {
      netError.value = false
      status.value = response.data
      nextTick(resizeMenu)
    })
    .catch(() => (netError.value = true))
}
</script>

<style scoped>
.app-header {
  height: 52px !important;
  padding: 0 24px !important;
  background: #fff;
  border-bottom: 1px solid var(--el-border-color-light, #e4e7ed);
  box-shadow: 0 1px 4px rgba(0,0,0,0.04);
}
html.dark .app-header {
  background: #1a1a2e;
  border-bottom-color: #2a2a3e;
}
.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 100%;
}
.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}
.header-title {
  font-size: 18px;
  font-weight: 600;
  margin: 0;
  white-space: nowrap;
  color: var(--el-text-color-primary);
}
.header-right {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-shrink: 0;
}
.header-error {
  color: #f56c6c;
  font-size: 12px;
}
.el-dropdown-menu__item.active {
  color: var(--el-color-primary);
  font-weight: 600;
}

/* 侧边栏动画 */
.app-aside {
  width: 200px;
  display: flex;
  flex-direction: column;
  transition: width 0.25s ease;
  overflow: hidden;
  border-right: 1px solid var(--el-border-color-light, #e4e7ed);
}
.app-aside.collapsed {
  width: 64px;
}
.app-aside .el-menu {
  border-right: none;
  flex: 1;
}

/* 菜单折叠过渡：文字随着宽度渐变自然隐现 */
.app-aside {
  overflow: hidden;
}
.app-aside.collapsed :deep(.el-menu-item span),
.app-aside.collapsed :deep(.el-sub-menu__title span) {
  display: inline !important;
  opacity: 0;
  transition: opacity 0.15s ease;
}
.app-aside :deep(.el-menu-item span),
.app-aside :deep(.el-sub-menu__title span) {
  display: inline !important;
  opacity: 1;
  transition: opacity 0.25s ease 0.05s;
}

/* 折叠按钮 - 底部 */
.menu-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  cursor: pointer;
  color: #909399;
  border-top: 1px solid var(--el-border-color-light, #e4e7ed);
  transition: color 0.2s, background 0.2s;
  flex-shrink: 0;
}
.app-aside.collapsed .menu-toggle {
  justify-content: center;
  padding: 12px 0;
}
.menu-toggle:hover {
  color: #409eff;
  background: var(--el-menu-hover-bg-color, #ecf5ff);
}
.toggle-label {
  font-size: 13px;
  white-space: nowrap;
}
</style>
