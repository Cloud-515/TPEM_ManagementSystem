<template>
  <div class="energy-dashboard" v-loading="loading">
    <section class="dashboard-header">
      <div>
        <div class="eyebrow">ENERGY OPERATIONS</div>
        <h1>飞毛腿能源科技智能管理中心</h1>
        <p>全局设备状态、实时负荷与用能趋势一览</p>
      </div>
      <div class="header-actions">
        <span class="update-time">数据更新：{{ generatedAt || '--' }}</span>
        <el-button icon="el-icon-refresh" size="small" @click="refreshDashboard">刷新数据</el-button>
      </div>
    </section>

    <section class="metric-grid">
      <button v-for="metric in metrics" :key="metric.label" type="button" class="metric-card" :class="metric.type" @click="openMetric(metric)">
        <div class="metric-icon"><i :class="metric.icon" /></div>
        <div class="metric-content">
          <div class="metric-label">{{ metric.label }}</div>
          <div class="metric-value">{{ metric.value }}<small v-if="metric.unit">{{ metric.unit }}</small></div>
          <div class="metric-note">{{ metric.note }}</div>
          <div class="metric-link">查看详情 <i class="el-icon-arrow-right" /></div>
        </div>
      </button>
    </section>

    <el-row :gutter="20" class="dashboard-row">
      <el-col :xs="24" :lg="16">
        <section class="panel trend-panel">
          <div class="panel-header">
            <div>
              <h2>能耗趋势</h2>
              <p>按仪表累计正向有功电能计算</p>
            </div>
            <el-radio-group v-model="trendRange" size="small" @change="loadTrend">
              <el-radio-button label="24h">24小时</el-radio-button>
              <el-radio-button label="7d">7天</el-radio-button>
              <el-radio-button label="30d">30天</el-radio-button>
            </el-radio-group>
          </div>
          <div ref="trendChart" class="chart trend-chart" />
          <div v-if="!trendHasData && !trendLoading" class="chart-empty">{{ trendError || '当前范围内没有可用于计算能耗趋势的完整读数' }}</div>
        </section>
      </el-col>
      <el-col :xs="24" :lg="8">
        <section class="panel health-panel">
          <div class="panel-header">
            <div>
              <h2>设备健康度</h2>
              <p>点击状态筛选异常设备</p>
            </div>
          </div>
          <div ref="healthChart" class="chart health-chart" />
          <div class="status-legend">
            <button v-for="item in statusDistribution" :key="item.code" class="legend-item" :class="{ active: statusFilter === item.code }" @click="toggleStatusFilter(item.code)">
              <span class="legend-dot" :style="{ background: item.color }" />
              <span>{{ item.label }}</span><strong>{{ item.value }}</strong>
            </button>
          </div>
        </section>
      </el-col>
    </el-row>

    <el-row :gutter="20" class="dashboard-row">
      <el-col :xs="24" :lg="12">
        <section class="panel">
          <div class="panel-header">
            <div><h2>实时负荷 Top 10</h2><p>当前有功功率最高的仪表</p></div>
            <span class="unit-label">单位：kW</span>
          </div>
          <div ref="loadChart" class="chart load-chart" />
          <div v-if="!topLoadMeters.length" class="chart-empty">暂无有效实时负荷数据</div>
        </section>
      </el-col>
      <el-col :xs="24" :lg="12">
        <section class="panel fault-panel">
          <div class="panel-header">
            <div><h2>报警故障</h2><p>按严重程度展示当前设备报警与故障</p></div>
            <div>
              <span class="unit-label">{{ exceptions.length }} 条</span>
              <el-button type="text" @click="openExceptionDevices">显示更多</el-button>
            </div>
          </div>
          <div class="fault-list">
            <div v-for="fault in exceptions.slice(0, 5)" :key="fault.meterId" class="fault-item">
              <span class="fault-dot" :class="fault.statusCode" />
              <div class="fault-info">
                <strong>{{ fault.meterName || '未命名设备' }}</strong>
                <span>{{ getStatusLabel(fault.statusCode) }} · {{ fault.siteName || '--' }} / {{ fault.boxName || '--' }}</span>
              </div>
              <el-button type="text" size="mini" @click="openMeter(fault)">详情</el-button>
            </div>
            <div v-if="!exceptions.length" class="fault-empty">当前没有报警或故障</div>
          </div>
        </section>
      </el-col>
    </el-row>

    <section class="panel exception-panel">
      <div class="panel-header">
        <div><h2>异常设备</h2><p>{{ statusFilter ? '已按状态筛选' : '按严重程度展示最新采集异常' }}</p></div>
        <div>
          <el-button v-if="statusFilter" type="text" @click="statusFilter = ''">清除筛选</el-button>
          <el-button type="text" @click="openExceptionDevices">显示更多</el-button>
        </div>
      </div>
      <el-table :data="visibleExceptions" size="small" :empty-text="'当前没有异常设备'">
        <el-table-column prop="meterName" label="设备名称" min-width="160" />
        <el-table-column label="位置" min-width="180"><template slot-scope="scope">{{ scope.row.siteName || '--' }} / {{ scope.row.boxName || '--' }}</template></el-table-column>
        <el-table-column label="当前负荷" width="125"><template slot-scope="scope">{{ formatNumber(scope.row.activePowerKw) }} kW</template></el-table-column>
        <el-table-column label="状态" width="130"><template slot-scope="scope"><el-tag size="mini" :type="statusTagType(scope.row.statusCode)">{{ getStatusLabel(scope.row.statusCode) }}</el-tag></template></el-table-column>
        <el-table-column prop="lastCollectTime" label="最后采集时间" min-width="165" />
        <el-table-column label="操作" width="90" align="right"><template slot-scope="scope"><el-button type="text" size="mini" @click="openMeter(scope.row)">详情</el-button></template></el-table-column>
      </el-table>
    </section>

  </div>
</template>

<script>
import * as echarts from 'echarts'
import resize from './dashboard/mixins/resize'
import { listMeterCards, getDashboardEnergyTrend } from '@/api/system/meter'

const statusMeta = [
  { code: 'OK', label: '正常', color: '#2f9e6f', tag: 'success' },
  { code: 'PF_LOW', label: '功率因数低', color: '#d98b1d', tag: 'warning' },
  { code: 'VOLTAGE_BAD', label: '电压异常', color: '#e06c3b', tag: 'warning' },
  { code: 'FAULT', label: '通信故障', color: '#c94747', tag: 'danger' },
  { code: 'NODATA', label: '无数据', color: '#77879a', tag: 'info' },
  { code: 'WAITING', label: '等待采集', color: '#9aabbc', tag: 'info' }
]

export default {
  name: 'Index',
  mixins: [resize],
  data() {
    return {
      loading: false,
      trendLoading: false,
      meters: [],
      generatedAt: '',
      trendRange: '24h',
      trendPoints: [],
      trendError: '',
      statusFilter: '',
      trendChart: null,
      loadChart: null,
      healthChart: null,
      refreshTimer: null
    }
  },
  computed: {
    metrics() {
      const total = this.meters.length
      const normal = this.meters.filter(item => item.statusCode === 'OK').length
      const offline = this.meters.filter(item => ['FAULT', 'NODATA', 'WAITING'].includes(item.statusCode)).length
      const powerValues = this.meters.map(item => Number(item.activePowerKw)).filter(value => Number.isFinite(value) && value >= 0)
      const power = powerValues.length ? powerValues.reduce((sum, value) => sum + value, 0) : null
      return [
        { label: '当前总负荷', value: this.formatNumber(power), unit: 'kW', note: power === null ? '暂无有效实时功率数据' : '所有有效仪表实时功率汇总', icon: 'el-icon-data-line', type: 'primary', routeName: 'MeterEnergy' },
        { label: '监测设备', value: total, unit: '台', note: `正常运行 ${normal} 台`, icon: 'el-icon-monitor', type: 'blue', routeName: 'MeterRealtime' },
        { label: '异常设备', value: total - normal, unit: '台', note: `离线或待数据 ${offline} 台`, icon: 'el-icon-warning-outline', type: 'warning', routeName: 'MeterExceptionDevices' },
        { label: '报警故障', value: this.exceptions.length, unit: '条', note: '当前设备报警与故障数量', icon: 'el-icon-bell', type: 'danger', routeName: 'MeterExceptionDevices' }
      ]
    },
    statusDistribution() {
      return statusMeta.map(meta => ({ ...meta, value: this.meters.filter(item => item.statusCode === meta.code).length }))
    },
    topLoadMeters() {
      return this.meters.filter(item => Number.isFinite(Number(item.activePowerKw)) && Number(item.activePowerKw) >= 0).sort((a, b) => Number(b.activePowerKw) - Number(a.activePowerKw)).slice(0, 10)
    },
    exceptions() {
      const severity = { FAULT: 0, VOLTAGE_BAD: 1, PF_LOW: 2, NODATA: 3, WAITING: 4 }
      return this.meters.filter(item => item.statusCode !== 'OK').sort((a, b) => severity[a.statusCode] - severity[b.statusCode])
    },
    visibleExceptions() {
      const exceptions = this.statusFilter ? this.exceptions.filter(item => item.statusCode === this.statusFilter) : this.exceptions
      return exceptions.slice(0, 5)
    },
    trendHasData() {
      return this.trendPoints.some(item => item.consumptionKwh !== null && item.consumptionKwh !== undefined)
    }
  },
  mounted() {
    this.initCharts()
    this.refreshDashboard()
    this.refreshTimer = setInterval(this.loadSnapshot, 15000)
  },
  beforeDestroy() {
    clearInterval(this.refreshTimer)
    ;[this.trendChart, this.loadChart, this.healthChart].forEach(chart => chart && chart.dispose())
  },
  methods: {
    initCharts() {
      this.trendChart = echarts.init(this.$refs.trendChart)
      this.loadChart = echarts.init(this.$refs.loadChart)
      this.healthChart = echarts.init(this.$refs.healthChart)
      this.renderCharts()
    },
    resize() {
      ;[this.trendChart, this.loadChart, this.healthChart].forEach(chart => chart && chart.resize())
    },
    async refreshDashboard() {
      await Promise.all([this.loadSnapshot(), this.loadTrend()])
    },
    async loadSnapshot() {
      this.loading = true
      try {
        const response = await listMeterCards()
        const data = response.data || {}
        this.meters = data.dashboard || []
        this.generatedAt = data.generatedAt || ''
        this.renderLoadChart()
        this.renderHealthChart()
      } catch (error) {
        this.meters = []
        this.generatedAt = ''
        this.renderLoadChart()
        this.renderHealthChart()
      } finally {
        this.loading = false
      }
    },
    async loadTrend() {
      this.trendLoading = true
      this.trendError = ''
      try {
        const response = await getDashboardEnergyTrend(this.trendRange)
        this.trendPoints = response.data || []
      } catch (error) {
        this.trendPoints = []
        this.trendError = '能耗趋势数据加载失败'
      } finally {
        this.renderTrendChart()
        this.trendLoading = false
      }
    },
    renderCharts() {
      this.renderTrendChart()
      this.renderLoadChart()
      this.renderHealthChart()
    },
    renderTrendChart() {
      if (!this.trendChart) return
      this.trendChart.setOption({
        color: ['#1677a8'],
        tooltip: { trigger: 'axis', valueFormatter: value => value == null ? '暂无完整读数' : `${value} kWh` },
        grid: { containLabel: true, left: 64, right: 30, top: 28, bottom: 48 },
        xAxis: { type: 'category', boundaryGap: false, data: this.trendPoints.map(item => item.label), axisLine: { lineStyle: { color: '#d9e2ec' } }, axisLabel: { color: '#718096', hideOverlap: true, formatter: value => value.length > 10 ? value.slice(5, 16) : value } },
        yAxis: { type: 'value', name: 'kWh', nameLocation: 'end', nameGap: 12, nameTextStyle: { color: '#718096' }, splitLine: { lineStyle: { color: '#edf2f7' } }, axisLabel: { color: '#718096' } },
        series: [{ name: '用电量', type: 'line', smooth: true, connectNulls: false, symbol: 'circle', symbolSize: 6, lineStyle: { width: 2 }, areaStyle: { color: 'rgba(22,119,168,.14)' }, data: this.trendPoints.map(item => item.consumptionKwh) }]
      }, true)
    },
    renderLoadChart() {
      if (!this.loadChart) return
      const data = [...this.topLoadMeters].reverse()
      this.loadChart.setOption({
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: params => { const meter = data[params[0].dataIndex]; return `${meter.meterName}<br/>${meter.siteName || '--'} / ${meter.boxName || '--'}<br/>当前负荷：${this.formatNumber(meter.activePowerKw)} kW<br/>状态：${this.getStatusLabel(meter.statusCode)}` } },
        grid: { containLabel: true, left: 122, right: 40, top: 16, bottom: 30 },
        xAxis: { type: 'value', axisLabel: { color: '#718096' }, splitLine: { lineStyle: { color: '#edf2f7' } } },
        yAxis: { type: 'category', data: data.map(item => item.meterName), axisLabel: { color: '#475569', width: 105, overflow: 'truncate' }, axisLine: { show: false }, axisTick: { show: false } },
        series: [{ name: '当前负荷', type: 'bar', barWidth: 14, itemStyle: { color: '#2586b6', borderRadius: [0, 7, 7, 0] }, data: data.map(item => Number(item.activePowerKw).toFixed(2)) }]
      }, true)
      this.loadChart.off('click')
      this.loadChart.on('click', params => this.openMeter(data[params.dataIndex]))
    },
    renderHealthChart() {
      if (!this.healthChart) return
      this.healthChart.setOption({
        tooltip: { trigger: 'item', formatter: '{b}<br/>{c} 台（{d}%）' },
        title: { text: `${this.meters.length}`, subtext: '监测设备', left: 'center', top: '36%', textStyle: { fontSize: 26, fontWeight: 600, color: '#1e293b' }, subtextStyle: { color: '#718096' } },
        series: [{ type: 'pie', radius: ['58%', '78%'], center: ['50%', '50%'], avoidLabelOverlap: true, label: { show: false }, itemStyle: { borderColor: '#fff', borderWidth: 3 }, data: this.statusDistribution.filter(item => item.value > 0).map(item => ({ name: item.label, value: item.value, itemStyle: { color: item.color }, code: item.code })) }]
      }, true)
      this.healthChart.off('click')
      this.healthChart.on('click', params => this.toggleStatusFilter(params.data.code))
    },
    toggleStatusFilter(code) {
      this.statusFilter = this.statusFilter === code ? '' : code
    },
    getStatusLabel(code) {
      const item = statusMeta.find(meta => meta.code === code)
      return item ? item.label : '状态未知'
    },
    statusTagType(code) {
      const item = statusMeta.find(meta => meta.code === code)
      return item ? item.tag : 'info'
    },
    formatNumber(value, digits = 2) {
      const number = Number(value)
      return Number.isFinite(number) ? number.toLocaleString('zh-CN', { maximumFractionDigits: digits }) : '--'
    },
    openMeter(meter) {
      if (meter && meter.meterId) this.$router.push({ name: 'MeterAlarmRecord', query: { meterId: meter.meterId } })
    },
    openExceptionDevices() {
      this.$router.push({ name: 'MeterExceptionDevices' })
    },
    openMetric(metric) {
      if (metric && metric.routeName) this.$router.push({ name: metric.routeName })
    }
  }
}
</script>

<style lang="scss" scoped>
.energy-dashboard { min-height: 100%; padding: 24px; background: #f5f7fa; color: #1e293b; }
.dashboard-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 22px; }
.eyebrow { color: #1677a8; font-size: 11px; font-weight: 700; letter-spacing: 1.4px; }
h1 { margin: 5px 0; font-size: 26px; font-weight: 650; } h2 { margin: 0; font-size: 16px; } p { margin: 5px 0 0; color: #718096; font-size: 13px; }
.header-actions { display: flex; align-items: center; gap: 14px; } .update-time { color: #718096; font-size: 12px; }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; margin-bottom: 20px; }
.metric-card { display: flex; width: 100%; min-height: 112px; align-items: center; gap: 14px; padding: 18px; color: inherit; border: 1px solid #e8eef4; border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(15, 23, 42, .035); cursor: pointer; font: inherit; text-align: left; transition: transform .18s ease, box-shadow .18s ease, border-color .18s ease; }
.metric-card:hover { border-color: #b9d8e7; box-shadow: 0 8px 18px rgba(15, 23, 42, .09); transform: translateY(-2px); }.metric-card:focus-visible { outline: 3px solid rgba(22, 119, 168, .32); outline-offset: 2px; }
.metric-content { min-width: 0; }.metric-icon { display: grid; width: 42px; height: 42px; place-items: center; border-radius: 10px; font-size: 21px; } .primary .metric-icon { color: #1677a8; background: #e5f3f9; } .blue .metric-icon { color: #406cc3; background: #edf1ff; } .warning .metric-icon { color: #ce7a11; background: #fff3df; } .danger .metric-icon { color: #c94747; background: #fdebec; }
.metric-label, .metric-note { color: #718096; font-size: 12px; } .metric-value { margin: 3px 0; font-size: 25px; font-weight: 650; letter-spacing: -.5px; } .metric-value small { margin-left: 4px; font-size: 12px; font-weight: 500; color: #718096; }.metric-link { margin-top: 7px; color: #1677a8; font-size: 12px; font-weight: 600; }
.dashboard-row { margin-bottom: 20px; } .panel { position: relative; min-height: 344px; padding: 20px; background: #fff; border: 1px solid #e8eef4; border-radius: 10px; box-shadow: 0 2px 8px rgba(15, 23, 42, .035); } .panel-header { display: flex; justify-content: space-between; align-items: flex-start; min-height: 43px; }.chart { width: 100%; }.trend-chart { height: 258px; }.health-chart { height: 206px; }.load-chart { height: 262px; }.chart-empty { position: absolute; top: 56%; left: 50%; color: #94a3b8; font-size: 13px; transform: translate(-50%, -50%); }.unit-label { color: #718096; font-size: 12px; }
.status-legend { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 5px 10px; }.legend-item { display: flex; align-items: center; gap: 6px; padding: 3px 0; color: #64748b; border: 0; background: transparent; cursor: pointer; text-align: left; }.legend-item.active { color: #1677a8; font-weight: 600; }.legend-item strong { margin-left: auto; color: #334155; }.legend-dot { width: 8px; height: 8px; border-radius: 50%; }
.fault-panel { min-height: 344px; }.fault-list { margin-top: 8px; }.fault-item { display: flex; align-items: center; gap: 10px; padding: 11px 0; border-bottom: 1px solid #edf2f7; }.fault-dot { flex: 0 0 8px; width: 8px; height: 8px; border-radius: 50%; background: #c94747; }.fault-dot.PF_LOW, .fault-dot.VOLTAGE_BAD { background: #d98b1d; }.fault-dot.NODATA, .fault-dot.WAITING { background: #77879a; }.fault-info { display: flex; flex: 1; min-width: 0; flex-direction: column; gap: 4px; }.fault-info strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 13px; }.fault-info span { overflow: hidden; color: #718096; text-overflow: ellipsis; white-space: nowrap; font-size: 12px; }.fault-empty { padding: 48px 0; color: #94a3b8; text-align: center; font-size: 13px; }
.exception-panel { min-height: auto; }.exception-panel ::v-deep .el-table::before { display: none; }
@media (max-width: 992px) { .metric-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } .dashboard-row .el-col + .el-col { margin-top: 20px; } }
@media (max-width: 576px) { .energy-dashboard { padding: 14px; }.dashboard-header { align-items: flex-start; flex-direction: column; gap: 12px; }.header-actions { width: 100%; justify-content: space-between; }.metric-grid { grid-template-columns: 1fr; }.panel { padding: 15px; }.panel-header { gap: 8px; }.panel-header .el-radio-group { white-space: nowrap; } }
</style>
