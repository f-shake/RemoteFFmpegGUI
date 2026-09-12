<template>
  <el-container style="height: 100%">
    <!-- 顶栏 -->
    <el-header class="app-header">
      <div class="header-inner">
        <div class="header-left">
          <div class="brand">
            <el-icon :size="26" class="brand-icon"><VideoCamera /></el-icon>
            <h1 class="brand-title">远程 FFmpeg 工具箱</h1>
          </div>
        </div>
        <div class="header-right">
          <!-- 实时连接指示：连接状态圆点 + 悬停文字（原来「获取状态失败」的位置） -->
          <el-tooltip :content="connectionText" placement="bottom">
            <span class="conn-dot" :class="'conn-' + connection"></span>
          </el-tooltip>
          <el-button text class="login-btn" v-show="logged" @click="logout">
            <el-icon><UserFilled /></el-icon>
            <span>已登录</span>
          </el-button>
          <el-dropdown @command="setTheme" size="small" trigger="click">
            <el-button text class="theme-btn">
              <el-icon :size="18"><Monitor v-if="themeMode === 'auto'" /><Sunny v-else-if="themeMode === 'light'" /><Moon v-else /></el-icon>
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

    <!-- 主体 -->
    <el-container class="main-container">
      <!-- 侧栏 -->
      <el-aside class="app-aside" :class="{ collapsed: menuCollapse }" v-if="route.path != '/login'">
        <div class="aside-inner">
          <el-menu router :default-active="activeMenu" :collapse="menuCollapse" :collapse-transition="false">
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
        </div>
        <div class="menu-toggle" @click="menuCollapse = !menuCollapse">
          <el-icon :size="16">
            <Fold v-if="!menuCollapse" />
            <Expand v-else />
          </el-icon>
          <span v-show="!menuCollapse" class="toggle-label">折叠菜单</span>
        </div>
      </el-aside>

      <!-- 内容区 -->
      <el-main class="app-main">
        <router-view v-slot="{ Component }">
          <transition name="page-fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </el-main>
    </el-container>

    <!-- 底部状态栏 -->
    <el-footer class="app-footer" v-if="status != null && status.isProcessing">
      <StatusBar />
    </el-footer>
  </el-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, computed } from 'vue'
import { useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { ElMessageBox } from 'element-plus'
import {
  Fold, Expand, HomeFilled, DocumentAdd, CirclePlus, VideoCamera,
  Monitor, Sunny, Moon, UserFilled
} from '@element-plus/icons-vue'
import Cookies from 'js-cookie'
import { getBasePath } from '@/config'
import { jump, loadDirs } from '@/utils/navigation'
import { TaskType } from '@/models/TaskType'
import { useQueueStore } from '@/stores/queue'
import { useUiStore } from '@/stores/ui'
import * as net from './api'
import StatusBar from './components/StatusBar.vue'

const route = useRoute()

const menus = [
  ['/info', 'Search', '媒体信息查询'],
  ['/tasks', 'Document', '任务列表'],
  ['/preset', 'CopyDocument', '预设'],
  ['/file', 'FolderOpened', '文件服务'],
  ['/power', 'Setting', '电源管理'],
  ['/log', 'TakeawayBox', '日志']
]
const types = TaskType.NavTypes
// 队列状态（含"取状态失败"标记）与 UI 状态（主题、侧栏折叠、窗口宽度）都在 pinia 里：
// 跨组件共享的状态只有一个持有者，任务页与底部状态栏不再各自轮询队列状态
const queue = useQueueStore()
const { status, connection } = storeToRefs(queue)
// 圆点的悬停文案：绿=推送、黄=重试中、蓝=轮询兜底、红=连不上
const connectionText = computed(() => {
  switch (connection.value) {
    case 'ws': return '实时推送已连接'
    case 'retrying': return '实时已断开，正在重试'
    case 'http': return '轮询兜底中（实时已断开，刷新页面重试）'
    case 'failed': return '获取状态失败'
    default: return '正在连接实时推送…'
  }
})
const ui = useUiStore()
const { themeMode, menuCollapse } = storeToRefs(ui)
const setTheme = ui.setTheme
// 菜单高亮项：进入 /add/* 且折叠(移动端)时映射到顶层 '/'，避免 Element Plus 因 active 项落在
// "新建任务"子菜单内而自动弹出该弹层（点子项后菜单应消失且不再出现）；桌面端保留高亮展开
const activeMenu = computed(() =>
  menuCollapse.value && route.path.startsWith('/add/') ? '/' : route.path
)
const logged = ref(false)

// 主题的读取/应用、以及"跟随系统"变化的监听都在 useUiStore 里（见该文件注释）。
// 实时通道由 queue store 里的 utils/realtime 承接：服务端推来的状态直接写进 store，
// 顶栏圆点、底部状态栏、任务页都读同一份；连不上时它自己降级为 HTTP 轮询。
// 根组件只挂载一次，这里连接也只建立一次（store 里做了幂等）
onMounted(() => {
  queue.startRealtime()
})

onBeforeUnmount(() => {
  queue.stopRealtime()
})

net.getNeedToken().then((r) => {
  if (r.data == true) {
    if (Cookies.get('token')) {
      net.getCheckToken(Cookies.get('token') as string).then((r) => {
        if (!r.data) {
          jump('login')
        } else {
          logged.value = true
        }
      })
    } else {
      jump('login')
    }
  }
})
loadDirs()

function logout() {
  ElMessageBox.confirm('是否注销？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    Cookies.remove('token', { path: getBasePath() })
    // 兼容升级前 cookie 存于根路径(/)的情况，一并清理，避免注销后仍处于"已登录"态
    Cookies.remove('token', { path: '/' })
    location.reload()
  })
}
</script>

<style scoped>
/* ==============================================================
   顶栏
   ============================================================== */
.app-header {
  height: 56px !important;
  padding: 0 24px !important;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-color);
  position: relative;
  z-index: 1001;
  flex-shrink: 0;
}

.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 100%;
}

/* 品牌区域 */
.header-left {
  display: flex;
  align-items: center;
}
.brand {
  display: flex;
  align-items: center;
  gap: 10px;
}
.brand-icon {
  color: var(--el-color-primary);
}
.brand-title {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
  white-space: nowrap;
  color: var(--text-primary);
  letter-spacing: 0.5px;
}

/* 顶栏右侧 */
.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
/* 实时连接圆点：绿=推送已连接、黄闪=断开重试中、蓝=轮询兜底、红=取状态失败、灰=首次连接中 */
.conn-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
  flex-shrink: 0;
}
.conn-connecting {
  background: var(--text-secondary);
}
.conn-ws {
  background: var(--el-color-success);
}
.conn-retrying {
  background: var(--el-color-warning);
  animation: conn-blink 1s step-end infinite;
}
.conn-http {
  background: var(--el-color-primary);
}
.conn-failed {
  background: var(--el-color-danger);
}
@keyframes conn-blink {
  50% { opacity: 0.2; }
}
/* 尊重"减少动态效果"偏好：此时黄色保持常亮，不做闪烁 */
@media (prefers-reduced-motion: reduce) {
  .conn-retrying { animation: none; }
}
.login-btn {
  font-size: 13px;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  gap: 4px;
}
.login-btn:hover {
  color: var(--el-color-primary);
}
.theme-btn {
  color: var(--text-secondary);
  padding: 4px;
}
.theme-btn:hover {
  color: var(--el-color-primary);
}
.el-dropdown-menu__item.active {
  color: var(--el-color-primary);
  font-weight: 600;
}

/* ==============================================================
   主体容器
   ============================================================== */
.main-container {
  overflow: hidden;
  flex: 1;
}

/* ==============================================================
   侧栏
   ============================================================== */
.app-aside {
  width: 200px;
  display: flex;
  flex-direction: column;
  transition: width 0.25s ease;
  border-right: 1px solid var(--border-color);
  background: var(--bg-card);
  flex-shrink: 0;
}
.app-aside.collapsed {
  width: 64px;
}

.aside-inner {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 8px 0;
}
.aside-inner::-webkit-scrollbar {
  width: 3px;
}

.app-aside .el-menu {
  border-right: none;
  background: transparent;
}

/* 菜单项间距和圆角 */
.app-aside :deep(.el-menu-item),
.app-aside :deep(.el-sub-menu__title) {
  height: 42px;
  line-height: 42px;
  margin: 2px 8px;
  border-radius: var(--radius-sm);
  width: auto;
  transition: background var(--transition-fast), color var(--transition-fast);
}
.app-aside :deep(.el-menu-item.is-active) {
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
  font-weight: 600;
}
html.dark .app-aside :deep(.el-menu-item.is-active) {
  background: rgba(255, 255, 255, 0.08);
}
.app-aside :deep(.el-menu-item:hover),
.app-aside :deep(.el-sub-menu__title:hover) {
  background: var(--el-fill-color-light);
}

/* 子菜单内边距 */
.app-aside :deep(.el-menu--inline .el-menu-item) {
  padding-left: 48px !important;
}

/* 折叠时菜单项居中 */
.app-aside.collapsed :deep(.el-menu-item),
.app-aside.collapsed :deep(.el-sub-menu__title) {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 !important;
  width: 100%;
  margin: 0;
}
.app-aside.collapsed :deep(.el-menu-item .el-icon),
.app-aside.collapsed :deep(.el-sub-menu__title .el-icon) {
  margin: 0 !important;
  width: 24px;
}
.app-aside.collapsed :deep(.el-sub-menu__title span),
.app-aside.collapsed :deep(.el-sub-menu__title .el-sub-menu__icon-arrow) {
  display: none;
}

/* 折叠按钮 */
.menu-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  cursor: pointer;
  color: var(--text-secondary);
  border-top: 1px solid var(--border-color);
  transition: color 0.2s, background 0.2s;
  flex-shrink: 0;
  font-size: 13px;
}
.app-aside.collapsed .menu-toggle {
  justify-content: center;
  padding: 10px 0;
}
.menu-toggle:hover {
  color: var(--el-color-primary);
  background: var(--el-fill-color-light);
}
.toggle-label {
  white-space: nowrap;
}

/* ==============================================================
   内容区
   ============================================================== */
.app-main {
  padding: 0;
  background: var(--bg-page);
  overflow-y: auto;
  overflow-x: hidden;
}

/* ==============================================================
   底部状态栏
   ============================================================== */
.app-footer {
  height: auto !important;
  flex-shrink: 0;
  z-index: 1000;
  padding: 0 !important;
}

/* ==============================================================
   页面过渡动画
   ============================================================== */
.page-fade-enter-active {
  transition: opacity var(--transition-normal), transform var(--transition-normal);
}
.page-fade-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.page-fade-enter-from {
  opacity: 0;
  transform: translateY(8px);
}
.page-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
