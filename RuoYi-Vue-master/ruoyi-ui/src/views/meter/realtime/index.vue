<template>
  <div class="app-container realtime-page">
    <section class="page-heading">
      <div>
        <h2>实时运行</h2>
        <p>查看仪表最新通信状态、负荷与三相电流</p>
      </div>
      <span class="refresh-note">每 15 秒自动刷新</span>
    </section>

    <el-form :model="queryParams" ref="queryForm" size="small" :inline="true" label-width="68px">
      <el-form-item label="设备名称" prop="meterName">
        <el-input v-model="queryParams.meterName" placeholder="请输入设备名称" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="站点" prop="siteName">
        <el-input v-model="queryParams.siteName" placeholder="请输入站点" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="箱体" prop="boxName">
        <el-input v-model="queryParams.boxName" placeholder="请输入箱体" clearable @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="运行状态" prop="statusCode">
        <el-select v-model="queryParams.statusCode" placeholder="全部" clearable>
          <el-option v-for="item in statusOptions" :key="item.code" :label="item.label" :value="item.code" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="el-icon-search" @click="handleQuery">搜索</el-button>
        <el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button>
      </el-form-item>
    </el-form>

    <el-table v-loading="loading" :data="meterList" border>
      <el-table-column label="设备名称" prop="meterName" min-width="155" show-overflow-tooltip />
      <el-table-column label="位置" min-width="190" show-overflow-tooltip>
        <template slot-scope="scope">{{ scope.row.siteName || '--' }} / {{ scope.row.boxName || '--' }}</template>
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
import { meterStatusMeta, getMeterStatusLabel, getMeterStatusType, formatMeterNumber } from '../components/meter-utils'

export default {
  name: 'MeterRealtime',
  components: { RealtimeDetail },
  data() {
    return {
      loading: false,
      total: 0,
      meterList: [],
      refreshTimer: null,
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
    this.refreshTimer = setInterval(this.getList, 15000)
  },
  beforeDestroy() {
    clearInterval(this.refreshTimer)
  },
  methods: {
    getMeterStatusLabel,
    getMeterStatusType,
    formatMeterNumber,
    getList() {
      this.loading = true
      listRealtimeMeters(this.queryParams).then(response => {
        this.meterList = response.rows || []
        this.total = response.total || 0
      }).catch(() => {
        this.meterList = []
        this.total = 0
      }).finally(() => { this.loading = false })
    },
    handleQuery() {
      this.queryParams.pageNum = 1
      this.getList()
    },
    resetQuery() {
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
