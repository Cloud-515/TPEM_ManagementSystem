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
      <el-table-column label="设备名称" align="center" min-width="180" prop="meterName" />
      <el-table-column label="设备编码" align="center" prop="meterCode" min-width="120" />
      <el-table-column label="正向有功电能" align="center" min-width="140">
        <template slot-scope="scope">{{ formatNumber(scope.row.forwardActiveEnergy) }} kWh</template>
      </el-table-column>
      <el-table-column label="反向有功电能" align="center" min-width="140">
        <template slot-scope="scope">{{ formatNumber(scope.row.reverseActiveEnergy) }} kWh</template>
      </el-table-column>
      <el-table-column label="正向无功电能" align="center" min-width="140">
        <template slot-scope="scope">{{ formatNumber(scope.row.forwardReactiveEnergy) }} kvarh</template>
      </el-table-column>
      <el-table-column label="反向无功电能" align="center" min-width="140">
        <template slot-scope="scope">{{ formatNumber(scope.row.reverseReactiveEnergy) }} kvarh</template>
      </el-table-column>
      <el-table-column label="状态" align="center" min-width="100">
        <template slot-scope="scope">
          <el-tag size="small" :type="statusTagType(scope.row.statusCode)">{{ scope.row.statusText || '--' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="更新时间" align="center" min-width="160">
        <template slot-scope="scope">{{ formatDateTime(scope.row.dataCollectTime || scope.row.lastCollectTime) }}</template>
      </el-table-column>
    </el-table>

    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" />
  </div>
</template>

<script>
import { listEnergyMeters } from '@/api/system/meter'

export default {
  name: 'MeterEnergy',
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
      listEnergyMeters(this.queryParams).then(response => {
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
        case 'NODATA':
          return 'danger'
        default:
          return 'info'
      }
    }
  }
}
</script>
