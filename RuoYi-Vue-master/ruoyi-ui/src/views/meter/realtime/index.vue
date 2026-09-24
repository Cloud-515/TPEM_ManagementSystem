<template>
  <div class="app-container realtime-page">
    <section class="page-heading">
      <div>
        <h2>实时运行</h2>
        <p>查看仪表最新通信状态、负荷与三相电流</p>
      </div>
      <span class="refresh-note">每 15 秒自动刷新</span>
    </section>

    <el-form :model="draft" ref="queryForm" size="small" :inline="true" label-width="68px">
      <el-form-item label="设备名称" prop="meterName">
        <el-input v-model="draft.meterName" placeholder="请输入设备名称" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="站点" prop="siteName">
        <el-input v-model="draft.siteName" placeholder="请输入站点" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="箱体" prop="boxName">
        <el-input v-model="draft.boxName" placeholder="请输入箱体" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="运行状态" prop="statusCode">
        <el-select v-model="draft.statusCode" placeholder="全部" clearable>
          <el-option v-for="item in statusOptions" :key="item.code" :label="item.label" :value="item.code" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="el-icon-search" @click="handleQuery">搜索</el-button>
        <el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button>
      </el-form-item>
    </el-form>

    <el-alert v-if="listFailed" class="list-failed" title="数据刷新失败，下表是上一次成功获取的结果" type="warning" :closable="false" show-icon />
    <el-table v-loading="loading" :data="meterList" border>
      <el-table-column label="设备名称" prop="meterName" min-width="155" show-overflow-tooltip />
      <el-table-column label="位置" min-width="190" show-overflow-tooltip>
        <template slot-scope="scope">{{ formatMeterLocation(scope.row) }}</template>
      </el-table-column>
      <el-table-column label="通信状态" width="115">
        <template slot-scope="scope"><el-tag size="mini" :type="getMeterStatusType(scope.row.statusCode)">{{ getMeterStatusLabel(scope.row.statusCode) }}</el-tag></template>
      </el-table-column>
      <el-table-column label="总有功功率" width="135" align="right">
        <template slot-scope="scope">{{ formatMeterNumber(scope.row.activePowerKw) }} kW</template>
      </el-table-column>
      <el-table-column label="A相电流" width="110" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentA) }} A</template></el-table-column>
      <el-table-column label="B相电流" width="110" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentB) }} A</template></el-table-column>
      <el-table-column label="C相电流" width="110" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentC) }} A</template></el-table-column>
      <el-table-column label="最新采集时间" prop="lastCollectTime" min-width="165" />
      <el-table-column label="操作" width="80" fixed="right" align="center">
        <template slot-scope="scope"><el-button size="mini" type="text" @click="showDetail(scope.row)">详情</el-button></template>
      </el-table-column>
    </el-table>

    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" />
    <realtime-detail ref="detail" />
  </div>
</template>

<script>
import { listRealtimeMeters } from '@/api/system/meter'
import RealtimeDetail from '../components/realtime-detail'
import { meterStatusMeta, getMeterStatusLabel, getMeterStatusType, formatMeterNumber, formatMeterLocation } from '../components/meter-utils'

export default {
  name: 'MeterRealtime',
  components: { RealtimeDetail },
  data() {
    return {
      loading: false,
      total: 0,
      meterList: [],
      listFailed: false,
      listRequestId: 0,
      refreshTimer: null,
      // draft 是输入框里的草稿，queryParams 是"已经生效"的查询条件。
      // 两者分开的原由：15 秒自动刷新用的是 queryParams，
      // 原来输入框直接绑 queryParams，打了字但没点搜索，下一次自动刷新就会按未提交的条件过滤列表。
      draft: { meterName: undefined, siteName: undefined, boxName: undefined, statusCode: undefined },
      queryParams: { pageNum: 1, pageSize: 10, meterName: undefined, siteName: undefined, boxName: undefined, statusCode: undefined }
    }
  },
  computed: {
    statusOptions() {
      return Object.keys(meterStatusMeta).map(code => ({ code, label: meterStatusMeta[code].label }))
    }
  },
  created() {
    this.getList()
    this.startRefreshTimer()
  },
  // 本页被 keep-alive 缓存（tagsView），离开时只触发 deactivated，
  // 因此定时器必须在 activated/deactivated 里启停 —— 只在 beforeDestroy 清理的话，
  // 切到别的页面后仍会每 15 秒发一次请求。
  activated() {
    this.startRefreshTimer()
  },
  deactivated() {
    this.stopRefreshTimer()
  },
  beforeDestroy() {
    this.stopRefreshTimer()
  },
  methods: {
    startRefreshTimer() {
      this.stopRefreshTimer()
      // 自动刷新走静默模式：不动整表 loading 遮罩，否则每 15 秒闪一次
      this.refreshTimer = setInterval(() => this.getList({ silent: true }), 15000)
    },
    stopRefreshTimer() {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
    getMeterStatusLabel,
    getMeterStatusType,
    formatMeterNumber,
    formatMeterLocation,
    getList(options) {
      const silent = !!(options && options.silent)
      const requestId = ++this.listRequestId
      if (!silent) this.loading = true
      listRealtimeMeters(this.queryParams).then(response => {
        // 自动刷新与手动翻页/搜索可能并发，先发的旧响应不能覆盖后到的新结果
        if (requestId !== this.listRequestId) return
        this.meterList = response.rows || []
        this.total = response.total || 0
        this.listFailed = false
      }).catch(() => {
        if (requestId !== this.listRequestId) return
        // 失败时保留上一次的数据：原来这里清空列表并把 total 置 0，
        // 一次网络抖动就会让页面变成"没有设备"、分页器也跟着消失，15 秒后又自己回来。
        this.listFailed = true
      }).finally(() => {
        if (!silent && requestId === this.listRequestId) this.loading = false
      })
    },
    handleQuery() {
      this.queryParams = { ...this.queryParams, ...this.draft, pageNum: 1 }
      this.getList()
    },
    resetQuery() {
      this.draft = { meterName: undefined, siteName: undefined, boxName: undefined, statusCode: undefined }
      this.resetForm('queryForm')
      this.handleQuery()
    },
    showDetail(row) {
      this.$refs.detail.open(row.meterId, 'running')
    }
  }
}
</script>

<style scoped lang="scss">
.page-heading { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; }
.page-heading h2 { margin: 0; color: #172b4d; font-size: 20px; }.page-heading p, .refresh-note { margin: 6px 0 0; color: #718096; font-size: 13px; }.refresh-note { padding-top: 3px; }
</style>
