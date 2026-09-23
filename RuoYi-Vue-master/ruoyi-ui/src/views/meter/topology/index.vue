<template>
  <div class="app-container topology-page" :class="{ 'is-dragging': dragging }" v-loading="loading">
    <div class="page-heading">
      <div><h1>设备拓扑图</h1><p>按区域与配电箱查看每台电表的实时数据</p></div>
    </div>

    <!-- 操作入口全部收进右下角这个可拖拽的悬浮球（长页面不用滚回顶部）。
         球是「图标 + 文字」的胶囊而不是纯图标圆点：光打一个勾看不出这里是编辑。
         悬停展开成横向工具条；每项悬停时在它正上方弹一句说明（说明里包含会丢弃改动这类风险）。 -->
    <div
      class="fab"
      :class="{ 'is-editing': editing, 'is-open': fabOpen, 'is-picked': fabDragging, 'is-meter-dragging': dragging }"
      :style="fabStyle"
      @pointerdown="onFabPointerDown"
      @mouseenter="onFabEnter"
      @mouseleave="onFabLeave"
    >
      <div class="bar">
        <button type="button" class="bar-item" @click="onFabAction('toggleView')">
          <i>▦</i>{{ viewMode === 'list' ? '图形视图' : '列表视图' }}
          <span class="tip">切到{{ viewMode === 'list' ? '图形' : '列表' }}：{{ viewMode === 'list' ? '按区域与配电箱分块，可拖拽微调' : '表格逐台改归属，可勾选多行批量落位' }}</span>
        </button>
        <button type="button" class="bar-item" @click="onFabAction('refresh')">
          <i>↻</i>刷新
          <span class="tip">重新拉取数据；会丢弃未保存的改动{{ dirty ? '（当前有未保存改动）' : '' }}</span>
        </button>
        <span class="bar-div"></span>
        <button v-if="editing" type="button" class="bar-item is-cancel" @click="onFabAction('cancel')">
          <i>✕</i>取消
          <span class="tip">放弃本次全部改动，回到进入编辑前的状态</span>
        </button>
        <button v-if="editing" type="button" class="bar-item is-save" :class="{ 'is-busy': saving }" @click="onFabAction('save')">
          <i>✓</i>保存布局
          <span class="tip">含未保存改动；保存后实时运行、电能等页面同步生效</span>
        </button>
        <button v-else type="button" class="bar-item is-save" @click="onFabAction('edit')">
          <i>✎</i>编辑
          <span class="tip">进入编辑模式，可拖拽调整归属与顺序{{ dirty ? '（有未保存改动）' : '' }}</span>
        </button>
      </div>
      <button type="button" class="ball" @click="onFabAction('collapse')">
        <i class="ico-open">✎</i><span class="txt-open">{{ editing ? '编辑中' : '编辑' }}</span>
        <i class="ico-close">✕</i><span class="txt-close">收起</span>
        <em v-if="dirty" class="dot" title="有未保存的改动"></em>
      </button>
    </div>

    <el-alert v-if="loadError" class="topology-load-error" :title="loadError" type="error" :closable="false" show-icon />

    <div v-if="editing" class="edit-tools">
      <el-button size="small" type="primary" plain icon="el-icon-plus" @click="addRegion">新增区域</el-button>
      <span v-if="viewMode === 'list'">
        列表里直接改「所属区域」下拉即可逐台调整；勾选多行后用批量条一次落位；「顺序」列的 ↑↓ 调整该表在自己配电箱内的先后。
      </span>
      <span v-else>
        拖动电表到同一配电箱内可调顺序，拖到另一个配电箱就是把它改挂到那个箱体；拖动配电箱铭牌可调整箱体先后；
        拖到区域底部的「拖入本区域」条可换区域，拖到「未分配设备」即移出区域。
        改箱体会写回设备自身的箱体归属，保存后实时运行等页面看到的箱体也会同步变化。
        注意：同一个配电箱内从站地址不能重复（现场每个箱体都从 1 开始编址），地址撞上时会被拦下并提示。
      </span>
    </div>

    <!-- 列表视图：批量与精确操作走这里，图形视图保留做微调和总览 -->
    <div v-if="viewMode === 'list'" class="list-view">
      <div class="list-toolbar">
        <el-select v-model="listFilter.siteName" size="small" placeholder="全部站点" clearable>
          <el-option v-for="item in siteOptions" :key="item" :label="item" :value="item" />
        </el-select>
        <el-select v-model="listFilter.boxName" size="small" placeholder="全部配电箱" clearable>
          <el-option v-for="item in boxOptions" :key="item" :label="item" :value="item" />
        </el-select>
        <el-select v-model="listFilter.statusCode" size="small" placeholder="全部状态" clearable>
          <el-option v-for="item in statusOptions" :key="item.code" :label="item.label" :value="item.code" />
        </el-select>
        <el-input v-model="listFilter.keyword" size="small" placeholder="搜索设备名称 / 编号" clearable class="list-search" />
        <span class="list-count">共 <b>{{ filteredRows.length }}</b> 台</span>
      </div>

      <div v-if="editing" class="batch-bar">
        <span class="picked">已选 <b>{{ selectedMeterIds.length }}</b> 台</span>
        <el-select v-model="batchTarget" size="small" class="batch-select" placeholder="批量设为区域" :disabled="!selectedMeterIds.length" @change="applyBatchRegion">
          <el-option v-for="(region, index) in regions" :key="index" :label="region.regionName" :value="index" />
          <el-option label="未分配设备" :value="-1" />
        </el-select>
        <el-button size="small" :disabled="!selectedMeterIds.length" @click="applyBatchRegion(-1)">移出区域</el-button>
        <el-button size="small" type="text" :disabled="!selectedMeterIds.length" @click="clearSelection">清空选择</el-button>
      </div>

      <el-table
        ref="meterTable"
        :data="filteredRows"
        row-key="meterId"
        size="small"
        border
        class="meter-table"
        @selection-change="onSelectionChange"
      >
        <el-table-column v-if="editing" type="selection" width="44" />
        <el-table-column label="设备名称" min-width="150" show-overflow-tooltip>
          <template slot-scope="scope">{{ scope.row.meterName || '未命名设备' }}</template>
        </el-table-column>
        <el-table-column label="站点 / 配电箱" min-width="180" show-overflow-tooltip>
          <template slot-scope="scope">{{ scope.row.siteName || '--' }} / {{ scope.row.boxName || '--' }}</template>
        </el-table-column>
        <el-table-column label="地址" width="72" align="right">
          <template slot-scope="scope">{{ addressText(scope.row) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="112">
          <template slot-scope="scope"><el-tag size="mini" :type="statusType(scope.row.statusCode)">{{ statusLabel(scope.row.statusCode) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="所属区域" width="190">
          <template slot-scope="scope">
            <el-select
              v-if="editing"
              :value="scope.row.regionIndex"
              size="mini"
              class="region-select"
              @change="value => moveMeter(scope.row.meter, value)"
            >
              <el-option v-for="(region, index) in regions" :key="index" :label="region.regionName" :value="index" />
              <el-option label="未分配设备" :value="-1" />
            </el-select>
            <span v-else :class="{ muted: scope.row.regionIndex < 0 }">{{ scope.row.regionName || '未分配设备' }}</span>
          </template>
        </el-table-column>
        <el-table-column v-if="editing" label="顺序" width="96" align="center">
          <template slot-scope="scope">
            <el-button size="mini" type="text" :disabled="!canMove(scope.row, -1)" @click="moveRow(scope.row, -1)">↑</el-button>
            <el-button size="mini" type="text" :disabled="!canMove(scope.row, 1)" @click="moveRow(scope.row, 1)">↓</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <template v-else>
    <section v-if="unassignedMeters.length || editing" class="region">
      <div class="region-head">
        <h2>未分配设备</h2>
        <div class="stats">
          <span><b>{{ unassignedMeters.length }}</b> 台设备</span>
          <span class="sep"></span>
          <span>{{ editing ? '这里是移出区域的中转区' : '尚未归入任何区域' }}</span>
        </div>
      </div>
      <div class="enclosure">
        <i class="screw tl"></i><i class="screw tr"></i><i class="screw bl"></i><i class="screw br"></i>
        <div class="enc-head">
          <div class="plate is-loose">
            <span class="code">未接入配电箱</span>
            <span class="nm">{{ unassignedMeters.length }} 台设备待分配</span>
            <span v-if="editing" class="cnt">放入后按从站地址排序</span>
          </div>
        </div>
        <div class="cavity">
          <div class="rail is-loose"></div>
          <!-- 顺序由后端（从站地址）决定，拖拽只改变归属，所以这里关闭内部排序，不做兑现不了的承诺 -->
          <draggable v-if="editing" v-bind="dragTrack" :list="unassignedMeters" :group="meterGroup" :sort="false" draggable=".drag-item" class="meters drop-zone" ghost-class="drag-ghost" animation="180" @start="startAutoScroll" @end="stopAutoScroll">
            <div v-for="meter in unassignedMeters" :key="meter.meterId" class="drag-item" :data-meter-id="meter.meterId">
              <meter-device :meter="meter" />
            </div>
            <template slot="footer"><div v-if="!unassignedMeters.length" class="drop-empty">把电表拖到这里即移出区域</div></template>
          </draggable>
          <div v-else class="meters">
            <meter-device v-for="meter in unassignedMeters" :key="meter.meterId" :meter="meter" @open="openRecord" />
          </div>
        </div>
      </div>
    </section>

    <section v-for="view in regionsView" :key="view.region.regionId" class="region">
      <div class="region-head">
        <h2>{{ view.region.regionName }}</h2>
        <div class="stats">
          <span><b>{{ view.counts.total }}</b> 台设备</span>
          <span class="sep"></span>
          <span>总负荷 <b>{{ formatNumber(view.power) }}</b> kW</span>
          <span class="sep"></span>
          <span>{{ view.visibleBoxes.length }} 个配电箱</span>
          <span class="counts">正常 {{ view.counts.ok }} · 告警 {{ view.counts.warn }} · 故障 {{ view.counts.danger }} · 无数据 {{ view.counts.idle }}</span>
        </div>
        <div v-if="editing" class="region-actions">
          <el-button type="text" size="mini" @click="renameRegion(view.region)">重命名</el-button>
          <el-button type="text" size="mini" class="delete-action" @click="removeRegion(view.index)">删除</el-button>
        </div>
      </div>

      <!-- 配电箱外壳列表。查看态与编辑态同一份结构；编辑态可用铭牌拖动调整箱体先后 -->
      <draggable
        :list="view.region.boxes"
        :group="boxGroup"
        :disabled="!editing"
        draggable=".enclosure-item"
        handle=".enc-head"
        :move="onlyBoxItems"
        v-bind="dragTrack"
        class="box-list"
        ghost-class="drag-ghost"
        animation="180"
        @start="startAutoScroll"
        @end="stopAutoScroll"
      >
        <div v-for="box in (editing ? view.boxes : view.visibleBoxes)" :key="box.key" class="enclosure-item">
          <div class="enclosure" :class="{ 'is-blocked': dragging && blockedBoxKeys.indexOf(box.key) >= 0 }">
            <i class="screw tl"></i><i class="screw tr"></i><i class="screw bl"></i><i class="screw br"></i>
            <div class="enc-head" :class="{ 'is-handle': editing }">
              <div class="plate">
                <span v-if="editing" class="grip">⠿</span>
                <span v-if="box.code" class="code">{{ box.code }}</span>
                <span class="nm">{{ box.name || '未标注配电箱' }}</span>
                <span class="cnt">{{ box.meters.length }} 回路 · {{ formatNumber(box.power) }} kW</span>
              </div>
              <div v-if="editing" class="enc-stats">按铭牌拖动调整箱体先后</div>
              <div v-else-if="box.alerts" class="enc-stats">{{ box.alerts }}</div>
            </div>
            <div class="cavity">
              <div class="rail"></div>
              <draggable v-if="editing" v-bind="dragTrack" :list="box.meters" :group="groupForBox(box)" draggable=".drag-item" class="meters drop-zone" ghost-class="drag-ghost" animation="180" @change="onMeterDrop($event, box)" @start="onMeterDragStart" @end="onDragEnd">
                <div v-for="meter in box.meters" :key="meter.meterId" class="drag-item" :data-meter-id="meter.meterId">
                  <meter-device :meter="meter" />
                </div>
                <template slot="footer"><div v-if="!box.meters.length" class="drop-empty">拖入电表（会改挂到本箱体）</div></template>
              </draggable>
              <div v-else class="meters">
                <meter-device v-for="meter in box.meters" :key="meter.meterId" :meter="meter" @open="openRecord" />
              </div>
            </div>
          </div>
        </div>
      </draggable>

      <draggable
        v-if="editing"
        v-bind="dragTrack"
        :list="view.region.dropSink"
        :group="sinkGroup"
        class="region-sink"
        ghost-class="drag-ghost"
        animation="180"
        @change="onSinkChange($event, view.index)"
      >
        <template slot="footer"><div class="sink-hint">＋ 拖入本区域（落在设备自己的配电箱分组里）</div></template>
      </draggable>

      <div v-if="!editing && !view.visibleBoxes.length" class="enclosure">
        <i class="screw tl"></i><i class="screw tr"></i><i class="screw bl"></i><i class="screw br"></i>
        <div class="enc-head"><div class="plate is-loose"><span class="code">空区域</span><span class="nm">该区域暂无设备</span></div></div>
      </div>
    </section>

    <div v-if="!regions.length" class="empty-state">
      <el-empty description="尚未创建拓扑区域"><el-button type="primary" @click="addRegion">创建第一个区域</el-button></el-empty>
    </div>
    </template>

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
import MeterDevice from '@/views/meter/components/meter-device'
import { getMeterTopology, saveMeterTopology } from '@/api/system/meter'
import { getMeterStatusTone, getMeterStatusLabel, getMeterStatusType, meterStatusMeta, formatMeterNumber, formatMeterAddress } from '@/views/meter/components/meter-utils'

const TONE_ORDER = ['ok', 'warn', 'danger', 'idle']
const TONE_LABELS = { warn: '告警', danger: '故障', idle: '无数据' }
// 悬浮球位置记在本地，换页面回来还在用户放的地方
const FAB_POSITION_KEY = 'tpem-topology-fab-pos'

/** 配电箱的稳定标识，优先用主键（箱体名可能重复）。 */
function boxKeyOf(meter) {
  if (meter.boxId !== null && meter.boxId !== undefined) return 'box:' + meter.boxId
  return meter.boxName || meter.boxCode || '__unlabeled__'
}

/**
 * 按配电箱把电表分组，并让分组持有自己的 meters 数组。
 * vuedraggable 只能就地改它绑定的那个数组，绑到派生出来的子列表上拖拽结果写不回去，
 * 所以让箱体持有数组，保存时再摊平成区域内的先后顺序。
 */
function buildBoxes(meters) {
  const groups = new Map()
  meters.forEach(meter => {
    const key = boxKeyOf(meter)
    if (!groups.has(key)) {
      groups.set(key, { key, id: meter.boxId, code: meter.boxCode, name: meter.boxName, meters: [] })
    }
    groups.get(key).meters.push(meter)
  })
  return Array.from(groups.values())
}

function flattenBoxes(boxes) {
  return boxes.reduce((all, box) => all.concat(box.meters), [])
}

function countTones(meters) {
  const counts = { total: meters.length, ok: 0, warn: 0, danger: 0, idle: 0 }
  meters.forEach(meter => { counts[getMeterStatusTone(meter.statusCode)] += 1 })
  return counts
}

function sumPower(meters) {
  return meters.reduce((total, meter) => total + (Number(meter.activePowerKw) || 0), 0)
}

function emptyCounts() {
  return { total: 0, ok: 0, warn: 0, danger: 0, idle: 0 }
}

export default {
  name: 'MeterTopology',
  components: { draggable, RealtimeDetail, MeterDevice },
  data() {
    return {
      loading: false, saving: false, editing: false, regions: [], unassignedMeters: [], originalLayout: null, loadError: '',
      // 列表视图：批量与逐台操作都在这里。图形视图保留做总览和微调。
      viewMode: 'graph',
      listFilter: { siteName: '', boxName: '', statusCode: '', keyword: '' },
      selectedMeterIds: [],
      batchTarget: null,
      regionDialog: { visible: false, name: '', region: null },
      // 被拦下的拖拽原因，等拖拽结束再提示一次（put 在拖拽过程中会被反复调用，不能每次都弹提示）
      blockedHint: '',
      // 拖拽相关：
      // scroll: false —— 关掉 Sortable 自带的自动滚动，改用下面按指针位置自己实现。
      // forceFallback: true —— 不走原生 HTML5 拖放。原生拖放期间浏览器会吞掉滚轮事件
      //   （拖拽时无法用滚轮移动页面），改成鼠标事件驱动的 fallback 模式后滚轮才可用。
      dragTrack: {
        scroll: false,
        forceFallback: true,
        fallbackClass: 'dragging-clone',
        fallbackOnBody: true,
        fallbackTolerance: 3
      },
      autoScrollEdge: 90,
      autoScrollMaxStep: 36,
      // 拖拽中：给页面挂 is-dragging，配合 .enclosure:hover 做投放高亮（fallback 模式下:hover 正常更新）
      dragging: false,
      draggingMeterId: null,
      // 悬浮球位置（null 表示用默认的右下角），记到本地，下次进来还在原处
      fabPos: null,
      fabDragging: false,
      fabHover: false,
      // 点过球上的「收起」就把工具条压住，等鼠标离开再进来才重新展开（用来临时看清被工具条遮住的东西）
      barCollapsed: false,
      // 有未保存的改动：球上亮小红点，刷新/取消会先确认
      dirty: false,
      // 箱体列表自己一组（箱体不跨区域搬），电表共用一个组，跨区域投放条用独立的中转数组
      boxGroup: { name: 'meter-topology-box', pull: false, put: false },
      // 未分配区：只当落点用，所以 put 直接放开；组名必须和箱体网格一致，否则 Sortable 会拒绝拖入
      meterGroup: { name: 'meter-topology-meter', pull: true, put: true },
      sinkGroup: { name: 'meter-topology-meter', pull: false, put: true }
    }
  },
  computed: {
    regionsView() {
      return this.regions.map((region, index) => {
        const boxes = region.boxes.map(box => {
          const counts = countTones(box.meters)
          const alerts = TONE_ORDER.filter(tone => tone !== 'ok' && counts[tone])
            .map(tone => TONE_LABELS[tone] + ' ' + counts[tone])
          return { ...box, counts, power: sumPower(box.meters), alerts: alerts.length ? '本箱 ' + alerts.join(' · ') : '' }
        })
        const all = flattenBoxes(boxes)
        return {
          region,
          index,
          boxes,
          visibleBoxes: boxes.filter(box => box.meters.length),
          counts: all.length ? countTones(all) : emptyCounts(),
          power: sumPower(all)
        }
      })
    },
    /** 列表视图的数据源：把区域里的表和未分配的表摊平成一行一台。 */
    meterRows() {
      const rows = []
      this.regions.forEach((region, regionIndex) => {
        region.boxes.forEach(box => {
          box.meters.forEach((meter, meterIndex) => {
            rows.push({ meter, meterId: meter.meterId, box, meterIndex, regionIndex, regionName: region.regionName, ...meter })
          })
        })
      })
      this.unassignedMeters.forEach(meter => {
        rows.push({ meter, meterId: meter.meterId, box: null, meterIndex: -1, regionIndex: -1, regionName: '', ...meter })
      })
      return rows
    },
    filteredRows() {
      const { siteName, boxName, statusCode, keyword } = this.listFilter
      const text = (keyword || '').trim().toLowerCase()
      return this.meterRows.filter(row => {
        if (siteName && row.siteName !== siteName) return false
        if (boxName && row.boxName !== boxName) return false
        if (statusCode && row.statusCode !== statusCode) return false
        if (text) {
          const hay = (row.meterName || '') + ' ' + (row.meterCode || '')
          if (hay.toLowerCase().indexOf(text) < 0) return false
        }
        return true
      })
    },
    siteOptions() {
      return Array.from(new Set(this.meterRows.map(row => row.siteName).filter(Boolean)))
    },
    boxOptions() {
      return Array.from(new Set(this.meterRows.map(row => row.boxName).filter(Boolean)))
    },
    statusOptions() {
      return Object.keys(meterStatusMeta).map(code => ({ code, label: meterStatusMeta[code].label }))
    },
    /** 悬浮球：没拖过就用右下角默认位置，拖过就按记录的位置放 */
    fabStyle() {
      if (!this.fabPos) return {}
      return { left: this.fabPos.left + 'px', top: this.fabPos.top + 'px', right: 'auto', bottom: 'auto' }
    },
    /** 工具条是否展开：悬停展开，点过「收起」就压住，直到鼠标离开再进来 */
    fabOpen() {
      return this.fabHover && !this.barCollapsed
    },
    /**
     * 正在拖的电表放不进哪些箱体（同箱从站地址冲突）。高亮时把它们标红，
     * 免得亮着蓝框却放不下去 —— 拖动阶段的 put 校验和这里是同一套判断。
     */
    blockedBoxKeys() {
      if (!this.draggingMeterId) return []
      const meter = this.findMeter(this.draggingMeterId)
      const address = meter ? meter.slaveAddress : null
      if (address === null || address === undefined) return []
      const keys = []
      this.regions.forEach(region => region.boxes.forEach(box => {
        const clash = box.meters.some(item => String(item.meterId) !== String(this.draggingMeterId) && String(item.slaveAddress) === String(address))
        if (clash) keys.push(box.key)
      }))
      return keys
    }
  },
  created() { this.loadTopology() },
  mounted() {
    this.loadFabPos()
    window.addEventListener('resize', this.onWindowResize)
  },
  beforeDestroy() {
    this.stopAutoScroll()
    this.endFabDrag()
    window.removeEventListener('resize', this.onWindowResize)
  },
  watch: {
    // el-table 在 display:none 的父节点里挂载时量不到宽度，切到列表视图会先按错误宽度画一帧
    // （表现是首列被挤成一列一个字的竖排）。等 DOM 更新完让它按真实宽度重算一次。
    viewMode(mode) {
      if (mode !== 'list') return
      this.$nextTick(() => {
        if (this.$refs.meterTable) this.$refs.meterTable.doLayout()
      })
    }
  },
  methods: {
    formatNumber(value) { return formatMeterNumber(value) },
    /** 悬浮球能拖到的范围：优先限制在 .app-main 里（别盖住左侧菜单），取不到就退回视口。 */
    fabHost() {
      const main = document.querySelector('.app-main')
      if (main) {
        const rect = main.getBoundingClientRect()
        return { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom }
      }
      return { left: 0, top: 0, right: window.innerWidth, bottom: window.innerHeight }
    },
    clampFabPos(pos) {
      const host = this.fabHost()
      const width = this._fabWidth || 52
      const height = this._fabHeight || 52
      const minLeft = host.left + 8
      const minTop = host.top + 8
      return {
        left: Math.min(Math.max(pos.left, minLeft), Math.max(minLeft, host.right - width - 8)),
        top: Math.min(Math.max(pos.top, minTop), Math.max(minTop, host.bottom - height - 8))
      }
    },
    loadFabPos() {
      try {
        const raw = window.localStorage.getItem(FAB_POSITION_KEY)
        if (!raw) return
        const pos = JSON.parse(raw)
        if (pos && typeof pos.left === 'number' && typeof pos.top === 'number') {
          this.fabPos = this.clampFabPos(pos)
        }
      } catch (e) {
        // 本地位置读不出来就当没存过，用默认的右下角
      }
    },
    saveFabPos() {
      try {
        if (this.fabPos) window.localStorage.setItem(FAB_POSITION_KEY, JSON.stringify(this.fabPos))
        else window.localStorage.removeItem(FAB_POSITION_KEY)
      } catch (e) {
        // 隐私模式等写不进去，忽略
      }
    },
    onWindowResize() {
      if (this.fabPos) this.fabPos = this.clampFabPos(this.fabPos)
    },
    onFabPointerDown(event) {
      if (event.button && event.button !== 0) return
      const rect = event.currentTarget.getBoundingClientRect()
      this._fabWidth = rect.width
      this._fabHeight = rect.height
      this._fabGrabX = event.clientX - rect.left
      this._fabGrabY = event.clientY - rect.top
      this._fabStartX = event.clientX
      this._fabStartY = event.clientY
      this._fabMoved = false
      this._onFabMove = e => this.moveFab(e)
      this._onFabUp = () => this.endFabDrag()
      document.addEventListener('pointermove', this._onFabMove, true)
      document.addEventListener('pointerup', this._onFabUp, true)
      document.addEventListener('pointercancel', this._onFabUp, true)
    },
    moveFab(event) {
      // 超过 4px 才算拖动，否则当成点击，别让手抖把按钮挪走
      if (!this._fabMoved
        && Math.abs(event.clientX - this._fabStartX) <= 4
        && Math.abs(event.clientY - this._fabStartY) <= 4) {
        return
      }
      this._fabMoved = true
      this.fabDragging = true
      this.fabPos = this.clampFabPos({
        left: event.clientX - this._fabGrabX,
        top: event.clientY - this._fabGrabY
      })
    },
    endFabDrag() {
      if (this._onFabMove) {
        document.removeEventListener('pointermove', this._onFabMove, true)
        this._onFabMove = null
      }
      if (this._onFabUp) {
        document.removeEventListener('pointerup', this._onFabUp, true)
        document.removeEventListener('pointercancel', this._onFabUp, true)
        this._onFabUp = null
      }
      if (this.fabDragging) {
        this.fabDragging = false
        this.saveFabPos()
      }
    },
    onFabAction(action) {
      // 这一下是在拖球，不是点击，别顺手把编辑模式切了
      if (this._fabMoved) {
        this._fabMoved = false
        return
      }
      if (action === 'edit') this.toggleEdit()
      else if (action === 'save') this.saveLayout()
      else if (action === 'cancel') this.cancelWithConfirm()
      else if (action === 'refresh') this.refreshWithConfirm()
      else if (action === 'toggleView') this.viewMode = this.viewMode === 'list' ? 'graph' : 'list'
      else if (action === 'collapse') this.barCollapsed = true
    },
    onFabEnter() { this.fabHover = true },
    onFabLeave() {
      this.fabHover = false
      // 离开后复位，下次进来重新展开
      this.barCollapsed = false
    },
    /** 有改动就标脏：球上亮小红点，刷新/取消会先确认，避免误操作丢掉编辑 */
    markDirty() { this.dirty = true },
    refreshWithConfirm() {
      if (!this.dirty) return this.loadTopology()
      this.$modal.confirm('刷新会丢弃未保存的改动，确定继续吗？').then(() => this.loadTopology()).catch(() => {})
    },
    cancelWithConfirm() {
      if (!this.dirty) return this.cancelEdit()
      this.$modal.confirm('确定放弃本次全部改动吗？').then(() => this.cancelEdit()).catch(() => {})
    },
    /**
     * 拖拽时的自动滚动 + 滚轮滚动。
     * 两个关键点：
     * 1) 监听必须用捕获阶段（第三个参数 true）。Sortable 处理完 dragover 会 stopPropagation
     *    （sortable.esm.js 里 `!options.dragoverBubble && evt.stopPropagation`），挂在 document
     *    冒泡阶段的监听收不到事件 —— 之前那版自动滚动就是这么哑掉的。
     * 2) 指针位置从 mousemove / pointermove / dragover / touchmove 任一来源取，哪种模式拖拽都能覆盖；
     *    原生拖放期间浏览器不派发 mousemove，fallback 模式（见 dragTrack）下则是 mousemove，两种都兼容。
     * 滚轮单独监听：原生拖放会吞掉 wheel，fallback 模式下才收得到，收到就按 deltaY 滚。
     */
    startAutoScroll() {
      this.stopAutoScroll()
      this._pointerY = null
      this._onDragMove = event => {
        const point = event.touches && event.touches[0] ? event.touches[0] : event
        if (typeof point.clientY === 'number') this._pointerY = point.clientY
      }
      this._onDragWheel = event => {
        if (!this._autoScrolling) return
        const host = this.scrollHost()
        host.scrollTop += event.deltaY
        event.preventDefault()
      }
      const capture = true
      ;['dragover', 'mousemove', 'pointermove', 'touchmove'].forEach(name =>
        document.addEventListener(name, this._onDragMove, capture))
      document.addEventListener('wheel', this._onDragWheel, { capture: capture, passive: false })
      // 拖拽被 Esc 取消、或者 fallback 模式下在容器外松手时不一定走 @end，这里兜底
      document.addEventListener('dragend', this.stopAutoScroll, capture)
      document.addEventListener('mouseup', this.stopAutoScroll, capture)
      this._autoScrolling = true
      this.dragging = true
      const tick = () => {
        if (!this._autoScrolling) return
        this.autoScrollStep()
        this._autoScrollRaf = window.requestAnimationFrame(tick)
      }
      this._autoScrollRaf = window.requestAnimationFrame(tick)
    },
    stopAutoScroll() {
      this._autoScrolling = false
      this.dragging = false
      if (this._autoScrollRaf) {
        window.cancelAnimationFrame(this._autoScrollRaf)
        this._autoScrollRaf = null
      }
      const capture = true
      if (this._onDragMove) {
        ;['dragover', 'mousemove', 'pointermove', 'touchmove'].forEach(name =>
          document.removeEventListener(name, this._onDragMove, capture))
        this._onDragMove = null
      }
      if (this._onDragWheel) {
        document.removeEventListener('wheel', this._onDragWheel, { capture: capture })
        this._onDragWheel = null
      }
      document.removeEventListener('dragend', this.stopAutoScroll, capture)
      document.removeEventListener('mouseup', this.stopAutoScroll, capture)
      this._pointerY = null
    },
    /** 页面在 .app-main 里滚动（RuoYi 固定头部模式）；没开那个模式就退回文档滚动。 */
    scrollHost() {
      const main = document.querySelector('.app-main')
      if (main && main.scrollHeight - main.clientHeight > 1) return main
      return document.scrollingElement || document.documentElement
    },
    autoScrollStep() {
      const y = this._pointerY
      if (y === null || y === undefined) return 0
      const host = this.scrollHost()
      const useViewport = host === document.scrollingElement || host === document.documentElement
      const top = useViewport ? 0 : host.getBoundingClientRect().top
      const bottom = useViewport ? window.innerHeight : host.getBoundingClientRect().bottom
      const edge = this.autoScrollEdge
      let delta = 0
      if (y < top + edge) {
        delta = -this.autoScrollMaxStep * Math.min(1, (top + edge - y) / edge)
      } else if (y > bottom - edge) {
        delta = this.autoScrollMaxStep * Math.min(1, (y - (bottom - edge)) / edge)
      }
      if (!delta) return 0
      host.scrollTop += delta
      return delta
    },
    /** 箱体列表这一层的拖拽只认配电箱本身，避免箱内拖电表时把整个箱子也带着动 */
    onlyBoxItems(evt) {
      return !!evt.dragged && evt.dragged.classList.contains('enclosure-item')
    },
    /**
     * 电表能不能落进这个配电箱。
     * meter 表上有唯一键 uk_box_slave(box_id, slave_address)：同一个配电箱内从站地址不能重复。
     * 现场每个箱体都从地址 1 开始编址，所以跨箱拖动经常撞地址 —— 这里在拖动阶段就拦掉并说明原因，
     * 免得拖完点保存才收到一句看不懂的数据库报错。
     */
    groupForBox(box) {
      return { name: 'meter-topology-meter', pull: true, put: (to, from, dragEl) => this.canPutInto(box, dragEl) }
    },
    canPutInto(box, dragEl) {
      const meterId = dragEl && dragEl.dataset ? dragEl.dataset.meterId : null
      const meter = meterId ? this.findMeter(meterId) : null
      const address = meter ? meter.slaveAddress : null
      if (address === null || address === undefined) return true
      const clash = box.meters.find(item => String(item.meterId) !== String(meterId) && String(item.slaveAddress) === String(address))
      if (!clash) return true
      this.blockedHint = '「' + (box.name || box.code || '该配电箱') + '」已有从站地址 ' + address + ' 的「' + (clash.meterName || '未命名设备') + '」，同一配电箱内从站地址不能重复'
      return false
    },
    findMeter(meterId) {
      const all = flattenBoxes(this.regions.reduce((acc, region) => acc.concat(region.boxes), [])).concat(this.unassignedMeters)
      return all.find(meter => String(meter.meterId) === String(meterId)) || null
    },
    flushBlockedHint() {
      if (!this.blockedHint) return
      this.$modal.msgWarning(this.blockedHint)
      this.blockedHint = ''
    },
    /** 一次拖拽结束：先弹被拦下的原因，再收掉自动滚动与高亮状态 */
    onDragEnd() {
      this.flushBlockedHint()
      this.stopAutoScroll()
      this.draggingMeterId = null
    },
    /** 电表拖拽开始：记下正在拖的是哪一台，用于把放不进去的箱体标红 */
    onMeterDragStart(event) {
      this.startAutoScroll()
      const item = event && event.item
      this.draggingMeterId = item && item.getAttribute ? item.getAttribute('data-meter-id') : null
    },
    /** 保存前的兜底校验：跨站点改箱体时 uk_site_meter(site_id, meter_code) 也可能冲突 */
    boxAddressProblems() {
      const problems = []
      this.regions.forEach(region => region.boxes.forEach(box => {
        const seen = new Map()
        box.meters.forEach(meter => {
          const address = meter.slaveAddress
          if (address === null || address === undefined) return
          const key = String(address)
          if (seen.has(key)) {
            problems.push((box.name || box.code || '未标注配电箱') + '：从站地址 ' + address + ' 被「' + seen.get(key) + '」和「' + (meter.meterName || '未命名设备') + '」同时占用')
          } else {
            seen.set(key, meter.meterName || '未命名设备')
          }
        })
      }))
      return problems
    },
    async loadTopology() {
      this.loading = true
      this.loadError = ''
      try {
        const response = await getMeterTopology()
        const data = response.data || response
        this.regions = (data.regions || []).map(region => ({
          regionId: region.regionId,
          regionName: region.regionName,
          sortOrder: region.sortOrder,
          boxes: buildBoxes(region.meters || []),
          dropSink: []
        }))
        this.unassignedMeters = data.unassignedMeters || []
        this.originalLayout = this.cloneLayout()
        this.dirty = false
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
    /** 把电表从所有箱体分组和未分配列表里摘掉，再放回目标位置，避免依赖拖拽事件的先后顺序。 */
    detachMeter(meterId) {
      this.regions.forEach(region => region.boxes.forEach(box => {
        const at = box.meters.findIndex(meter => meter.meterId === meterId)
        if (at >= 0) box.meters.splice(at, 1)
      }))
      const looseAt = this.unassignedMeters.findIndex(meter => meter.meterId === meterId)
      if (looseAt >= 0) this.unassignedMeters.splice(looseAt, 1)
    },
    /**
     * 电表落进某个配电箱：把它的箱体归属改成目标箱体。
     * 位置由 vuedraggable 就地插好（onDragAdd 会按落点 splice），这里只负责改归属，
     * 否则保存后 buildBoxes 又会按旧箱体把它分回去，显示和落点就对不上了。
     */
    onMeterDrop(event, box) {
      if (event.added) {
        const meter = event.added.element
        meter.boxId = box.id
        meter.boxCode = box.code
        meter.boxName = box.name
        this.markDirty()
        return
      }
      // removed：这台表被拖走了。如果这个箱体因此空了，落位成功之后再问要不要把它移掉，
      // 不在拖动过程中悄悄删 —— 之前那个空箱子可能在拖到一半就消失了，反而不好判断。
      if (event.removed && !box.meters.length) {
        this.askRemoveEmptyBox(box)
      }
    },
    /**
     * 问一下空掉的箱体要不要移除。
     * 注意箱体是从设备自身的箱体归属推导出来的，所以"保留"只对当前编辑会话有效：
     * 保存并重新加载后没有设备支撑的箱体本来就不会出现，这点在提示里写清楚，免得以为能留住。
     */
    askRemoveEmptyBox(box) {
      const label = box.name || box.code || '未标注配电箱'
      this.$modal.confirm('配电箱「' + label + '」里已经没有设备了，要从本区域移除吗？\n（就算保留，保存并重新加载后它也会自动消失；除非再往里面放设备）')
        .then(() => this.removeEmptyBox(box))
        .catch(() => {})
    },
    removeEmptyBox(box) {
      this.regions.forEach(region => {
        const at = region.boxes.indexOf(box)
        if (at >= 0) region.boxes.splice(at, 1)
      })
      this.markDirty()
    },
    onSinkChange(event, regionIndex) {
      const sink = this.regions[regionIndex].dropSink
      if (!event.added) {
        sink.splice(0)
        return
      }
      // vuedraggable 已经把电表从原箱体摘掉、插进了这个中转数组；把中转数组清空再按它自己的箱体归位
      const meter = event.added.element
      sink.splice(0)
      this.moveMeter(meter, regionIndex)
    },
    /**
     * 把一台表放到目标区域（-1 表示未分配设备）。
     * 列表里的下拉、批量落位、图形视图的投放条都走这一个实现，行为才不会出现两套。
     */
    moveMeter(meter, targetRegionIndex) {
      if (!meter) return
      this.detachMeter(meter.meterId)
      const index = Number(targetRegionIndex)
      if (!(index >= 0) || !this.regions[index]) {
        this.unassignedMeters.push(meter)
      } else {
        const region = this.regions[index]
        const key = boxKeyOf(meter)
        let box = region.boxes.find(item => item.key === key)
        if (!box) {
          box = { key, id: meter.boxId, code: meter.boxCode, name: meter.boxName, meters: [] }
          region.boxes.push(box)
        }
        box.meters.push(meter)
      }
      this.markDirty()
      // 这里不再自动清理空箱体：拖拽路径会弹确认（见 onMeterDrop），列表路径保留到重载自然消失
    },
    onSelectionChange(rows) {
      this.selectedMeterIds = rows.map(row => row.meterId)
    },
    clearSelection() {
      this.selectedMeterIds = []
      if (this.$refs.meterTable) this.$refs.meterTable.clearSelection()
    },
    /** 批量落位。一次一台地走 moveMeter，顺序与单台操作完全一致。 */
    applyBatchRegion(targetRegionIndex) {
      const ids = this.selectedMeterIds.slice()
      if (!ids.length) return
      const meters = this.meterRows.filter(row => ids.indexOf(row.meterId) >= 0).map(row => row.meter)
      meters.forEach(meter => this.moveMeter(meter, targetRegionIndex))
      this.batchTarget = null
      this.$modal.msgSuccess('已移动 ' + meters.length + ' 台设备，确认后记得点「保存布局」')
    },
    /** 表在它自己配电箱内的先后（箱体顺序另说，那是拖铭牌的事）。 */
    canMove(row, direction) {
      if (!row.box) return false
      const at = row.box.meters.findIndex(meter => meter.meterId === row.meterId)
      const to = at + direction
      return at >= 0 && to >= 0 && to < row.box.meters.length
    },
    moveRow(row, direction) {
      if (!this.canMove(row, direction)) return
      const meters = row.box.meters
      const at = meters.findIndex(meter => meter.meterId === row.meterId)
      meters.splice(at + direction, 0, meters.splice(at, 1)[0])
      this.markDirty()
    },
    addressText(row) { return formatMeterAddress(row.slaveAddress) },
    statusType(code) { return getMeterStatusType(code) },
    statusLabel(code) { return getMeterStatusLabel(code) },
    addRegion() { this.regionDialog = { visible: true, name: '', region: null } },
    renameRegion(region) { this.regionDialog = { visible: true, name: region.regionName, region } },
    confirmRegion() {
      // 弹窗关掉之前重复触发（连按回车最常见）会一次创建出多个同名区域。
      // visible 是本函数唯一的同步闸门：第一次进来就置 false，后续几次直接返回。
      if (!this.regionDialog.visible) return
      const name = this.regionDialog.name.trim()
      if (!name) return this.$modal.msgWarning('请输入区域名称')
      // 区域名不允许重复：一个区域名对应一块地方，重名之后布局、下拉和汇总都分不清是哪一个
      const duplicated = this.regions.some(region => region !== this.regionDialog.region && region.regionName === name)
      if (duplicated) return this.$modal.msgWarning('已存在名为「' + name + '」的区域，请换一个名称')
      if (this.regionDialog.region) this.regionDialog.region.regionName = name
      else this.regions.push({ regionId: null, regionName: name, sortOrder: this.regions.length + 1, boxes: [], dropSink: [] })
      this.regionDialog.visible = false
      this.markDirty()
    },
    removeRegion(index) {
      const region = this.regions[index]
      if (!region) return
      if (flattenBoxes(region.boxes).length) return this.$modal.msgWarning('请先迁移该区域内的设备')
      // 原来是点一下直接删。连点两下会连删两个：第一次删掉后 regions 重排，第二次点击已经落到别的区域上了。
      // 走一次确认既拦住重复触发，也是删除该有的确认。
      this.$modal.confirm('确认删除区域「' + region.regionName + '」？').then(() => {
        // 确认期间索引可能已经变了，按对象本身定位更稳
        const at = this.regions.indexOf(region)
        if (at >= 0) this.regions.splice(at, 1)
        this.markDirty()
      }).catch(() => {})
    },
    cancelEdit() {
      const layout = this.originalLayout || { regions: [], unassignedMeters: [] }
      this.regions = (layout.regions || []).map(region => ({ ...region, dropSink: region.dropSink || [] }))
      this.unassignedMeters = layout.unassignedMeters || []
      this.editing = false
      this.dirty = false
    },
    async saveLayout() {
      // 加载失败时 regions 是空的，此时保存等于用空配置覆盖现有区域 —— 直接拒绝。
      if (this.loadError) {
        this.$modal.msgError('拓扑配置未成功加载，已阻止保存以免覆盖现有区域，请先刷新')
        return
      }
      this.saving = true
      try {
        const problems = this.boxAddressProblems()
        if (problems.length) {
          this.$modal.msgError('保存前请先解决从站地址冲突：' + problems.join('；'))
          return
        }
        // 箱体分组是展示层；落库两件事：区域→电表及其先后顺序，以及每台表的箱体归属
        const regions = this.regions.map(region => ({
          regionId: region.regionId,
          regionName: region.regionName,
          meters: flattenBoxes(region.boxes)
        }))
        await saveMeterTopology({ regions })
        this.$modal.msgSuccess('拓扑布局已保存')
        this.editing = false
        await this.loadTopology()
      } catch (error) {
        // 具体原因由 request.js 的拦截器按后端 msg 弹出（含 ServiceException 的说明），
        // 这里只补一句"编辑内容还在"，别去覆盖它。
        this.$modal.msgError('保存未成功，当前编辑内容已保留')
      } finally { this.saving = false }
    },
    openRecord(meter) {
      if (meter && meter.meterId) this.$refs.realtimeDetail.open(meter.meterId, 'running')
    }
  }
}
</script>

<style scoped>
.topology-page { min-height: calc(100vh - 84px); background: #f5f7fa; }
.page-heading { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }
.page-heading h1 { margin: 0 0 6px; color: #1f2937; font-size: 24px; }
.page-heading p { margin: 0; color: #718096; font-size: 13px; }
.toolbar { display: flex; gap: 8px; }
.edit-tools { display: flex; align-items: flex-start; gap: 12px; margin-bottom: 16px; padding: 10px 14px; color: #587083; border: 1px solid #dceaf1; border-radius: 8px; background: #edf8fc; font-size: 13px; line-height: 20px; }

/* ---------- 区域表头：区域名、汇总、状态药丸全部靠左一排 ---------- */
.region { margin-bottom: 20px; }
.region-head { display: flex; align-items: baseline; flex-wrap: wrap; gap: 14px; margin-bottom: 12px; padding-left: 2px; }
.region-head h2 { margin: 0; color: #1f2937; font-size: 18px; font-weight: 600; letter-spacing: .2px; }
.region-head .stats { display: flex; align-items: baseline; flex-wrap: wrap; gap: 11px; color: #718096; font-size: 13px; }
.region-head .stats b { color: #1677a8; font-size: 16px; font-weight: 700; }
.region-head .sep { width: 1px; height: 12px; background: #d5dee7; align-self: center; }
.region-head .counts { padding: 3px 10px; color: #475569; border: 1px solid #e1e8ef; border-radius: 20px; background: #fff; font-size: 12px; }
.region-actions { margin-left: auto; }
.delete-action { color: #d9534f; }

/* ---------- 配电箱外壳 ---------- */
.enclosure {
  position: relative;
  margin-bottom: 14px;
  padding: 13px 15px 15px;
  border: 1px solid #aeb9c5;
  border-radius: 13px;
  background: linear-gradient(180deg, #fdfefe 0%, #eff3f7 16%, #e3e9ef 62%, #d8dfe7 100%);
  box-shadow: inset 0 1px 0 #fff, inset 0 -2px 3px rgba(15, 23, 42, .08), 0 3px 11px rgba(15, 23, 42, .1);
}
.screw { position: absolute; width: 9px; height: 9px; border-radius: 50%; background: radial-gradient(circle at 34% 30%, #fff 0%, #c6cfd9 45%, #8b98a5 100%); box-shadow: inset 0 -1px 1px rgba(15, 23, 42, .3), 0 1px 1px rgba(255, 255, 255, .85); }
.screw::after { content: ""; position: absolute; inset: 0; width: 5.5px; height: 1.3px; margin: auto; border-radius: 1px; background: #6d7a88; transform: rotate(-32deg); }
.screw.tl { left: 7px; top: 7px; } .screw.tr { right: 7px; top: 7px; }
.screw.bl { left: 7px; bottom: 7px; } .screw.br { right: 7px; bottom: 7px; }

.enc-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.enc-head.is-handle { cursor: grab; }
.enc-head.is-handle:active { cursor: grabbing; }
.plate { display: inline-flex; align-items: baseline; gap: 10px; padding: 6px 12px; border: 1px solid #c4cfda; border-radius: 5px; background: linear-gradient(180deg, #fff, #eef3f8); box-shadow: inset 0 1px 0 #fff, 0 1px 2px rgba(15, 23, 42, .1); }
.plate .grip { color: #8b98a5; font-size: 13px; letter-spacing: -1px; }
.plate .code { color: #26343f; font-family: "Cascadia Mono", Consolas, "Courier New", monospace; font-size: 13.5px; font-weight: 700; letter-spacing: .6px; }
.plate .nm { color: #5d6d7e; font-size: 12.5px; }
.plate .cnt { color: #8b98a5; font-size: 12px; }
.plate.is-loose .code { color: #7c8896; }
.enc-stats { color: #64748b; font-size: 12px; }

/* 拖拽时悬停到哪个配电箱就整体高亮，明确"会放到这里"。
   用 :hover 而不是拖拽事件：fallback 拖拽模式下页面照常收鼠标事件，:hover 会正常更新；
   而克隆体被 Sortable 设了 pointer-events:none，不会挡住底下的元素。 */
.topology-page.is-dragging .enclosure { transition: border-color .12s ease, box-shadow .12s ease; }
.topology-page.is-dragging .enclosure:hover {
  border-color: #17557f;
  box-shadow: 0 0 0 2px rgba(23, 85, 127, .28), 0 4px 14px rgba(15, 23, 42, .14);
}
.topology-page.is-dragging .enclosure:hover .plate {
  border-color: #124a70;
  background: linear-gradient(180deg, #2a7099, #17557f);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, .22), 0 1px 3px rgba(15, 23, 42, .25);
}
.topology-page.is-dragging .enclosure:hover .plate .code,
.topology-page.is-dragging .enclosure:hover .plate .nm,
.topology-page.is-dragging .enclosure:hover .plate .cnt,
.topology-page.is-dragging .enclosure:hover .plate .grip { color: #fff; }
.topology-page.is-dragging .enclosure:hover .cavity {
  background: linear-gradient(180deg, #c9deec 0%, #d8e8f3 11%, #e6f0f7 100%);
  box-shadow: inset 0 3px 8px rgba(23, 85, 127, .3), inset 0 -1px 0 rgba(255, 255, 255, .85);
}
.topology-page.is-dragging .enclosure:hover .enc-stats { color: #17557f; font-weight: 600; }
.topology-page.is-dragging .enclosure:hover .drop-empty { color: #17557f; border-color: #7fb0cd; }
/* 放不进去的箱体（从站地址会撞）用红色区分，免得亮着蓝框却落不下去 */
.topology-page.is-dragging .enclosure.is-blocked:hover { border-color: #a4413c; box-shadow: 0 0 0 2px rgba(164, 65, 60, .22), 0 4px 14px rgba(15, 23, 42, .14); }
.topology-page.is-dragging .enclosure.is-blocked:hover .plate { border-color: #8a3936; background: linear-gradient(180deg, #b35954, #a4413c); }
.topology-page.is-dragging .enclosure.is-blocked:hover .cavity {
  background: linear-gradient(180deg, #ecd7d5 0%, #f1e0df 11%, #f7eceb 100%);
  box-shadow: inset 0 3px 8px rgba(164, 65, 60, .28), inset 0 -1px 0 rgba(255, 255, 255, .85);
}
.topology-page.is-dragging .enclosure.is-blocked:hover .enc-stats { color: #a4413c; }

/* 内凹腔体 + DIN 导轨（导轨躲在电表身后，只在列间缝隙露出来） */
.cavity { position: relative; margin-top: 10px; padding: 14px; border-radius: 9px; background: linear-gradient(180deg, #ccd5de 0%, #dbe2e9 11%, #e7ecf1 100%); box-shadow: inset 0 3px 7px rgba(15, 23, 42, .24), inset 0 -1px 0 rgba(255, 255, 255, .85); }
.rail { position: absolute; left: 14px; right: 14px; top: 21px; height: 9px; border-radius: 2px; background: linear-gradient(180deg, #a9b4c0 0%, #8d9aa7 38%, #6e7c8a 64%, #98a4b1 100%); box-shadow: 0 1px 2px rgba(15, 23, 42, .32), inset 0 1px 0 rgba(255, 255, 255, .45); }
.rail::after { content: ""; position: absolute; inset: 0; border-radius: 2px; background: repeating-linear-gradient(90deg, transparent 0 13px, rgba(15, 23, 42, .2) 13px 15px); }
.rail.is-loose { background: repeating-linear-gradient(90deg, #b6c1cc 0 12px, transparent 12px 20px); box-shadow: none; }
.rail.is-loose::after { display: none; }

.box-list { display: block; }
.meters { position: relative; display: grid; grid-template-columns: repeat(auto-fill, minmax(322px, 1fr)); gap: 12px; }
.drop-zone { min-height: 92px; padding: 2px; border-radius: 7px; transition: background .18s ease; }
.drop-zone:empty { background: #f8fafc; }
.drop-empty { display: grid; min-height: 76px; place-items: center; color: #94a3b8; border: 1px dashed #cdd9e3; border-radius: 7px; font-size: 12px; }
.drag-ghost { opacity: .4; }
.meters .drag-item { cursor: grab; }
/* forceFallback 下跟着鼠标走的是原卡的克隆体（Sortable 已设好尺寸与 pointer-events），这里只加"被拎起来"的观感 */
.dragging-clone { border-radius: 8px; box-shadow: 0 14px 30px rgba(15, 23, 42, .35); }

/* 跨区域投放条 */
.region-sink { min-height: 46px; margin-bottom: 14px; border: 1px dashed #a9c3d4; border-radius: 9px; background: #f2f9fd; transition: background .18s ease, border-color .18s ease; }
.region-sink:hover { border-color: #6fa8c8; background: #e8f5fc; }
.sink-hint { display: grid; min-height: 44px; place-items: center; color: #4d7f9c; font-size: 12.5px; }

.empty-state { padding: 50px 0; border-radius: 10px; background: #fff; }

/* ---------- 悬浮球（编辑入口，带文字标签的胶囊） ---------- */
.fab { position: fixed; right: 28px; bottom: 40px; z-index: 1500; display: flex; align-items: center; gap: 8px; user-select: none; touch-action: none; }
.ball { position: relative; display: inline-flex; align-items: center; gap: 7px; height: 46px; padding: 0 18px 0 15px; color: #fff; border: 0; border-radius: 23px;
  background: linear-gradient(160deg, #2b7fb8, #17557f); box-shadow: 0 6px 18px rgba(15, 23, 42, .28);
  font: inherit; font-size: 13.5px; font-weight: 600; white-space: nowrap; cursor: pointer; transition: transform .16s ease, box-shadow .16s ease; }
.ball:hover { transform: translateY(-2px); box-shadow: 0 10px 24px rgba(15, 23, 42, .32); }
.fab.is-editing .ball { background: linear-gradient(160deg, #35a06e, #1d7a4b); }
.fab.is-picked .ball { transition: none; transform: none; }
.ball .ico-close, .ball .txt-close { display: none; }
/* 鼠标进来（工具条展开）时，球从「编辑」变成「收起」——点一下就把工具条压住 */
.fab.is-open .ball .ico-open, .fab.is-open .ball .txt-open { display: none; }
.fab.is-open .ball .ico-close, .fab.is-open .ball .txt-close { display: inline; }
.ball .ico-open, .ball .ico-close { font-style: normal; font-size: 15px; }
.ball .dot { width: 8px; height: 8px; margin-left: -1px; border-radius: 50%; background: #ff5a52; box-shadow: 0 0 0 2px rgba(255, 90, 82, .3); }
/* 正在拖电表时让开：别挡住落点，也别干扰悬停高亮 */
.fab.is-meter-dragging { opacity: .35; pointer-events: none; }

/* 横向工具条：悬停展开，向左拉出 */
.bar { display: flex; align-items: center; gap: 4px; padding: 6px; border: 1px solid #e1e8ef; border-radius: 30px; background: #fff;
  box-shadow: 0 8px 22px rgba(15, 23, 42, .18); opacity: 0; transform: translateX(14px) scaleX(.85); transform-origin: right center;
  pointer-events: none; transition: opacity .18s ease, transform .18s ease; }
.fab.is-open .bar { opacity: 1; transform: translateX(0) scaleX(1); pointer-events: auto; }
.bar-item { position: relative; display: inline-flex; align-items: center; gap: 6px; padding: 8px 12px; color: #475569; border: 0; border-radius: 22px; background: #f7fafc;
  font: inherit; font-size: 12.5px; white-space: nowrap; cursor: pointer; }
.bar-item i { font-style: normal; }
.bar-item:hover { background: #eef4f9; }
.bar-item.is-save { color: #fff; background: linear-gradient(160deg, #35a06e, #1d7a4b); }
.bar-item.is-cancel { color: #fff; background: linear-gradient(160deg, #b35954, #8a3936); }
.bar-item.is-save:hover { filter: brightness(1.07); background: linear-gradient(160deg, #35a06e, #1d7a4b); }
.bar-item.is-cancel:hover { filter: brightness(1.07); background: linear-gradient(160deg, #b35954, #8a3936); }
.bar-item.is-busy { opacity: .65; pointer-events: none; }
.bar-div { width: 1px; height: 20px; margin: 0 3px; background: #e1e8ef; }
/* 每项悬停时在它正上方弹一句说明（含"会丢弃改动"这类风险）。用自绘气泡而不是 el-tooltip：
   后者的 auto 定位会往左弹，正好压住行内按钮。 */
.bar-item .tip { position: absolute; bottom: calc(100% + 11px); left: 50%; z-index: 5; padding: 7px 11px; color: #fff; background: #1f2d3d;
  border-radius: 7px; box-shadow: 0 8px 20px rgba(15, 23, 42, .3); font-size: 11.5px; font-weight: 400; white-space: nowrap;
  opacity: 0; transform: translateX(-50%) translateY(5px); pointer-events: none; transition: opacity .15s ease, transform .15s ease; }
.bar-item .tip::after { content: ""; position: absolute; left: 50%; bottom: -4px; width: 8px; height: 8px; margin-left: -4px; background: #1f2d3d; transform: rotate(45deg); }
.bar-item:hover .tip { opacity: 1; transform: translateX(-50%) translateY(0); }

/* ---------- 列表视图 ---------- */
.view-switch { margin-right: 4px; }
.list-view { padding: 14px 16px 16px; border: 1px solid #e1e8ef; border-radius: 10px; background: #fff; box-shadow: 0 2px 8px rgba(15, 23, 42, .04); }
.list-toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; margin-bottom: 12px; }
.list-toolbar .el-select { width: 150px; }
.list-search { width: 220px; }
.list-count { margin-left: auto; color: #718096; font-size: 13px; }
.list-count b { color: #1677a8; font-size: 15px; }
.batch-bar { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; margin-bottom: 12px; padding: 8px 12px; border: 1px solid #dceaf1; border-radius: 8px; background: #edf8fc; }
.batch-bar .picked { color: #587083; font-size: 13px; }
.batch-bar .picked b { color: #1677a8; font-size: 15px; }
.batch-select { width: 170px; }
.meter-table .region-select { width: 100%; }
.meter-table .muted { color: #94a3b8; }

@media (max-width: 768px) {
  .page-heading { align-items: flex-start; flex-direction: column; gap: 12px; }
  .toolbar { flex-wrap: wrap; }
  .meters { grid-template-columns: 1fr; }
}
</style>
