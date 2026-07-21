import request from '@/utils/request'

export function getHeatPumpOverview() {
  return request({
    url: '/heatpump/monitor/overview',
    method: 'get'
  })
}

export function listHeatPumpModules(query) {
  return request({
    url: '/heatpump/monitor/modules',
    method: 'get',
    params: query
  })
}

export function getHeatPumpHistory(query) {
  return request({
    url: '/heatpump/monitor/history',
    method: 'get',
    params: query
  })
}

export function listHeatPumpAlarms() {
  return request({
    url: '/heatpump/monitor/alarms',
    method: 'get'
  })
}
