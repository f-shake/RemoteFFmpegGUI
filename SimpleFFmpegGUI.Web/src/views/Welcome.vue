<template>
  <div>
    <el-row :gutter="12">
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <div slot="header" class="clearfix">
            <span>新建任务</span>
          </div>
          <div v-for="type in types" :key="type.Name">
            <el-button type="text" @click="jump('add/' + type.Route)">
              {{ type.Description }}</el-button
            >
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <div slot="header" class="clearfix">
            <span>工具</span>
          </div>
          <div>
            <el-button type="text" @click="jump('info')">
              查询媒体信息</el-button
            >
          </div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12">
        <el-card class="bottom12">
          <div slot="header" class="clearfix">
            <span>信息</span>
          </div>
          <div>
            <el-button type="text" @click="jump('tasks')"> 任务列表</el-button>
          </div>
          <div>
            <el-button type="text" @click="jump('preset')"> 预设</el-button>
          </div>
          <div>
            <el-button type="text" @click="jump('log')"> 日志</el-button>
          </div>
        </el-card></el-col
      ></el-row
    >
    <el-link href="https://github.com/f-shake/RemoteFFmpegGUI" target="_blank">项目Github</el-link>
  </div>
</template>
<script lang="ts">
import Vue from "vue";
import { jump, TaskType } from "../common";
export default Vue.extend({
  name: "Home",
  data() {
    return {
      types: TaskType.Types,
    };
  },
  computed: {},
  methods: {
    jump: jump,
  },
  components: {},
  mounted: function () {
    this.$nextTick(function () {
      return;
    });
  },
});
</script>
<style scoped>
</style>