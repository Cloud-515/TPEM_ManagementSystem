const SIMULATED_METER_CODE = 'SIM-METER-001'
const SIMULATION_MODE_KEY = 'tpem-meter-simulation-mode'
const ENERGY_EPOCH = Date.UTC(2026, 0, 1)

function now() { return new Date() }
function pad(value) { return String(value).padStart(2, '0') }
function formatDate(date) { return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` }
function formatLabel(date, range) { return range === '24h' ? `${pad(date.getHours())}:00` : `${date.getMonth() + 1}/${date.getDate()}` }
function secondsAt(date) { return (date.getTime() - ENERGY_EPOCH) / 1000 }
function loadAt(date) { const seconds = secondsAt(date); return 0.82 + 0.14 * Math.sin(seconds / 75) + 0.04 * Math.sin(seconds / 17) }
function round(value, digits = 2) { const factor = Math.pow(10, digits); return Math.round(value * factor) / factor }
function currentData(date = now()) {
  const seconds = secondsAt(date)
  const load = loadAt(date)
  const activePower = 72 * load
  const powerFactor = 0.93 + 0.025 * Math.sin(seconds / 120)
  return {
    meterId: 900001,
    meterCode: SIMULATED_METER_CODE,
    meterName: '模拟观测电表',
    slaveAddress: 250,
    siteName: '模拟园区',
    boxName: '模拟数据源',
    location: '网页模拟模式',
    statusCode: 'OK',
    statusText: '正常',
    lastCollectTime: formatDate(date),
    dataCollectTime: formatDate(date),
    voltageA: round(220.4 + 2.2 * Math.sin(seconds / 53)), voltageB: round(219.8 + 2 * Math.sin(seconds / 59 + 1.6)), voltageC: round(220.7 + 1.9 * Math.sin(seconds / 47 + 3.1)),
    currentA: round(114 * load), currentB: round(111 * load), currentC: round(116 * load),
    activePowerTotal: round(activePower * 1000), activePowerKw: round(activePower), reactivePowerTotal: round(activePower * Math.tan(Math.acos(powerFactor)) * 1000), reactivePowerKvar: round(activePower * Math.tan(Math.acos(powerFactor))), apparentPowerTotal: round(activePower / powerFactor * 1000), apparentPowerKva: round(activePower / powerFactor), powerFactorTotal: round(powerFactor, 3), frequency: round(50 + 0.03 * Math.sin(seconds / 37), 3),
    forwardActiveEnergy: round(50000 + Math.max(0, (date.getTime() - ENERGY_EPOCH) / 3600000) * 59.04),
    reverseActiveEnergy: round(300 + Math.max(0, (date.getTime() - ENERGY_EPOCH) / 3600000) * 0.12),
    forwardReactiveEnergy: round(12000 + Math.max(0, (date.getTime() - ENERGY_EPOCH) / 3600000) * 21.6),
    reverseReactiveEnergy: round(900 + Math.max(0, (date.getTime() - ENERGY_EPOCH) / 3600000) * 0.06),
    voltageThdA: round(1.8 + 0.15 * Math.sin(seconds / 90)), voltageThdB: round(1.9 + 0.12 * Math.sin(seconds / 85 + 0.8)), voltageThdC: round(1.7 + 0.14 * Math.sin(seconds / 95 + 1.7)),
    currentThdA: round(2.6 + 0.22 * Math.sin(seconds / 70)), currentThdB: round(2.5 + 0.18 * Math.sin(seconds / 74 + 1.2)), currentThdC: round(2.7 + 0.2 * Math.sin(seconds / 78 + 2.1)),
    voltageUnbalance: round(0.6 + 0.08 * Math.sin(seconds / 105)), currentUnbalance: round(1.1 + 0.12 * Math.sin(seconds / 88))
  }
}

export function isMeterSimulationEnabled() { return localStorage.getItem(SIMULATION_MODE_KEY) === 'true' }
export function setMeterSimulationEnabled(enabled) { localStorage.setItem(SIMULATION_MODE_KEY, String(Boolean(enabled))) }
export function simulatedMeterResponse() { return { data: currentData() } }
export function simulatedMeterListResponse() { const meter = currentData(); return { rows: [meter], total: 1 } }
export function simulatedMeterCardsResponse() { const meter = currentData(); return { data: { dashboard: [meter], toolbar: [], generatedAt: meter.lastCollectTime } } }
export function simulatedEnergyTrend(range = '24h') {
  const points = []; const count = range === '24h' ? 24 : range === '7d' ? 7 : 30; const interval = range === '24h' ? 3600000 : 86400000; const end = now();
  for (let index = count - 1; index >= 0; index -= 1) { const point = new Date(end.getTime() - index * interval); const consumption = range === '24h' ? 54 + 7 * Math.sin(secondsAt(point) / 75) : 1296 + 84 * Math.sin(secondsAt(point) / 5400); points.push({ label: formatLabel(point, range), consumptionKwh: round(consumption), meterCount: 1 }) }
  return { data: points }
}
export function simulatedHistoryTrend(category, params = {}) {
  const end = params.endTime ? new Date(params.endTime) : now(); const begin = params.beginTime ? new Date(params.beginTime) : new Date(end.getTime() - 24 * 3600000); const total = Math.max(2, Math.min(96, Math.floor((end - begin) / 900000) + 1)); const points = []
  for (let index = 0; index < total; index += 1) { const point = new Date(begin.getTime() + index * (end - begin) / (total - 1)); const data = currentData(point); points.push({ meterId: data.meterId, meterCode: data.meterCode, meterName: data.meterName, dataCollectTime: formatDate(point), activePowerTotal: data.activePowerTotal, activePowerKw: data.activePowerKw, voltageA: data.voltageA, voltageB: data.voltageB, voltageC: data.voltageC, currentA: data.currentA, currentB: data.currentB, currentC: data.currentC, forwardActiveEnergy: data.forwardActiveEnergy, reverseActiveEnergy: data.reverseActiveEnergy, forwardReactiveEnergy: data.forwardReactiveEnergy, reverseReactiveEnergy: data.reverseReactiveEnergy, voltageThdA: data.voltageThdA, voltageThdB: data.voltageThdB, voltageThdC: data.voltageThdC, currentThdA: data.currentThdA, currentThdB: data.currentThdB, currentThdC: data.currentThdC, voltageUnbalance: data.voltageUnbalance, currentUnbalance: data.currentUnbalance }) }
  return { data: { points } }
}
export function simulatedHistoryList(category, params = {}) { const trend = simulatedHistoryTrend(category, params).data.points; return { rows: trend.reverse(), total: trend.length } }
export function simulatedDashboardResponse() {
  const meter = currentData(); return { data: { kpis: { totalMeters: 1, onlineMeters: 1, offlineMeters: 0, totalActivePower: meter.activePowerTotal, todayEnergy: 1296, warningMeters: 0 }, overview: [meter], alerts: [] } }
}
