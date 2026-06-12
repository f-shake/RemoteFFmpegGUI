<template>
  <div>
    <code-arguments ref="args" :type="3" />
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
import TimeInput from "@/components/TimeInput.vue";
export default Vue.extend({
  name: "Home",
  data() {
    return {};
  },
  computed: {},
  methods: {
    addTask(start: boolean) {
      let args = (this.$refs.args as any).getArgs();

      net
        .postAddCustomTask({
          input: null,
          output: null,
          parameter: args,
          start: start,
        })
        .then((response) => {
          showSuccess("已加入队列");
        })
        .catch(showError);
    },
  },
  components: { CodeArguments, AddToTaskButtons },
  mounted: function () {
    this.$nextTick(function () {
      loadArgs(this.$refs.args);
    });
  },
});
</script>
<style scoped>
</style>
