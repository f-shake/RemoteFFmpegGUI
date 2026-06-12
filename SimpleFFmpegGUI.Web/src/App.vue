<template>
  <el-container style="height: 100%">
    <el-header class="header one-line" style="height: auto; padding-left: 28px; padding-top: 8px">
      <div>
        <h2 style="display: inline-block; margin-top: 8px; margin-bottom: 8px">
          远程FFmpeg工具箱
        </h2>
        <a
          v-if="netError"
          style="color: red; display: inline-block; float: right; margin-top: 18px; font-size: 12px"
        >获取状态失败</a>
        <el-button
          style="display: inline-block; float: right; margin-top: 12px"
          text
          v-show="logged"
          @click="logout"
        >已登录</el-button>
        <el-switch
          style="display: inline-block; float: right; margin-top: 14px; margin-right: 12px"
          v-model="isDark"
          active-icon="Moon"
          inactive-icon="Sunny"
          @change="toggleDark"
          size="small"
        />
      </div>
    </el-header>
    <el-container
      class="center"
      :style="{
        height: 'calc(100% - ' + (headerHeight + footerHeight) + 'px)',
        marginBottom: status == null || status.isProcessing == false ? '0' : '12px',
        paddingBottom: status == null || status.isProcessing == false ? '0' : '8px'
      }"
    >
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
  Fold, Expand, HomeFilled, DocumentAdd, CirclePlus,
  Search, Document, CopyDocument, FolderOpened, Setting, TakeawayBox
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
const headerHeight = ref(0)
const footerHeight = ref(0)
const logged = ref(false)
const isDark = ref(localStorage.getItem('theme') === 'dark')

function toggleDark(val: boolean) {
  document.documentElement.classList.toggle('dark', val)
  localStorage.setItem('theme', val ? 'dark' : 'light')
}

if (isDark.value) {
  document.documentElement.classList.add('dark')
}

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
  const header = document.getElementsByClassName('header')[0]
  const footer = document.getElementsByClassName('footer')[0]
  headerHeight.value = header?.scrollHeight ?? 0
  footerHeight.value = footer?.scrollHeight ?? 0
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
.header {
  margin-left: -12px;
  margin-right: -12px;
  margin-top: -12px;
  background: #ebeef5;
  color: #606266;
}
html.dark .header {
  background: #1a1a2e;
  color: #e0e0e0;
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
