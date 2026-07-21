<template>
  <div class="app-container heat-pump-monitor">
    <el-row :gutter="16" class="summary-row">
      <el-col :xs="12" :sm="6"><div class="summary-item"><span>模块总数</span><strong>{{ overview.total_count || 0 }}</strong></div></el-col>
      <el-col :xs="12" :sm="6"><div class="summary-item online"><span>在线模块</span><strong>{{ overview.online_count || 0 }}</strong></div></el-col>
      <el-col :xs="12" :sm="6"><div class="summary-item offline"><span>离线模块</span><strong>{{ overview.offline_count || 0 }}</strong></div></el-col>
      <el-col :xs="12" :sm="6"><div class="summary-item alarm"><span>活动告警</span><strong>{{ overview.alarm_count || 0 }}</strong></div></el-col>
    </el-row>

    <el-form :inline="true" class="query-form">
      <el-form-item label="站点"><el-input v-model="query.siteCode" clearable placeholder="站点编码" @keyup.enter.native="refresh" /></el-form-item>
      <el-form-item label="状态"><el-select v-model="query.online" clearable placeholder="全部"><el-option label="在线" :value="1" /><el-option label="离线" :value="0" /></el-select></el-form-item>
      <el-form-item><el-checkbox v-model="query.alarm">仅显示告警</el-checkbox></el-form-item>
      <el-form-item><el-button type="primary" icon="el-icon-search" @click="refresh">查询</el-button><el-button icon="el-icon-refresh" @click="resetQuery">重置</el-button></el-form-item>
    </el-form>

    <el-tabs v-model="activeTab">
      <el-tab-pane label="实时状态" name="modules">
        <el-table v-loading="loading" :data="modules" row-key="moduleKey" @expand-change="loadHistory">
          <el-table-column type="expand">
            <template slot-scope="scope">
              <div class="history-panel" v-loading="scope.row.historyLoading">
                <span class="history-title">近 24 小时温度记录</span>
                <el-table :data="scope.row.history" size="mini" max-height="260">
                  <el-table-column prop="collected_at" label="采集时间" min-width="170" />
                  <el-table-column prop="target_temperature" label="目标温度" min-width="90" />
                  <el-table-column prop="water_in_temperature" label="回水温度" min-width="90" />
                  <el-table-column prop="water_out_temperature" label="出水温度" min-width="90" />
                  <el-table-column prop="ambient_temperature" label="环境温度" min-width="90" />
                  <el-table-column prop="fault_code" label="故障码" min-width="80" />
                </el-table>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="site_code" label="站点" min-width="110" />
          <el-table-column label="控制器" min-width="140"><template slot-scope="scope">{{ controllerLabel(scope.row) }}</template></el-table-column>
          <el-table-column prop="module_name" label="模块" min-width="130"><template slot-scope="scope">{{ scope.row.module_name || ('模块 ' + scope.row.module_index) }}</template></el-table-column>
          <el-table-column label="在线" width="80"><template slot-scope="scope"><el-tag :type="scope.row.is_online ? 'success' : 'info'">{{ scope.row.is_online ? '在线' : '离线' }}</el-tag></template></el-table-column>
          <el-table-column prop="state_code" label="状态码" width="90" />
          <el-table-column prop="run_mode" label="运行模式" width="90" />
          <el-table-column prop="target_temperature" label="目标温度" width="95" />
          <el-table-column prop="water_in_temperature" label="回水温度" width="95" />
          <el-table-column prop="water_out_temperature" label="出水温度" width="95" />
          <el-table-column prop="ambient_temperature" label="环境温度" width="95" />
          <el-table-column prop="fault_code" label="故障码" width="80" />
          <el-table-column label="告警" width="80"><template slot-scope="scope"><el-tag v-if="scope.row.alarm_count > 0" type="danger">{{ scope.row.alarm_count }}</el-tag><span v-else>--</span></template></el-table-column>
          <el-table-column prop="last_collect_time" label="最后采集" min-width="170" />
        </el-table>
      </el-tab-pane>
      <el-tab-pane :label="'活动告警 (' + alarms.length + ')'" name="alarms">
        <el-table v-loading="loading" :data="alarms">
          <el-table-column prop="site_code" label="站点" min-width="110" />
          <el-table-column label="控制器" min-width="140"><template slot-scope="scope">{{ controllerLabel(scope.row) }}</template></el-table-column>
          <el-table-column prop="module_index" label="模块序号" width="90" />
          <el-table-column prop="alarm_type" label="类型" width="90" />
          <el-table-column prop="alarm_code" label="代码" width="90" />
          <el-table-column prop="message" label="说明" min-width="150" />
          <el-table-column prop="opened_at" label="发生时间" min-width="170" />
          <el-table-column prop="last_seen_at" label="最近检测" min-width="170" />
        </el-table>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script>
import { getHeatPumpOverview, listHeatPumpModules, getHeatPumpHistory, listHeatPumpAlarms } from '@/api/heatpump/monitor'

export default {
  name: 'HeatPumpMonitor',
  data() {
    return {
      loading: false,
      activeTab: 'modules',
      overview: {},
      modules: [],
      alarms: [],
      query: { siteCode: undefined, online: undefined, alarm: false },
      refreshTimer: undefined
    }
  },
  created() {
    this.refresh()
    this.refreshTimer = window.setInterval(this.refresh, 15000)
  },
  beforeDestroy() {
    window.clearInterval(this.refreshTimer)
  },
  methods: {
    async refresh() {
      this.loading = true
      try {
        const params = Object.assign({}, this.query, { alarm: this.query.alarm ? 1 : undefined })
        const [overviewResponse, modulesResponse, alarmsResponse] = await Promise.all([
          getHeatPumpOverview(), listHeatPumpModules(params), listHeatPumpAlarms()
        ])
        this.overview = overviewResponse.data || {}
        this.modules = (modulesResponse.data || []).map(item => Object.assign(item, { moduleKey: item.device_id + '-' + item.module_index, history: [], historyLoading: false }))
        this.alarms = alarmsResponse.data || []
      } finally {
        this.loading = false
      }
    },
    resetQuery() {
      this.query = { siteCode: undefined, online: undefined, alarm: false }
      this.refresh()
    },
    controllerLabel(row) {
      return row.controller_name || ('地址 ' + row.controller_address)
    },
    async loadHistory(row, expandedRows) {
      if (!expandedRows.some(item => item.moduleKey === row.moduleKey) || row.history.length) return
      row.historyLoading = true
      try {
        const response = await getHeatPumpHistory({ deviceId: row.device_id, moduleIndex: row.module_index, hours: 24 })
        row.history = response.data || []
      } finally {
        row.historyLoading = false
      }
    }
  }
}
</script>

<style scoped>
.summary-row { margin-bottom: 20px; }
.summary-item { min-height: 86px; padding: 16px; border: 1px solid #dcdfe6; border-left: 4px solid #409eff; background: #fff; }
.summary-item span { display: block; color: #606266; font-size: 14px; }
.summary-item strong { display: block; margin-top: 8px; font-size: 28px; color: #303133; }
.summary-item.online { border-left-color: #67c23a; }
.summary-item.offline { border-left-color: #909399; }
.summary-item.alarm { border-left-color: #f56c6c; }
.query-form { margin-bottom: 12px; }
.history-panel { padding: 8px 24px 16px; }
.history-title { display: block; margin-bottom: 10px; color: #303133; }
</style>
