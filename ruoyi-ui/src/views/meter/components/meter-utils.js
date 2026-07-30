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
