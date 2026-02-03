<template>
  <div>
    <div>
      <span style="float: right">
        <a
          class="right12"
          v-if="isProcessing == false && hasSchedule"
        >已计划开始时间</a>
        <el-date-picker
          v-if="isProcessing == false"
          class="right12"
          v-model="scheduleTime"
          value-format="yyyy-MM-dd[T]HH:mm:ss[Z]"
          placeholder="计划开始时间"
          type="datetime"
          :disabled="hasSchedule"
        ></el-date-picker>

        <el-button
          v-if="isProcessing == false && hasSchedule == false"
          :disabled="scheduleTime == null || scheduleTime == ''"
          type="secondary"
          @click="schedule"
          class="right24"
        >设置计划</el-button>

        <el-button
          v-if="isProcessing == false && hasSchedule"
          type="secondary"
          @click="cancelSchedule"
          class="right24"
        >取消计划</el-button>

        <el-button
          v-if="isProcessing == false"
          type="primary"
          @click="start"
        >开始队列</el-button>
        <el-popconfirm
          v-if="isProcessing"
          title="真的要取消任务吗？"
          @confirm="cancel"
          class="right12"
        >
          <el-button
            type="danger"
            slot="reference"
          >停止</el-button></el-popconfirm>
        <el-button
          class="right12"
          v-if="isProcessing && isPaused == false"
          type="warning"
          @click="pause"
        >暂停</el-button>
        <el-button
          class="right12"
          v-if="isProcessing && isPaused"
          type="warn"
          @click="resume"
        >继续</el-button>
      </span>
      <span style="float: left">

        <el-button
          v-if="selection.length > 0"
          type="danger"
          class="right12"
          @click="cancelTasks"
        >取消</el-button>
        <el-button
          v-if="selection.length > 0"
          class="right12"
          @click="resetTasks"
        >重置</el-button>
      </span>
    </div>
    <el-table
      ref="table"
      :data="list"
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="expand">
        <template slot-scope="props">
          <el-form
            label-position="left"
            label-width="120px"
            class="pre-wrap"
          >
            <el-form-item label="输入">
              <div
                v-for="file in props.row.inputs"
                :key="file.filePath"
              >
                <a class="right24">
                  {{ file.filePath }}
                  <br v-if="file.image2" />
                  {{ file.image2 ? "图像序列，帧率为" + file.framerate : "" }}
                  <br v-if="file.extra" />
                  {{ file.extra ? "额外参数：" + file.extra : "" }}
                </a>
                <a
                  v-if="file.from"
                  class="right12"
                > 开始：{{ file.from }}s</a>
                <a
                  v-if="file.to"
                  class="right12"
                > 结束：{{ file.to }}s</a>
                <a
                  v-if="file.duration"
                  class="right12"
                >
                  经过：{{ file.duration }}s</a>
              </div>
            </el-form-item>
            <el-form-item label="输出">{{ props.row.output }} </el-form-item>
            <el-form-item label="创建时间">{{ props.row.createTime }}
            </el-form-item>
            <el-form-item label="开始时间">{{ props.row.startTime }}
            </el-form-item>
            <el-form-item label="结束时间">{{ props.row.finishTime }}
            </el-form-item>
            <el-form-item label="FFmpeg参数">{{ props.row.fFmpegArguments }}
            </el-form-item>
            <el-form-item
              label="信息"
              class="s"
            >{{ props.row.message }}
            </el-form-item>
            <el-form-item label="参数">
              <code-arguments-description
                :type="props.row.type"
                :args="props.row.arguments"
              ></code-arguments-description>
            </el-form-item>
          </el-form>
        </template>
      </el-table-column>
      <el-table-column
        type="selection"
        width="55"
      />
      <el-table-column
        prop="typeText"
        label="类型"
        width="90"
      />
      <el-table-column
        label="状态"
        width="80"
      >
        <template slot-scope="scope">
          <span
            style=""
            v-if="scope.row.status == 1"
          >待处理</span>
          <span
            style="color: orange; font-weight: bold"
            v-if="scope.row.status == 2"
          >进行中</span>
          <span
            style="color: green"
            v-if="scope.row.status == 3"
          >完成</span>
          <span
            style="color: red"
            v-if="scope.row.status == 4"
          >错误</span>
          <span
            style="color: gray"
            v-if="scope.row.status == 5"
          >取消</span>
        </template></el-table-column>
      <el-table-column
        prop="inputText"
        label="输入"
        min-width="360"
      />

      <el-table-column
        label="操作"
        width="180"
      >
        <template slot-scope="scope">
          <el-button
            @click="resetTask(scope.row)"
            type="text"
            size="small"
            :disabled="scope.row.status == 1 || scope.row.status == 2"
          >重置</el-button>
          <el-popconfirm
            v-if="scope.row.status == 2"
            title="真的要取消任务吗？任务会终止"
            style="margin-left: 10px; margin-right: 10px"
            @confirm="cancelTask(scope.row)"
          >
            <el-button
              slot="reference"
              type="text"
              size="small"
              :disabled="
                scope.row.status == 3 ||
                scope.row.status == 4 ||
                scope.row.status == 5
              "
            >取消</el-button></el-popconfirm>
          <el-button
            v-else
            slot="reference"
            type="text"
            size="small"
            :disabled="
              scope.row.status == 3 ||
              scope.row.status == 4 ||
              scope.row.status == 5
            "
            @click="cancelTask(scope.row)"
          >取消</el-button>

          <el-button
            slot="reference"
            type="text"
            size="small"
            @click="remake(scope.row)"
          >重制</el-button>

          <el-button
            slot="reference"
            type="text"
            size="small"
            @click="jump('log?id=' + scope.row.id)"
          >日志</el-button>
        </template>
      </el-table-column>

      <el-table-column align="right">
        <template slot="header">
          <el-button
            type="text"
            @click="fillData()"
          >刷新</el-button>
        </template>
      </el-table-column>
    </el-table>
    <div>
      <div class="top12">
        <el-pagination
          style="float: left"
          @size-change="fillData"
          @current-change="fillData"
          layout="sizes,prev, pager, next"
          :page-sizes="[10, 20, 50, 100]"
          :page-size.sync="countPerPage"
          :current-page.sync="page"
          :total="totalCount"
        >
        </el-pagination>
        <el-radio-group
          v-model="statusFilter"
          size="mini"
          @change="fillData"
          style="float: right"
        >
          <el-radio-button :label="null"><b>全部</b></el-radio-button>
          <el-radio-button :label="1">排队中</el-radio-button>
          <el-radio-button :label="2">进行中</el-radio-button>
          <el-radio-button :label="3">已完成</el-radio-button>
          <el-radio-button :label="4">错误</el-radio-button>
          <el-radio-button :label="5">取消</el-radio-button>
        </el-radio-group>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import Vue from "vue";
import {
  showError,
  showSuccess,
  formatDateTime,
  jump,
  getTaskTypeDescription,
  showLoading,
  closeLoading,
  jumpByArgs,
} from "../common";

import * as net from "../net";
import { Notification, Table } from "element-ui";
import { ElTable } from "element-ui/types/table";
import CodeArgumentsDescription from "@/components/CodeArgumentsDescription.vue";
export default Vue.extend({
  name: "Home",
  data() {
    return {
      list: [],
      isProcessing: false,
      isPaused: false,
      totalCount: 0,
      selection: [],
      taskID: 0,
      pageCount: 1,
      page: 1,
      countPerPage: 100,
      statusFilter: null,
      scheduleTime: "",
      hasSchedule: false,
    };
  },
  props: ["status"],
  watch: {
    status(value) {
      if (
        this.isProcessing != value.isProcessing ||
        this.isPaused != value.isPaused ||
        this.taskID != (value.task == null ? 0 : value.task.id)
      ) {
        this.fillData();
      }
      this.isProcessing = value.isProcessing;
      this.isPaused = value.isPaused;
      this.taskID = value.task == null ? 0 : value.task.id;
    },
  },
  methods: {
    jump,
    schedule(item: any) {
      if (this.scheduleTime == null || this.scheduleTime == "") {
        showError("请选择计划时间");
        return;
      }
      
      net
        .postSchedule(this.scheduleTime)
        .then((r) => {
          showSuccess("设置成功");
          this.hasSchedule = true;
        })
        .catch(showError);
    },
    cancelSchedule(item: any) {
      net
        .postCancelSchedule()
        .then((r) => {
          showSuccess("取消成功");
          this.hasSchedule = false;
        })
        .catch(showError);
    },
    remake(item: any) {
      jumpByArgs(item.arguments, item.inputs, item.output, item.type);
    },
    getSelectionIds(): number[] {
      return this.toIdList(this.selection as []);
    },
    toIdList(items: []): number[] {
      let list: number[] = [];
      items.forEach((item: any) => {
        list.push(item.id);
      });
      return list;
    },
    handleSelectionChange(val: any) {
      this.selection = val;
    },
    resetTask(item: any) {
      net
        .postResetTask(item.id)
        .then((r) => {
          this.fillData();
        })
        .catch(showError);
    },
    resetTasks() {
      net
        .postResetTasks(this.getSelectionIds())
        .then((r) => {
          this.fillData();
        })
        .catch(showError);
    },
    deleteTasks() {
      net
        .postDeleteTasks(this.getSelectionIds())
        .then((r) => {
          this.fillData();
        })
        .catch(showError);
    },
    cancelTasks() {
      net
        .postCancelTasks(this.getSelectionIds())
        .then((r) => {
          this.fillData();
        })
        .catch(showError);
    },
    cancelTask(item: any) {
      net
        .postCancelTask(item.id)
        .then((r) => {
          this.fillData();
        })
        .catch(showError);
    },
    start() {
      net
        .postStartQueue()
        .then((r) => {
          this.isProcessing = true;
          this.fillData();
          this.$emit("statusChanged");
        })
        .catch(showError);
    },
    pause() {
      net
        .postPauseQueue()
        .then((r) => {
          this.isPaused = true;
          this.$emit("statusChanged");
        })
        .catch(showError);
    },
    resume() {
      net
        .postResumeQueue()
        .then((r) => {
          this.isPaused = false;
          this.$emit("statusChanged");
        })
        .catch(showError);
    },
    cancel() {
      net
        .postCancelQueue()
        .then((r) => {
          this.isProcessing = false;
          this.$emit("statusChanged");
          setTimeout(() => {
            this.fillData();
          }, 500);
        })
        .catch(showError);
    },
    fillData() {
      return net
        .getTaskList(
          this.statusFilter,
          (this.page - 1) * this.countPerPage,
          this.countPerPage
        )
        .then((response) => {
          this.totalCount = response.data.totalCount;
          this.pageCount = Math.ceil(this.totalCount / this.countPerPage);
          response.data.list.forEach((element: any) => {
            element.typeText = getTaskTypeDescription(element.type);
            element.inputText =
              element.inputs == null
                ? "未知"
                : element.inputs.length == 1
                ? element.inputs[0].filePath
                : element.inputs[0].filePath + " 等";
          });
          this.list = response.data.list;
        })
        .catch(showError)
        .finally(closeLoading);
    },
  },
  computed: {},
  mounted: function () {
    showLoading();
    this.$nextTick(function () {
      this.fillData();
      net
        .getQueueScheduleTime()
        .then((r) => {
          const time = r.data;
          console.log(r);
          if (time != null && time != "") {
            this.scheduleTime = time;
            this.hasSchedule = true;
          }
        })
        .catch(showError);
    });
  },
  components: { CodeArgumentsDescription },
});
</script>

<style scoped>
.el-table .cell {
  white-space: pre-line;
  word-wrap: break-word;
}

.cell .el-button {
  margin-right: 6px;
}
</style>
