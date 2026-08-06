<template>
  <div class="app-container alarm-record-page" v-loading="loading">
    <div class="page-heading">
      <div>
        <h1>报警记录</h1>
        <p>查看当前设备报警状态及设备运行详情</p>
      </div>
      <el-button icon="el-icon-back" @click="$router.back()">返回</el-button>
    </div>

    <el-card class="record-card" shadow="never">
      <div slot="header" class="section-header">
        <span>报警记录</span>
        <el-tag v-if="record" :type="statusType(record.statusCode)" size="small">{{ record.statusText }}</el-tag>
      </div>
      <el-table v-if="record" :data="[record]" border>
        <el-table-column prop="region" label="所属区域" min-width="220">
          <template slot-scope="scope">{{ scope.row.region }}</template>
        </el-table-column>
        <el-table-column prop="description" label="故障描述" min-width="300">
          <template slot-scope="scope">{{ scope.row.description }}</template>
        </el-table-column>
        <el-table-column prop="occurredAt" label="发生时间" min-width="190">
          <template slot-scope="scope">{{ scope.row.occurredAt }}</template>
        </el-table-column>
      </el-table>
      <div v-else class="empty-state">{{ emptyMessage }}</div>
    </el-card>

    <el-card class="record-card device-card" shadow="never">
      <div slot="header" class="section-header"><span>设备详情</span></div>
      <template v-if="detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="设备名称">{{ detail.meterName || '--' }}</el-descriptions-item>
          <el-descriptions-item label="设备编码">{{ detail.meterCode || '--' }}</el-descriptions-item>
          <el-descriptions-item label="所属区域">{{ detail.siteName || '--' }}</el-descriptions-item>
          <el-descriptions-item label="配电箱">{{ detail.boxName || '--' }}</el-descriptions-item>
          <el-descriptions-item label="从站地址">{{ detail.slaveAddress || '--' }}</el-descriptions-item>
          <el-descriptions-item label="设备状态">
            <el-tag :type="statusType(detail.statusCode)" size="small">{{ detail.statusText || getMeterStatusLabel(detail.statusCode) }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="有功功率">{{ formatNumber(detail.activePowerKw) }} kW</el-descriptions-item>
          <el-descriptions-item label="无功功率">{{ formatNumber(detail.reactivePowerKvar) }} kvar</el-descriptions-item>
          <el-descriptions-item label="A相电流">{{ formatNumber(detail.currentA) }} A</el-descriptions-item>
          <el-descriptions-item label="B相电流">{{ formatNumber(detail.currentB) }} A</el-descriptions-item>
          <el-descriptions-item label="C相电流">{{ formatNumber(detail.currentC) }} A</el-descriptions-item>
          <el-descriptions-item label="功率因数">{{ formatNumber(detail.powerFactorTotal) }}</el-descriptions-item>
          <el-descriptions-item label="累计正向电能">{{ formatNumber(detail.forwardActiveEnergyKwh) }} kWh</el-descriptions-item>
          <el-descriptions-item label="累计反向电能">{{ formatNumber(detail.reverseActiveEnergyKwh) }} kWh</el-descriptions-item>
          <el-descriptions-item label="最后采集时间">{{ detail.lastCollectTime || '--' }}</el-descriptions-item>
        </el-descriptions>
      </template>
      <div v-else class="empty-state">暂无设备详情</div>
    </el-card>
  </div>
</template>

<script>
import { getRealtimeDetail } from '@/api/system/meter'
import { getMeterStatusLabel, meterStatusMeta } from '@/views/meter/components/meter-utils'

export default {
  name: 'MeterAlarmRecord',
  data() {
    return { loading: false, detail: null, emptyMessage: '暂无报警记录' }
  },
  computed: {
    record() {
      if (!this.detail || this.detail.statusCode === 'OK') return null
      return {
        region: [this.detail.siteName, this.detail.boxName].filter(Boolean).join(' / ') || '--',
        description: this.detail.statusText || getMeterStatusLabel(this.detail.statusCode),
        occurredAt: this.detail.lastCollectTime || '--',
        statusCode: this.detail.statusCode,
        statusText: this.detail.statusText || getMeterStatusLabel(this.detail.statusCode)
      }
    }
  },
  created() { this.loadDetail() },
  methods: {
    async loadDetail() {
      const meterId = this.$route.query.meterId
      if (!meterId) { this.emptyMessage = '缺少设备信息'; return }
      this.loading = true
      try {
        const response = await getRealtimeDetail(meterId)
        this.detail = response.data || response
        if (!this.detail || this.detail.statusCode === 'OK') this.emptyMessage = '该设备当前没有报警或故障'
      } catch (error) {
        this.emptyMessage = '报警记录加载失败'
      } finally { this.loading = false }
    },
    statusType(code) { return (meterStatusMeta[code] || {}).type || 'info' },
    getMeterStatusLabel(code) { return getMeterStatusLabel(code) },
    formatNumber(value) { return value === null || value === undefined || value === '' ? '--' : Number(value).toLocaleString('zh-CN', { maximumFractionDigits: 3 }) }
  }
}
</script>

<style scoped>
.alarm-record-page { background: #f5f7fa; min-height: calc(100vh - 84px); }
.page-heading { display: flex; justify-content: space-between; align-items: center; margin-bottom: 18px; }
.page-heading h1 { margin: 0 0 6px; color: #1f2937; font-size: 24px; }
.page-heading p { margin: 0; color: #718096; font-size: 13px; }
.record-card { margin-bottom: 18px; border: 1px solid #e8edf3; }
.section-header { display: flex; align-items: center; justify-content: space-between; color: #1f2937; font-size: 16px; font-weight: 600; }
.device-card { min-height: 240px; }
.empty-state { padding: 56px 0; color: #94a3b8; text-align: center; font-size: 14px; }
</style>
