-- =============================================================================
-- 004 迁移记录表
--
-- 起因：现网出现了「003 已应用、002 没应用」的状态，而没有任何地方记录过哪个执行过 ——
-- 全靠人记。第二批实施时是靠查 information_schema 里有没有对应的表才推断出来的，
-- 这种推断只在「迁移恰好会建表」时才成立，一旦有纯 ALTER 或纯 INSERT 的迁移就失效。
--
-- 本脚本幂等，全部为新增对象，不修改也不删除任何现有表与数据。
-- =============================================================================

CREATE TABLE IF NOT EXISTS "public"."schema_migration" (
    "version"    varchar(32)  PRIMARY KEY,
    "name"       varchar(255) NOT NULL,
    -- 迁移文件内容的 sha256。应用后若文件被改动，比对即可发现（迁移文件应视为不可变）。
    "checksum"   varchar(64),
    "applied_at" timestamptz  NOT NULL DEFAULT now(),
    "note"       varchar(500)
);

COMMENT ON TABLE "public"."schema_migration" IS
'数据库迁移应用记录。version 取迁移文件名前缀（001/002/...）。未出现在本表中的迁移即为未应用。';

-- -----------------------------------------------------------------------------
-- 回填历史迁移的应用状态
--
-- 不能凭空写「已应用」，所以按各迁移的标志性对象是否存在来判断：
--   001 → heat_pump_device 等五张表
--   002 → thermostat_device 等四张表
--   003 → meter_threshold 表
-- 判断结果为「未应用」的不写入本表，保持「不在表里 = 没应用」这条不变量。
-- -----------------------------------------------------------------------------
INSERT INTO "public"."schema_migration" ("version", "name", "note")
SELECT '001', '001_heat_pump_telemetry.sql',
       '回填：依据 heat_pump_device 表存在判定为已应用（应用时间未知，取回填时间）'
WHERE EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'heat_pump_device'
)
ON CONFLICT ("version") DO NOTHING;

INSERT INTO "public"."schema_migration" ("version", "name", "note")
SELECT '002', '002_thermostat_telemetry.sql',
       '回填：依据 thermostat_device 表存在判定为已应用'
WHERE EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'thermostat_device'
)
ON CONFLICT ("version") DO NOTHING;

INSERT INTO "public"."schema_migration" ("version", "name", "note")
SELECT '003', '003_threshold_idempotency_alarm.sql',
       '回填：依据 meter_threshold 表存在判定为已应用'
WHERE EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'meter_threshold'
)
ON CONFLICT ("version") DO NOTHING;

INSERT INTO "public"."schema_migration" ("version", "name", "note")
VALUES ('004', '004_schema_migration.sql', '本表自身')
ON CONFLICT ("version") DO NOTHING;

-- =============================================================================
-- 执行结果自检：列出已记录的迁移
-- =============================================================================
DO $$
DECLARE
    v_applied text;
BEGIN
    SELECT string_agg("version", ', ' ORDER BY "version") INTO v_applied FROM "public"."schema_migration";
    RAISE NOTICE '004 迁移完成。已记录为已应用的迁移: %', COALESCE(v_applied, '(无)');
    RAISE NOTICE '未出现在上面列表里的迁移文件即为未应用，用 sql/migrations/apply.sh 可查看。';
END $$;
