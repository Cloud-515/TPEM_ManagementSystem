-- =============================================================================
-- 003 判定阈值下沉 + 消息幂等 + 告警唯一约束
--
-- 对应《代码审查与改进清单.md》第二批：
--   P1-1 合格/不合格判定此前有五套独立实现（上位机 / 入库服务 / Java 服务端 /
--        前端 meter-utils / 前端模拟数据），阈值互不一致。本脚本建立唯一口径表。
--   P1-4 电表消息此前无 MessageId，幂等仅靠内存 10 秒窗口，重启与多实例均失效。
--   P1-6 报警此前无去抖、无滞回、无带电前置条件。阈值表同时承载这三项配置。
--   P1-2 电表离线此前不写 alarm_event，需要一个能配合 ON CONFLICT 的唯一约束。
--
-- 本脚本幂等，可重复执行。全部为新增对象，不修改也不删除任何现有表与数据。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. 判定阈值表（P1-1 / P1-6）
--
-- 作用域优先级：meter > box > site > global，由代码按此顺序取第一条命中的记录。
-- scope_key 用空串表示 global，避免 NULL 在唯一索引里被当作互不相等而产生重复行。
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "public"."meter_threshold" (
    "id"                bigserial     PRIMARY KEY,
    "scope"             varchar(16)   NOT NULL,
    "scope_key"         varchar(64)   NOT NULL DEFAULT '',
    "metric_code"       varchar(64)   NOT NULL,
    "metric_name"       varchar(128)  NOT NULL,
    "unit"              varchar(16),
    "min_value"         numeric(18,4),
    "max_value"         numeric(18,4),
    -- 滞回余量：低限报警需回升到 min_value + recover_margin 才恢复；
    -- 高限报警需回落到 max_value - recover_margin 才恢复。0 表示不做滞回。
    "recover_margin"    numeric(18,4) NOT NULL DEFAULT 0,
    -- 去抖次数：连续命中该次数才置位报警，避免单次抖动产生一条事件。
    "debounce_count"    integer       NOT NULL DEFAULT 1,
    -- 是否要求"已带电"才参与判定。空载 / 停电时 PF 与电压天然为 0，
    -- 不加这个前置条件会把停电误判成功率因数过低、电压过低。
    "require_energized" boolean       NOT NULL DEFAULT true,
    "alarm_code_low"    varchar(64),
    "alarm_code_high"   varchar(64),
    "alarm_name_low"    varchar(128),
    "alarm_name_high"   varchar(128),
    "alarm_level"       varchar(16)   NOT NULL DEFAULT 'warning',
    "is_alarm_enabled"  boolean       NOT NULL DEFAULT true,
    "remark"            varchar(255),
    "created_at"        timestamptz   NOT NULL DEFAULT now(),
    "updated_at"        timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT "uq_meter_threshold" UNIQUE ("scope", "scope_key", "metric_code"),
    CONSTRAINT "ck_meter_threshold_scope" CHECK ("scope" IN ('global', 'site', 'box', 'meter')),
    CONSTRAINT "ck_meter_threshold_debounce" CHECK ("debounce_count" >= 1)
);

COMMENT ON TABLE "public"."meter_threshold" IS
'电表判定阈值唯一口径表。上位机、入库服务、Java 服务端均从此表读取，不再各自硬编码。作用域优先级 meter > box > site > global。';

-- -----------------------------------------------------------------------------
-- 2. 全局默认阈值种子数据
--
-- 取值来源：RuoYi-Vue-master/docs/仪表监测判定标准.md
-- 该文档是本项目唯一有工程依据的一份判定标准（相电压 220V±10%、功率因数考核值 0.85 等），
-- 因此以它为基准，而不是沿用上位机里硬编码的 180~260V / PF 0.5。
--
-- 例外说明：
--   frequency 沿用上位机原有的 40~70Hz。这个区间不是电能质量标准，而是"设备是否还在
--   正常送电"的存活性检查，收窄它需要现场确认，故本次保持行为不变。
--   current_phase 沿用入库服务 App.config 原有的 400A。
--   energized_voltage_min 是 require_energized 判定用的带电门槛，本身不产生报警。
-- -----------------------------------------------------------------------------
INSERT INTO "public"."meter_threshold"
    ("scope", "scope_key", "metric_code", "metric_name", "unit",
     "min_value", "max_value", "recover_margin", "debounce_count", "require_energized",
     "alarm_code_low", "alarm_code_high", "alarm_name_low", "alarm_name_high",
     "alarm_level", "is_alarm_enabled", "remark")
VALUES
    ('global', '', 'voltage_phase', '相电压', 'V',
     198, 242, 2, 3, true,
     'voltage_low', 'voltage_high', '电压过低', '电压过高',
     'warning', true, '220V ±10%，来源：仪表监测判定标准.md'),

    ('global', '', 'power_factor_total', '总功率因数', NULL,
     0.85, NULL, 0.02, 5, true,
     'power_factor_low', NULL, '功率因数过低', NULL,
     'warning', true, '考核值 0.85，来源：仪表监测判定标准.md。去抖 5 次 + 滞回 0.02 用于抑制在阈值附近反复抖动'),

    ('global', '', 'current_phase', '相电流', 'A',
     NULL, 400, 5, 3, false,
     NULL, 'current_high', NULL, '电流过高',
     'critical', true, '沿用入库服务原 AlarmCurrentHighThreshold=400'),

    ('global', '', 'voltage_thd', '电压总谐波畸变率', '%',
     NULL, 5, 0.5, 3, true,
     NULL, 'voltage_thd_high', NULL, '电压谐波超限',
     'warning', true, '来源：仪表监测判定标准.md。入库服务此前未评估该项'),

    ('global', '', 'current_thd', '电流总谐波畸变率', '%',
     NULL, 8, 0.5, 3, true,
     NULL, 'current_thd_high', NULL, '电流谐波超限',
     'warning', true, '来源：仪表监测判定标准.md。入库服务此前未评估该项'),

    ('global', '', 'voltage_unbalance', '电压不平衡度', '%',
     NULL, 2, 0.2, 3, true,
     NULL, 'voltage_unbalance_high', NULL, '电压不平衡超限',
     'warning', true, '来源：仪表监测判定标准.md。入库服务此前未评估该项'),

    ('global', '', 'current_unbalance', '电流不平衡度', '%',
     NULL, 3, 0.3, 3, true,
     NULL, 'current_unbalance_high', NULL, '电流不平衡超限',
     'warning', true, '来源：仪表监测判定标准.md。入库服务此前未评估该项'),

    ('global', '', 'frequency', '频率', 'Hz',
     40, 70, 0.2, 3, true,
     'frequency_low', 'frequency_high', '频率过低', '频率过高',
     'warning', true, '沿用上位机原有存活性检查区间，非电能质量标准，收窄需现场确认'),

    ('global', '', 'energized_voltage_min', '带电判定电压下限', 'V',
     50, NULL, 0, 1, false,
     NULL, NULL, NULL, NULL,
     'info', false, '三相电压均低于此值视为未带电，此时跳过所有 require_energized 的判定项')
ON CONFLICT ("scope", "scope_key", "metric_code") DO NOTHING;

-- -----------------------------------------------------------------------------
-- 3. 电表消息幂等表（P1-4）
--
-- 热泵与温控器链路早就用 MessageId + 唯一约束做库级去重，电表链路却只有内存 10 秒窗口，
-- 进程重启或多实例部署时完全失效。本表让电表链路与那两条链路对齐。
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "public"."meter_message_log" (
    "message_id"  uuid         PRIMARY KEY,
    "topic"       varchar(255) NOT NULL,
    "meter_code"  varchar(64),
    "message_type" varchar(64),
    "collect_time" timestamptz,
    "received_at" timestamptz  NOT NULL DEFAULT now()
);

COMMENT ON TABLE "public"."meter_message_log" IS
'电表遥测消息幂等表。仅存 message_id 用于去重，由入库服务定期清理过期记录。';

-- 供定期清理使用
CREATE INDEX IF NOT EXISTS "idx_meter_message_log_received_at"
    ON "public"."meter_message_log" USING btree ("received_at");

-- -----------------------------------------------------------------------------
-- 4. 告警唯一约束（P1-2 / P1-6）
--
-- 同一台表、同一 alarm_code 在任意时刻最多只能有一条 active 记录。
-- 原实现用"先 SELECT 再 INSERT/UPDATE"，在并发（ProcessWorkers>1）下会插出重复的
-- active 告警；有了这个部分唯一索引，才能改用 ON CONFLICT 一条语句完成，且天然防重。
--
-- 执行前已确认现网 263 条告警中不存在 (meter_id, alarm_code) 的 active 重复。
-- -----------------------------------------------------------------------------
CREATE UNIQUE INDEX IF NOT EXISTS "uq_alarm_event_active"
    ON "public"."alarm_event" USING btree ("meter_id", "alarm_code")
    WHERE "status" = 'active';

-- 供网页端"报警记录"按时间倒序翻页
CREATE INDEX IF NOT EXISTS "idx_alarm_event_meter_code_time"
    ON "public"."alarm_event" USING btree ("meter_id", "alarm_code", "start_time" DESC);

-- =============================================================================
-- 执行结果自检
-- =============================================================================
DO $$
DECLARE
    v_threshold_count integer;
BEGIN
    SELECT count(*) INTO v_threshold_count FROM "public"."meter_threshold" WHERE "scope" = 'global';
    RAISE NOTICE '003 迁移完成：全局阈值 % 条，meter_message_log 与 uq_alarm_event_active 已就绪。', v_threshold_count;
END $$;


