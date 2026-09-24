<template>
  <button type="button" class="meter-device" :class="['is-' + tone, { 'is-off': isOff }]" :title="deviceTitle" @click="handleClick">
    <span class="accent"></span>

    <div class="m-head">
      <span class="m-led" :class="'led-' + tone"></span>
      <span class="m-name">{{ meter.meterName || '未命名设备' }}</span>
      <span v-if="tone !== 'ok'" class="m-state" :class="'t-' + tone">{{ statusLabel }}</span>
    </div>

    <div class="m-lcd">
      <div class="lcd-p">
        <span class="v">{{ isOff ? '----' : formatNumber(meter.activePowerKw) }}</span>
        <span class="u">kW 总有功</span>
      </div>
      <div class="lcd-row">
        <span class="rl">I<span class="u2">(A)</span></span>
        <span v-for="cell in currentCells" :key="cell.key" class="cell" :class="{ warn: cell.warn }">
          <i>{{ cell.label }}</i><b>{{ cell.text }}</b>
        </span>
      </div>
      <div class="lcd-row">
        <span class="rl">U<span class="u2">(V)</span></span>
        <span v-for="cell in voltageCells" :key="cell.key" class="cell" :class="{ warn: cell.warn }">
          <i>{{ cell.label }}</i><b>{{ cell.text }}</b>
        </span>
      </div>
      <div class="lcd-row dim">
        <span class="rl">PF</span>
        <span class="cell" :class="{ warn: pfBelowReference }"><b>{{ value(meter.powerFactorTotal) }}</b></span>
        <span class="rl">F<span class="u2">(Hz)</span></span>
        <span class="cell"><b>{{ value(meter.frequency) }}</b></span>
      </div>
    </div>

    <div class="m-pf">
      <span class="cap">功率因数</span>
      <span class="track">
        <i :class="{ warn: pfBelowReference }" :style="{ width: pfPercent + '%' }"></i>
        <u :style="{ left: reference * 100 + '%' }"></u>
      </span>
      <span class="ref">考核 {{ reference.toFixed(2) }}</span>
    </div>

    <div class="m-foot">
      <span>地址 {{ addressText }}</span>
      <span class="sp"></span>
      <span>{{ meter.lastCollectTime || '暂无采集' }}</span>
      <span class="m-keys"><i>SET</i><i>▲</i><i>▼</i></span>
    </div>
  </button>
</template>

<script>
import {
  getMeterStatusLabel,
  getMeterStatusTone,
  formatMeterNumber,
  formatMeterAddress,
  hasMeterValue,
  toMeterNumber,
  VOLTAGE_PHASE_MIN,
  VOLTAGE_PHASE_MAX,
  POWER_FACTOR_REFERENCE
} from './meter-utils'

const PHASES = ['A', 'B', 'C']

export default {
  name: 'MeterDevice',
  props: {
    meter: { type: Object, required: true }
  },
  computed: {
    tone() {
      return getMeterStatusTone(this.meter.statusCode)
    },
    statusLabel() {
      return getMeterStatusLabel(this.meter.statusCode)
    },
    // 设备编号不再占表尾的位置（真实编号很长，会把采集时间挤到第二行），改成悬停提示
    deviceTitle() {
      return [this.meter.meterCode, this.meter.meterName].filter(Boolean).join(' · ')
    },
    // 没有采到实时功率时，屏上留占位符而不是 0 —— 0 会被读成"负载为零"。
    isOff() {
      return !hasMeterValue(this.meter.activePowerKw)
    },
    pfBelowReference() {
      const pf = toMeterNumber(this.meter.powerFactorTotal)
      return pf !== null && pf < POWER_FACTOR_REFERENCE
    },
    pfPercent() {
      const pf = toMeterNumber(this.meter.powerFactorTotal)
      return pf === null ? 0 : Math.min(100, Math.max(0, pf * 100))
    },
    reference() {
      return POWER_FACTOR_REFERENCE
    },
    addressText() {
      return formatMeterAddress(this.meter.slaveAddress)
    },
    currentCells() {
      return this.phaseCells(['currentA', 'currentB', 'currentC'])
    },
    voltageCells() {
      return this.phaseCells(['voltageA', 'voltageB', 'voltageC'], VOLTAGE_PHASE_MIN, VOLTAGE_PHASE_MAX)
    }
  },
  methods: {
    formatNumber(value) {
      return formatMeterNumber(value)
    },
    value(raw) {
      if (!hasMeterValue(raw)) return '----'
      const number = Number(raw)
      return Number.isFinite(number) ? number.toFixed(2) : '----'
    },
    phaseCells(keys, min, max) {
      const outOfRange = min !== undefined && keys.some(key => {
        const number = toMeterNumber(this.meter[key])
        return number !== null && (number < min || number > max)
      })
      return keys.map((key, index) => {
        const number = toMeterNumber(this.meter[key])
        return {
          key,
          label: PHASES[index],
          text: number === null ? '----' : number.toFixed(1),
          warn: outOfRange && number !== null
        }
      })
    },
    handleClick() {
      this.$emit('open', this.meter)
    }
  }
}
</script>

<style scoped>
.meter-device {
  position: relative;
  display: block;
  width: 100%;
  padding: 9px 11px 12px 14px;
  color: inherit;
  border: 1px solid #1c242e;
  border-radius: 7px;
  background: linear-gradient(180deg, #49535f 0%, #3b4551 6%, #333c47 100%);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, .2), inset 0 -2px 5px rgba(0, 0, 0, .42), 0 2px 5px rgba(15, 23, 42, .3);
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: transform .16s ease, box-shadow .16s ease;
}
.meter-device:hover {
  transform: translateY(-2px);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, .22), inset 0 -2px 5px rgba(0, 0, 0, .42), 0 7px 14px rgba(15, 23, 42, .26);
}
.accent { position: absolute; left: 0; top: 7px; bottom: 7px; width: 3px; border-radius: 0 2px 2px 0; background: #8b98a6; }
.is-ok .accent { background: #35c07d; }
.is-warn .accent { background: #e0a13c; }
.is-danger .accent { background: #e0524f; }

.m-head { display: flex; align-items: center; gap: 8px; margin-bottom: 9px; }
.m-led { flex: none; width: 8px; height: 8px; border-radius: 50%; background: #626e7b; box-shadow: inset 0 1px 1px rgba(0, 0, 0, .4); }
.led-ok { background: #35c07d; box-shadow: 0 0 7px 1.5px rgba(53, 192, 125, .75), inset 0 0 2px rgba(255, 255, 255, .9); }
.led-warn { background: #f0b24a; box-shadow: 0 0 7px 1.5px rgba(240, 178, 74, .75), inset 0 0 2px rgba(255, 255, 255, .9); }
.led-danger { background: #ef5350; box-shadow: 0 0 8px 2px rgba(239, 83, 80, .8), inset 0 0 2px rgba(255, 255, 255, .9); animation: meter-blink 1.6s ease-in-out infinite; }
@keyframes meter-blink { 0%, 100% { opacity: 1; } 50% { opacity: .35; } }
.m-name { flex: 1; min-width: 0; overflow: hidden; color: #e9eff6; font-size: 13.5px; font-weight: 600; letter-spacing: .2px; text-overflow: ellipsis; white-space: nowrap; }
.m-state { flex: none; font-size: 11px; font-weight: 600; letter-spacing: .3px; }
.t-ok { color: #4ecf95; } .t-warn { color: #f0b24a; } .t-danger { color: #ef6b68; } .t-idle { color: #8f9caa; }

.m-lcd {
  position: relative;
  padding: 11px 12px 10px;
  border-radius: 5px;
  overflow: hidden;
  background: linear-gradient(180deg, #06110e 0%, #091714 55%, #0c1a16 100%);
  box-shadow: inset 0 2px 6px rgba(0, 0, 0, .85), inset 0 -1px 0 rgba(255, 255, 255, .05), 0 1px 0 rgba(255, 255, 255, .1);
}
/* 玻璃反光 */
.m-lcd::after {
  content: "";
  position: absolute;
  inset: 0;
  border-radius: 5px;
  pointer-events: none;
  background: linear-gradient(118deg, rgba(255, 255, 255, .13) 0%, rgba(255, 255, 255, .035) 26%, rgba(255, 255, 255, 0) 48%);
}
.lcd-p { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; padding-bottom: 8px; border-bottom: 1px solid rgba(127, 240, 192, .15); }
.lcd-p .v { color: #7ff0c0; font-family: "Cascadia Mono", Consolas, "Courier New", monospace; font-size: 27px; font-weight: 700; line-height: 1; letter-spacing: .5px; text-shadow: 0 0 9px rgba(127, 240, 192, .5); }
.lcd-p .u { color: #5aa98a; font-size: 11.5px; letter-spacing: .3px; }
.lcd-row { display: grid; grid-template-columns: 32px repeat(3, 1fr); gap: 9px; margin-top: 9px; font-family: "Cascadia Mono", Consolas, "Courier New", monospace; font-size: 12px; }
.lcd-row .rl { color: #4a9c7d; font-size: 10px; letter-spacing: .3px; }
.lcd-row .cell { display: flex; align-items: baseline; gap: 5px; }
.lcd-row .cell i { color: #5fb894; font-style: normal; font-size: 10px; }
.lcd-row .cell b { display: inline-block; min-width: 46px; color: #7ae9bb; font-weight: 600; text-align: right; }
.lcd-row.dim { margin-top: 10px; font-size: 11.5px; }
/* dim 行的数值没有相别标签，补一个等宽内缩（标签 7px + 间距 5px），才能和上面两行的数值列对齐 */
.lcd-row.dim .cell { padding-left: 12px; }
.lcd-row .cell.warn b { color: #ffce7a; text-shadow: 0 0 8px rgba(255, 206, 122, .45); }
.lcd-row .cell.warn i { color: #c79a4a; }
/* 无读数：屏上只留占位符 */
.is-off .lcd-p .v { color: #2e5248; text-shadow: none; }
.is-off .lcd-p .u { color: #26463d; }
.is-off .lcd-row .cell b { color: #2b4d43; }
.is-off .lcd-row .cell i, .is-off .lcd-row .rl { color: #26463d; }

.m-pf { display: flex; align-items: center; gap: 10px; margin: 11px 1px 0; }
.m-pf .cap { flex: none; color: #8b98a6; font-size: 11px; letter-spacing: .2px; }
.m-pf .track { position: relative; flex: 1; height: 6px; border-radius: 3px; background: #232c36; box-shadow: inset 0 1px 2px rgba(0, 0, 0, .6); }
.m-pf .track i { position: absolute; left: 0; top: 0; bottom: 0; border-radius: 3px; background: linear-gradient(90deg, #3f8f74, #6ee0b0); }
.m-pf .track i.warn { background: linear-gradient(90deg, #a8712a, #f5c069); }
.m-pf .track u { position: absolute; top: -3px; bottom: -3px; width: 1.5px; background: #e6edf5; opacity: .8; }
.m-pf .ref { flex: none; color: #6b7784; font-size: 10.5px; }

.m-foot { display: flex; align-items: center; gap: 9px; margin-top: 10px; padding-left: 2px; color: #8b98a6; font-family: "Cascadia Mono", Consolas, "Courier New", monospace; font-size: 10.5px; letter-spacing: .2px; }
.m-foot .sp { flex: 1; }
.m-keys { display: flex; gap: 3px; }
.m-keys i { display: grid; place-items: center; min-width: 19px; height: 13px; padding: 0 3px; color: #adb9c6; border-radius: 2.5px; background: linear-gradient(180deg, #4d5763, #343d48); box-shadow: inset 0 1px 0 rgba(255, 255, 255, .22), 0 1px 1px rgba(0, 0, 0, .45); font-family: -apple-system, "Segoe UI", "Microsoft YaHei", sans-serif; font-size: 8px; font-style: normal; letter-spacing: .3px; }
</style>
