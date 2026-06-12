<template>
  <div>
    <el-form label-width="100px">
      <h2>输入和输出</h2>
      <el-form-item label="视频">
        <file-select
          ref="videoFile"
          :file.sync="video"
          class="right24"
        ></file-select>
      </el-form-item>
      <el-form-item label="音频">
        <file-select
          ref="audioFile"
          :file.sync="audio"
          class="right24"
        ></file-select>
      </el-form-item>

      <el-form-item label="输出">
        <el-input
          placeholder="输出文件名"
          style="width: 300px; display: block"
          v-model="output"
        />
        <a class="gray"
          >输出文件名在处理时会自动重命名为首个不存在重复文件的文件名</a
        >
      </el-form-item>
    </el-form>
    <h2>参数</h2>
    <code-arguments :type="1" ref="args" />

    <add-to-task-buttons :addFunc="addTask"></add-to-task-buttons>
  </div>
</template>
<script lang="ts">
import Vue from "vue";
import Cookies from "js-cookie";
import { showError, jump, showSuccess, loadArgs } from "../../common";
import * as net from "../../net";
import CodeArguments from "@/components/CodeArguments.vue";
import AddToTaskButtons from "@/components/AddToTaskButtons.vue";
export default Vue.extend({
  name: "Home",
  data() {
    return {
      video: "",
      audio: "",
      output: "",
    };
  },
  computed: {},
  watch:{
    video(){
      this.output=this.video;
    }
  },
  methods: {
    jump: jump,

    add() {
      this.addTask(false);
    },
    addAndStart() {
      this.addTask(true);
    },

    addTask(start: boolean) {
      if (this.video == "" || this.audio == "") {
        showError("请选择输入文件");
        return;
      }

      net
        .postAddCombineTask({
          inputs: [{filePath:this.video},{filePath: this.audio}],
          output: this.output,
          parameter: (this.$refs.args as any).getArgs(),
          start: start,
        })
        .then((response) => {
          this.video = "";
          this.audio = "";
          this.output = "";
          showSuccess("已加入队列");
        })
        .catch(showError);
    },
  },
  components: { CodeArguments, AddToTaskButtons },
  mounted: function () {
    this.$nextTick(function () {
      const inputOutput = loadArgs(this.$refs.args);
      
      if (inputOutput.inputs) {
        this.video = inputOutput.inputs[0].filePath;
        this.audio = inputOutput.inputs[1].filePath;
      }
      if (inputOutput.output) {
        this.output = inputOutput.output;
      }
    });
  },
});
</script>
<style scoped>
</style>
