
<template>
  <el-select
    filterable
    v-model="selectedFile"
    @change="selectChanged"
    :placeholder="
      files == null ? '请选择文件' : '请选择文件（共' + files.length + '个）'
    "
  >
    <el-option v-for="item in files" :key="item.path" :label="item.relativePath" :value="item.relativePath">
    </el-option>
  </el-select>
</template>
<script >
import Vue from "vue";
import Cookies from "js-cookie";
import * as net from "../net";
import { showError, jump, formatDateTime } from "../common";
export default Vue.component("file-select", {
  data() {
    return {
      files: null,
      selectedFile: this.file,
    };
  },
  props: ["file"],
  computed: {},
  watch: {
    file() {
      this.selectedFile = this.file;
    },
  },
  methods: {
    selectChanged(e) {
      this.$emit("update:file", e);
    },
  },
  components: {},
  mounted: function () {
    this.$nextTick(function () {
      net
        .getMediaNames()
        .then((response) => {
          this.files = response.data;
        })
        .catch(showError);
    });
  },
});
</script>
<style scoped>
.el-select-dropdown__item {
  max-width: 100%;
  height: auto;
  min-height: 34px;
  white-space: break-spaces;
  padding-left: 28px;
  padding-right: 28px;
}

</style>
<style>

.el-collapse-item__header{
  white-space: nowrap;
}
</style>