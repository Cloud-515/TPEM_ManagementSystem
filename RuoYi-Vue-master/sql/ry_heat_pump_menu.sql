-- Execute in the RuoYi MySQL database after deploying the heat-pump monitoring module.
-- The script is rerunnable and does not depend on a hard-coded menu_id.

SET NAMES utf8 COLLATE utf8_general_ci;

SET @parent_id := (SELECT menu_id FROM sys_menu WHERE parent_id = 0 AND menu_name = '热泵群控' LIMIT 1);

INSERT INTO sys_menu
(menu_name, parent_id, order_num, path, component, query, is_frame, is_cache, menu_type, visible, status, perms, icon, create_by, create_time, update_by, update_time, remark)
SELECT '热泵群控', 0, 8, 'heatpump', NULL, '', 1, 0, 'M', '0', '0', '', 'monitor', 'admin', NOW(), '', NULL, '热泵群控监控'
WHERE @parent_id IS NULL;

SET @parent_id := (SELECT menu_id FROM sys_menu WHERE parent_id = 0 AND menu_name = '热泵群控' LIMIT 1);

INSERT INTO sys_menu
(menu_name, parent_id, order_num, path, component, query, is_frame, is_cache, menu_type, visible, status, perms, icon, create_by, create_time, update_by, update_time, remark)
SELECT '实时监控', @parent_id, 1, 'monitor', 'heatpump/monitor/index', '', 1, 0, 'C', '0', '0', 'heatpump:monitor:list', 'monitor', 'admin', NOW(), '', NULL, '热泵实时状态与告警'
WHERE NOT EXISTS (
    SELECT 1 FROM sys_menu WHERE parent_id = @parent_id AND perms = 'heatpump:monitor:list'
);
