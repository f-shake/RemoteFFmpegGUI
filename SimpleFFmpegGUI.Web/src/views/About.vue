<template>
  <div class="about-page">
    <el-card shadow="never" class="about-card">
      <template #header>
        <div class="about-header">
          <el-icon :size="32" class="about-icon"><VideoCameraFilled /></el-icon>
          <div>
            <h2>远程 FFmpeg 工具箱</h2>
            <p class="about-version">RemoteFFmpegGUI v2.0</p>
          </div>
        </div>
      </template>
      <div class="about-body">
        <p>基于 FFmpeg 的远程视频处理工具，支持转码、拼接、合并、媒体信息查询等功能。</p>
        <el-divider />
        <el-link href="https://github.com/f-shake/RemoteFFmpegGUI" target="_blank" :underline="false" class="about-link">
          <el-icon><Share /></el-icon> GitHub 仓库
        </el-link>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.about-page {
  max-width: 640px;
  margin: 0 auto;
  padding: 32px 16px;
}
.about-card {
  border-radius: var(--radius-lg) !important;
}
.about-header {
  display: flex;
  align-items: center;
  gap: 16px;
}
.about-icon {
  color: var(--el-color-primary);
  flex-shrink: 0;
}
.about-header h2 {
  margin: 0;
  font-size: 18px;
  color: var(--text-primary);
}
.about-version {
  margin: 2px 0 0;
  font-size: 12px;
  color: var(--text-secondary);
  font-family: var(--font-mono);
}
.about-body {
  font-size: 14px;
  color: var(--text-regular);
  line-height: var(--lh-relaxed);
}
.about-link {
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 4px;
}
</style>
