<template>
  <div class="app-container quality-page">
    <section class="page-heading"><div><h2>电能质量</h2><p>基于最新采集数据识别电压、功率因数、谐波与不平衡风险</p></div><el-switch v-model="showAll" active-text="显示全部设备" @change="handleQuery" /></section>
    <el-row :gutter="16" class="risk-row"><el-col v-for="item in riskKpis" :key="item.label" :xs="12" :md="6"><div class="risk-kpi" :class="item.type"><span>{{ item.label }}</span><strong>{{ item.value }}</strong><small>台</small></div></el-col></el-row>
    <el-form ref="queryForm" :model="queryParams" size="small" :inline="true" label-width="68px">
      <el-form-item label="设备名称"><el-input v-model="queryParams.meterName" placeholder="请输入设备名称" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item label="站点"><el-input v-model="queryParams.siteName" placeholder="请输入站点" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item label="箱体"><el-input v-model="queryParams.boxName" placeholder="请输入箱体" clearable @keyup.enter.native="handleQuery" /></el-form-item>
      <el-form-item><el-button type="primary" icon="el-icon-search" @click="handleQuery">搜索</el-button><el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button></el-form-item>
    </el-form>
    <el-table v-loading="loading" :data="filteredList" border :empty-text="showAll ? '暂无电能质量数据' : '当前没有识别到质量风险设备'">
      <el-table-column label="设备名称" prop="meterName" min-width="160" show-overflow-tooltip />
      <el-table-column label="位置" min-width="180"><template slot-scope="scope">{{ scope.row.siteName || '--' }} / {{ scope.row.boxName || '--' }}</template></el-table-column>
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
import { listQualityMeters } from '@/api/system/meter'
import RealtimeDetail from '../components/realtime-detail'
import { formatMeterNumber } from '../components/meter-utils'

export default {
  name: 'MeterQuality', components: { RealtimeDetail },
  data() { return { loading: false, total: 0, meterList: [], showAll: false, queryParams: { pageNum: 1, pageSize: 20, meterName: undefined, siteName: undefined, boxName: undefined } } },
  computed: {
    filteredList() { return this.showAll ? this.meterList : this.meterList.filter(item => item.statusCode === 'VOLTAGE_BAD' || item.statusCode === 'PF_LOW') },
    riskKpis() {
      const list = this.meterList
      return [
        { label: '电压异常', value: list.filter(item => item.statusCode === 'VOLTAGE_BAD').length, type: 'danger' },
        { label: '功率因数低', value: list.filter(item => item.statusCode === 'PF_LOW').length, type: 'warning' },
        { label: 'THD 待评估', value: list.filter(item => this.hasThd(item)).length, type: 'warning' },
        { label: '不平衡待评估', value: list.filter(item => Number(item.voltageUnbalance) > 0 || Number(item.currentUnbalance) > 0).length, type: 'info' }
      ]
    }
  },
  created() { this.getList() },
  methods: {
    formatMeterNumber,
    hasRisk(item) { return item.statusCode === 'VOLTAGE_BAD' || item.statusCode === 'PF_LOW' },
    hasThd(item) {
      return ['voltageThdA', 'voltageThdB', 'voltageThdC', 'currentThdA', 'currentThdB', 'currentThdC']
        .some(field => Number.isFinite(Number(item[field])) && Number(item[field]) > 0)
    },
    riskLabel(item) { if (item.statusCode === 'VOLTAGE_BAD') return '电压异常'; if (item.statusCode === 'PF_LOW') return '功率因数低'; return '待评估' },
    riskTagType(item) { return item.statusCode === 'VOLTAGE_BAD' ? 'danger' : item.statusCode === 'PF_LOW' ? 'warning' : 'info' },
    getList() { this.loading = true; listQualityMeters(this.queryParams).then(response => { this.meterList = response.rows || []; this.total = response.total || 0 }).catch(() => { this.meterList = []; this.total = 0 }).finally(() => { this.loading = false }) },
    handleQuery() { this.queryParams.pageNum = 1; this.getList() },
    resetQuery() { this.resetForm('queryForm'); this.handleQuery() },
    showDetail(row) { this.$refs.detail.open(row.meterId, 'quality') }
  }
}
</script>

<style scoped lang="scss">
.page-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }.page-heading h2 { margin: 0; color: #172b4d; font-size: 20px; }.page-heading p { margin: 6px 0 0; color: #718096; font-size: 13px; }.risk-row { margin-bottom: 16px; }.risk-kpi { padding: 15px; border: 1px solid #e3edf3; border-left: 4px solid #7d92a8; border-radius: 7px; background: #fafcff; }.risk-kpi.warning { border-left-color: #d98b1d; }.risk-kpi.danger { border-left-color: #c94747; }.risk-kpi span { color: #718096; font-size: 12px; }.risk-kpi strong { margin: 0 5px 0 10px; color: #263a54; font-size: 24px; }.risk-kpi small { color: #718096; }
</style>
