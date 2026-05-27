import request from '@/utils/request'

export function listMeterCards() {
  return request({
    url: '/system/meter/cards',
    method: 'get'
  })
}

export function listRealtimeMeters(query) {
  return request({
    url: '/system/meter/realtime/page',
    method: 'get',
    params: query
  })
}

export function listEnergyMeters(query) {
  return request({
    url: '/system/meter/energy/page',
    method: 'get',
    params: query
  })
}

export function listQualityMeters(query) {
  return request({
    url: '/system/meter/quality/page',
    method: 'get',
    params: query
  })
}

export function listRealtimeHistory(query) {
  return request({
    url: '/system/meter/history/realtime/page',
    method: 'get',
    params: query
  })
}

export function listEnergyHistory(query) {
  return request({
    url: '/system/meter/history/energy/page',
    method: 'get',
    params: query
  })
}

export function listQualityHistory(query) {
  return request({
    url: '/system/meter/history/quality/page',
    method: 'get',
    params: query
  })
}

export function getRealtimeDetail(meterId) {
  return request({
    url: '/system/meter/realtime/detail/' + meterId,
    method: 'get'
  })
}
