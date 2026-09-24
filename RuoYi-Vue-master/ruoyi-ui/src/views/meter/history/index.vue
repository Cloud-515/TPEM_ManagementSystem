<template>
  <div class="app-container history-page">
    <section class="page-heading">
      <div><h1>历史追溯</h1><p>选择设备与时间范围，查看原始采样记录及变化趋势</p></div>
    </section>

    <section class="query-panel">
      <el-form :model="queryParams" size="small" :inline="true" label-width="68px">
        <el-form-item label="设备">
          <el-select v-model="queryParams.meterId" filterable clearable :loading="optionsLoading" :no-data-text="optionsLoading ? '设备列表加载中' : '暂无设备'" placeholder="请选择设备" class="meter-selector" @change="handleDeviceChange">
            <el-option v-for="meter in meterOptions" :key="meter.meterId" :label="meterLabel(meter)" :value="String(meter.meterId)" />
          </el-select>
        </el-form-item>
        <el-form-item label="时间范围">
          <el-date-picker v-model="dateRange" type="datetimerange" value-format="yyyy-MM-dd HH:mm:ss" range-separator="至" start-placeholder="开始时间" end-placeholder="结束时间" :picker-options="pickerOptions" @change="handleRangeChange" />
        </el-form-item>
        <el-form-item><el-button type="primary" icon="el-icon-search" :disabled="!canQuery" @click="handleQuery">查询</el-button><el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button></el-form-item>
      </el-form>
      <div class="quick-ranges"><span>快捷时间：</span><div class="quick-range-buttons"><el-button v-for="item in quickRanges" :key="item.key" size="mini" :class="{ 'quick-range-active': selectedQuickRange === item.key }" @click="applyQuickRange(item)">{{ item.label }}</el-button></div></div>
    </section>

    <el-tabs v-model="activeCategory" @tab-click="handleCategoryChange">
      <el-tab-pane label="运行快照" name="realtime" />
      <el-tab-pane label="电能读数" name="energy" />
      <el-tab-pane label="电能质量" name="quality" />
    </el-tabs>

    <section class="trend-panel" v-loading="trendLoading">
      <div class="panel-header"><div><h3>{{ trendTitle }}</h3><p>{{ trendDescription }}</p></div><span v-if="selectedMeter" class="selected-meter">{{ meterLabel(selectedMeter) }}</span></div>
      <div ref="primaryChart" class="trend-chart" />
      <div v-if="!canQuery" class="chart-empty">请选择设备与时间范围后查询</div>
      <div v-else-if="!trendLoading && !trendPoints.length" class="chart-empty">当前条件下没有可绘制的趋势数据</div>
    </section>

    <el-alert class="history-tip" title="下方表格展示原始采集记录。趋势时间正序，明细时间倒序；历史记录不会使用当前设备状态进行推断。" type="info" :closable="false" show-icon />
    <el-table v-loading="loading" :data="historyList" border :empty-text="emptyText">
      <template v-if="activeCategory === 'realtime'">
        <el-table-column label="采集时间" prop="dataCollectTime" min-width="170" />
        <el-table-column label="总有功功率" min-width="135" align="right"><template slot-scope="scope">{{ formatMeterNumber(toKilowatts(scope.row.activePowerTotal)) }} kW</template></el-table-column>
        <el-table-column label="A / B / C 相电压" min-width="185" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.voltageA) }} / {{ formatMeterNumber(scope.row.voltageB) }} / {{ formatMeterNumber(scope.row.voltageC) }} V</template></el-table-column>
        <el-table-column label="A / B / C 相电流" min-width="185" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentA) }} / {{ formatMeterNumber(scope.row.currentB) }} / {{ formatMeterNumber(scope.row.currentC) }} A</template></el-table-column>
      </template>
      <template v-else-if="activeCategory === 'energy'">
        <el-table-column label="采集时间" prop="dataCollectTime" min-width="170" />
        <el-table-column label="正向有功" min-width="145" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.forwardActiveEnergy) }} kWh</template></el-table-column>
        <el-table-column label="反向有功" min-width="145" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.reverseActiveEnergy) }} kWh</template></el-table-column>
        <el-table-column label="正向无功" min-width="145" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.forwardReactiveEnergy) }} kvarh</template></el-table-column>
        <el-table-column label="反向无功" min-width="145" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.reverseReactiveEnergy) }} kvarh</template></el-table-column>
      </template>
      <template v-else>
        <el-table-column label="采集时间" prop="dataCollectTime" min-width="170" />
        <el-table-column label="A / B / C 相电压 THD" min-width="190" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.voltageThdA) }} / {{ formatMeterNumber(scope.row.voltageThdB) }} / {{ formatMeterNumber(scope.row.voltageThdC) }} %</template></el-table-column>
        <el-table-column label="电压不平衡度" min-width="130" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.voltageUnbalance) }} %</template></el-table-column>
        <el-table-column label="电流不平衡度" min-width="130" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentUnbalance) }} %</template></el-table-column>
      </template>
    </el-table>
    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="loadDetails" />
  </div>
</template>

<script>
import * as echarts from 'echarts'
import resize from '@/views/dashboard/mixins/resize'
import { listMeterCards, getHistoryTrend, listRealtimeHistory, listEnergyHistory, listQualityHistory } from '@/api/system/meter'
import { formatMeterNumber, toKilowatts, formatMeterLocation } from '../components/meter-utils'

const categoryInfo = {
  realtime: { title: '运行状态趋势', description: '查看设备运行参数在所选时间范围内的变化情况。' },
  energy: { title: '累计电能趋势', description: '展示累计电能读数，帮助了解设备用电变化。' },
  quality: { title: '电能质量趋势', description: '查看谐波、不平衡度与功率因数在所选时间范围内的变化情况。' }
}

export default {
  name: 'MeterHistory',
  mixins: [resize],
  data() {
    return {
      loading: false, trendLoading: false, optionsLoading: false, total: 0, historyList: [], trendPoints: [], meterOptions: [], selectedQuickRange: '', activeCategory: 'realtime', dateRange: [], primaryChart: null, trendRequestId: 0, detailRequestId: 0, detailFailed: false,
      queryParams: { pageNum: 1, pageSize: 20, meterId: undefined },
      pickerOptions: { disabledDate(time) { return time.getTime() > Date.now() } },
      quickRanges: [{ key: '1h', label: '近1小时', hours: 1 }, { key: '6h', label: '近6小时', hours: 6 }, { key: '24h', label: '近24小时', hours: 24 }, { key: '7d', label: '近7天', hours: 168 }, { key: '30d', label: '近30天', hours: 720 }]
    }
  },
  computed: {
    canQuery() { return Boolean(this.queryParams.meterId && this.isValidDateRange(this.dateRange)) },
    selectedMeter() { return this.meterOptions.find(item => String(item.meterId) === this.queryParams.meterId) },
    trendTitle() { return (categoryInfo[this.activeCategory] || {}).title || '趋势' },
    trendDescription() { return (categoryInfo[this.activeCategory] || {}).description || '' },
    emptyText() {
      if (this.detailFailed) return '明细加载失败，请重试'
      return this.canQuery ? '当前条件下没有原始采集记录' : '请选择设备与时间范围后查询'
    }
  },
  created() { this.restoreQuery(); this.loadMeterOptions().then(() => { if (this.canQuery) this.handleQuery() }) },
  mounted() { this.primaryChart = echarts.init(this.$refs.primaryChart); this.renderTrend() },
  beforeDestroy() { if (this.primaryChart) this.primaryChart.dispose() },
  watch: {
    '$route.query'(query) {
      const sameMeter = (query.meterId ? String(query.meterId) : undefined) === this.queryParams.meterId
      const sameCategory = (query.category || 'realtime') === this.activeCategory
      const sameRange = (query.beginTime || undefined) === this.dateRange[0] && (query.endTime || undefined) === this.dateRange[1]
      if (sameMeter && sameCategory && sameRange) return
      this.restoreQuery()
      if (this.canQuery) this.handleQuery()
    }
  },
  methods: {
    formatMeterNumber,
    toKilowatts,
    resize() { if (this.primaryChart) this.primaryChart.resize() },
    meterLabel(meter) { return meter ? `${meter.meterName || '未命名设备'}（${meter.meterCode || meter.slaveAddress || '--'}）· ${formatMeterLocation(meter)}` : '--' },
    loadMeterOptions() {
      this.optionsLoading = true
      const timeout = new Promise((resolve, reject) => setTimeout(() => reject(new Error('设备列表加载超时')), 10000))
      return Promise.race([listMeterCards(), timeout]).then(response => {
        const groups = response.data || {}
        const meters = [...(groups.dashboard || []), ...(groups.toolbar || [])]
        this.meterOptions = Array.from(new Map(meters.map(meter => [String(meter.meterId), meter])).values())
          .sort((a, b) => this.meterLabel(a).localeCompare(this.meterLabel(b), 'zh-CN'))
      }).catch(error => {
        this.meterOptions = []
        this.$message.error(error.message || '设备列表加载失败，请刷新页面后重试')
      }).finally(() => {
        this.optionsLoading = false
      })
    },
    applyQuickRange(item) {
      const end = new Date()
      const begin = new Date(end.getTime() - item.hours * 60 * 60 * 1000)
      this.dateRange = [this.formatDate(begin), this.formatDate(end)]
      this.selectedQuickRange = item.key
      this.handleQuery()
    },
    formatDate(date) { const pad = value => String(value).padStart(2, '0'); return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` },
    isValidDateRange(range) {
      if (!range || range.length !== 2 || !range[0] || !range[1]) return false
      const begin = new Date(range[0]).getTime()
      const end = new Date(range[1]).getTime()
      return !Number.isNaN(begin) && !Number.isNaN(end) && begin <= end && end <= Date.now()
    },
    restoreQuery() {
      const { meterId, category, beginTime, endTime } = this.$route.query
      this.queryParams.meterId = meterId ? String(meterId) : undefined
      this.activeCategory = ['energy', 'quality'].includes(category) ? category : 'realtime'
      this.dateRange = this.isValidDateRange([beginTime, endTime]) ? [beginTime, endTime] : []
      if (!this.dateRange.length) this.applyDefaultRange()
    },
    applyDefaultRange() {
      const item = this.quickRanges[2]
      const end = new Date()
      const begin = new Date(end.getTime() - item.hours * 60 * 60 * 1000)
      this.dateRange = [this.formatDate(begin), this.formatDate(end)]
      this.selectedQuickRange = item.key
    },
    handleDeviceChange() { this.handleQuery() },
    handleRangeChange() {
      this.selectedQuickRange = ''
      if (this.dateRange && this.dateRange.length === 2 && !this.isValidDateRange(this.dateRange)) {
        this.$message.warning('时间范围应在当前时间之前，且开始时间不能晚于结束时间')
        return
      }
      this.handleQuery()
    },
    handleCategoryChange() { this.handleQuery() },
    handleQuery() { if (!this.canQuery) return; this.queryParams.pageNum = 1; this.loadTrend(); this.loadDetails(); this.syncQuery() },
    buildParams() { return { meterId: this.queryParams.meterId, beginTime: this.dateRange[0], endTime: this.dateRange[1] } },
    loadTrend() {
      const requestId = ++this.trendRequestId
      this.trendLoading = true
      this.trendPoints = []
      this.renderTrend()
      getHistoryTrend(this.activeCategory, this.buildParams()).then(response => {
        if (requestId !== this.trendRequestId) return
        this.trendPoints = (response.data && response.data.points) || []
        this.renderTrend()
      }).catch(() => {
        if (requestId !== this.trendRequestId) return
        this.trendPoints = []
        this.renderTrend()
      }).finally(() => {
        if (requestId === this.trendRequestId) this.trendLoading = false
      })
    },
    // 明细也要请求归属校验（趋势那条本来就有）。没有守卫时，快速切设备/时间段/翻页，
    // 先发的旧请求后到就会覆盖新结果：表头与总数已按新条件更新，表格里却是上一台设备的记录。
    loadDetails() {
      if (!this.canQuery) return
      const requestId = ++this.detailRequestId
      this.loading = true
      this.detailFailed = false
      const request = this.activeCategory === 'energy' ? listEnergyHistory : this.activeCategory === 'quality' ? listQualityHistory : listRealtimeHistory
      request({ ...this.buildParams(), pageNum: this.queryParams.pageNum, pageSize: this.queryParams.pageSize }).then(response => {
        if (requestId !== this.detailRequestId) return
        this.historyList = response.rows || []
        this.total = response.total || 0
      }).catch(() => {
        if (requestId !== this.detailRequestId) return
        this.historyList = []
        this.total = 0
        this.detailFailed = true
      }).finally(() => {
        if (requestId === this.detailRequestId) this.loading = false
      })
    },
    renderTrend() {
      if (!this.primaryChart) return
      const labels = this.trendPoints.map(item => item.dataCollectTime)
      const series = this.getSeries()
      const unit = this.activeCategory === 'energy' ? 'kWh' : this.activeCategory === 'quality' ? '%' : 'kW'
      this.primaryChart.setOption({ tooltip: { trigger: 'axis', valueFormatter: value => value == null ? '--' : `${value} ${unit}` }, legend: { top: 4, data: series.map(item => item.name) }, grid: { containLabel: true, left: 64, right: 32, top: 46, bottom: 54 }, xAxis: { type: 'category', data: labels, axisLabel: { color: '#718096', hideOverlap: true, formatter: value => value ? value.replace('T', ' ').slice(5, 16) : '--' }, axisLine: { lineStyle: { color: '#d9e2ec' } } }, yAxis: { type: 'value', name: unit, nameLocation: 'end', nameGap: 12, nameTextStyle: { color: '#718096' }, axisLabel: { color: '#718096' }, splitLine: { lineStyle: { color: '#edf2f7' } } }, series }, true)
    },
    getSeries() {
      const line = (name, data, color) => ({ name, type: 'line', smooth: true, connectNulls: false, symbol: 'none', lineStyle: { width: 2, color }, data })
      if (this.activeCategory === 'energy') return [line('正向有功累计 kWh', this.trendPoints.map(item => item.forwardActiveEnergy), '#1677a8')]
      if (this.activeCategory === 'quality') return [line('功率因数', this.trendPoints.map(item => item.powerFactorTotal), '#1677a8'), line('电压 THD A', this.trendPoints.map(item => item.voltageThdA), '#d98b1d'), line('电流 THD A', this.trendPoints.map(item => item.currentThdA), '#c94747'), line('电压不平衡', this.trendPoints.map(item => item.voltageUnbalance), '#6c63b5'), line('电流不平衡', this.trendPoints.map(item => item.currentUnbalance), '#2f9e6f')]
      return [line('总有功功率 kW', this.trendPoints.map(item => this.toKilowatts(item.activePowerTotal)), '#1677a8')]
    },
    syncQuery() {
      const query = { meterId: this.queryParams.meterId, category: this.activeCategory, beginTime: this.dateRange[0], endTime: this.dateRange[1] }
      if (JSON.stringify(this.$route.query) !== JSON.stringify(query)) this.$router.replace({ query }).catch(() => {})
    },
    resetQuery() {
      this.queryParams.meterId = undefined
      this.queryParams.pageNum = 1
      this.historyList = []
      this.trendPoints = []
      this.total = 0
      this.applyDefaultRange()
      this.renderTrend()
      this.$router.replace({ query: {} }).catch(() => {})
    }
  }
}
</script>

<style scoped lang="scss">
.page-heading { margin-bottom: 18px; }.page-heading h1 { margin: 0 0 4px; color: #1f2937; font-size: 24px; font-weight: 600; }.page-heading p, .panel-header p { margin: 6px 0 0; color: #718096; font-size: 13px; }.query-panel { padding: 16px 16px 10px; border: 1px solid #e3edf3; border-radius: 8px; background: #fafcff; }.meter-selector { width: 330px; }.quick-ranges { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; margin-top: 2px; color: #718096; font-size: 12px; }.quick-range-buttons { display: inline-flex; flex-wrap: wrap; gap: 6px; }.quick-range-buttons .el-button { min-width: 68px; height: 28px; margin: 0; padding: 0 10px; border-color: #d9e2ec; color: #606266; background: #fff; }.quick-range-buttons .el-button:hover { color: #1677a8; border-color: #1677a8; background: #fff; }.quick-range-buttons ::v-deep .quick-range-active, .quick-range-buttons ::v-deep .quick-range-active:hover { color: #fff; border-color: #1677a8; background: #1677a8; }.trend-panel { position: relative; min-height: 325px; padding: 18px; border: 1px solid #e3edf3; border-radius: 8px; }.panel-header { display: flex; justify-content: space-between; gap: 12px; }.panel-header h3 { margin: 0; color: #172b4d; font-size: 15px; }.selected-meter { color: #718096; font-size: 12px; text-align: right; }.trend-chart { width: 100%; height: 245px; }.chart-empty { position: absolute; top: 60%; left: 50%; color: #94a3b8; font-size: 13px; transform: translate(-50%, -50%); }.history-tip { margin: 18px 0; }.history-page ::v-deep .el-tabs__header { margin: 18px 0 12px; }@media (max-width: 768px) { .meter-selector { width: 100%; }.query-panel ::v-deep .el-form-item { display: block; }.selected-meter { display: none; } }
</style>
