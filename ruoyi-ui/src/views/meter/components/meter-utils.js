export const meterStatusMeta = {
  OK: { label: '正常', type: 'success' },
  PF_LOW: { label: '功率因数低', type: 'warning' },
  VOLTAGE_BAD: { label: '电压异常', type: 'warning' },
  FAULT: { label: '通信故障', type: 'danger' },
  NODATA: { label: '无数据', type: 'info' },
  WAITING: { label: '等待采集', type: 'info' }
}

export function getMeterStatusLabel(code) {
  return meterStatusMeta[code] ? meterStatusMeta[code].label : '状态未知'
}

export function getMeterStatusType(code) {
  return meterStatusMeta[code] ? meterStatusMeta[code].type : 'info'
}

export function formatMeterNumber(value, digits = 2) {
  const number = Number(value)
  return Number.isFinite(number) ? number.toLocaleString('zh-CN', { maximumFractionDigits: digits }) : '--'
}

export function formatMeterDateTime(value) {
  return value || '--'
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
  if (isBelow(meter && meter.powerFactorTotal, 0.85)) risks.push('POWER_FACTOR_LOW')
  if (['voltageA', 'voltageB', 'voltageC'].some(key => isOutside(meter && meter[key], 198, 242))) risks.push('VOLTAGE_OUT_OF_RANGE')
  if (['voltageThdA', 'voltageThdB', 'voltageThdC'].some(key => isAbove(meter && meter[key], 5))) risks.push('VOLTAGE_THD_EXCEEDED')
  if (['currentThdA', 'currentThdB', 'currentThdC'].some(key => isAbove(meter && meter[key], 8))) risks.push('CURRENT_THD_EXCEEDED')
  if (isAbove(meter && meter.voltageUnbalance, 2)) risks.push('VOLTAGE_UNBALANCE_EXCEEDED')
  if (isAbove(meter && meter.currentUnbalance, 3)) risks.push('CURRENT_UNBALANCE_EXCEEDED')
  return risks
}

export function getMeterQualityRisks(meter) {
  return getMeterQualityRiskCodes(meter).map(code => meterQualityRiskLabels[code] || code)
}
