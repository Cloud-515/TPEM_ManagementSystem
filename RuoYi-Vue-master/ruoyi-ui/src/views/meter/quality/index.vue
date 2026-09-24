<template>
  <div class="app-container quality-page">
    <section class="page-heading"><div><h1>电能质量</h1><p>基于最新采集数据识别电压、功率因数、谐波与不平衡风险</p></div><el-switch v-model="showAll" active-text="显示全部设备" @change="handleQuery" /></section>
    <!-- 用 auto-fit 自适应栅格：el-col 只写 :xs/:md 时，768~991px 之间没有规则命中会退化成一行一张 -->
    <div class="tpem-card-row risk-row">
      <div v-for="item in riskKpis" :key="item.label" class="tpem-card" :class="[item.tone, { 'is-warn': item.warn }]"><div class="tpem-card-head"><i class="tpem-icon" :class="item.icon"></i><span>{{ item.label }}</span></div><b class="tpem-value">{{ item.value }}<small v-if="item.value !== '--'">台</small></b></div>
    </div>
    <el-form ref="queryForm" :model="queryParams" size="small" :inline="true" label-width="68px">
      <el-form-item label="设备名称" prop="meterName"><el-input v-model="queryParams.meterName" placeholder="请输入设备名称" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item label="站点" prop="siteName"><el-input v-model="queryParams.siteName" placeholder="请输入站点" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item label="箱体" prop="boxName"><el-input v-model="queryParams.boxName" placeholder="请输入箱体" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item><el-button type="primary" icon="el-icon-search" @click="handleQuery">搜索</el-button><el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button></el-form-item>
    </el-form>
    <el-table v-loading="loading" :data="meterList" border :empty-text="emptyText">
      <el-table-column label="设备名称" prop="meterName" min-width="160" show-overflow-tooltip />
      <el-table-column label="位置" min-width="180"><template slot-scope="scope">{{ formatMeterLocation(scope.row) }}</template></el-table-column>
      <el-table-column label="风险类型" width="145"><template slot-scope="scope"><el-tag size="mini" :type="riskTagType(scope.row)">{{ riskLabel(scope.row) }}</el-tag></template></el-table-column>
      <el-table-column label="总功率因数" width="125" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.powerFactorTotal, 3) }}</template></el-table-column>
      <el-table-column label="A / B / C 电压 THD" min-width="190" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.voltageThdA) }} / {{ formatMeterNumber(scope.row.voltageThdB) }} / {{ formatMeterNumber(scope.row.voltageThdC) }} %</template></el-table-column>
      <el-table-column label="A / B / C 电流 THD" min-width="190" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.currentThdA) }} / {{ formatMeterNumber(scope.row.currentThdB) }} / {{ formatMeterNumber(scope.row.currentThdC) }} %</template></el-table-column>
      <el-table-column label="电压/电流不平衡度" min-width="160" align="right"><template slot-scope="scope">{{ formatMeterNumber(scope.row.voltageUnbalance) }} / {{ formatMeterNumber(scope.row.currentUnbalance) }} %</template></el-table-column>
      <el-table-column label="最新采集时间" prop="lastCollectTime" min-width="165" />
      <el-table-column label="操作" width="80" fixed="right" align="center"><template slot-scope="scope"><el-button type="text" size="mini" @click="showDetail(scope.row)">详情</el-button></template></el-table-column>
    </el-table>
    <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" />
    <realtime-detail ref="detail" />
  </div>
</template>

<script>
import { listQualityMeters, getQualityMeterStats } from '@/api/system/meter'
import RealtimeDetail from '../components/realtime-detail'
import { formatMeterNumber, getMeterQualityRisks, formatMeterLocation } from '../components/meter-utils'

export default {
  name: 'MeterQuality', components: { RealtimeDetail },
  data() {
    const showAll = this.$route.query.showAll === 'true'
    return { loading: false, total: 0, meterList: [], showAll, stats: null, listFailed: false, queryParams: { pageNum: 1, pageSize: 20, meterName: undefined, siteName: undefined, boxName: undefined, riskOnly: !showAll } }
  },
  computed: {
    // 统计卡：统一走 .tpem-card（图标 + 顶部主题色），有异常就把整卡标黄
    riskKpis() {
      const count = key => (this.stats ? (this.stats[key] || 0) : '--')
      const value = key => count(key)
      return [
        { label: '状态异常', icon: 'el-icon-warning-outline', tone: 'is-amber', value: value('statusAbnormalCount'), warn: Number(value('statusAbnormalCount')) > 0 },
        { label: '功率因数低', icon: 'el-icon-pie-chart', tone: 'is-teal', value: value('powerFactorLowCount'), warn: Number(value('powerFactorLowCount')) > 0 },
        { label: 'THD 超限', icon: 'el-icon-data-line', tone: 'is-violet', value: value('thdExceededCount'), warn: Number(value('thdExceededCount')) > 0 },
        { label: '不平衡超限', icon: 'el-icon-odometer', tone: 'is-slate', value: value('unbalanceExceededCount'), warn: Number(value('unbalanceExceededCount')) > 0 }
      ]
    },
    emptyText() {
      if (this.listFailed) return '加载失败，请点「搜索」重试'
      return this.showAll ? '暂无电能质量数据' : '当前没有识别到质量风险设备'
    }
  },
  created() { this.getList() },
  mounted() { if (this.$route.query.meterId) this.openRouteMeter(this.$route.query.meterId) },
  watch: { '$route.query.meterId'(meterId) { if (meterId) this.openRouteMeter(meterId) } },
  methods: {
    formatMeterNumber,
    formatMeterLocation,
    hasRisk(item) { return getMeterQualityRisks(item).length > 0 },
    riskLabel(item) { return getMeterQualityRisks(item).join('、') || '正常' },
    riskTagType(item) { return getMeterQualityRisks(item).some(risk => risk === '设备状态异常' || risk === '电压越限') ? 'danger' : getMeterQualityRisks(item).length ? 'warning' : 'success' },
    getList() {
      this.loading = true
      this.listFailed = false
      const statsQuery = { meterName: this.queryParams.meterName, siteName: this.queryParams.siteName, boxName: this.queryParams.boxName }
      // 列表与统计是两个互不依赖的请求，不能合成一个失败域：
      // 原来用 Promise.all，统计接口一挂就把列表也清空，页面显示成"当前没有识别到质量风险设备"。
      listQualityMeters(this.queryParams).then(response => {
        this.meterList = response.rows || []
        this.total = response.total || 0
      }).catch(() => {
        this.meterList = []
        this.total = 0
        this.listFailed = true
      }).finally(() => { this.loading = false })
      getQualityMeterStats(statsQuery).then(statsResponse => {
        this.stats = statsResponse.data || statsResponse || {}
      }).catch(() => { this.stats = null })
    },
    handleQuery() {
      this.queryParams.riskOnly = !this.showAll
      this.queryParams.pageNum = 1
      const query = { ...this.$route.query }
      if (this.showAll) query.showAll = 'true'
      else delete query.showAll
      this.$router.replace({ name: 'MeterQuality', query })
      this.getList()
    },
    // 重置要真的清空输入框：resetForm 只重置带 prop 的 el-form-item，三项原本都没有 prop，点了等于空转
    resetQuery() {
      this.resetForm('queryForm')
      this.showAll = false
      this.handleQuery()
    },
    // 只改路由、由 watcher 统一打开抽屉：两处都 open 会为同一台设备发两次详情请求。
    // 但路由值没变化时 watcher 不会触发，所以同一行再点一次要在这里直接打开。
    showDetail(row) {
      const current = this.$route.query.meterId
      if (current !== undefined && String(current) === String(row.meterId)) return this.openRouteMeter(row.meterId)
      this.$router.replace({ name: 'MeterQuality', query: { ...this.$route.query, meterId: row.meterId } })
    },
    openRouteMeter(meterId) { this.$nextTick(() => this.$refs.detail.open(Number(meterId), 'quality')) }
  }
}
</script>

<style scoped lang="scss">
.page-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }.page-heading h1 { margin: 0 0 4px; color: #1f2937; font-size: 24px; font-weight: 600; }.page-heading p { margin: 6px 0 0; color: #718096; font-size: 13px; }.risk-row { margin-bottom: 16px; }
</style>
