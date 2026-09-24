<template>
  <div class="app-container exception-devices-page" v-loading="loading">
    <div class="page-heading">
      <div>
        <h1>异常设备</h1>
        <p>展示当前采集状态异常的全部设备</p>
      </div>
      <div><el-button @click="$router.push({ name: 'MeterRealtime' })">返回实时运行</el-button><el-button icon="el-icon-refresh" :loading="loading" @click="loadDevices">刷新</el-button></div>
    </div>

    <el-card shadow="never">
      <div slot="header" class="section-header">
        <span>当前异常设备</span>
        <span class="count-label">{{ exceptions.length }} 台</span>
      </div>
      <el-table v-if="exceptions.length" :data="exceptions" border>
        <el-table-column prop="meterName" label="设备名称" min-width="180" show-overflow-tooltip />
        <el-table-column label="所属区域" min-width="220" show-overflow-tooltip>
          <template slot-scope="scope">{{ formatRegion(scope.row) }}</template>
        </el-table-column>
        <el-table-column label="当前负荷" width="140" align="right">
          <template slot-scope="scope">{{ formatMeterNumber(scope.row.activePowerKw) }} kW</template>
        </el-table-column>
        <el-table-column label="状态" width="130" align="center">
          <template slot-scope="scope"><el-tag :type="getMeterStatusType(scope.row.statusCode)" size="small">{{ getMeterStatusLabel(scope.row.statusCode) }}</el-tag></template>
        </el-table-column>
        <el-table-column prop="lastCollectTime" label="最后采集时间" width="180" />
        <el-table-column label="操作" width="90" align="center">
          <template slot-scope="scope"><el-button type="text" size="mini" @click="openRecord(scope.row)">详情</el-button></template>
        </el-table-column>
      </el-table>
      <div v-else class="empty-state">{{ emptyMessage }}</div>
    </el-card>
  </div>
</template>

<script>
import { listMeterCards } from '@/api/system/meter'
import { getMeterStatusLabel, getMeterStatusType, formatMeterNumber, formatMeterLocation, getMeterSeverityRank } from '@/views/meter/components/meter-utils'

export default {
  name: 'MeterExceptionDevices',
  data() {
    return { loading: false, exceptions: [], emptyMessage: '当前没有异常设备' }
  },
  created() { this.loadDevices() },
  methods: {
    getMeterStatusLabel,
    getMeterStatusType,
    formatMeterNumber,
    async loadDevices() {
      this.loading = true
      try {
        const response = await listMeterCards()
        const payload = response.data || response
        // /cards 把设备分成互斥两组：dashboard = 仪表盘扫描串口，toolbar = 外部串口。
        // 本页写的是"全部设备"，只取 dashboard 会让外部串口的表永远查不到（而它在能耗页的下拉里是可选的）。
        const all = Array.from(new Map([...(payload.dashboard || []), ...(payload.toolbar || [])].map(item => [String(item.meterId), item])).values())
        this.exceptions = all.filter(item => item.statusCode !== 'OK').sort((a, b) => getMeterSeverityRank(a.statusCode) - getMeterSeverityRank(b.statusCode))
        this.emptyMessage = '当前没有异常设备'
      } catch (error) {
        this.exceptions = []
        this.emptyMessage = '异常设备加载失败'
      } finally { this.loading = false }
    },
    formatRegion(device) { return formatMeterLocation(device) },
    openRecord(device) {
      if (device && device.meterId) this.$router.push({ name: 'MeterAlarmRecord', query: { meterId: device.meterId, from: 'exceptions' } })
    }
  }
}
</script>

<style scoped>
.exception-devices-page { min-height: calc(100vh - 84px); background: #f5f7fa; }
.page-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }
.page-heading h1 { margin: 0 0 6px; color: #1f2937; font-size: 24px; }
.page-heading p { margin: 0; color: #718096; font-size: 13px; }
.section-header { display: flex; align-items: center; justify-content: space-between; color: #1f2937; font-size: 16px; font-weight: 600; }
.count-label { color: #718096; font-size: 13px; font-weight: 400; }
.empty-state { padding: 68px 0; color: #94a3b8; text-align: center; font-size: 14px; }
</style>
