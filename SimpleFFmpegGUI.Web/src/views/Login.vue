<template>
  <div class="login">
    <h1>登录</h1>
    <div class="box center" style="max-width: 320px">
      <el-input id="name" v-model="token" placeholder="请输入Token">
        <template #prepend>Token</template>
      </el-input>
      <br /><br />
      <el-button :loading="btnLoading" id="login" @click="login" style="width: 100%" type="primary">登录</el-button>
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
        showError('Token错误')
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
.box { width: 100%; }
.box .el-row { width: 100%; }
.center { display: table; margin: 0 auto; border: 0; }
</style>
