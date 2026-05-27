<template>
  <div class="app-container">
    <el-form :model="queryParams" ref="queryForm" size="small" :inline="true" v-show="showSearch" label-width="80px">
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
      <el-form-item label="开始时间" prop="beginTime">
        <el-date-picker
          v-model="queryTimeRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          value-format="yyyy-MM-dd HH:mm:ss"
          style="width: 380px"
        />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" icon="el-icon-search" size="mini" @click="handleQuery">搜索</el-button>
        <el-button icon="el-icon-refresh" size="mini" @click="resetQuery">重置</el-button>
      </el-form-item>
    </el-form>

    <el-row :gutter="10" class="mb8">
      <right-toolbar :showSearch.sync="showSearch" @queryTable="getList" />
    </el-row>

    <el-tabs v-model="activeTab" @tab-click="handleTabChange">
      <el-tab-pane label="实时历史" name="realtime">
        <el-table v-loading="loading" :data="list">
          <el-table-column label="采集时间" align="center" min-width="160">
            <template slot-scope="scope">{{ formatDateTime(scope.row.dataCollectTime) }}</template>
          </el-table-column>
          <el-table-column label="设备名称" align="center" min-width="160" prop="meterName" />
          <el-table-column label="设备编码" align="center" min-width="120" prop="meterCode" />
          <el-table-column label="站点/箱体" align="center" min-width="180">
            <template slot-scope="scope">{{ scope.row.siteCode || '--' }} / {{ scope.row.boxCode || '--' }}</template>
          </el-table-column>
          <el-table-column label="A相电压" align="center" min-width="100">
            <template slot-scope="scope">{{ formatNumber(scope.row.voltageA) }} V</template>
          </el-table-column>
          <el-table-column label="B相电压" align="center" min-width="100">
            <template slot-scope="scope">{{ formatNumber(scope.row.voltageB) }} V</template>
          </el-table-column>
          <el-table-column label="C相电压" align="center" min-width="100">
            <template slot-scope="scope">{{ formatNumber(scope.row.voltageC) }} V</template>
          </el-table-column>
          <el-table-column label="总有功功率" align="center" min-width="120">
            <template slot-scope="scope">{{ formatPower(scope.row.activePowerTotal) }}</template>
          </el-table-column>
          <el-table-column label="频率" align="center" min-width="100">
            <template slot-scope="scope">{{ formatNumber(scope.row.frequency) }} Hz</template>
          </el-table-column>
          <el-table-column label="状态" align="center" min-width="100">
            <template slot-scope="scope">
              <el-tag size="small" :type="statusTagType(scope.row.statusCode)">{{ scope.row.statusText || '--' }}</el-tag>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>

      <el-tab-pane label="电能历史" name="energy">
        <el-table v-loading="loading" :data="list">
          <el-table-column label="采集时间" align="center" min-width="160">
            <template slot-scope="scope">{{ formatDateTime(scope.row.dataCollectTime) }}</template>
          </el-table-column>
          <el-table-column label="设备名称" align="center" min-width="160" prop="meterName" />
          <el-table-column label="设备编码" align="center" min-width="120" prop="meterCode" />
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
        </el-table>
      </el-tab-pane>

      <el-tab-pane label="电能质量历史" name="quality">
        <el-table v-loading="loading" :data="list">
          <el-table-column label="采集时间" align="center" min-width="160">
            <template slot-scope="scope">{{ formatDateTime(scope.row.dataCollectTime) }}</template>
          </el-table-column>
          <el-table-column label="设备名称" align="center" min-width="160" prop="meterName" />
          <el-table-column label="设备编码" align="center" min-width="120" prop="meterCode" />
          <el-table-column label="A相电流THD" align="center" min-width="120">
            <template slot-scope="scope">{{ formatNumber(scope.row.currentThdA) }} %</template>
          </el-table-column>
          <el-table-column label="B相电流THD" align="center" min-width="120">
            <template slot-scope="scope">{{ formatNumber(scope.row.currentThdB) }} %</template>
          </el-table-column>
          <el-table-column label="C相电流THD" align="center" min-width="120">
            <template slot-scope="scope">{{ formatNumber(scope.row.currentThdC) }} %</template>
          </el-table-column>
          <el-table-column label="电压不平衡度" align="center" min-width="120">
            <template slot-scope="scope">{{ formatNumber(scope.row.voltageUnbalance) }} %</template>
          </el-table-column>
          <el-table-column label="电流不平衡度" align="center" min-width="120">
            <template slot-scope="scope">{{ formatNumber(scope.row.currentUnbalance) }} %</template>
          </el-table-column>
          <el-table-column label="状态" align="center" min-width="100">
            <template slot-scope="scope">
              <el-tag size="small" :type="statusTagType(scope.row.statusCode)">{{ scope.row.statusText || '--' }}</el-tag>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>

    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" />
  </div>
</template>

<script>
import { listRealtimeHistory, listEnergyHistory, listQualityHistory } from '@/api/system/meter'

export default {
  name: 'MeterHistory',
  data() {
    return {
      loading: false,
      showSearch: true,
      total: 0,
      list: [],
      activeTab: 'realtime',
      queryTimeRange: [],
      queryParams: {
        pageNum: 1,
        pageSize: 10,
        meterName: undefined,
        meterCode: undefined,
        siteCode: undefined,
        boxCode: undefined,
        beginTime: undefined,
        endTime: undefined
      }
    }
  },
  created() {
    this.getList()
  },
  methods: {
    getCurrentApi() {
      if (this.activeTab === 'energy') {
        return listEnergyHistory
      }
      if (this.activeTab === 'quality') {
        return listQualityHistory
      }
      return listRealtimeHistory
    },
    syncTimeRange() {
      this.queryParams.beginTime = this.queryTimeRange && this.queryTimeRange.length ? this.queryTimeRange[0] : undefined
      this.queryParams.endTime = this.queryTimeRange && this.queryTimeRange.length ? this.queryTimeRange[1] : undefined
    },
    getList() {
      this.syncTimeRange()
      this.loading = true
      this.getCurrentApi()(this.queryParams).then(response => {
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
    handleTabChange() {
      this.queryParams.pageNum = 1
      this.getList()
    },
    resetQuery() {
      this.queryTimeRange = []
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
    formatPower(value) {
      if (value === null || value === undefined || value === '') {
        return '--'
      }
      return `${(Number(value) / 1000).toFixed(2)} kW`
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
        case 'WAITING':
          return 'info'
        default:
          return 'info'
      }
    }
  }
}
</script>
