-- 设备拓扑图配置表（主库）
DROP TABLE IF EXISTS meter_topology_device;
DROP TABLE IF EXISTS meter_topology_region;

CREATE TABLE meter_topology_region (
  region_id bigint NOT NULL,
  region_name varchar(64) NOT NULL,
  sort_order int NOT NULL DEFAULT 0,
  PRIMARY KEY (region_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='电表拓扑区域';

CREATE TABLE meter_topology_device (
  region_id bigint NOT NULL,
  meter_id bigint NOT NULL,
  sort_order int NOT NULL DEFAULT 0,
  PRIMARY KEY (meter_id),
  KEY idx_topology_region (region_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='电表拓扑区域设备归属';

INSERT INTO sys_menu (menu_name, parent_id, order_num, path, component, query, is_frame, is_cache, menu_type, visible, status, perms, icon, create_by, create_time, remark)
VALUES ('设备拓扑图', 2000, 5, 'topology', 'meter/topology/index', '', 1, 0, 'C', '0', '0', 'meter:topology:list', 'guide', 'admin', NOW(), '电表监控设备拓扑图');

SET @topology_menu_id = LAST_INSERT_ID();
INSERT INTO sys_menu (menu_name, parent_id, order_num, path, component, query, is_frame, is_cache, menu_type, visible, status, perms, icon, create_by, create_time, remark)
VALUES ('保存拓扑', @topology_menu_id, 1, '', '', '', 1, 0, 'F', '0', '0', 'meter:topology:edit', '#', 'admin', NOW(), '保存设备拓扑布局权限');
