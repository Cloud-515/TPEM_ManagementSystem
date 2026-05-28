CREATE UNIQUE INDEX IF NOT EXISTS uq_meter_realtime_history_meter_time
ON public.meter_realtime_history (meter_id, collect_time);

CREATE UNIQUE INDEX IF NOT EXISTS uq_meter_energy_history_meter_time
ON public.meter_energy_history (meter_id, collect_time);

CREATE UNIQUE INDEX IF NOT EXISTS uq_meter_power_quality_history_meter_time
ON public.meter_power_quality_history (meter_id, collect_time);
