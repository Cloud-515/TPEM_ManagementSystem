/*
 Navicat Premium Data Transfer

 Source Server         : MeterAcquisition
 Source Server Type    : MySQL
 Source Server Version : 50744 (5.7.44-log)
 Source Host           : localhost:3306
 Source Schema         : ma

 Target Server Type    : MySQL
 Target Server Version : 50744 (5.7.44-log)
 File Encoding         : 65001

 Date: 28/05/2026 09:21:22
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for alarm_event
-- ----------------------------
DROP TABLE IF EXISTS `alarm_event`;
CREATE TABLE `alarm_event`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `meter_id` bigint(20) NOT NULL,
  `alarm_code` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `alarm_name` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `alarm_level` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `alarm_value` decimal(18, 4) NULL DEFAULT NULL,
  `threshold_value` decimal(18, 4) NULL DEFAULT NULL,
  `status` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'active',
  `start_time` datetime NOT NULL,
  `end_time` datetime NULL DEFAULT NULL,
  `duration_seconds` bigint(20) NULL DEFAULT NULL,
  `extra_json` json NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_meter_status_time`(`meter_id`, `status`, `start_time`) USING BTREE,
  INDEX `idx_start_time`(`start_time`) USING BTREE,
  CONSTRAINT `fk_alarm_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for distribution_box
-- ----------------------------
DROP TABLE IF EXISTS `distribution_box`;
CREATE TABLE `distribution_box`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `site_id` bigint(20) NOT NULL,
  `box_code` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `box_name` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `location` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `remark` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_site_box`(`site_id`, `box_code`) USING BTREE,
  CONSTRAINT `fk_box_site` FOREIGN KEY (`site_id`) REFERENCES `site` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter
-- ----------------------------
DROP TABLE IF EXISTS `meter`;
CREATE TABLE `meter`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `site_id` bigint(20) NOT NULL,
  `box_id` bigint(20) NOT NULL,
  `meter_code` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `meter_name` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `slave_address` int(11) NOT NULL,
  `serial_number` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `device_type` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `protocol_version` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `program_version` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `version_date` date NULL DEFAULT NULL,
  `location` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `is_enabled` tinyint(1) NOT NULL DEFAULT 1,
  `is_toolbar` tinyint(1) NOT NULL DEFAULT 0,
  `mqtt_topic` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `last_seen_time` datetime NULL DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_site_meter`(`site_id`, `meter_code`) USING BTREE,
  UNIQUE INDEX `uk_box_slave`(`box_id`, `slave_address`) USING BTREE,
  CONSTRAINT `fk_meter_box` FOREIGN KEY (`box_id`) REFERENCES `distribution_box` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `fk_meter_site` FOREIGN KEY (`site_id`) REFERENCES `site` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 22 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter_comm_config
-- ----------------------------
DROP TABLE IF EXISTS `meter_comm_config`;
CREATE TABLE `meter_comm_config`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `meter_id` bigint(20) NOT NULL,
  `baud_rate` int(11) NOT NULL DEFAULT 9600,
  `parity` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'Even',
  `stop_bits` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'One',
  `data_bits` int(11) NOT NULL DEFAULT 8,
  `port_name` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `uk_meter_comm`(`meter_id`) USING BTREE,
  CONSTRAINT `fk_comm_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter_energy_history
-- ----------------------------
DROP TABLE IF EXISTS `meter_energy_history`;
CREATE TABLE `meter_energy_history`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `forward_active_energy` decimal(18, 4) NULL DEFAULT NULL,
  `reverse_active_energy` decimal(18, 4) NULL DEFAULT NULL,
  `forward_reactive_energy` decimal(18, 4) NULL DEFAULT NULL,
  `reverse_reactive_energy` decimal(18, 4) NULL DEFAULT NULL,
  PRIMARY KEY (`id`, `collect_time`) USING BTREE,
  INDEX `idx_meter_time`(`meter_id`, `collect_time`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 74293 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic PARTITION BY RANGE (TO_DAYS(collect_time))
PARTITIONS 3
(PARTITION `p202605` VALUES LESS THAN (740133) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `p202606` VALUES LESS THAN (740163) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `pmax` VALUES LESS THAN (MAXVALUE) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 )
;

-- ----------------------------
-- Table structure for meter_energy_latest
-- ----------------------------
DROP TABLE IF EXISTS `meter_energy_latest`;
CREATE TABLE `meter_energy_latest`  (
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `forward_active_energy` decimal(18, 4) NULL DEFAULT NULL,
  `reverse_active_energy` decimal(18, 4) NULL DEFAULT NULL,
  `forward_reactive_energy` decimal(18, 4) NULL DEFAULT NULL,
  `reverse_reactive_energy` decimal(18, 4) NULL DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`meter_id`) USING BTREE,
  CONSTRAINT `fk_energy_latest_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter_power_quality_history
-- ----------------------------
DROP TABLE IF EXISTS `meter_power_quality_history`;
CREATE TABLE `meter_power_quality_history`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `current_thd_a` decimal(10, 4) NULL DEFAULT NULL,
  `current_thd_b` decimal(10, 4) NULL DEFAULT NULL,
  `current_thd_c` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_a` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_b` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_c` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_unbalance` decimal(10, 4) NULL DEFAULT NULL,
  `current_unbalance` decimal(10, 4) NULL DEFAULT NULL,
  PRIMARY KEY (`id`, `collect_time`) USING BTREE,
  INDEX `idx_meter_time`(`meter_id`, `collect_time`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 74888 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic PARTITION BY RANGE (TO_DAYS(collect_time))
PARTITIONS 3
(PARTITION `p202605` VALUES LESS THAN (740133) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `p202606` VALUES LESS THAN (740163) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `pmax` VALUES LESS THAN (MAXVALUE) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 )
;

-- ----------------------------
-- Table structure for meter_power_quality_latest
-- ----------------------------
DROP TABLE IF EXISTS `meter_power_quality_latest`;
CREATE TABLE `meter_power_quality_latest`  (
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `current_thd_a` decimal(10, 4) NULL DEFAULT NULL,
  `current_thd_b` decimal(10, 4) NULL DEFAULT NULL,
  `current_thd_c` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_a` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_b` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_thd_c` decimal(10, 4) NULL DEFAULT NULL,
  `voltage_unbalance` decimal(10, 4) NULL DEFAULT NULL,
  `current_unbalance` decimal(10, 4) NULL DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`meter_id`) USING BTREE,
  CONSTRAINT `fk_quality_latest_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter_realtime_history
-- ----------------------------
DROP TABLE IF EXISTS `meter_realtime_history`;
CREATE TABLE `meter_realtime_history`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `ts_minute` bigint(20) NOT NULL,
  `voltage_a` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_b` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_c` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_ab` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_bc` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_ca` decimal(10, 3) NULL DEFAULT NULL,
  `current_a` decimal(12, 4) NULL DEFAULT NULL,
  `current_b` decimal(12, 4) NULL DEFAULT NULL,
  `current_c` decimal(12, 4) NULL DEFAULT NULL,
  `active_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `reactive_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `apparent_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `power_factor_total` decimal(8, 4) NULL DEFAULT NULL,
  `frequency` decimal(8, 4) NULL DEFAULT NULL,
  `source` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'modbus',
  PRIMARY KEY (`id`, `collect_time`) USING BTREE,
  INDEX `idx_meter_time`(`meter_id`, `collect_time`) USING BTREE,
  INDEX `idx_time`(`collect_time`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 75695 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic PARTITION BY RANGE (TO_DAYS(collect_time))
PARTITIONS 3
(PARTITION `p202605` VALUES LESS THAN (740133) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `p202606` VALUES LESS THAN (740163) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 ,
PARTITION `pmax` VALUES LESS THAN (MAXVALUE) ENGINE = InnoDB MAX_ROWS = 0 MIN_ROWS = 0 )
;

-- ----------------------------
-- Table structure for meter_realtime_latest
-- ----------------------------
DROP TABLE IF EXISTS `meter_realtime_latest`;
CREATE TABLE `meter_realtime_latest`  (
  `meter_id` bigint(20) NOT NULL,
  `collect_time` datetime NOT NULL,
  `voltage_a` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_b` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_c` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_ab` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_bc` decimal(10, 3) NULL DEFAULT NULL,
  `voltage_ca` decimal(10, 3) NULL DEFAULT NULL,
  `current_a` decimal(12, 4) NULL DEFAULT NULL,
  `current_b` decimal(12, 4) NULL DEFAULT NULL,
  `current_c` decimal(12, 4) NULL DEFAULT NULL,
  `active_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `reactive_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `apparent_power_total` decimal(14, 4) NULL DEFAULT NULL,
  `power_factor_total` decimal(8, 4) NULL DEFAULT NULL,
  `frequency` decimal(8, 4) NULL DEFAULT NULL,
  `data_quality` tinyint(4) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`meter_id`) USING BTREE,
  CONSTRAINT `fk_realtime_latest_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for meter_status
-- ----------------------------
DROP TABLE IF EXISTS `meter_status`;
CREATE TABLE `meter_status`  (
  `meter_id` bigint(20) NOT NULL,
  `is_online` tinyint(1) NOT NULL DEFAULT 0,
  `last_collect_time` datetime NULL DEFAULT NULL,
  `last_publish_time` datetime NULL DEFAULT NULL,
  `last_error_time` datetime NULL DEFAULT NULL,
  `last_error_message` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `status_code` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'offline',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`meter_id`) USING BTREE,
  CONSTRAINT `fk_status_meter` FOREIGN KEY (`meter_id`) REFERENCES `meter` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for mqtt_message_log
-- ----------------------------
DROP TABLE IF EXISTS `mqtt_message_log`;
CREATE TABLE `mqtt_message_log`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `client_id` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `topic` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `qos` tinyint(4) NOT NULL DEFAULT 0,
  `payload_json` json NOT NULL,
  `meter_code` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `received_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `process_status` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL DEFAULT 'pending',
  `error_message` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  INDEX `idx_topic_time`(`topic`, `received_at`) USING BTREE,
  INDEX `idx_meter_time`(`meter_code`, `received_at`) USING BTREE,
  INDEX `idx_status_time`(`process_status`, `received_at`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 80704 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for site
-- ----------------------------
DROP TABLE IF EXISTS `site`;
CREATE TABLE `site`  (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `site_code` varchar(64) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `site_name` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `address` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `remark` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `site_code`(`site_code`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 2 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
