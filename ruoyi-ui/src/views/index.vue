<template>
  <div class="app-container home">
    <el-row :gutter="20" class="summary-row">
      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="summary-card">
          <div class="summary-label">设备总数</div>
          <div class="summary-value">{{ totalCount }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="summary-card">
          <div class="summary-label">外部串口</div>
          <div class="summary-value">{{ toolbarCards.length }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="summary-card">
          <div class="summary-label">仪表盘扫描</div>
          <div class="summary-value">{{ dashboardCards.length }}</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <el-card class="summary-card">
          <div class="summary-label">最近刷新</div>
          <div class="summary-value summary-time">{{ generatedAtText }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card class="section-card">
      <div slot="header" class="section-header">
        <span>{{ toolbarTitle }} - {{ toolbarCards.length }} 台设备</span>
      </div>
      <el-empty v-if="!loading && toolbarCards.length === 0" description="暂无设备数据" />
      <el-row v-else :gutter="16">
        <el-col v-for="card in toolbarCards" :key="card.meterId" :xs="24" :sm="12" :md="8" :lg="6" class="card-col">
          <el-card shadow="hover" class="meter-card meter-card--clickable" @click.native="openDetail(card)">
            <div class="meter-card__title">
              <span class="meter-name">{{ card.meterName || card.meterCode }}</span>
              <span class="meter-id">ID: {{ card.slaveAddress || '--' }}</span>
            </div>
            <div class="meter-card__meta">{{ card.siteCode || '--' }} / {{ card.boxCode || '--' }}</div>
            <div class="meter-card__metrics">
              <div class="metric-item">
                <div class="metric-label">三相功率</div>
                <div class="metric-value">{{ formatNumber(card.activePowerKw) }} kW</div>
              </div>
              <div class="metric-item">
                <div class="metric-label">最大电流</div>
                <div class="metric-value">{{ formatNumber(card.maxCurrentA) }} A</div>
              </div>
            </div>
            <div class="meter-card__footer">
              <span class="status-dot" :class="statusClass(card.statusCode)"></span>
              <span class="status-text">{{ card.statusText || '等待数据' }}</span>
              <span class="collect-time">{{ formatDateTime(card.lastCollectTime) }}</span>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <el-card class="section-card">
      <div slot="header" class="section-header">
        <span>{{ dashboardTitle }} - {{ dashboardCards.length }} 台设备</span>
      </div>
      <el-empty v-if="!loading && dashboardCards.length === 0" description="暂无设备数据" />
      <el-row v-else :gutter="16">
        <el-col v-for="card in dashboardCards" :key="card.meterId" :xs="24" :sm="12" :md="8" :lg="6" class="card-col">
          <el-card shadow="hover" class="meter-card meter-card--clickable" @click.native="openDetail(card)">
            <div class="meter-card__title">
              <span class="meter-name">{{ card.meterName || card.meterCode }}</span>
              <span class="meter-id">ID: {{ card.slaveAddress || '--' }}</span>
            </div>
            <div class="meter-card__meta">{{ card.siteCode || '--' }} / {{ card.boxCode || '--' }}</div>
            <div class="meter-card__metrics">
              <div class="metric-item">
                <div class="metric-label">三相功率</div>
                <div class="metric-value">{{ formatNumber(card.activePowerKw) }} kW</div>
              </div>
              <div class="metric-item">
                <div class="metric-label">最大电流</div>
                <div class="metric-value">{{ formatNumber(card.maxCurrentA) }} A</div>
              </div>
            </div>
            <div class="meter-card__footer">
              <span class="status-dot" :class="statusClass(card.statusCode)"></span>
              <span class="status-text">{{ card.statusText || '等待数据' }}</span>
              <span class="collect-time">{{ formatDateTime(card.lastCollectTime) }}</span>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </el-card>

    <realtime-detail ref="detailRef" />
  </div>
</template>

<script>
import { listMeterCards } from '@/api/system/meter'
import RealtimeDetail from '@/views/meter/components/realtime-detail'

export default {
  name: 'Index',
  components: { RealtimeDetail },
  data() {
    return {
      loading: false,
      timer: null,
      toolbarTitle: '外部串口（真实串口）',
      dashboardTitle: '仪表盘扫描串口',
      toolbarCards: [],
      dashboardCards: [],
      generatedAt: null
    }
  },
  computed: {
    totalCount() {
      return this.toolbarCards.length + this.dashboardCards.length
    },
    generatedAtText() {
      return this.formatDateTime(this.generatedAt)
    }
  },
  created() {
    this.fetchCards(true)
    this.timer = setInterval(() => {
      this.fetchCards(false)
    }, 15000)
  },
  beforeDestroy() {
    if (this.timer) {
      clearInterval(this.timer)
      this.timer = null
    }
  },
  methods: {
    fetchCards(showLoading) {
      if (showLoading) {
        this.loading = true
        this.$modal.loading('正在加载设备卡片数据，请稍候！')
      }
      listMeterCards().then(response => {
        const data = response.data || {}
        this.toolbarCards = data.toolbar || []
        this.dashboardCards = data.dashboard || []
        this.toolbarTitle = data.toolbarTitle || '外部串口（真实串口）'
        this.dashboardTitle = data.dashboardTitle || '仪表盘扫描串口'
        this.generatedAt = data.generatedAt || new Date()
      }).finally(() => {
        if (showLoading) {
          this.loading = false
          this.$modal.closeLoading()
        }
      })
    },
    openDetail(card) {
      this.$refs.detailRef.open(card.meterId)
    },
    formatNumber(value) {
      if (value === null || value === undefined || value === '') {
        return '--'
      }
      return Number(value).toFixed(2)
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
    statusClass(statusCode) {
      switch (statusCode) {
        case 'OK':
          return 'status-ok'
        case 'PF_LOW':
        case 'VOLTAGE_BAD':
          return 'status-warn'
        case 'FAULT':
          return 'status-error'
        case 'NODATA':
          return 'status-offline'
        case 'WAITING':
        default:
          return 'status-idle'
      }
    }
  }
}
</script>

<style scoped lang="scss">
.home {
  background: #f5f7fa;
  min-height: calc(100vh - 84px);
}

.summary-row {
  margin-bottom: 20px;
}

.summary-card {
  margin-bottom: 16px;
}

.summary-label {
  font-size: 13px;
  color: #909399;
}

.summary-value {
  margin-top: 8px;
  font-size: 28px;
  font-weight: 600;
  color: #303133;
}

.summary-time {
  font-size: 18px;
}

.section-card {
  margin-bottom: 20px;
}

.section-header {
  font-size: 16px;
  font-weight: 600;
}

.card-col {
  margin-bottom: 16px;
}

.meter-card {
  border-radius: 8px;
}

.meter-card--clickable {
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.meter-card--clickable:hover {
  transform: translateY(-2px);
}

.meter-card__title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.meter-name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.meter-id,
.meter-card__meta,
.collect-time {
  font-size: 12px;
  color: #909399;
}

.meter-card__meta {
  margin-bottom: 14px;
}

.meter-card__metrics {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}

.metric-item {
  flex: 1;
  padding: 12px;
  background: #f8f9fb;
  border-radius: 6px;
}

.metric-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 6px;
}

.metric-value {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
}

.meter-card__footer {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
}

.status-text {
  font-size: 13px;
  color: #606266;
  flex: 1;
}

.status-ok {
  background: #67c23a;
}

.status-warn {
  background: #e6a23c;
}

.status-error {
  background: #f56c6c;
}

.status-offline {
  background: #7f56d9;
}

.status-idle {
  background: #909399;
}
</style>
