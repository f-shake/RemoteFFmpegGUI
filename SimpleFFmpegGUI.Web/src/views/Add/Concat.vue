<template>
  <div>
    <el-form label-width="100px">
      <h2>输入和输出</h2>
      <FileIOGroup
        :inputs="files"
        :output="output" :min="2"
        ref="io" :showClip="false"
        :singleOutput="true"
      ></FileIOGroup>
      <h2>参数</h2>
    </el-form>

    <code-arguments ref="args" :type="4" />
    <add-to-task-buttons :addFunc="addTask"></add-to-task-buttons>
  </div>
</template>
<script lang="ts">
import Vue from "vue";
import Cookies from "js-cookie";
import { showError, jump, showSuccess, loadArgs } from "../../common";
import * as net from "../../net";
import CodeArguments from "@/components/CodeArguments.vue";
import PresetSelect from "@/components/PresetSelect.vue";
import AddToTaskButtons from "@/components/AddToTaskButtons.vue";
import FileIOGroup from "@/components/FileIOGroup.vue";
export default Vue.extend({
  name: "Home",
  data() {
    return {
      activeInput: [0],
      files: [
        {
          filePath: "",
          from: null,
          to: null,
          duration: null,
        },
      ],
      output: "",
    };
  },
  created() {
    return;
  },
  computed: {},
  methods: {
    getNewFile() {
      return { filePath: "", from: null, to: null, duration: null };
    },
    addTask(start: boolean) {    
        this.files = (this.$refs.io as any).getArgs();
      this.output = (this.$refs.io as any).outputFile;
      if (this.files.filter((p) => p.filePath != "").length == 0) {
        showError("请选择输入文件");
        return;
      }
      let args = (this.$refs.args as any).getArgs();
      if (args == null) {
        return;
      }

      net
        .postAddConcatTask({
          inputs: this.files,
          output: this.output,
          parameter: args,
          start: start,
        })
        .then((response) => {
          this.files = [];
          this.files.push(this.getNewFile());
          this.files.push(this.getNewFile());
          this.output = "";
          showSuccess("已加入队列");
        })
        .catch(showError);
    },
  },
  components: { CodeArguments, AddToTaskButtons, FileIOGroup },
  mounted: function () {
    this.$nextTick(function () {
      const inputOutput = loadArgs(this.$refs.args);
      if (inputOutput.inputs) {
        this.files = inputOutput.inputs;
      }
      if (inputOutput.output) {
        this.output = inputOutput.output;
      }
    });
  },
});
</script>
<style >
.el-collapse-item__header {
  height: 32px !important;
}
</style>
