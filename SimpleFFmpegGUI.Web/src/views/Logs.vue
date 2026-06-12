<template>
  <div>
    <div>
      <a class="right24">时间范围：</a>
      <el-date-picker
        @change="fillData"
        v-model="timeRange"
        type="datetimerange"
        range-separator="至"
        start-placeholder="开始日期"
        end-placeholder="结束日期"
        align="right"
      >
      </el-date-picker>
    </div>
    <div class="gray top12" v-if="taskName">任务：{{ taskName }}</div>
    <el-table ref="table" :data="list" size="small">
      <el-table-column type="expand">
        <template slot-scope="props">
          <div class="pre-wrap">
            {{ props.row.message }}
          </div>
        </template>
      </el-table-column>

      <el-table-column prop="timeText" label="时间" width="180" />
      <el-table-column label="类型" width="80">
        <template slot-scope="scope">
          <span v-if="scope.row.type == 'E'" style="color: red">错误</span>
          <span style="color: orange" v-if="scope.row.type == 'W'">警告</span>
          <span v-if="scope.row.type == 'I'">信息</span>
          <span style="color: gray" v-if="scope.row.type == 'O'">输出</span>
        </template></el-table-column
      >
      <el-table-column label="信息" min-width="180">
        <template slot-scope="scope">
          <div class="single-line">{{ scope.row.message }}</div>
        </template>
      </el-table-column>
      <el-table-column align="right">
        <template slot="header">
          <el-button type="text" @click="fillData()">刷新</el-button>
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
          :page-sizes="[10, 20, 50, 100, 200, 500, 1000]"
          :page-size.sync="countPerPage"
          :current-page.sync="page"
          :total="totalCount"
        >
        </el-pagination>
        <el-radio-group
          v-model="typeFilter"
          size="mini"
          @change="fillData"
          style="float: right"
        >
          <el-radio-button :label="null"><b>全部</b></el-radio-button>
          <el-radio-button label="E">错误</el-radio-button>
          <el-radio-button label="W">警告</el-radio-button>
          <el-radio-button label="I">信息</el-radio-button>
          <el-radio-button label="O">输出</el-radio-button>
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
  TaskType,
  displayPath,
} from "../common";

import * as net from "../net";
import { Notification, Table } from "element-ui";
import { ElTable } from "element-ui/types/table";
export default Vue.extend({
  name: "Home",
  data() {
    return {
      list: [],
      totalCount: 0,
      pageCount: 1,
      page: 1,
      countPerPage: 100,
      typeFilter: null,
      timeRange: [],
      taskName: "",
    };
  },
  created() {
    if (this.$route.query.id) {
      net.getTask(Number.parseInt(this.$route.query.id as string)).then((r) => {
        this.taskName = TaskType.GetByID(r.data.type).Description+"（";

        if (r.data.inputs && r.data.inputs.length > 0) {
          this.taskName += displayPath(r.data.inputs[0].filePath);
          if (r.data.inputs.length > 1) {
            this.taskName += " 等";
          }
        } else {
          this.taskName = "未知";
        }
        // this.taskName += " => ";
        // if (r.data.output) {
        //   this.taskName += r.data.output;
        // } else {
        //   this.taskName += "？";
        // }
        this.taskName+="）";
      });
    }
  },
  methods: {
    fillData() {
      showLoading();
      const from =
        this.timeRange && this.timeRange.length == 2
          ? (this.timeRange[0] as Date).toJSON()
          : null;
      const to =
        this.timeRange && this.timeRange.length == 2
          ? (this.timeRange[1] as Date).toJSON()
          : null;
      const taskId = this.$route.query.id
        ? Number.parseInt(this.$route.query.id as string)
        : 0;

      return net
        .getLogs(
          this.typeFilter,
          taskId,
          from,
          to,
          (this.page - 1) * this.countPerPage,
          this.countPerPage
        )
        .then((response) => {
          this.totalCount = response.data.totalCount;
          this.pageCount = Math.ceil(this.totalCount / this.countPerPage);
          response.data.list.forEach((element: any) => {
            element.timeText = formatDateTime(new Date(element.time));
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
    });
  },
  components: {},
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
