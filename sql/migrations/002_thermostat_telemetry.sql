CREATE TABLE IF NOT EXISTS thermostat_device (
    id BIGSERIAL PRIMARY KEY,
    site_code VARCHAR(128) NOT NULL,
    device_key VARCHAR(128) NOT NULL,
    slave_id INTEGER NOT NULL CHECK (slave_id BETWEEN 1 AND 99),
    name VARCHAR(256),
    group_name VARCHAR(256),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (site_code, device_key)
);

CREATE TABLE IF NOT EXISTS thermostat_message_log (
    message_id UUID PRIMARY KEY,
    received_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS thermostat_telemetry_history (
    id BIGSERIAL PRIMARY KEY,
    device_id BIGINT NOT NULL REFERENCES thermostat_device(id),
    collected_at TIMESTAMPTZ NOT NULL,
    is_online BOOLEAN NOT NULL,
    room_temperature_celsius NUMERIC(5,1),
    set_temperature_celsius NUMERIC(5,1),
    power_state VARCHAR(32),
    mode VARCHAR(32),
    fan_speed VARCHAR(32),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_thermostat_telemetry_history_device_time
    ON thermostat_telemetry_history (device_id, collected_at DESC);

CREATE TABLE IF NOT EXISTS thermostat_device_state (
    device_id BIGINT PRIMARY KEY REFERENCES thermostat_device(id),
    collected_at TIMESTAMPTZ NOT NULL,
    is_online BOOLEAN NOT NULL,
    room_temperature_celsius NUMERIC(5,1),
    set_temperature_celsius NUMERIC(5,1),
    power_state VARCHAR(32),
    mode VARCHAR(32),
    fan_speed VARCHAR(32),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
