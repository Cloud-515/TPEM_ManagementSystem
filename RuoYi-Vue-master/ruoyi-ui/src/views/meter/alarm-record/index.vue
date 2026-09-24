<template>
  <div class="app-container alarm-record-page" v-loading="loading">
    <section class="page-heading">
      <div>
        <h1>{{ pageTitle }}</h1>
        <p>{{ subtitle }}</p>
      </div>
      <el-button icon="el-icon-back" @click="returnToSource">{{ returnLabel }}</el-button>
    </section>

    <el-alert v-if="loadError" class="load-error" :title="loadError" type="error" :closable="false" show-icon />

    <template v-if="detail && detail.meterId">
      <!-- 状态横幅：一眼回答"什么状态、数据新不新鲜" -->
      <div class="state-banner" :class="'is-' + banner.tone">
        <div class="banner-main">
          <strong>{{ banner.title }}</strong>
          <span>{{ banner.why }}</span>
        </div>
        <div class="banner-fresh">
          <span>最后采集</span>
          <b>{{ detail.lastCollectTime || '--' }}</b>
          <em v-if="freshness">{{ freshness }}</em>
        </div>
      </div>

      <!-- 实时读数：这一页最该先看到的东西。窄屏按 flex-wrap 折行，不做硬挤 -->
      <div class="kpi-row">
        <div v-for="item in kpis" :key="item.label" class="kpi" :class="[{ 'is-warn': item.warn, 'is-wide': item.wide }, 'tone-' + item.tone]">
          <div class="kpi-head">
            <i class="kpi-icon" :class="item.icon"></i>
            <span>{{ item.label }}</span>
          </div>
          <b>{{ item.value }}<i v-if="item.unit" class="unit">{{ item.unit }}</i></b>
          <div v-if="item.bar" class="kpi-bar">
            <span class="fill" :style="{ width: item.bar.percent + '%' }"></span>
            <span class="mark" :style="{ left: item.bar.markPercent + '%' }"></span>
          </div>
          <small v-if="item.hint">{{ item.hint }}</small>
        </div>
      </div>

      <el-card shadow="never" class="panel">
        <div slot="header" class="section-header"><span>设备档案</span><span class="count-label">低频信息，排在这里</span></div>
        <div class="archive">
          <dl>
            <dt>设备名称</dt><dd>{{ detail.meterName || '--' }}</dd>
            <dt>设备编码</dt><dd>{{ detail.meterCode || '--' }}</dd>
            <dt>从站地址</dt><dd>{{ formatMeterAddress(detail.slaveAddress) }}</dd>
          </dl>
          <dl>
            <dt>站点</dt><dd>{{ detail.siteName || '--' }}</dd>
            <dt>配电箱</dt><dd>{{ detail.boxName || '--' }}</dd>
            <dt>设备状态</dt><dd><el-tag size="mini" :type="getMeterStatusType(detail.statusCode)">{{ statusText }}</el-tag></dd>
          </dl>
          <dl>
            <dt>累计正向电能</dt><dd>{{ formatMeterNumber(detail.forwardActiveEnergy) }} kWh</dd>
            <dt>累计反向电能</dt><dd>{{ formatMeterNumber(detail.reverseActiveEnergy) }} kWh</dd>
            <dt>数据质量</dt><dd>{{ detail.dataQuality || '--' }}</dd>
          </dl>
        </div>
      </el-card>

      <div class="jump-row">
        <el-button size="small" @click="openPage('MeterEnergy')">能耗分析</el-button>
        <el-button size="small" @click="openPage('MeterQuality')">电能质量</el-button>
        <el-button size="small" @click="openPage('MeterHistory', 'quality')">质量历史</el-button>
        <el-button size="small" @click="openPage('MeterHistory')">历史追溯</el-button>
      </div>
    </template>

    <el-empty v-else-if="!loading && !loadError" :description="emptyMessage" />
  </div>
</template>

<script>
import { getRealtimeDetail } from '@/api/system/meter'
import {
  getMeterStatusLabel,
  getMeterStatusType,
  formatMeterNumber,
  formatMeterAddress,
  hasMeterValue,
  toMeterNumber,
  VOLTAGE_PHASE_MIN,
  VOLTAGE_PHASE_MAX,
  POWER_FACTOR_REFERENCE
} from '@/views/meter/components/meter-utils'

// 数据多久没更新算陈旧（与仪表盘"数据陈旧"提示同口径：超过 5 分钟）
const STALE_MINUTES = 5

export default {
  name: 'MeterAlarmRecord',
  data() {
    return { loading: false, detail: null, requestId: 0, emptyMessage: '未找到对应设备', loadError: '' }
  },
  computed: {
    /** 设备号优先取路径参数（每台设备独立标签页），兼容旧的 ?meterId= 链接 */
    currentMeterId() {
      return this.$route.params.meterId || this.$route.query.meterId
    },
    /**
     * 页面标题：报警时是「设备名报警」，正常时只显示设备名。
     * 只写设备名区分不出这页是干什么的（和实时运行里的设备名重复）；
     * 而正常设备挂"报警"又说不通，所以按状态拼。
     * h1、标签页、面包屑都用这一处，避免三处文案各说各的。
     */
    pageTitle() {
      const name = (this.detail && this.detail.meterName) || '设备详情'
      if (!this.detail || !this.detail.meterId) return name
      return this.detail.statusCode === 'OK' ? name : name + '报警'
    },
    statusText() {
      // 后端下发的 statusText 更权威，缺失时回退到前端映射
      return (this.detail && (this.detail.statusText || getMeterStatusLabel(this.detail.statusCode))) || '--'
    },
    subtitle() {
      if (!this.detail || !this.detail.meterId) return '按设备查看告警状态与运行读数'
      const parts = [this.detail.siteName, this.detail.boxName].filter(hasMeterValue)
      return (parts.length ? parts.join(' / ') + ' · ' : '') + '最后采集 ' + (this.detail.lastCollectTime || '--')
    },
    /**
     * 状态横幅。三档说清"发生了什么"，但不下没有依据的结论：
     * 状态本身就是由最近一次采集判定的，所以文案只说这一点。
     */
    banner() {
      const code = this.detail ? this.detail.statusCode : ''
      const type = getMeterStatusType(code)
      const tone = code === 'OK' ? 'ok' : (type === 'warning' ? 'warn' : 'alarm')
      const why = {
        OK: '最近一次采集没有触发任何判定',
        FAULT: '最近一次采集没有有效读数，下方为最后一次成功采集的值',
        NODATA: '最近一次采集没有有效读数，下方为最后一次成功采集的值',
        WAITING: '尚未完成首次采集，下方为最后一次成功采集的值',
        ABNORMAL: '最近一次采集触发了判定阈值',
        VOLTAGE_BAD: '最近一次采集触发了判定阈值',
        PF_LOW: '最近一次采集触发了判定阈值'
      }[code] || '最近一次采集触发了判定阈值'
      return { tone, title: code === 'OK' ? '运行正常' : this.statusText, why }
    },
    /** "约 12 分钟前 · 已陈旧"。时间解析不了就整条不显示，不猜。 */
    freshness() {
      const raw = this.detail && this.detail.lastCollectTime
      if (!hasMeterValue(raw)) return ''
      const at = new Date(String(raw).replace(/-/g, '/'))
      if (Number.isNaN(at.getTime())) return ''
      const minutes = Math.floor((Date.now() - at.getTime()) / 60000)
      if (minutes < 1) return '刚刚'
      const ago = minutes < 60 ? minutes + ' 分钟前' : Math.floor(minutes / 60) + ' 小时前'
      return minutes >= STALE_MINUTES ? '约 ' + ago + ' · 已陈旧' : '约 ' + ago
    },
    returnLabel() {
      return this.$route.query.from === 'exceptions' ? '返回异常设备' : '返回实时运行'
    },
    kpis() {
      const d = this.detail || {}
      const pf = toMeterNumber(d.powerFactorTotal)
      const frequency = toMeterNumber(d.frequency)
      const phaseVoltages = [d.voltageA, d.voltageB, d.voltageC]
      return [
        { label: '总有功功率', icon: 'el-icon-odometer', tone: 'blue', value: formatMeterNumber(d.activePowerKw), unit: 'kW' },
        { label: '总无功功率', icon: 'el-icon-coin', tone: 'indigo', value: formatMeterNumber(d.reactivePowerKvar), unit: 'kvar' },
        {
          label: '功率因数',
          icon: 'el-icon-pie-chart',
          tone: 'teal',
          value: formatMeterNumber(d.powerFactorTotal),
          // 0~1 的刻度条 + 0.85 考核线，和电表卡片上的功率因数条同一套口径
          bar: pf === null ? null : { percent: Math.min(100, Math.max(0, pf * 100)), markPercent: POWER_FACTOR_REFERENCE * 100 },
          hint: '考核值 ' + POWER_FACTOR_REFERENCE,
          warn: pf !== null && pf < POWER_FACTOR_REFERENCE
        },
        {
          label: '频率',
          icon: 'el-icon-timer',
          tone: 'slate',
          value: formatMeterNumber(d.frequency),
          unit: 'Hz',
          warn: frequency !== null && (frequency < 49.5 || frequency > 50.5)
        },
        {
          label: 'A / B / C 相电压',
          icon: 'el-icon-lightning',
          tone: 'amber',
          wide: true,
          value: phaseVoltages.map(value => formatMeterNumber(value)).join(' / '),
          unit: 'V',
          hint: VOLTAGE_PHASE_MIN + '~' + VOLTAGE_PHASE_MAX + ' V',
          warn: phaseVoltages.some(value => {
            const number = toMeterNumber(value)
            return number !== null && (number < VOLTAGE_PHASE_MIN || number > VOLTAGE_PHASE_MAX)
          })
        },
        {
          label: 'A / B / C 相电流',
          icon: 'el-icon-data-line',
          tone: 'violet',
          wide: true,
          value: [d.currentA, d.currentB, d.currentC].map(value => formatMeterNumber(value)).join(' / '),
          unit: 'A'
        }
      ]
    }
  },
  created() { this.loadDetail() },
  // 本页被 tagsView 缓存（keep-alive）：再次进来不会重跑 created，
  // 只在 created 里加载的话，从 A 设备返回后再点 B 会一直显示 A 的数据。
  // 首次挂载时 created 已经在加载，这里用 detail 是否已有值避免重复请求。
  activated() { if (this.detail) this.loadDetail() },
  watch: { currentMeterId(meterId) { if (meterId) this.loadDetail() } },
  methods: {
    getMeterStatusType,
    formatMeterNumber,
    formatMeterAddress,
    returnToSource() {
      this.$router.push({ name: this.$route.query.from === 'exceptions' ? 'MeterExceptionDevices' : 'MeterRealtime' })
    },
    openPage(name, category) {
      if (!this.detail || !this.detail.meterId) return
      const query = { meterId: this.detail.meterId }
      if (category) query.category = category
      this.$router.push({ name, query })
    },
    async loadDetail() {
      const meterId = this.currentMeterId
      if (!meterId) { this.emptyMessage = '缺少设备信息'; return }
      // 请求归属校验：快速切设备时，先发的旧响应不能覆盖后到的新结果
      const requestId = ++this.requestId
      this.loading = true
      this.loadError = ''
      try {
        const response = await getRealtimeDetail(meterId)
        if (requestId !== this.requestId) return
        this.detail = response.data || response
        // 失败与"设备不存在"要分开：前者给错误提示 + 可重试，后者才是空态
        if (!this.detail || !this.detail.meterId) { this.detail = null; this.emptyMessage = '未找到对应设备' }
        this.applyTitle()
      } catch (error) {
        if (requestId !== this.requestId) return
        this.detail = null
        this.loadError = '设备详情加载失败，请刷新重试'
      } finally { if (requestId === this.requestId) this.loading = false }
    },
    /**
     * 把设备名同步给标签页与面包屑。
     * 注意**不能改 $route.meta.title**：meta 是路由记录上的同一个对象，所有设备共用，
     * 改了会把这台设备的名字留给下一次导航（表现为"打开 B 却显示 A 的名字"）。
     * tagsView 的标签渲染的是 view.title（addVisitedView 时从 meta.title 拷了一份），
     * 所以这里给一份带 title 的视图对象，只改这一个标签；面包屑的末级也读这个 title。
     * 预览环境没有 Vuex，dispatch 前先判一下。
     */
    applyTitle() {
      if (!this.$store || typeof this.$store.dispatch !== 'function') return
      this.$store.dispatch('tagsView/updateVisitedView', Object.assign({}, this.$route, { title: this.pageTitle })).catch(() => {})
    }
  }
}
</script>

<style scoped>
.alarm-record-page { min-height: calc(100vh - 84px); background: #f5f7fa; }
.page-heading { display: flex; flex-wrap: wrap; align-items: flex-start; justify-content: space-between; gap: 10px 16px; margin-bottom: 16px; }
.page-heading h1 { margin: 0 0 4px; color: #1f2937; font-size: 24px; font-weight: 600; }
.page-heading p { margin: 0; color: #718096; font-size: 13px; }
.load-error { margin-bottom: 14px; }

/* 状态横幅 */
.state-banner { display: flex; flex-wrap: wrap; align-items: center; gap: 12px 18px; padding: 16px 20px; border-radius: 8px; border: 1px solid #e3edf3; background: #fff; }
.state-banner.is-alarm { border-color: #f3cfcb; border-left: 5px solid #c94747; background: linear-gradient(100deg, #fdf0ef, #fff); }
.state-banner.is-warn { border-color: #f0d9a8; border-left: 5px solid #d98b1d; background: linear-gradient(100deg, #fff8ea, #fff); }
.state-banner.is-ok { border-color: #cbe8d9; border-left: 5px solid #2f9e6f; background: linear-gradient(100deg, #eefaf3, #fff); }
.banner-main { flex: 1 1 320px; min-width: 0; }
.banner-main strong { display: block; font-size: 20px; }
.is-alarm .banner-main strong { color: #a4413c; }
.is-warn .banner-main strong { color: #a9711a; }
.is-ok .banner-main strong { color: #1d7a4b; }
.banner-main span { display: block; margin-top: 4px; color: #718096; font-size: 13px; }
.banner-fresh { flex: 0 0 auto; margin-left: auto; text-align: right; }
.banner-fresh span { display: block; color: #718096; font-size: 12px; }
.banner-fresh b { display: block; color: #243b53; font-size: 15px; font-variant-numeric: tabular-nums; }
.banner-fresh em { display: block; margin-top: 2px; font-style: normal; color: #a9711a; font-size: 12px; }
/* 窄屏时把"最后采集"移到左侧单独一行，不再挤在右边 */
@media (max-width: 720px) {
  .banner-fresh { margin-left: 0; text-align: left; }
}

/* 实时读数：auto-fit 自适应栅格（宽了一行多张、窄了自动减列）。
   不用断点阶梯：系统 150% 缩放时 CSS 宽度会落在断点之间，容易退化成一行一张 */
.kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(158px, 1fr)); gap: 12px; margin: 14px 0; }
.kpi { position: relative; grid-column: span 1; min-width: 0; padding: 14px 16px 15px; overflow: hidden;
       background: #fff; border: 1px solid #e3edf3; border-radius: 8px; transition: box-shadow .18s ease, transform .18s ease; }
.kpi:hover { transform: translateY(-1px); box-shadow: 0 6px 16px rgba(15, 23, 42, .08); }
/* 顶部一条 3px 的主题色，替掉"一整片白"的平淡感 */
.kpi::before { content: ""; position: absolute; inset: 0 0 auto 0; height: 3px; background: #cbd8e4; }
.kpi.tone-blue::before { background: linear-gradient(90deg, #2586b6, #4fb0d8); }
.kpi.tone-indigo::before { background: linear-gradient(90deg, #5a6cb5, #8b9ae0); }
.kpi.tone-teal::before { background: linear-gradient(90deg, #2f9e6f, #6ec39a); }
.kpi.tone-amber::before { background: linear-gradient(90deg, #d98b1d, #eab765); }
.kpi.tone-violet::before { background: linear-gradient(90deg, #7b61b5, #a693d8); }
.kpi.tone-slate::before { background: linear-gradient(90deg, #64798f, #9aabbd); }

.kpi-head { display: flex; align-items: center; gap: 7px; }
.kpi-head span { color: #718096; font-size: 12px; }
.kpi-icon { display: inline-flex; align-items: center; justify-content: center; width: 20px; height: 20px; border-radius: 6px;
            font-size: 12px; color: #fff; background: #9aabbd; }
.tone-blue .kpi-icon { background: #2586b6; }
.tone-indigo .kpi-icon { background: #5a6cb5; }
.tone-teal .kpi-icon { background: #2f9e6f; }
.tone-amber .kpi-icon { background: #d98b1d; }
.tone-violet .kpi-icon { background: #7b61b5; }
.kpi b { display: block; margin-top: 9px; color: #243b53; font-size: 22px;
         font-variant-numeric: tabular-nums; line-height: 1.2; }
.kpi b .unit { margin-left: 4px; font-style: normal; color: #718096; font-size: 12px; }
.kpi small { display: block; margin-top: 6px; color: #9aa8b8; font-size: 11px; }
.kpi.is-warn { border-color: #f0d9a8; background: #fffdf6; }
.kpi.is-warn::before { background: linear-gradient(90deg, #d98b1d, #f0c987); }
.kpi.is-warn b { color: #b8791a; }

/* 功率因数的 0~1 刻度条 + 考核线（与电表卡片同一套表现） */
.kpi-bar { position: relative; height: 6px; margin-top: 10px; border-radius: 3px; background: #eef3f8; }
.kpi-bar .fill { position: absolute; inset: 0 auto 0 0; border-radius: 3px; background: linear-gradient(90deg, #2f9e6f, #6ec39a); }
.kpi.is-warn .kpi-bar .fill { background: linear-gradient(90deg, #d98b1d, #eab765); }
.kpi-bar .mark { position: absolute; top: -3px; width: 2px; height: 12px; background: #94a3b8; }

.panel { margin-bottom: 14px; }
.section-header { display: flex; align-items: center; justify-content: space-between; color: #1f2937; font-size: 15px; font-weight: 600; }
.count-label { color: #9aa8b8; font-size: 12px; font-weight: 400; }
/* 档案：窄屏整列落下，不会把标签和值挤成竖排 */
.archive { display: flex; flex-wrap: wrap; gap: 6px 28px; }
.archive dl { flex: 1 1 260px; min-width: 240px; display: grid; grid-template-columns: 84px 1fr; row-gap: 10px; column-gap: 8px; margin: 0; font-size: 13px; }
@media (max-width: 560px) {
  .archive dl { flex-basis: 100%; }
}
.archive dt { color: #718096; }
.archive dd { margin: 0; color: #243b53; overflow-wrap: break-word; }
.jump-row { display: flex; flex-wrap: wrap; gap: 8px; }

</style>
