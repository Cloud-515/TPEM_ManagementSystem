CREATE TABLE IF NOT EXISTS heat_pump_device
(
    id BIGSERIAL PRIMARY KEY,
    site_code VARCHAR(100) NOT NULL,
    controller_address INTEGER NOT NULL,
    controller_name VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (site_code, controller_address)
);

CREATE TABLE IF NOT EXISTS heat_pump_message_log
(
    id BIGSERIAL PRIMARY KEY,
    topic VARCHAR(500) NOT NULL,
    message_id UUID NOT NULL,
    received_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (topic, message_id)
);

CREATE TABLE IF NOT EXISTS heat_pump_telemetry_history
(
    id BIGSERIAL PRIMARY KEY,
    device_id BIGINT NOT NULL REFERENCES heat_pump_device(id),
    module_index INTEGER NOT NULL,
    collected_at TIMESTAMPTZ NOT NULL,
    message_id UUID NOT NULL,
    module_name VARCHAR(200),
    enabled BOOLEAN NOT NULL,
    state_code VARCHAR(50),
    run_mode INTEGER,
    target_temperature REAL,
    water_in_temperature REAL,
    water_out_temperature REAL,
    ambient_temperature REAL,
    fault_code INTEGER NOT NULL DEFAULT 0,
    protection_code INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (device_id, module_index, message_id)
);

CREATE INDEX IF NOT EXISTS ix_heat_pump_telemetry_history_device_module_time
    ON heat_pump_telemetry_history (device_id, module_index, collected_at DESC);

CREATE TABLE IF NOT EXISTS heat_pump_device_state
(
    device_id BIGINT NOT NULL REFERENCES heat_pump_device(id),
    module_index INTEGER NOT NULL,
    module_name VARCHAR(200),
    last_collect_time TIMESTAMPTZ NOT NULL,
    last_message_id UUID NOT NULL,
    is_online BOOLEAN NOT NULL DEFAULT TRUE,
    state_code VARCHAR(50),
    run_mode INTEGER,
    target_temperature REAL,
    water_in_temperature REAL,
    water_out_temperature REAL,
    ambient_temperature REAL,
    fault_code INTEGER NOT NULL DEFAULT 0,
    protection_code INTEGER NOT NULL DEFAULT 0,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (device_id, module_index)
);

CREATE INDEX IF NOT EXISTS ix_heat_pump_device_state_offline
    ON heat_pump_device_state (is_online, last_collect_time);

CREATE TABLE IF NOT EXISTS heat_pump_alarm_event
(
    id BIGSERIAL PRIMARY KEY,
    device_id BIGINT NOT NULL REFERENCES heat_pump_device(id),
    module_index INTEGER NOT NULL,
    alarm_type VARCHAR(30) NOT NULL,
    alarm_code INTEGER NOT NULL DEFAULT 0,
    opened_at TIMESTAMPTZ NOT NULL,
    last_seen_at TIMESTAMPTZ NOT NULL,
    recovered_at TIMESTAMPTZ,
    message VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_heat_pump_alarm_event_active
    ON heat_pump_alarm_event (device_id, module_index, alarm_type, alarm_code)
    WHERE recovered_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_heat_pump_alarm_event_active
    ON heat_pump_alarm_event (recovered_at, last_seen_at DESC);
