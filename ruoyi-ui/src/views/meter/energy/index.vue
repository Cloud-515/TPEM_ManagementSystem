<template>
  <div class="app-container energy-page" v-loading="analysisLoading">
    <section class="page-heading">
      <div><h2>能耗分析</h2><p>按设备和时间范围分析累计电能与运行功率</p></div>
      <el-radio-group v-model="rangePreset" size="small" @change="applyPreset"><el-radio-button label="24h">24小时</el-radio-button><el-radio-button label="7d">7天</el-radio-button><el-radio-button label="30d">30天</el-radio-button></el-radio-group>
    </section>

    <el-form size="small" :inline="true" class="filter-form">
      <el-form-item label="分析设备">
        <el-select v-model="meterId" filterable clearable placeholder="请选择设备" @change="handleMeterChange">
          <el-option v-for="meter in meterOptions" :key="meter.meterId" :label="meter.meterName + ' (' + meter.meterCode + ')'" :value="meter.meterId" />
        </el-select>
      </el-form-item>
      <el-form-item label="时间范围"><el-date-picker v-model="dateRange" type="datetimerange" value-format="yyyy-MM-dd HH:mm:ss" range-separator="至" start-placeholder="开始时间" end-placeholder="结束时间" @change="handleDateRangeChange" /></el-form-item>
      <el-form-item><el-button type="primary" icon="el-icon-search" :disabled="!meterId" @click="loadAnalysis">分析</el-button></el-form-item>
    </el-form>

    <el-alert v-if="analysisError" :title="analysisError" type="warning" :closable="false" show-icon />

    <el-row :gutter="16" class="kpi-row">
      <el-col v-for="item in energyKpis" :key="item.label" :xs="12" :md="4"><div class="energy-kpi"><span>{{ item.label }}</span><el-tooltip :content="item.fullValue" placement="top" :disabled="!item.fullValue"><div class="energy-value"><strong>{{ item.value }}</strong><small>{{ item.unit }}</small></div></el-tooltip></div></el-col>
    </el-row>

    <section class="chart-panel">
      <div class="panel-header"><div><h3>总有功功率趋势</h3><p>所选设备在当前时间范围内的采集功率</p></div><span class="unit-label">单位：kW</span></div>
      <div ref="powerChart" class="trend-chart" />
      <div v-if="!powerPoints.length && !analysisLoading" class="chart-empty">当前范围内没有可用的实时功率历史数据</div>
    </section>

    <el-divider content-position="left">所选设备累计读数</el-divider>
    <el-table :data="selectedMeter ? [selectedMeter] : []" border>
      <el-table-column label="设备名称" prop="meterName" min-width="180" />
      <el-table-column label="位置" min-width="180"><template slot-scope="scope">{{ scope.row.siteName || '--' }} / {{ scope.row.boxName || '--' }}</template></el-table-column>
      <el-table-column label="正向有功累计" width="160" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.forwardActiveEnergy) }} kWh</template></el-table-column>
      <el-table-column label="反向有功累计" width="160" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.reverseActiveEnergy) }} kWh</template></el-table-column>
      <el-table-column label="正向无功累计" width="160" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.forwardReactiveEnergy) }} kvarh</template></el-table-column>
      <el-table-column label="最新采集时间" prop="lastCollectTime" min-width="175" />
      <el-table-column label="操作" width="80" fixed="right" align="center"><template slot-scope="scope"><el-button type="text" size="mini" @click="showDetail(scope.row)">详情</el-button></template></el-table-column>
    </el-table>
    <realtime-detail ref="detail" />
  </div>
</template>

<script>
import * as echarts from 'echarts'
import resize from '@/views/dashboard/mixins/resize'
import { getEnergyAnalysis, listMeterCards } from '@/api/system/meter'
import RealtimeDetail from '../components/realtime-detail'
import { formatMeterNumber } from '../components/meter-utils'

function formatDate(date) {
  const pad = value => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

export default {
  name: 'MeterEnergy',
  components: { RealtimeDetail },
  mixins: [resize],
  data() {
    return { analysisLoading: false, meterOptions: [], meterId: undefined, dateRange: [], rangePreset: '24h', selectedMeter: null, energyPoints: [], powerPoints: [], energySummary: null, powerChart: null, analysisError: '', analysisRequestId: 0 }
  },
  computed: {
    energyKpis() {
      const powers = this.powerPoints.map(item => Number(item.activePowerTotal) / 1000).filter(Number.isFinite)
      const summary = this.energySummary || {}
      const start = Number(summary.startForwardActiveEnergy)
      const end = Number(summary.endForwardActiveEnergy)
      const interval = summary.valid ? Number(summary.intervalEnergy) : null
      const peak = powers.length ? Math.max(...powers) : null
      const average = powers.length ? powers.reduce((sum, value) => sum + value, 0) / powers.length : null
      const compactEnergy = value => {
        if (!Number.isFinite(value)) return { value: '--', unit: 'kWh', fullValue: '' }
        if (Math.abs(value) >= 1000000) return { value: this.formatMeterNumber(value / 1000000, 2), unit: 'GWh', fullValue: `${this.formatMeterNumber(value)} kWh` }
        if (Math.abs(value) >= 10000) return { value: this.formatMeterNumber(value / 10000, 2), unit: '万 kWh', fullValue: `${this.formatMeterNumber(value)} kWh` }
        return { value: this.formatMeterNumber(value), unit: 'kWh', fullValue: `${this.formatMeterNumber(value)} kWh` }
      }
      const startEnergy = compactEnergy(start)
      const endEnergy = compactEnergy(end)
      const intervalEnergy = compactEnergy(interval)
      return [
        { label: '起始累计电能', ...startEnergy },
        { label: '结束累计电能', ...endEnergy },
        { label: '区间用电量', ...intervalEnergy },
        { label: '峰值功率', value: this.formatMeterNumber(peak), unit: 'kW', fullValue: '' },
        { label: '平均功率', value: this.formatMeterNumber(average), unit: 'kW', fullValue: '' },
        { label: '有效功率点', value: powers.length || '--', unit: '个', fullValue: '' }
      ]
    }
  },
  mounted() { this.powerChart = echarts.init(this.$refs.powerChart); this.meterId = this.$route.query.meterId ? Number(this.$route.query.meterId) : undefined; this.applyPreset() },
  watch: { '$route.query.meterId'(meterId) { if (meterId) { this.meterId = Number(meterId); this.loadAnalysis() } } },
  beforeDestroy() { if (this.powerChart) this.powerChart.dispose() },
  methods: {
    formatMeterNumber,
    resize() { if (this.powerChart) this.powerChart.resize() },
    applyPreset() {
      const hours = this.rangePreset === '24h' ? 24 : this.rangePreset === '7d' ? 168 : 720
      const end = new Date(); const begin = new Date(end.getTime() - hours * 3600000)
      this.dateRange = [formatDate(begin), formatDate(end)]
      this.loadMeters()
    },
    handleDateRangeChange() { this.rangePreset = ''; this.loadAnalysis() },
    handleMeterChange() {
      const query = { ...this.$route.query }
      if (this.meterId) query.meterId = this.meterId
      else delete query.meterId
      this.$router.replace({ name: 'MeterEnergy', query })
      this.loadAnalysis()
    },
    loadMeters() {
      listMeterCards().then(response => {
        const data = response.data || {}; this.meterOptions = [...(data.dashboard || []), ...(data.toolbar || [])]
        if (!this.meterId && this.meterOptions.length) this.meterId = this.meterOptions[0].meterId
        this.loadAnalysis()
      }).catch(() => { this.meterOptions = []; this.meterId = undefined; this.selectedMeter = null; this.analysisError = '无法加载设备列表' })
    },
    loadAnalysis() {
      if (!this.meterId || !this.dateRange || this.dateRange.length !== 2) return
      const requestId = ++this.analysisRequestId
      this.analysisLoading = true
      this.analysisError = ''
      const params = { meterId: this.meterId, beginTime: this.dateRange[0], endTime: this.dateRange[1] }
      this.energySummary = null
      this.energySummary = null
      getEnergyAnalysis(params).then(response => {
        if (requestId !== this.analysisRequestId) return
        const analysis = response.data || response || {}
        this.energySummary = analysis.summary || null
        this.energyPoints = (analysis.energyPoints || []).slice().sort((a, b) => String(a.dataCollectTime).localeCompare(String(b.dataCollectTime)))
        this.powerPoints = (analysis.powerPoints || []).slice().sort((a, b) => String(a.dataCollectTime).localeCompare(String(b.dataCollectTime)))
        this.selectedMeter = this.meterOptions.find(item => item.meterId === this.meterId) || this.energyPoints[0] || this.powerPoints[0] || null
        const reasonMessages = {
          NO_ENERGY_READINGS: '所选时间范围没有有效累计电能读数。',
          INSUFFICIENT_ENERGY_READINGS: '所选时间范围只有一条有效累计电能读数，无法计算区间用电量。',
          COUNTER_REGRESSION: '累计电能读数回退或设备复位，无法计算区间用电量。'
        }
        this.analysisError = reasonMessages[(this.energySummary || {}).reason] || ''
        this.renderPowerChart()
      }).catch(() => {
        if (requestId !== this.analysisRequestId) return
        this.energySummary = null
        this.energyPoints = []
        this.powerPoints = []
        this.selectedMeter = null
        this.analysisError = '无法读取该设备在当前范围内的历史遥测数据'
        this.renderPowerChart()
      }).finally(() => {
        if (requestId === this.analysisRequestId) this.analysisLoading = false
      })
    },
    renderPowerChart() {
      if (!this.powerChart) return
      this.powerChart.setOption({
        color: ['#1677a8'], tooltip: { trigger: 'axis', valueFormatter: value => value == null ? '--' : `${value} kW` },
        grid: { containLabel: true, left: 64, right: 30, top: 28, bottom: 48 },
        xAxis: { type: 'category', boundaryGap: false, data: this.powerPoints.map(item => item.dataCollectTime), axisLabel: { color: '#718096', hideOverlap: true, formatter: value => value ? value.slice(5, 16) : '' }, axisLine: { lineStyle: { color: '#d9e2ec' } } },
        yAxis: { type: 'value', name: 'kW', nameLocation: 'end', nameGap: 12, nameTextStyle: { color: '#718096' }, axisLabel: { color: '#718096' }, splitLine: { lineStyle: { color: '#edf2f7' } } },
        series: [{ name: '总有功功率', type: 'line', smooth: true, symbol: 'circle', symbolSize: 6, lineStyle: { width: 2 }, areaStyle: { color: 'rgba(22,119,168,.14)' }, data: this.powerPoints.map(item => Number.isFinite(Number(item.activePowerTotal)) ? Number(item.activePowerTotal) / 1000 : null) }]
      }, true)
    },
    showDetail(row) { this.$refs.detail.open(row.meterId, 'energy') }
  }
}
</script>

<style scoped lang="scss">
.energy-page { min-height: 100%; }
.page-heading, .panel-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.page-heading h2, .panel-header h3 { margin: 0; color: #243b53; }
.page-heading p, .panel-header p, .unit-label { margin: 6px 0 0; color: #718096; font-size: 13px; }
.filter-form { margin: 20px 0 12px; }
.kpi-row { margin: 16px 0; }
.energy-kpi { min-height: 104px; padding: 20px; overflow: hidden; background: #fff; border: 1px solid #e6edf3; border-radius: 6px; }
.energy-kpi span { display: block; color: #718096; font-size: 13px; }
.energy-value { display: flex; align-items: baseline; gap: 6px; margin-top: 10px; cursor: default; white-space: nowrap; }
.energy-kpi strong { color: #243b53; font-family: Consolas, Monaco, monospace; font-size: clamp(18px, 2vw, 24px); font-variant-numeric: tabular-nums; line-height: 1.25; }
.energy-kpi small { flex: 0 0 auto; color: #718096; font-size: 13px; }
.chart-panel { position: relative; padding: 20px; background: #fff; border: 1px solid #e6edf3; border-radius: 6px; }
.trend-chart { height: 320px; }
.chart-empty { position: absolute; inset: 100px 0 0; text-align: center; color: #718096; }
</style>
