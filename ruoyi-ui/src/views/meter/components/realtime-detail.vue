<template>
  <el-drawer :title="detail.meterName || '设备详情'" :visible.sync="visible" size="680px" append-to-body>
    <div v-loading="loading" class="detail-content">
      <el-tabs v-model="activeTab">
        <el-tab-pane label="实时运行" name="running">
          <el-descriptions :column="2" border size="small">
            <el-descriptions-item label="设备名称">{{ detail.meterName || '--' }}</el-descriptions-item>
            <el-descriptions-item label="设备地址">{{ detail.slaveAddress == null ? '--' : detail.slaveAddress }}</el-descriptions-item>
            <el-descriptions-item label="站点 / 箱体">{{ detail.siteName || '--' }} / {{ detail.boxName || '--' }}</el-descriptions-item>
            <el-descriptions-item label="运行状态"><el-tag size="mini" :type="getMeterStatusType(detail.statusCode)">{{ getMeterStatusLabel(detail.statusCode) }}</el-tag></el-descriptions-item>
            <el-descriptions-item label="总有功功率">{{ formatMeterNumber(detail.activePowerKw) }} kW</el-descriptions-item>
            <el-descriptions-item label="总无功功率">{{ formatMeterNumber(detail.reactivePowerKvar) }} kvar</el-descriptions-item>
            <el-descriptions-item label="A / B / C 相电流" :span="2">{{ formatMeterNumber(detail.currentA) }} / {{ formatMeterNumber(detail.currentB) }} / {{ formatMeterNumber(detail.currentC) }} A</el-descriptions-item>
            <el-descriptions-item label="最后采集时间" :span="2">{{ detail.lastCollectTime || '--' }}</el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>
        <el-tab-pane label="电能累计" name="energy">
          <el-alert title="以下为最新累计读数；周期用电量请在能耗分析页面查看。" type="info" :closable="false" show-icon />
          <el-descriptions class="tab-detail" :column="2" border size="small">
            <el-descriptions-item label="正向有功电能">{{ formatMeterNumber(detail.forwardActiveEnergy) }} kWh</el-descriptions-item>
            <el-descriptions-item label="反向有功电能">{{ formatMeterNumber(detail.reverseActiveEnergy) }} kWh</el-descriptions-item>
            <el-descriptions-item label="正向无功电能">{{ formatMeterNumber(detail.forwardReactiveEnergy) }} kvarh</el-descriptions-item>
            <el-descriptions-item label="反向无功电能">{{ formatMeterNumber(detail.reverseReactiveEnergy) }} kvarh</el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>
        <el-tab-pane label="质量诊断" name="quality">
          <el-alert title="以下为最新采集评估；历史质量变化请在历史追溯页面查看。" type="info" :closable="false" show-icon />
          <el-descriptions class="tab-detail" :column="2" border size="small">
            <el-descriptions-item label="A / B / C 相电压" :span="2">{{ formatMeterNumber(detail.voltageA) }} / {{ formatMeterNumber(detail.voltageB) }} / {{ formatMeterNumber(detail.voltageC) }} V</el-descriptions-item>
            <el-descriptions-item label="总功率因数">{{ formatMeterNumber(detail.powerFactorTotal, 3) }}</el-descriptions-item>
            <el-descriptions-item label="频率">{{ formatMeterNumber(detail.frequency) }} Hz</el-descriptions-item>
            <el-descriptions-item label="电压不平衡度">{{ formatMeterNumber(detail.voltageUnbalance) }} %</el-descriptions-item>
            <el-descriptions-item label="电流不平衡度">{{ formatMeterNumber(detail.currentUnbalance) }} %</el-descriptions-item>
            <el-descriptions-item label="A / B / C 电压 THD" :span="2">{{ formatMeterNumber(detail.voltageThdA) }} / {{ formatMeterNumber(detail.voltageThdB) }} / {{ formatMeterNumber(detail.voltageThdC) }} %</el-descriptions-item>
            <el-descriptions-item label="A / B / C 电流 THD" :span="2">{{ formatMeterNumber(detail.currentThdA) }} / {{ formatMeterNumber(detail.currentThdB) }} / {{ formatMeterNumber(detail.currentThdC) }} %</el-descriptions-item>
          </el-descriptions>
        </el-tab-pane>
      </el-tabs>
    </div>
  </el-drawer>
</template>

<script>
import { getRealtimeDetail } from '@/api/system/meter'
import { getMeterStatusLabel, getMeterStatusType, formatMeterNumber } from './meter-utils'

export default {
  name: 'RealtimeDetail',
  data() {
    return { visible: false, loading: false, activeTab: 'running', detail: {} }
  },
  methods: {
    getMeterStatusLabel,
    getMeterStatusType,
    formatMeterNumber,
    open(meterId, tab = 'running') {
      this.activeTab = tab
      this.visible = true
      this.loading = true
      getRealtimeDetail(meterId).then(response => { this.detail = response.data || {} }).catch(() => { this.detail = {} }).finally(() => { this.loading = false })
    }
  }
}
</script>

<style scoped>
.detail-content { padding: 0 20px 24px; }.tab-detail { margin-top: 16px; }
</style>
