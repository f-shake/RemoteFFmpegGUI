<template>
  <div id="app">
    <el-container>
      <el-header
        class="header one-line"
        style="height: auto; padding-left: 28px; padding-top: 8px"
      >
        <div>
          <h2
            style="display: inline-block; margin-top: 8px; margin-bottom: 8px"
          >
            远程FFmpeg工具箱
          </h2>
          <a
            v-if="netError"
            style="
              color: red;
              display: inline-block;
              float: right;
              margin-top: 18px;
              font-size: 12px;
            "
            >获取状态失败</a
          >
          <el-button
            style="display: inline-block; float: right; margin-top: 12px"
            type="text"
            v-show="logged"
            @click="logout"
            >已登录</el-button
          >
        </div>
      </el-header>
      <el-container
        class="center"
        :style="{
          height: 'calc(100% - ' + (headerHeight + footerHeight) + 'px)',
          marginBottom:
            status == null || status.isProcessing == false ? '0' : '12px',
          paddingBottom:
            status == null || status.isProcessing == false ? '0' : '8px',
        }"
      >
        <el-aside
          :width="(menuCollapse ? 68 : 200) + 'px'"
          v-if="this.$route.path != '/login'"
        >
          <el-button
            @click="changeMenuSize"
            :icon="menuCollapse ? 'el-icon-s-unfold' : 'el-icon-s-fold'"
            style="color: #909399; font-size: 24px; font-weight: 400; border: 0"
          ></el-button>
          <el-menu router :default-active="activeMenu" :collapse="menuCollapse">
            <el-menu-item index="/">
              <i class="el-icon-s-home"></i>
              <template #title>欢迎</template>
            </el-menu-item>
            <el-submenu index="new">
              <template #title>
                <i class="el-icon-document-add"></i>
                <span>新建任务</span>
              </template>
              <el-menu-item
                v-for="type in types"
                :key="type.Name"
                :index="'/add/' + type.Route"
              >
                <i class="el-icon-circle-plus-outline"></i>
                <span>{{ type.Description }}</span></el-menu-item
              >
            </el-submenu>
            <el-menu-item
              v-for="value in menus"
              :key="value[0]"
              :index="value[0]"
            >
              <i :class="value[1]"></i>
              <template #title>{{ value[2] }}</template>
            </el-menu-item>
          </el-menu></el-aside
        >

        <el-main>
          <router-view
            :status="status"
            @statusChanged="delayGetStatus"
          ></router-view>
        </el-main>
      </el-container>
      <el-footer class="footer" style="height: auto; z-index: 1000">
        <status-bar :status="status" :window-width="windowWidth"></status-bar>
      </el-footer>
    </el-container>
  </div>
</template>
<script lang="ts">
import Vue from "vue";
import Cookies from "js-cookie";
import {
  jump,
  showError,
  formatDateTime,
  formatDoubleTimeSpan,
  TaskType,
  loadDirs,
} from "./common";
import * as net from "./net";
import StatusBar from "./components/StatusBar.vue";

export default Vue.extend({
  name: "App",
  data: function () {
    return {
      activeMenu:"/",
      menus: [
        ["/info", "el-icon-search", "媒体信息查询"],
        ["/tasks", "el-icon-document", "任务列表"],
        ["/preset", "el-icon-document-copy", "预设"],
        ["/file", "el-icon-folder-opened", "文件服务"],
        ["/power", "el-icon-set-up", "电源管理"],
        ["/log", "el-icon-takeaway-box", "日志"],
        // ["/about", "el-icon-info", "关于"],
      ],
      types: TaskType.Types,
      showHeader: true,
      status: null,
      netError: false,
      menuCollapse: false,
      windowWidth: 0,
      headerHeight: 0,
      footerHeight: 0,
      logged: false,
    };
  },
  computed: {},
  mounted: function () {
    this.$nextTick(function () {
      this.resizeMenu();
      setInterval(this.getStatus, 2000);
    });
  },
  created() {
    net.getNeedToken().then((r) => {
      if (r.data == true) {
        if (Cookies.get("token")) {
          net.getCheckToken(Cookies.get("token") as string).then((r) => {
            if (!r.data) {
              //token错误
              jump("login");
            } else {
              net.setHeader();
              this.logged = true;
            }
          });
        } else {
          jump("login");
        }
      }
    });
    net.setHeader();
    loadDirs();
    this.getStatus();
    window.addEventListener("resize", this.resizeMenu);
  },
  watch: {
    $route(to, from) {
      this.activeMenu=to.path;
    },
  },
  methods: {
    jump: jump,
    formatDoubleTimeSpan: formatDoubleTimeSpan,
    formatDateTime: formatDateTime,
    changeMenuSize() {
      this.menuCollapse = !this.menuCollapse;
    },
    resizeMenu() {
      this.headerHeight =
        document.getElementsByClassName("header")[0].scrollHeight;
      this.footerHeight =
        document.getElementsByClassName("footer")[0].scrollHeight;

      this.windowWidth = window.innerWidth;
      this.menuCollapse = window.innerWidth < 500;
    },

    delayGetStatus() {
      setTimeout(this.getStatus, 500);
    },
    logout() {
      this.$confirm("是否注销？", "提示", {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning",
      }).then(() => {
        Cookies.remove("token");
        location.reload();
      });
    },
    getStatus() {
      net
        .getQueueStatus()
        .then((response) => {
          this.netError = false;
          this.status = response.data;
          this.$nextTick(this.resizeMenu);
        })
        .catch((e) => (this.netError = true));
    },
    clickUsername() {
      this.$confirm("是否注销账户？", "提示", {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning",
      }).then(() => {
        Cookies.remove("username");
        Cookies.remove("userID");
        Cookies.remove("token");
        location.reload();
      });
    },
  },
  components: { StatusBar },
});
</script>

<style scoped>
#app {
  height: 100%;
}

.header {
  margin-left: -12px;
  margin-right: -12px;
  margin-top: -12px;
  background: #ebeef5;
  color: #606266;
}
</style>
