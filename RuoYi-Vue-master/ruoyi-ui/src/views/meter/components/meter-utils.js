export const meterStatusMeta = {
  OK: { label: '正常', type: 'success' },
  PF_LOW: { label: '功率因数低', type: 'warning' },
  VOLTAGE_BAD: { label: '电压异常', type: 'warning' },
  FAULT: { label: '通信故障', type: 'danger' },
  ABNORMAL: { label: '数值异常', type: 'danger' },
  NODATA: { label: '无数据', type: 'info' },
  WAITING: { label: '等待采集', type: 'info' }
}

export function getMeterStatusLabel(code) {
  return meterStatusMeta[code] ? meterStatusMeta[code].label : '状态未知'
}

export function getMeterStatusType(code) {
  return meterStatusMeta[code] ? meterStatusMeta[code].type : 'info'
}

// 展示与判定共用的口径，与 meter_threshold 表保持一致：
// 相电压 220V±10% → 198~242V、功率因数考核值 0.85（来源 docs/仪表监测判定标准.md）。
export const VOLTAGE_PHASE_MIN = 198
export const VOLTAGE_PHASE_MAX = 242
export const POWER_FACTOR_REFERENCE = 0.85

// Element 的四种语义色 → 拓扑页电表皮肤的四档色调。
const meterToneByType = { success: 'ok', warning: 'warn', danger: 'danger', info: 'idle' }

export function getMeterStatusTone(code) {
  const type = (meterStatusMeta[code] || {}).type
  return meterToneByType[type] || 'idle'
}

/**
 * 格式化电表数值。
 * 入参为空（null / undefined / 空串）时返回占位符 —— 不能让 Number(null) === 0 把"没有数据"
 * 显示成 0.00，那会被读成"负载为零"而不是"没采到"。
 */
export function formatMeterNumber(value, digits = 2) {
  if (value === null || value === undefined || value === '') return '--'
  const number = Number(value)
  return Number.isFinite(number) ? number.toLocaleString('zh-CN', { maximumFractionDigits: digits }) : '--'
}

export function formatMeterDateTime(value) {
  return value || '--'
}

/** 从站地址按两位显示（现场习惯），没有值就留占位符。 */
export function formatMeterAddress(value) {
  return value === null || value === undefined || value === '' ? '--' : String(value).padStart(2, '0')
}

export const meterQualityRiskLabels = {
  STATUS_ABNORMAL: '设备状态异常',
  POWER_FACTOR_LOW: '功率因数偏低',
  VOLTAGE_OUT_OF_RANGE: '电压越限',
  VOLTAGE_THD_EXCEEDED: '电压 THD 超限',
  CURRENT_THD_EXCEEDED: '电流 THD 超限',
  VOLTAGE_UNBALANCE_EXCEEDED: '电压不平衡',
  CURRENT_UNBALANCE_EXCEEDED: '电流不平衡'
}

function isBelow(value, threshold) { return value !== null && value !== undefined && Number(value) < threshold }
function isAbove(value, threshold) { return value !== null && value !== undefined && Number(value) > threshold }
function isOutside(value, min, max) { return value !== null && value !== undefined && (Number(value) < min || Number(value) > max) }

export function getMeterQualityRiskCodes(meter) {
  if (Array.isArray(meter && meter.qualityRiskCodes)) return meter.qualityRiskCodes
  const risks = []
  if (!meter || meter.statusCode !== 'OK') risks.push('STATUS_ABNORMAL')
  if (isBelow(meter && meter.powerFactorTotal, POWER_FACTOR_REFERENCE)) risks.push('POWER_FACTOR_LOW')
  if (['voltageA', 'voltageB', 'voltageC'].some(key => isOutside(meter && meter[key], VOLTAGE_PHASE_MIN, VOLTAGE_PHASE_MAX))) risks.push('VOLTAGE_OUT_OF_RANGE')
  if (['voltageThdA', 'voltageThdB', 'voltageThdC'].some(key => isAbove(meter && meter[key], 5))) risks.push('VOLTAGE_THD_EXCEEDED')
  if (['currentThdA', 'currentThdB', 'currentThdC'].some(key => isAbove(meter && meter[key], 8))) risks.push('CURRENT_THD_EXCEEDED')
  if (isAbove(meter && meter.voltageUnbalance, 2)) risks.push('VOLTAGE_UNBALANCE_EXCEEDED')
  if (isAbove(meter && meter.currentUnbalance, 3)) risks.push('CURRENT_UNBALANCE_EXCEEDED')
  return risks
}

export function getMeterQualityRisks(meter) {
  return getMeterQualityRiskCodes(meter).map(code => meterQualityRiskLabels[code] || code)
}
