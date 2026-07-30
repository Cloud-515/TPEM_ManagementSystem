-- Local MySQL demo seed for database `ma`.
-- Safe to re-run: DEMO-* business keys and meter/time keys make inserts idempotent.

SET NAMES utf8mb4;
SET @expected_database = 'ma';
SET @actual_database = DATABASE();
SELECT IF(@actual_database = @expected_database, 1, (SELECT 1 / 0)) AS database_guard;

START TRANSACTION;

INSERT INTO site (site_code, site_name, address, remark, created_at, updated_at)
VALUES
  ('DEMO-S01', 'DEMO 东区办公园区', '演示地址：东区 1 号', '本地演示数据', NOW(), NOW()),
  ('DEMO-S02', 'DEMO 西区生产园区', '演示地址：西区 2 号', '本地演示数据', NOW(), NOW()),
  ('DEMO-S03', 'DEMO 南区物流园区', '演示地址：南区 3 号', '本地演示数据', NOW(), NOW()),
  ('DEMO-S04', 'DEMO 北区研发园区', '演示地址：北区 4 号', '本地演示数据', NOW(), NOW())
ON DUPLICATE KEY UPDATE site_name = VALUES(site_name), address = VALUES(address), remark = VALUES(remark), updated_at = NOW();

INSERT INTO distribution_box (site_id, box_code, box_name, location, remark, created_at, updated_at)
SELECT s.id, b.box_code, b.box_name, b.location, '本地演示数据', NOW(), NOW()
FROM site s
JOIN (
  SELECT 'DEMO-S01' AS site_code, 'DEMO-B01' AS box_code, 'DEMO 东区一号配电箱' AS box_name, '东区办公楼一层' AS location UNION ALL
  SELECT 'DEMO-S01', 'DEMO-B02', 'DEMO 东区二号配电箱', '东区办公楼二层' UNION ALL
  SELECT 'DEMO-S02', 'DEMO-B03', 'DEMO 西区一号配电箱', '西区生产车间 A' UNION ALL
  SELECT 'DEMO-S02', 'DEMO-B04', 'DEMO 西区二号配电箱', '西区生产车间 B' UNION ALL
  SELECT 'DEMO-S03', 'DEMO-B05', 'DEMO 南区一号配电箱', '南区仓储中心' UNION ALL
  SELECT 'DEMO-S03', 'DEMO-B06', 'DEMO 南区二号配电箱', '南区装卸区' UNION ALL
  SELECT 'DEMO-S04', 'DEMO-B07', 'DEMO 北区一号配电箱', '北区研发楼 A' UNION ALL
  SELECT 'DEMO-S04', 'DEMO-B08', 'DEMO 北区二号配电箱', '北区研发楼 B'
) b ON b.site_code = s.site_code
ON DUPLICATE KEY UPDATE box_name = VALUES(box_name), location = VALUES(location), remark = VALUES(remark), updated_at = NOW();

INSERT INTO meter (site_id, box_id, meter_code, meter_name, slave_address, serial_number, device_type, protocol_version, program_version, location, is_enabled, is_toolbar, mqtt_topic, last_seen_time, created_at, updated_at)
SELECT s.id, b.id,
       CONCAT('DEMO-M', LPAD(n.n, 3, '0')),
       CONCAT('DEMO 智能电表 ', LPAD(n.n, 2, '0')),
       MOD(n.n - 1, 5) + 1,
       CONCAT('DEMO-SN-', LPAD(n.n, 5, '0')),
       'AMC96L-E4', '1.0', '1.0',
       CONCAT(b.location, ' - 回路 ', MOD(n.n - 1, 5) + 1),
       1, IF(MOD(n.n, 3) = 0, 1, 0),
       CONCAT('demo/meter/', LPAD(n.n, 3, '0')), NOW(), NOW(), NOW()
FROM (
  SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL
  SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL
  SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15 UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL
  SELECT 19 UNION ALL SELECT 20 UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL
  SELECT 25 UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30 UNION ALL
  SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35 UNION ALL SELECT 36
) n
JOIN distribution_box b ON b.box_code = CONCAT('DEMO-B', LPAD(FLOOR((n.n - 1) / 5) + 1, 2, '0'))
JOIN site s ON s.id = b.site_id
ON DUPLICATE KEY UPDATE meter_name = VALUES(meter_name), serial_number = VALUES(serial_number), location = VALUES(location), is_enabled = 1, is_toolbar = VALUES(is_toolbar), mqtt_topic = VALUES(mqtt_topic), last_seen_time = NOW(), updated_at = NOW();

INSERT INTO meter_status (meter_id, is_online, last_collect_time, last_publish_time, status_code, updated_at)
SELECT id, 1, NOW(), NOW(), 'ONLINE', NOW()
FROM meter WHERE meter_code LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE is_online = 1, last_collect_time = NOW(), last_publish_time = NOW(), status_code = 'ONLINE', updated_at = NOW();

INSERT INTO meter_realtime_latest (meter_id, collect_time, voltage_a, voltage_b, voltage_c, voltage_ab, voltage_bc, voltage_ca, current_a, current_b, current_c, active_power_total, reactive_power_total, apparent_power_total, power_factor_total, frequency, data_quality, created_at, updated_at)
SELECT id, NOW(), 220.5 + MOD(id, 7) / 10, 221.0 + MOD(id, 5) / 10, 219.8 + MOD(id, 9) / 10,
       381.7, 382.1, 380.9, 18.2 + MOD(id, 8), 17.6 + MOD(id, 7), 18.0 + MOD(id, 6),
       11800 + MOD(id, 12) * 630, 2400 + MOD(id, 6) * 110, 12200 + MOD(id, 12) * 650, 0.96, 50.00, 'GOOD', NOW(), NOW()
FROM meter WHERE meter_code LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE collect_time = VALUES(collect_time), voltage_a = VALUES(voltage_a), voltage_b = VALUES(voltage_b), voltage_c = VALUES(voltage_c), voltage_ab = VALUES(voltage_ab), voltage_bc = VALUES(voltage_bc), voltage_ca = VALUES(voltage_ca), current_a = VALUES(current_a), current_b = VALUES(current_b), current_c = VALUES(current_c), active_power_total = VALUES(active_power_total), reactive_power_total = VALUES(reactive_power_total), apparent_power_total = VALUES(apparent_power_total), power_factor_total = VALUES(power_factor_total), frequency = VALUES(frequency), data_quality = 'GOOD', updated_at = NOW();

INSERT INTO meter_energy_latest (meter_id, collect_time, forward_active_energy, reverse_active_energy, forward_reactive_energy, reverse_reactive_energy, created_at, updated_at)
SELECT id, NOW(), 12000 + id * 87, 4.2 + MOD(id, 10) / 10, 2580 + id * 21, 1.6 + MOD(id, 5) / 10, NOW(), NOW()
FROM meter WHERE meter_code LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE collect_time = VALUES(collect_time), forward_active_energy = VALUES(forward_active_energy), reverse_active_energy = VALUES(reverse_active_energy), forward_reactive_energy = VALUES(forward_reactive_energy), reverse_reactive_energy = VALUES(reverse_reactive_energy), updated_at = NOW();

INSERT INTO meter_power_quality_latest (meter_id, collect_time, current_thd_a, current_thd_b, current_thd_c, voltage_thd_a, voltage_thd_b, voltage_thd_c, voltage_unbalance, current_unbalance, created_at, updated_at)
SELECT id, NOW(), 2.1 + MOD(id, 5) / 10, 2.3 + MOD(id, 4) / 10, 2.2 + MOD(id, 6) / 10, 1.5 + MOD(id, 3) / 10, 1.6 + MOD(id, 4) / 10, 1.4 + MOD(id, 5) / 10, 0.42 + MOD(id, 3) / 100, 1.15 + MOD(id, 4) / 100, NOW(), NOW()
FROM meter WHERE meter_code LIKE 'DEMO-%'
ON DUPLICATE KEY UPDATE collect_time = VALUES(collect_time), current_thd_a = VALUES(current_thd_a), current_thd_b = VALUES(current_thd_b), current_thd_c = VALUES(current_thd_c), voltage_thd_a = VALUES(voltage_thd_a), voltage_thd_b = VALUES(voltage_thd_b), voltage_thd_c = VALUES(voltage_thd_c), voltage_unbalance = VALUES(voltage_unbalance), current_unbalance = VALUES(current_unbalance), updated_at = NOW();

INSERT IGNORE INTO meter_realtime_history (meter_id, collect_time, ts_minute, voltage_a, voltage_b, voltage_c, voltage_ab, voltage_bc, voltage_ca, current_a, current_b, current_c, active_power_total, reactive_power_total, apparent_power_total, power_factor_total, frequency, source)
SELECT m.id, DATE_SUB(NOW(), INTERVAL (seq.n * 30) MINUTE), DATE_FORMAT(DATE_SUB(NOW(), INTERVAL (seq.n * 30) MINUTE), '%Y-%m-%d %H:%i:00'),
       220.1 + MOD(m.id + seq.n, 8) / 10, 220.7 + MOD(m.id + seq.n, 7) / 10, 219.6 + MOD(m.id + seq.n, 6) / 10,
       381.8, 382.0, 381.1, 16.0 + MOD(m.id + seq.n, 11), 15.6 + MOD(m.id + seq.n, 9), 15.8 + MOD(m.id + seq.n, 10),
       9800 + MOD(m.id * 7 + seq.n * 17, 6500), 1800 + MOD(m.id + seq.n, 9) * 120, 10300 + MOD(m.id * 9 + seq.n * 11, 6900), 0.95 + MOD(seq.n, 4) / 100, 49.98 + MOD(seq.n, 5) / 100, 'DEMO'
FROM meter m
JOIN (
  SELECT ones.n + tens.n * 10 AS n
  FROM (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) ones
  CROSS JOIN (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) tens
) seq
WHERE m.meter_code LIKE 'DEMO-%' AND seq.n < 96;

INSERT IGNORE INTO meter_energy_history (meter_id, collect_time, forward_active_energy, reverse_active_energy, forward_reactive_energy, reverse_reactive_energy)
SELECT m.id, DATE_SUB(NOW(), INTERVAL (seq.n * 30) MINUTE),
       12000 + m.id * 87 - seq.n * (0.18 + MOD(m.id, 5) / 100), 4.2 + MOD(m.id, 10) / 10,
       2580 + m.id * 21 - seq.n * (0.04 + MOD(m.id, 3) / 100), 1.6 + MOD(m.id, 5) / 10
FROM meter m
JOIN (
  SELECT ones.n + tens.n * 10 AS n
  FROM (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) ones
  CROSS JOIN (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) tens
) seq
WHERE m.meter_code LIKE 'DEMO-%' AND seq.n < 96;

INSERT IGNORE INTO meter_power_quality_history (meter_id, collect_time, current_thd_a, current_thd_b, current_thd_c, voltage_thd_a, voltage_thd_b, voltage_thd_c, voltage_unbalance, current_unbalance)
SELECT m.id, DATE_SUB(NOW(), INTERVAL (seq.n * 30) MINUTE),
       2.0 + MOD(m.id + seq.n, 7) / 10, 2.1 + MOD(m.id + seq.n, 6) / 10, 2.2 + MOD(m.id + seq.n, 5) / 10,
       1.3 + MOD(m.id + seq.n, 4) / 10, 1.4 + MOD(m.id + seq.n, 5) / 10, 1.5 + MOD(m.id + seq.n, 3) / 10,
       0.38 + MOD(m.id + seq.n, 4) / 100, 1.08 + MOD(m.id + seq.n, 6) / 100
FROM meter m
JOIN (
  SELECT ones.n + tens.n * 10 AS n
  FROM (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) ones
  CROSS JOIN (SELECT 0 n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) tens
) seq
WHERE m.meter_code LIKE 'DEMO-%' AND seq.n < 96;

COMMIT;
