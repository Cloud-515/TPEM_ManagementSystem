<template>
  <el-dialog title="设备实时详情" :visible.sync="visible" width="900px" append-to-body>
    <div v-loading="loading" class="detail-wrap">
      <template v-if="detail.meterId">
        <div class="detail-card">
          <div class="detail-card__title">基础信息</div>
          <el-row :gutter="16">
            <el-col :span="12"><div class="detail-item"><span class="detail-label">设备名称</span><span class="detail-value">{{ detail.meterName || '--' }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">设备编码</span><span class="detail-value">{{ detail.meterCode || '--' }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">站点</span><span class="detail-value">{{ detail.siteCode || '--' }} / {{ detail.siteName || '--' }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">箱体</span><span class="detail-value">{{ detail.boxCode || '--' }} / {{ detail.boxName || '--' }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">从站地址</span><span class="detail-value">{{ detail.slaveAddress || '--' }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">运行状态</span><span class="detail-value"><el-tag size="small" :type="statusTagType(detail.statusCode)">{{ detail.statusText || '--' }}</el-tag></span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">最后采集</span><span class="detail-value">{{ formatDateTime(detail.lastCollectTime) }}</span></div></el-col>
            <el-col :span="12"><div class="detail-item"><span class="detail-label">最后发布</span><span class="detail-value">{{ formatDateTime(detail.lastPublishTime) }}</span></div></el-col>
          </el-row>
        </div>

        <div class="detail-card">
          <div class="detail-card__title">电压与电流</div>
          <el-row :gutter="16">
            <el-col :span="8"><div class="metric-item"><div class="metric-label">A相电压</div><div class="metric-value">{{ formatNumber(detail.voltageA) }} V</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">B相电压</div><div class="metric-value">{{ formatNumber(detail.voltageB) }} V</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">C相电压</div><div class="metric-value">{{ formatNumber(detail.voltageC) }} V</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">A相电流</div><div class="metric-value">{{ formatNumber(detail.currentA) }} A</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">B相电流</div><div class="metric-value">{{ formatNumber(detail.currentB) }} A</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">C相电流</div><div class="metric-value">{{ formatNumber(detail.currentC) }} A</div></div></el-col>
          </el-row>
        </div>

        <div class="detail-card">
          <div class="detail-card__title">功率参数</div>
          <el-row :gutter="16">
            <el-col :span="8"><div class="metric-item"><div class="metric-label">总有功功率</div><div class="metric-value">{{ formatNumber(detail.activePowerKw) }} kW</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">总无功功率</div><div class="metric-value">{{ formatNumber(detail.reactivePowerTotal) }} var</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">总视在功率</div><div class="metric-value">{{ formatNumber(detail.apparentPowerTotal) }} VA</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">总功率因数</div><div class="metric-value">{{ formatNumber(detail.powerFactorTotal) }}</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">频率</div><div class="metric-value">{{ formatNumber(detail.frequency) }} Hz</div></div></el-col>
            <el-col :span="8"><div class="metric-item"><div class="metric-label">最大电流</div><div class="metric-value">{{ formatNumber(detail.maxCurrentA) }} A</div></div></el-col>
          </el-row>
        </div>
      </template>
      <el-empty v-else description="暂无实时详情数据" />
    </div>
  </el-dialog>
</template>

<script>
import { getRealtimeDetail } from '@/api/system/meter'

export default {
  name: 'RealtimeDetail',
  data() {
    return {
      visible: false,
      loading: false,
      detail: {}
    }
  },
  methods: {
    open(meterId) {
      this.visible = true
      this.loading = true
      this.detail = {}
      getRealtimeDetail(meterId).then(response => {
        this.detail = response.data || {}
      }).finally(() => {
        this.loading = false
      })
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
    statusTagType(statusCode) {
      switch (statusCode) {
        case 'OK':
          return 'success'
        case 'PF_LOW':
        case 'VOLTAGE_BAD':
          return 'warning'
        case 'FAULT':
          return 'danger'
        case 'NODATA':
          return 'info'
        default:
          return 'info'
      }
    }
  }
}
</script>

<style scoped lang="scss">
.detail-card {
  margin-bottom: 16px;
  padding: 16px;
  border: 1px solid #ebeef5;
  border-radius: 8px;
  background: #fff;
}

.detail-card__title {
  margin-bottom: 14px;
  font-size: 15px;
  font-weight: 600;
  color: #303133;
}

.detail-item {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.detail-label {
  color: #909399;
}

.detail-value {
  color: #303133;
  font-weight: 500;
  text-align: right;
}

.metric-item {
  margin-bottom: 16px;
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
</style>
