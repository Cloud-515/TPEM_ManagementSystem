const SIMULATION_MODE_KEY = 'tpem-meter-simulation-mode'
const TOPOLOGY_STORAGE_KEY = 'tpem-meter-topology-layout'
const SNAPSHOT_INTERVAL = 30000

const definitions = [
  { meterId: 900001, meterCode: 'SIM-METER-001', meterName: '园区总进线电表', siteName: '模拟园区', boxName: '总配电室', slaveAddress: 250, factor: 1, scenario: 'OK' },
  { meterId: 900002, meterCode: 'SIM-METER-002', meterName: '办公区用电表', siteName: '模拟园区', boxName: '办公区配电箱', slaveAddress: 251, factor: 0.38, scenario: 'OK' },
  { meterId: 900003, meterCode: 'SIM-METER-003', meterName: '生产车间电表', siteName: '模拟园区', boxName: '车间配电箱', slaveAddress: 252, factor: 0.76, scenario: 'PF_LOW' },
  { meterId: 900004, meterCode: 'SIM-METER-004', meterName: '空调机房电表', siteName: '模拟园区', boxName: '机房配电箱', slaveAddress: 253, factor: 0.54, scenario: 'VOLTAGE_BAD' },
  { meterId: 900005, meterCode: 'SIM-METER-005', meterName: '仓储区用电表', siteName: '模拟园区', boxName: '仓储配电箱', slaveAddress: 254, factor: 0.29, scenario: 'OK' },
  { meterId: 900006, meterCode: 'SIM-METER-006', meterName: '消防系统电表', siteName: '模拟园区', boxName: '消防配电箱', slaveAddress: 255, factor: 0.17, scenario: 'NODATA' }
]
let cachedSnapshot = null
let cachedBucket = null

function pad(value) { return String(value).padStart(2, '0') }
function round(value, digits) { const factor = Math.pow(10, digits === undefined ? 2 : digits); return Math.round(value * factor) / factor }
function formatDate(date) { return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` }
function snapshotDate() { return new Date(Math.floor(Date.now() / SNAPSHOT_INTERVAL) * SNAPSHOT_INTERVAL) }
function statusText(code) { return ({ OK: '正常', PF_LOW: '功率因数偏低', VOLTAGE_BAD: '电压异常', FAULT: '通信故障', NODATA: '等待数据', WAITING: '等待数据' })[code] || '状态未知' }

function buildMeter(definition, date) {
  const seconds = date.getTime() / 1000
  const load = 0.78 + 0.12 * Math.sin(seconds / 75 + definition.meterId) + 0.04 * Math.sin(seconds / 17)
  const activePowerKw = round(72 * definition.factor * load)
  let voltageA = round(220.4 + 1.8 * Math.sin(seconds / 53 + definition.meterId))
  let voltageB = round(219.8 + 1.6 * Math.sin(seconds / 59 + definition.meterId))
  let voltageC = round(220.7 + 1.7 * Math.sin(seconds / 47 + definition.meterId))
  let powerFactorTotal = round(0.95 + 0.015 * Math.sin(seconds / 120 + definition.meterId), 3)
  let statusCode = definition.scenario
  let lastCollectTime = formatDate(date)
  let isOnline = 1
  if (statusCode === 'PF_LOW') powerFactorTotal = 0.42
  if (statusCode === 'VOLTAGE_BAD') { voltageA = 247.6; voltageB = 246.8; voltageC = 248.1 }
  if (statusCode === 'NODATA') { lastCollectTime = formatDate(new Date(date.getTime() - 2 * 3600000)); isOnline = 0 }
  const apparentPowerKva = powerFactorTotal > 0 ? round(activePowerKw / powerFactorTotal) : null
  const reactivePowerKvar = powerFactorTotal > 0 ? round(activePowerKw * Math.tan(Math.acos(powerFactorTotal))) : null
  return {
    meterId: definition.meterId, meterCode: definition.meterCode, meterName: definition.meterName, slaveAddress: definition.slaveAddress, siteName: definition.siteName, boxName: definition.boxName, location: `${definition.siteName}/${definition.boxName}`,
    statusCode, statusText: statusText(statusCode), isOnline, dataQuality: statusCode === 'OK' ? 'GOOD' : statusCode === 'NODATA' ? 'STALE' : 'WARNING', lastCollectTime, dataCollectTime: lastCollectTime, publishTime: lastCollectTime,
    voltageA, voltageB, voltageC, currentA: round(114 * definition.factor * load), currentB: round(111 * definition.factor * load), currentC: round(116 * definition.factor * load),
    activePowerKw, activePowerTotal: round(activePowerKw * 1000), reactivePowerKvar, reactivePowerTotal: round(reactivePowerKvar * 1000), apparentPowerKva, apparentPowerTotal: round(apparentPowerKva * 1000), powerFactorTotal, frequency: round(50 + 0.03 * Math.sin(seconds / 37), 3),
    forwardActiveEnergy: round(50000 * definition.factor + seconds / 3600 * 72 * definition.factor * 0.78), reverseActiveEnergy: round(300 * definition.factor + seconds / 3600 * 0.12), forwardReactiveEnergy: round(12000 * definition.factor + seconds / 3600 * Math.max(0, reactivePowerKvar || 0)), reverseReactiveEnergy: round(900 * definition.factor + seconds / 3600 * 0.06),
    voltageThdA: round(1.8 + 0.15 * Math.sin(seconds / 90)), voltageThdB: round(1.9 + 0.12 * Math.sin(seconds / 85)), voltageThdC: round(1.7 + 0.14 * Math.sin(seconds / 95)), currentThdA: round(2.6 + 0.22 * Math.sin(seconds / 70)), currentThdB: round(2.5 + 0.18 * Math.sin(seconds / 74)), currentThdC: round(2.7 + 0.2 * Math.sin(seconds / 78)), voltageUnbalance: round(0.6 + 0.08 * Math.sin(seconds / 105)), currentUnbalance: round(1.1 + 0.12 * Math.sin(seconds / 88))
  }
}

function meters() {
  const date = snapshotDate(); const bucket = date.getTime()
  if (cachedBucket !== bucket) { cachedBucket = bucket; cachedSnapshot = definitions.map(item => buildMeter(item, date)) }
  return cachedSnapshot
}
function copy(value) { return JSON.parse(JSON.stringify(value)) }
function page(items, query) { const q = query || {}; const keyword = q.meterName || q.keyword; let result = items.slice(); if (keyword) result = result.filter(item => item.meterName.indexOf(keyword) >= 0 || item.meterCode.indexOf(keyword) >= 0); if (q.siteName) result = result.filter(item => item.siteName.indexOf(q.siteName) >= 0); if (q.boxName) result = result.filter(item => item.boxName.indexOf(q.boxName) >= 0); if (q.statusCode) result = result.filter(item => item.statusCode === q.statusCode); const total = result.length; const size = Number(q.pageSize || 10); const start = (Number(q.pageNum || 1) - 1) * size; return { rows: copy(result.slice(start, start + size)), total } }

function qualityRiskCodes(meter) { const risks = []; const below = (value, threshold) => value !== null && value !== undefined && Number(value) < threshold; const above = (value, threshold) => value !== null && value !== undefined && Number(value) > threshold; const outside = (value, min, max) => value !== null && value !== undefined && (Number(value) < min || Number(value) > max); if (meter.statusCode !== 'OK') risks.push('STATUS_ABNORMAL'); if (below(meter.powerFactorTotal, 0.85)) risks.push('POWER_FACTOR_LOW'); if ([meter.voltageA, meter.voltageB, meter.voltageC].some(value => outside(value, 198, 242))) risks.push('VOLTAGE_OUT_OF_RANGE'); if ([meter.voltageThdA, meter.voltageThdB, meter.voltageThdC].some(value => above(value, 5))) risks.push('VOLTAGE_THD_EXCEEDED'); if ([meter.currentThdA, meter.currentThdB, meter.currentThdC].some(value => above(value, 8))) risks.push('CURRENT_THD_EXCEEDED'); if (above(meter.voltageUnbalance, 2)) risks.push('VOLTAGE_UNBALANCE_EXCEEDED'); if (above(meter.currentUnbalance, 3)) risks.push('CURRENT_UNBALANCE_EXCEEDED'); return risks }

function qualityMeters(query) { const q = query || {}; const filtered = page(meters(), { ...q, pageNum: 1, pageSize: Number.MAX_SAFE_INTEGER }).rows.map(item => ({ ...item, qualityRiskCodes: qualityRiskCodes(item) })); return q.riskOnly === true || q.riskOnly === 'true' ? filtered.filter(item => item.qualityRiskCodes.length) : filtered }
function topologyIds() { return { regions: [{ regionId: -1, regionName: '配电中心', sortOrder: 1, meterIds: [900001, 900002] }, { regionId: -2, regionName: '生产区域', sortOrder: 2, meterIds: [900003, 900004] }], unassignedMeterIds: [900005, 900006] } }
function savedTopology() { try { return JSON.parse(localStorage.getItem(TOPOLOGY_STORAGE_KEY)) || topologyIds() } catch (e) { return topologyIds() } }

export function isMeterSimulationEnabled() { return localStorage.getItem(SIMULATION_MODE_KEY) === 'true' }
export function setMeterSimulationEnabled(enabled) { localStorage.setItem(SIMULATION_MODE_KEY, String(Boolean(enabled))) }
export function simulatedMeterResponse(meterId) { const meter = meters().find(item => String(item.meterId) === String(meterId)); return { data: meter ? copy(meter) : null } }
export function simulatedMeterListResponse(query) { return page(meters(), query) }
export function simulatedQualityMeterListResponse(query) { return page(qualityMeters(query), query) }
export function simulatedQualityMeterStats(query) { const list = qualityMeters({ ...(query || {}), riskOnly: false }); const stats = { totalCount: list.length, riskCount: 0, statusAbnormalCount: 0, powerFactorLowCount: 0, thdExceededCount: 0, unbalanceExceededCount: 0 }; list.forEach(item => { const risks = item.qualityRiskCodes; if (risks.length) stats.riskCount += 1; if (risks.includes('STATUS_ABNORMAL')) stats.statusAbnormalCount += 1; if (risks.includes('POWER_FACTOR_LOW')) stats.powerFactorLowCount += 1; if (risks.includes('VOLTAGE_THD_EXCEEDED') || risks.includes('CURRENT_THD_EXCEEDED')) stats.thdExceededCount += 1; if (risks.includes('VOLTAGE_UNBALANCE_EXCEEDED') || risks.includes('CURRENT_UNBALANCE_EXCEEDED')) stats.unbalanceExceededCount += 1 }); return { data: stats } }
export function simulatedMeterCardsResponse() { const snapshot = meters(); return { data: { dashboard: copy(snapshot), toolbar: [], generatedAt: snapshot[0].lastCollectTime } } }
export function simulatedMeterTopologyResponse() { const snapshot = meters(); const lookup = new Map(snapshot.map(item => [item.meterId, item])); const layout = savedTopology(); const assigned = new Set(); const regions = layout.regions.map(region => { const meterIds = region.meterIds || []; meterIds.forEach(id => assigned.add(id)); return { regionId: region.regionId, regionName: region.regionName, sortOrder: region.sortOrder, meters: meterIds.map(id => lookup.get(id)).filter(Boolean).map(copy) } }); const unassignedMeterIds = layout.unassignedMeterIds || snapshot.filter(item => !assigned.has(item.meterId)).map(item => item.meterId); return { data: { regions, unassignedMeters: unassignedMeterIds.map(id => lookup.get(id)).filter(Boolean).map(copy) } } }
export function saveSimulatedMeterTopology(layout) { const regions = (layout.regions || []).map((region, index) => ({ regionId: region.regionId || -(Date.now() + index), regionName: region.regionName, sortOrder: index + 1, meterIds: (region.meters || []).map(meter => meter.meterId) })); const assigned = new Set(regions.reduce((all, region) => all.concat(region.meterIds), [])); const unassignedMeterIds = meters().filter(item => !assigned.has(item.meterId)).map(item => item.meterId); localStorage.setItem(TOPOLOGY_STORAGE_KEY, JSON.stringify({ regions, unassignedMeterIds })); return { code: 200, msg: '操作成功' } }
export function simulatedEnergyTrend(range) { const snapshot = meters(); const count = range === '24h' ? 24 : range === '7d' ? 7 : 30; return { data: Array.from({ length: count }, (_, index) => ({ label: range === '24h' ? `${pad(index)}:00` : `第 ${index + 1} 天`, consumptionKwh: round(snapshot.reduce((sum, item) => sum + item.activePowerKw, 0) * (range === '24h' ? 1 : 24) * (0.86 + index / 500)), meterCount: snapshot.length })) } }
export function simulatedHistoryTrend(category, query) { const q = query || {}; const meter = meters().find(item => String(item.meterId) === String(q.meterId)) || meters()[0]; const parsedEnd = q.endTime ? new Date(q.endTime.replace(/-/g, '/')) : new Date(); const parsedStart = q.beginTime ? new Date(q.beginTime.replace(/-/g, '/')) : new Date(parsedEnd.getTime() - 24 * 3600000); const end = Number.isNaN(parsedEnd.getTime()) ? new Date() : parsedEnd; const start = Number.isNaN(parsedStart.getTime()) || parsedStart >= end ? new Date(end.getTime() - 24 * 3600000) : parsedStart; const interval = Math.max(60000, Math.floor((end.getTime() - start.getTime()) / 23)); const points = Array.from({ length: 24 }, (_, index) => { const at = new Date(start.getTime() + interval * index); const factor = 0.86 + index / 500; return Object.assign({}, buildMeter(definitions.find(item => item.meterId === meter.meterId), at), { dataCollectTime: formatDate(at), lastCollectTime: formatDate(at), activePowerKw: round(meter.activePowerKw * factor), activePowerTotal: round(meter.activePowerTotal * factor) }) }); return { data: { points } } }
export function simulatedEnergyAnalysis(query) { const energyPoints = simulatedHistoryTrend('energy', query).data.points; const powerPoints = simulatedHistoryTrend('realtime', query).data.points; const validPoints = energyPoints.filter(item => item.forwardActiveEnergy !== null && item.forwardActiveEnergy !== undefined); const start = validPoints[0] && Number(validPoints[0].forwardActiveEnergy); const end = validPoints[validPoints.length - 1] && Number(validPoints[validPoints.length - 1].forwardActiveEnergy); const summary = { startForwardActiveEnergy: Number.isFinite(start) ? start : null, endForwardActiveEnergy: Number.isFinite(end) ? end : null, intervalEnergy: null, validReadingCount: validPoints.length, valid: false, reason: '' }; if (!validPoints.length) summary.reason = 'NO_ENERGY_READINGS'; else if (validPoints.length < 2) summary.reason = 'INSUFFICIENT_ENERGY_READINGS'; else if (end < start) summary.reason = 'COUNTER_REGRESSION'; else { summary.valid = true; summary.intervalEnergy = round(end - start) } return { data: { summary, energyPoints, powerPoints } } }

export function simulatedHistoryList(category, query) { const points = simulatedHistoryTrend(category, query).data.points.slice().reverse(); return page(points, query) }
export function simulatedDashboardResponse() { const snapshot = meters(); return { data: { kpis: { totalMeters: snapshot.length, onlineMeters: snapshot.filter(item => item.isOnline).length, offlineMeters: snapshot.filter(item => !item.isOnline).length, totalActivePower: snapshot.reduce((sum, item) => sum + item.activePowerTotal, 0), todayEnergy: 1296, warningMeters: snapshot.filter(item => item.statusCode !== 'OK').length }, overview: copy(snapshot), alerts: [] } } }
