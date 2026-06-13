<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-brand">
        <el-icon :size="40" class="login-icon"><VideoCameraFilled /></el-icon>
        <h1 class="login-title">远程 FFmpeg 工具箱</h1>
        <p class="login-desc">请输入 Token 进行身份验证</p>
      </div>
      <el-input
        v-model="token"
        placeholder="请输入 Token"
        size="large"
        clearable
        @keyup.enter="login"
      >
        <template #prefix>
          <el-icon><Lock /></el-icon>
        </template>
      </el-input>
      <el-button
        :loading="btnLoading"
        type="primary"
        size="large"
        @click="login"
        class="login-btn"
      >登录</el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import * as net from '../net'
import Cookies from 'js-cookie'
import { showError } from '../common'

const router = useRouter()
const token = ref('')
const btnLoading = ref(false)

function login() {
  btnLoading.value = true
  net.getCheckToken(token.value)
    .then((r) => {
      if (!r.data) {
        showError('Token 错误')
      } else {
        Cookies.set('token', token.value, { expires: 365 })
        net.setHeader()
        router.push({ path: '/' })
      }
    })
    .catch(showError)
    .finally(() => { btnLoading.value = false })
}
</script>

<style scoped>
.login-page {
  min-height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: linear-gradient(135deg, var(--el-color-primary-light-9) 0%, var(--bg-page) 50%, var(--el-color-primary-light-8) 100%);
}
html.dark .login-page {
  background: linear-gradient(135deg, rgba(51, 112, 255, 0.08) 0%, var(--bg-page) 50%, rgba(51, 112, 255, 0.04) 100%);
}

.login-card {
  width: 100%;
  max-width: 400px;
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  padding: 40px 32px 32px;
  box-shadow: var(--shadow-lg);
  display: flex;
  flex-direction: column;
  gap: 20px;
  border: 1px solid var(--border-color);
}

.login-brand {
  text-align: center;
  margin-bottom: 8px;
}
.login-icon {
  color: var(--el-color-primary);
  margin-bottom: 12px;
}
.login-title {
  font-size: 22px;
  font-weight: 700;
  margin: 0 0 6px;
  color: var(--text-primary);
}
.login-desc {
  font-size: 14px;
  color: var(--text-secondary);
  margin: 0;
}

.login-btn {
  width: 100%;
  border-radius: var(--radius-md);
  height: 44px;
  font-size: 15px;
  font-weight: 600;
}
</style>
