package com.ruoyi.web.controller.heatpump;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import com.ruoyi.common.core.domain.AjaxResult;

@RestController
@RequestMapping("/heatpump/monitor")
public class HeatPumpMonitorController
{
    @Autowired
    @Qualifier("heatPumpJdbcTemplate")
    private NamedParameterJdbcTemplate heatPumpJdbcTemplate;

    @PreAuthorize("@ss.hasPermi('heatpump:monitor:list')")
    @GetMapping("/overview")
    public AjaxResult overview()
    {
        String sql = "SELECT COUNT(*) AS total_count, "
                + "COUNT(*) FILTER (WHERE s.is_online) AS online_count, "
                + "COUNT(*) FILTER (WHERE NOT s.is_online) AS offline_count, "
                + "COUNT(*) FILTER (WHERE a.device_id IS NOT NULL) AS alarm_count "
                + "FROM heat_pump_device_state s "
                + "LEFT JOIN (SELECT DISTINCT device_id, module_index FROM heat_pump_alarm_event WHERE recovered_at IS NULL) a "
                + "ON a.device_id = s.device_id AND a.module_index = s.module_index";
        return AjaxResult.success(heatPumpJdbcTemplate.queryForMap(sql, new HashMap<String, Object>()));
    }

    @PreAuthorize("@ss.hasPermi('heatpump:monitor:list')")
    @GetMapping("/modules")
    public AjaxResult modules(@RequestParam(required = false) String siteCode,
            @RequestParam(required = false) Integer online,
            @RequestParam(required = false) Integer alarm)
    {
        StringBuilder sql = new StringBuilder();
        sql.append("SELECT d.id AS device_id, d.site_code, d.controller_address, d.controller_name, ");
        sql.append("s.module_index, s.module_name, s.last_collect_time, s.is_online, s.state_code, s.run_mode, ");
        sql.append("s.target_temperature, s.water_in_temperature, s.water_out_temperature, s.ambient_temperature, s.fault_code, ");
        sql.append("COALESCE(a.alarm_count, 0) AS alarm_count ");
        sql.append("FROM heat_pump_device_state s JOIN heat_pump_device d ON d.id = s.device_id ");
        sql.append("LEFT JOIN (SELECT device_id, module_index, COUNT(*) AS alarm_count FROM heat_pump_alarm_event WHERE recovered_at IS NULL GROUP BY device_id, module_index) a ");
        sql.append("ON a.device_id = s.device_id AND a.module_index = s.module_index WHERE 1 = 1 ");

        Map<String, Object> params = new HashMap<>();
        if (siteCode != null && !siteCode.trim().isEmpty())
        {
            sql.append("AND d.site_code = :siteCode ");
            params.put("siteCode", siteCode.trim());
        }
        if (online != null)
        {
            sql.append("AND s.is_online = :online ");
            params.put("online", online == 1);
        }
        if (alarm != null && alarm == 1)
        {
            sql.append("AND COALESCE(a.alarm_count, 0) > 0 ");
        }
        sql.append("ORDER BY d.site_code, d.controller_address, s.module_index");
        return AjaxResult.success(heatPumpJdbcTemplate.queryForList(sql.toString(), params));
    }

    @PreAuthorize("@ss.hasPermi('heatpump:monitor:list')")
    @GetMapping("/history")
    public AjaxResult history(@RequestParam Long deviceId, @RequestParam Integer moduleIndex,
            @RequestParam(required = false, defaultValue = "24") Integer hours)
    {
        int safeHours = Math.max(1, Math.min(hours, 168));
        String sql = "SELECT collected_at, target_temperature, water_in_temperature, water_out_temperature, ambient_temperature, fault_code "
                + "FROM heat_pump_telemetry_history WHERE device_id = :deviceId AND module_index = :moduleIndex "
                + "AND collected_at >= CURRENT_TIMESTAMP - (:hours * INTERVAL '1 hour') ORDER BY collected_at";
        Map<String, Object> params = new HashMap<>();
        params.put("deviceId", deviceId);
        params.put("moduleIndex", moduleIndex);
        params.put("hours", safeHours);
        return AjaxResult.success(heatPumpJdbcTemplate.queryForList(sql, params));
    }

    @PreAuthorize("@ss.hasPermi('heatpump:monitor:list')")
    @GetMapping("/alarms")
    public AjaxResult alarms()
    {
        String sql = "SELECT a.id, d.site_code, d.controller_address, d.controller_name, a.module_index, a.alarm_type, a.alarm_code, a.opened_at, a.last_seen_at, a.message "
                + "FROM heat_pump_alarm_event a JOIN heat_pump_device d ON d.id = a.device_id "
                + "WHERE a.recovered_at IS NULL ORDER BY a.last_seen_at DESC";
        List<Map<String, Object>> result = heatPumpJdbcTemplate.queryForList(sql, new HashMap<String, Object>());
        return AjaxResult.success(result);
    }
}
