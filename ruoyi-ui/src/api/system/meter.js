import request from '@/utils/request'
import {
  isMeterSimulationEnabled,
  simulatedDashboardResponse,
  simulatedEnergyTrend,
  simulatedHistoryList,
  simulatedHistoryTrend,
  simulatedMeterCardsResponse,
  simulatedMeterListResponse,
  simulatedMeterResponse
} from '@/utils/meter-simulation'

function simulated(response) {
  return isMeterSimulationEnabled() ? Promise.resolve(response()) : null
}

function requestPage(url, query, response) {
  const mock = simulated(response)
  return mock || request({ url, method: 'get', params: query })
}

export function listMeterCards() {
  const mock = simulated(simulatedMeterCardsResponse)
  return mock || request({ url: '/system/meter/cards', method: 'get' })
}

export function getDashboardEnergyTrend(range) {
  const mock = simulated(() => simulatedEnergyTrend(range))
  return mock || request({ url: '/system/meter/dashboard/energy-trend', method: 'get', params: { range } })
}

export function getEnergyTrend(range) {
  const mock = simulated(() => simulatedEnergyTrend(range))
  return mock || request({ url: '/system/meter/dashboard/energy-trend', method: 'get', params: { range } })
}

export function listRealtimeMeters(query) {
  return requestPage('/system/meter/realtime/page', query, simulatedMeterListResponse)
}

export function listEnergyMeters(query) {
  return requestPage('/system/meter/energy/page', query, simulatedMeterListResponse)
}

export function listQualityMeters(query) {
  return requestPage('/system/meter/quality/page', query, simulatedMeterListResponse)
}

export function getHistoryTrend(category, query) {
  const mock = simulated(() => simulatedHistoryTrend(category, query))
  return mock || request({ url: `/system/meter/history/${category}/trend`, method: 'get', params: query })
}

export function listRealtimeHistory(query) {
  return requestPage('/system/meter/history/realtime/page', query, () => simulatedHistoryList('realtime', query))
}

export function listEnergyHistory(query) {
  return requestPage('/system/meter/history/energy/page', query, () => simulatedHistoryList('energy', query))
}

export function listQualityHistory(query) {
  return requestPage('/system/meter/history/quality/page', query, () => simulatedHistoryList('quality', query))
}

export function getMeterRealtimeHistory(meterId, query) {
  return getHistoryTrend('realtime', { ...query, meterId })
}

export function getMeterEnergyHistory(meterId, query) {
  return getHistoryTrend('energy', { ...query, meterId })
}

export function getMeterQualityHistory(meterId, query) {
  return getHistoryTrend('quality', { ...query, meterId })
}

export function getRealtimeDetail(meterId) {
  const mock = simulated(simulatedMeterResponse)
  return mock || request({ url: `/system/meter/realtime/detail/${meterId}`, method: 'get' })
}
