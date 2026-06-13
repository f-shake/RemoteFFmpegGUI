import { createRouter, createWebHashHistory } from 'vue-router'
import { TaskType } from '../common'
import Welcome from '../views/Welcome.vue'
import MediaInfo from '../views/MediaInfo.vue'
import Code from '../views/Add/Code.vue'
import Combine from '../views/Add/Combine.vue'
import Compare from '../views/Add/Compare.vue'
import Concat from '../views/Add/Concat.vue'
import Custom from '../views/Add/Custom.vue'
import Tasks from '../views/Tasks.vue'
import Files from '../views/Files.vue'
import Presets from '../views/Presets.vue'
import Logs from '../views/Logs.vue'
import Power from '../views/Power.vue'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/', name: 'welcome', component: Welcome },
    { path: '/info', name: 'MediaInfo', component: MediaInfo },
    { path: '/preset', name: 'Preset', component: Presets },
    { path: '/log', name: 'Logs', component: Logs },
    { path: '/file', name: 'File', component: Files },
    { path: '/tasks', name: 'Tasks', component: Tasks },
    { path: '/power', name: 'Power', component: Power },
    { path: '/add/' + TaskType.GetByID(0).Route, name: TaskType.GetByID(0).Name, component: Code },
    { path: '/add/' + TaskType.GetByID(1).Route, name: TaskType.GetByID(1).Name, component: Combine },
    { path: '/add/' + TaskType.GetByID(2).Route, name: TaskType.GetByID(2).Name, component: Compare },
    { path: '/add/' + TaskType.GetByID(99).Route, name: TaskType.GetByID(99).Name, component: Custom },
    { path: '/add/' + TaskType.GetByID(4).Route, name: TaskType.GetByID(4).Name, component: Concat },
    { path: '/password', redirect: '/tasks' },
    {
      path: '/about',
      name: 'About',
      component: () => import('../views/About.vue')
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/Login.vue')
    }
  ]
})

export default router
