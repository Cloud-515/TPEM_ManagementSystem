<template>
  <div class="app-container topology-page" v-loading="loading">
    <div class="page-heading">
      <div><h1>设备拓扑图</h1><p>按区域组织电表设备，查看实时状态与负荷</p></div>
      <div class="toolbar">
        <el-button v-if="editing" @click="cancelEdit">取消</el-button>
        <el-button v-if="editing" type="primary" :loading="saving" @click="saveLayout">保存布局</el-button>
        <el-button v-else icon="el-icon-refresh" @click="loadTopology">刷新</el-button>
        <el-button type="primary" round :icon="editing ? 'el-icon-check' : 'el-icon-edit'" @click="toggleEdit">{{ editing ? '编辑中' : '编辑模式' }}</el-button>
      </div>
    </div>

    <el-alert v-if="loadError" class="topology-load-error" :title="loadError" type="error" :closable="false" show-icon />

    <div v-if="editing" class="edit-tools">
      <el-button size="small" type="primary" plain icon="el-icon-plus" @click="addRegion">新增区域</el-button>
      <span>新增、重命名和拖拽仅在点击“保存布局”后生效；删除区域前请先将设备移至其他区域或未分配设备。</span>
    </div>

    <section v-if="unassignedMeters.length || editing" class="unassigned-section">
      <div class="section-title"><span>未分配设备</span><em>{{ unassignedMeters.length }} 台</em></div>
      <div v-if="!editing" class="meter-grid">
        <button v-for="meter in unassignedMeters" :key="meter.meterId" type="button" class="meter-card" @click="openRecord(meter)">
          <div class="meter-card-title"><strong>{{ meter.meterName || '未命名设备' }}</strong><el-tag :type="statusType(meter.statusCode)" size="mini">{{ statusLabel(meter.statusCode) }}</el-tag></div>
          <div class="meter-power">{{ formatNumber(meter.activePowerKw) }} <small>kW</small></div>
          <div class="meter-meta">{{ meter.siteName || '--' }} / {{ meter.boxName || '--' }}</div>
          <div class="meter-meta">{{ meter.lastCollectTime || '暂无采集数据' }}</div>
        </button>
        <div v-if="!unassignedMeters.length" class="drop-empty">暂无未分配设备</div>
      </div>
      <draggable v-else v-model="unassignedMeters" :group="dragOptions" draggable=".meter-grid-item" class="meter-grid drop-zone" ghost-class="drag-ghost" animation="180">
        <div v-for="meter in unassignedMeters" :key="meter.meterId" class="meter-grid-item">
          <button type="button" class="meter-card is-editing">
            <div class="meter-card-title"><strong>{{ meter.meterName || '未命名设备' }}</strong><el-tag :type="statusType(meter.statusCode)" size="mini">{{ statusLabel(meter.statusCode) }}</el-tag></div>
            <div class="meter-power">{{ formatNumber(meter.activePowerKw) }} <small>kW</small></div>
            <div class="meter-meta">{{ meter.siteName || '--' }} / {{ meter.boxName || '--' }}</div>
            <div class="meter-meta">{{ meter.lastCollectTime || '暂无采集数据' }}</div>
          </button>
        </div>
        <template slot="footer"><div v-if="!unassignedMeters.length" class="drop-empty">拖入电表至此处</div></template>
      </draggable>
    </section>

    <div v-if="regions.length" class="region-grid">
      <section v-for="(region, index) in regions" :key="region.regionId" class="region-card">
        <div class="region-header">
          <div>
            <h2>{{ region.regionName }}</h2>
            <p>{{ region.meters.length }} 台设备 · {{ normalCount(region) }} 台正常 · {{ formatNumber(regionPower(region)) }} kW</p>
          </div>
          <div v-if="editing" class="region-actions">
            <el-button type="text" size="mini" @click="renameRegion(region)">重命名</el-button>
            <el-button type="text" size="mini" class="delete-action" @click="removeRegion(index)">删除</el-button>
          </div>
        </div>
        <div v-if="!editing" class="meter-grid">
          <button v-for="meter in region.meters" :key="meter.meterId" type="button" class="meter-card" @click="openRecord(meter)">
            <div class="meter-card-title"><strong>{{ meter.meterName || '未命名设备' }}</strong><el-tag :type="statusType(meter.statusCode)" size="mini">{{ statusLabel(meter.statusCode) }}</el-tag></div>
            <div class="meter-power">{{ formatNumber(meter.activePowerKw) }} <small>kW</small></div>
            <div class="meter-meta">{{ meter.siteName || '--' }} / {{ meter.boxName || '--' }}</div>
            <div class="meter-meta">{{ meter.lastCollectTime || '暂无采集数据' }}</div>
          </button>
          <div v-if="!region.meters.length" class="drop-empty">该区域暂无设备</div>
        </div>
        <draggable v-else v-model="region.meters" :group="dragOptions" draggable=".meter-grid-item" class="meter-grid drop-zone" ghost-class="drag-ghost" animation="180">
          <div v-for="meter in region.meters" :key="meter.meterId" class="meter-grid-item">
            <button type="button" class="meter-card is-editing">
              <div class="meter-card-title"><strong>{{ meter.meterName || '未命名设备' }}</strong><el-tag :type="statusType(meter.statusCode)" size="mini">{{ statusLabel(meter.statusCode) }}</el-tag></div>
              <div class="meter-power">{{ formatNumber(meter.activePowerKw) }} <small>kW</small></div>
              <div class="meter-meta">{{ meter.siteName || '--' }} / {{ meter.boxName || '--' }}</div>
              <div class="meter-meta">{{ meter.lastCollectTime || '暂无采集数据' }}</div>
            </button>
          </div>
          <template slot="footer"><div v-if="!region.meters.length" class="drop-empty">拖入电表至此区域</div></template>
        </draggable>
      </section>
    </div>
    <div v-else class="empty-state"><el-empty description="尚未创建拓扑区域"><el-button type="primary" @click="addRegion">创建第一个区域</el-button></el-empty></div>

    <el-dialog title="区域名称" :visible.sync="regionDialog.visible" width="400px" append-to-body>
      <el-input v-model="regionDialog.name" maxlength="64" show-word-limit placeholder="请输入区域名称" @keyup.enter.native="confirmRegion" />
      <span slot="footer"><el-button @click="regionDialog.visible = false">取消</el-button><el-button type="primary" @click="confirmRegion">确定</el-button></span>
    </el-dialog>
    <realtime-detail ref="realtimeDetail" />
  </div>
</template>

<script>
import draggable from 'vuedraggable'
import RealtimeDetail from '@/views/meter/components/realtime-detail'
import { getMeterTopology, saveMeterTopology } from '@/api/system/meter'
import { getMeterStatusLabel, meterStatusMeta, formatMeterNumber } from '@/views/meter/components/meter-utils'

export default {
  name: 'MeterTopology',
  components: { draggable, RealtimeDetail },
  data() {
    return {
      loading: false, saving: false, editing: false, regions: [], unassignedMeters: [], originalLayout: null, loadError: '',
      regionDialog: { visible: false, name: '', region: null }, dragOptions: { name: 'meter-topology', pull: true, put: true }
    }
  },
  created() { this.loadTopology() },
  methods: {
    async loadTopology() {
      this.loading = true
      this.loadError = ''
      try {
        const response = await getMeterTopology()
        const data = response.data || response
        this.regions = data.regions || []
        this.unassignedMeters = data.unassignedMeters || []
        this.originalLayout = this.cloneLayout()
      } catch (error) {
        // 原实现只有 try/finally：接口失败时 regions 停在空数组，
        // 页面落到"尚未创建拓扑区域"的空态，把一次请求失败误导成"现场还没有拓扑配置"，
        // 而且此时保存会用空配置覆盖掉现有区域。
        this.loadError = '拓扑配置加载失败。在刷新成功之前请不要保存，否则会用空配置覆盖现有区域。'
        this.$message.error(this.loadError)
      } finally { this.loading = false }
    },
    cloneLayout() { return JSON.parse(JSON.stringify({ regions: this.regions, unassignedMeters: this.unassignedMeters })) },
    toggleEdit() {
      if (!this.editing && this.loadError) {
        this.$modal.msgError('拓扑配置未成功加载，请先刷新再编辑')
        return
      }
      this.editing = !this.editing
      if (this.editing) this.originalLayout = this.cloneLayout()
    },
    addRegion() { this.regionDialog = { visible: true, name: '', region: null } },
    renameRegion(region) { this.regionDialog = { visible: true, name: region.regionName, region } },
    confirmRegion() {
      const name = this.regionDialog.name.trim()
      if (!name) return this.$modal.msgWarning('请输入区域名称')
      if (this.regionDialog.region) this.regionDialog.region.regionName = name
      else this.regions.push({ regionId: null, regionName: name, meters: [] })
      this.regionDialog.visible = false
    },
    removeRegion(index) {
      const region = this.regions[index]
      if (region.meters.length) return this.$modal.msgWarning('请先迁移该区域内的设备')
      this.regions.splice(index, 1)
    },
    cancelEdit() {
      const layout = this.originalLayout || { regions: [], unassignedMeters: [] }
      this.regions = layout.regions || []
      this.unassignedMeters = layout.unassignedMeters || []
      this.editing = false
    },
    async saveLayout() {
      // 加载失败时 regions 是空的，此时保存等于用空配置覆盖现有区域 —— 直接拒绝。
      if (this.loadError) {
        this.$modal.msgError('拓扑配置未成功加载，已阻止保存以免覆盖现有区域，请先刷新')
        return
      }
      this.saving = true
      try {
        await saveMeterTopology({ regions: this.regions })
        this.$modal.msgSuccess('拓扑布局已保存')
        this.editing = false
        await this.loadTopology()
      } catch (error) {
        this.$modal.msgError('保存失败，当前编辑内容已保留，请检查后重试')
      } finally { this.saving = false }
    },
    regionPower(region) { return (region.meters || []).reduce((sum, meter) => sum + (Number(meter.activePowerKw) || 0), 0) },
    normalCount(region) { return (region.meters || []).filter(meter => meter.statusCode === 'OK').length },
    statusType(code) { return (meterStatusMeta[code] || {}).type || 'info' },
    statusLabel(code) { return getMeterStatusLabel(code) },
    formatNumber(value) { return formatMeterNumber(value) },
    openRecord(meter) {
      if (meter && meter.meterId) this.$refs.realtimeDetail.open(meter.meterId, 'running')
    }
  }
}
</script>

<style scoped>
.topology-page { min-height: calc(100vh - 84px); background: #f5f7fa; }.page-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }.page-heading h1 { margin: 0 0 6px; color: #1f2937; font-size: 24px; }.page-heading p { margin: 0; color: #718096; font-size: 13px; }.toolbar { display: flex; gap: 8px; }.edit-tools { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; padding: 10px 14px; color: #587083; border: 1px solid #dceaf1; border-radius: 8px; background: #edf8fc; font-size: 13px; }.unassigned-section, .region-card { margin-bottom: 16px; padding: 18px; border: 1px solid #e1e8ef; border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(15, 23, 42, .035); }.section-title, .region-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 14px; }.section-title span, .region-header h2 { margin: 0; color: #1f2937; font-size: 17px; font-weight: 600; }.section-title em { color: #718096; font-size: 13px; font-style: normal; }.region-header p { margin: 6px 0 0; color: #718096; font-size: 12px; }.delete-action { color: #d9534f; }.region-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; }.meter-grid { display: grid; min-height: 92px; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 10px; }.drop-zone { padding: 2px; border-radius: 7px; transition: background .18s ease; }.drop-zone:empty { background: #f8fafc; }.meter-card { display: block; min-width: 0; padding: 12px; color: inherit; border: 1px solid #e6edf3; border-radius: 8px; background: #fbfdff; cursor: pointer; font: inherit; text-align: left; }.meter-card:hover { border-color: #a7cddd; background: #f2faff; }.meter-card.is-editing { cursor: grab; }.meter-card-title { display: flex; align-items: center; justify-content: space-between; gap: 8px; }.meter-card-title strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 13px; }.meter-power { margin: 10px 0 5px; color: #1677a8; font-size: 21px; font-weight: 650; }.meter-power small, .meter-meta { color: #718096; font-size: 11px; font-weight: 400; }.meter-meta { overflow: hidden; line-height: 18px; text-overflow: ellipsis; white-space: nowrap; }.drop-empty { display: grid; min-height: 76px; place-items: center; color: #94a3b8; border: 1px dashed #cdd9e3; border-radius: 7px; font-size: 12px; }.drag-ghost { opacity: .4; background: #d9eef8; } .empty-state { padding: 50px 0; background: #fff; border-radius: 10px; } @media (max-width: 768px) { .page-heading { align-items: flex-start; flex-direction: column; gap: 12px; }.toolbar { flex-wrap: wrap; }.edit-tools { align-items: flex-start; flex-direction: column; }.region-grid { grid-template-columns: 1fr; } }
</style>
