import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  // 用相对基址：实际部署前缀由后端托管时注入的 <base href="{PathBase}/"> 决定（见 src/config.ts 的 getBase）。
  // 这样 /ffmpeg 等前缀只出现在后端 appsettings 的 PathBase 一处，改前缀无需改前端/重建。
  base: './',
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    port: 8080
  }
})
