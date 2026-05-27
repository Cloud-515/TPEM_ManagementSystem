<template>
  <div class="app-container">
    <el-form :model="queryParams" ref="queryForm" size="small" :inline="true" v-show="showSearch" label-width="68px">
      <el-form-item label="设备名称" prop="meterName">
        <el-input v-model="queryParams.meterName" placeholder="请输入设备名称" clearable style="width: 220px" @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="设备编码" prop="meterCode">
        <el-input v-model="queryParams.meterCode" placeholder="请输入设备编码" clearable style="width: 220px" @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="站点编码" prop="siteCode">
        <el-input v-model="queryParams.siteCode" placeholder="请输入站点编码" clearable style="width: 220px" @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item label="箱体编码" prop="boxCode">
        <el-input v-model="queryParams.boxCode" placeholder="请输入箱体编码" clearable style="width: 220px" @keyup.enter.native="handleQuery" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="el-icon-search" size="mini" @click="handleQuery">搜索</el-button>
        <el-button icon="el-icon-refresh" size="mini" @click="resetQuery">重置</el-button>
      </el-form-item>
    </el-form>

    <el-row :gutter="10" class="mb8">
      <right-toolbar :showSearch.sync="showSearch" @queryTable="getList" />
    </el-row>

    <el-table v-loading="loading" :data="list">
      <el-table-column label="设备名称" align="center" min-width="180">
        <template slot-scope="scope">
          <el-button type="text" @click="openDetail(scope.row)">{{ scope.row.meterName || scope.row.meterCode }}</el-button>
        </template>
      </el-table-column>
      <el-table-column label="设备编码" align="center" prop="meterCode" min-width="120" />
      <el-table-column label="站点" align="center" min-width="140">
        <template slot-scope="scope">{{ scope.row.siteCode || '--' }} / {{ scope.row.siteName || '--' }}</template>
      </el-table-column>
      <el-table-column label="箱体" align="center" min-width="140">
        <template slot-scope="scope">{{ scope.row.boxCode || '--' }} / {{ scope.row.boxName || '--' }}</template>
      </el-table-column>
      <el-table-column label="三相功率" align="center" min-width="100">
        <template slot-scope="scope">{{ formatNumber(scope.row.activePowerKw) }} kW</template>
      </el-table-column>
      <el-table-column label="最大电流" align="center" min-width="100">
        <template slot-scope="scope">{{ formatNumber(scope.row.maxCurrentA) }} A</template>
      </el-table-column>
      <el-table-column label="功率因数" align="center" min-width="100">
        <template slot-scope="scope">{{ formatNumber(scope.row.powerFactorTotal) }}</template>
      </el-table-column>
      <el-table-column label="频率" align="center" min-width="100">
        <template slot-scope="scope">{{ formatNumber(scope.row.frequency) }} Hz</template>
      </el-table-column>
      <el-table-column label="状态" align="center" min-width="100">
        <template slot-scope="scope">
          <el-tag size="small" :type="statusTagType(scope.row.statusCode)">{{ scope.row.statusText || '--' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="最后采集时间" align="center" min-width="160">
        <template slot-scope="scope">{{ formatDateTime(scope.row.lastCollectTime) }}</template>
      </el-table-column>
      <el-table-column label="操作" align="center" width="120">
        <template slot-scope="scope">
          <el-button size="mini" type="text" icon="el-icon-view" @click="openDetail(scope.row)">详情</el-button>
        </template>
      </el-table-column>
    </el-table>

    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" />

    <realtime-detail ref="detailRef" />
  </div>
</template>

<script>
import { listRealtimeMeters } from '@/api/system/meter'
import RealtimeDetail from '../components/realtime-detail'

export default {
  name: 'MeterRealtime',
  components: { RealtimeDetail },
  data() {
    return {
      loading: false,
      showSearch: true,
      total: 0,
      list: [],
      queryParams: {
        pageNum: 1,
        pageSize: 10,
        meterName: undefined,
        meterCode: undefined,
        siteCode: undefined,
        boxCode: undefined
      }
    }
  },
  created() {
    this.getList()
  },
  methods: {
    getList() {
      this.loading = true
      listRealtimeMeters(this.queryParams).then(response => {
        this.list = response.rows || []
        this.total = response.total || 0
      }).finally(() => {
        this.loading = false
      })
    },
    handleQuery() {
      this.queryParams.pageNum = 1
      this.getList()
    },
    resetQuery() {
      this.resetForm('queryForm')
      this.queryParams.pageNum = 1
      this.getList()
    },
    openDetail(row) {
      this.$refs.detailRef.open(row.meterId)
    },
    formatNumber(value) {
      if (value === null || value === undefined || value === '') {
        return '--'
      }
      return Number(value).toFixed(2)
    },
    formatDateTime(value) {
      if (!value) {
        return '--'
      }
      if (typeof value === 'string') {
        return value.replace('T', ' ').slice(0, 19)
      }
      const date = new Date(value)
      if (Number.isNaN(date.getTime())) {
        return '--'
      }
      const pad = number => String(number).padStart(2, '0')
      return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
    },
    statusTagType(statusCode) {
      switch (statusCode) {
        case 'OK':
          return 'success'
        case 'PF_LOW':
        case 'VOLTAGE_BAD':
          return 'warning'
        case 'FAULT':
          return 'danger'
        case 'NODATA':
          return 'info'
        default:
          return 'info'
      }
    }
  }
}
</script>
