package com.ruoyi.system.repository;

import java.math.BigDecimal;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.Date;
import java.util.List;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterEnergyReading;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.stereotype.Repository;

@Repository
@ConditionalOnProperty(prefix = "ruoyi.meter-telemetry", name = "enabled", havingValue = "true")
public class MeterTelemetryRepository
{
    private static final String METER_COLUMNS = "m.id AS meter_id, m.meter_code, m.meter_name, m.slave_address, "
        + "b.box_code, b.box_name, s.site_code, s.site_name";
    private static final String REALTIME_COLUMNS = "rl.voltage_a, rl.voltage_b, rl.voltage_c, rl.voltage_ab, rl.voltage_bc, rl.voltage_ca, "
        + "rl.current_a, rl.current_b, rl.current_c, rl.active_power_total, rl.reactive_power_total, "
        + "rl.reactive_power_total AS reactive_power_kvar, rl.apparent_power_total, rl.power_factor_total, rl.frequency, rl.data_quality, "
        + "COALESCE(ms.last_collect_time, rl.collect_time) AS last_collect_time, "
        + "ms.last_publish_time, ms.status_code AS meter_status_code, CASE WHEN ms.is_online THEN 1 WHEN ms.is_online = FALSE THEN 0 END AS is_online";
    private static final String BASE_FROM = " FROM meter m "
        + "LEFT JOIN distribution_box b ON b.id = m.box_id "
        + "LEFT JOIN site s ON s.id = m.site_id "
        + "LEFT JOIN meter_realtime_latest rl ON rl.meter_id = m.id "
        + "LEFT JOIN meter_status ms ON ms.meter_id = m.id ";

    private final NamedParameterJdbcTemplate jdbcTemplate;

    public MeterTelemetryRepository(@Qualifier("meterTelemetryJdbcTemplate") NamedParameterJdbcTemplate jdbcTemplate)
    {
        this.jdbcTemplate = jdbcTemplate;
    }

    public List<MeterCard> selectMeterCards()
    {
        String sql = "SELECT " + METER_COLUMNS + ", " + REALTIME_COLUMNS + ", rl.collect_time AS data_collect_time" + BASE_FROM
            + "WHERE m.is_enabled = TRUE ORDER BY m.is_toolbar DESC, m.slave_address ASC, m.id ASC";
        return jdbcTemplate.query(sql, new MapSqlParameterSource(), meterCardRowMapper());
    }

    public List<MeterCard> selectRealtimeMeters(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", " + REALTIME_COLUMNS + ", rl.collect_time AS data_collect_time" + BASE_FROM
            + whereClause(query, "m") + " ORDER BY m.is_toolbar DESC, m.slave_address ASC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectEnergyMeters(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", " + REALTIME_COLUMNS + ", COALESCE(el.collect_time, rl.collect_time) AS data_collect_time, "
            + "el.forward_active_energy, el.reverse_active_energy, el.forward_reactive_energy, el.reverse_reactive_energy" + BASE_FROM
            + "LEFT JOIN meter_energy_latest el ON el.meter_id = m.id " + whereClause(query, "m")
            + " ORDER BY m.is_toolbar DESC, m.slave_address ASC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectQualityMeters(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", " + REALTIME_COLUMNS + ", COALESCE(ql.collect_time, rl.collect_time) AS data_collect_time, "
            + "ql.current_thd_a, ql.current_thd_b, ql.current_thd_c, ql.voltage_thd_a, ql.voltage_thd_b, ql.voltage_thd_c, "
            + "ql.voltage_unbalance, ql.current_unbalance" + BASE_FROM
            + "LEFT JOIN meter_power_quality_latest ql ON ql.meter_id = m.id " + whereClause(query, "m")
            + " ORDER BY m.is_toolbar DESC, m.slave_address ASC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectRealtimeHistory(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", h.voltage_a, h.voltage_b, h.voltage_c, h.voltage_ab, h.voltage_bc, h.voltage_ca, "
            + "h.current_a, h.current_b, h.current_c, h.active_power_total, h.reactive_power_total, "
            + "h.reactive_power_total AS reactive_power_kvar, h.apparent_power_total, h.power_factor_total, h.frequency, "
            + "h.collect_time AS data_collect_time, h.collect_time AS last_collect_time "
            + "FROM meter_realtime_history h " + historyJoins() + whereClause(query, "m") + historyTimeClause(query, "h")
            + " ORDER BY h.collect_time DESC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectEnergyHistory(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", h.collect_time AS data_collect_time, h.collect_time AS last_collect_time, "
            + "h.forward_active_energy, h.reverse_active_energy, h.forward_reactive_energy, h.reverse_reactive_energy "
            + "FROM meter_energy_history h " + historyJoins() + whereClause(query, "m") + historyTimeClause(query, "h")
            + " ORDER BY h.collect_time DESC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectQualityHistory(MeterCard query)
    {
        String sql = "SELECT " + METER_COLUMNS + ", COALESCE(ms.last_collect_time, h.collect_time) AS last_collect_time, "
            + "ms.last_publish_time, ms.status_code AS meter_status_code, "
            + "CASE WHEN ms.is_online THEN 1 WHEN ms.is_online = FALSE THEN 0 END AS is_online, h.collect_time AS data_collect_time, "
            + "h.current_thd_a, h.current_thd_b, h.current_thd_c, h.voltage_thd_a, h.voltage_thd_b, h.voltage_thd_c, "
            + "h.voltage_unbalance, h.current_unbalance "
            + "FROM meter_power_quality_history h " + historyJoins() + "LEFT JOIN meter_status ms ON ms.meter_id = m.id "
            + whereClause(query, "m") + historyTimeClause(query, "h") + " ORDER BY h.collect_time DESC, m.id ASC";
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public MeterCard selectRealtimeDetail(Long meterId)
    {
        MeterCard query = new MeterCard();
        query.setMeterId(meterId);
        List<MeterCard> cards = selectRealtimeMeters(query);
        return cards.isEmpty() ? null : cards.get(0);
    }

    public List<MeterCard> selectRealtimeHistoryTrend(MeterCard query)
    {
        String sql = sampledHistorySql("meter_realtime_history", "h.collect_time AS data_collect_time, h.collect_time AS last_collect_time, "
            + "h.voltage_a, h.voltage_b, h.voltage_c, h.current_a, h.current_b, h.current_c, h.active_power_total, "
            + "h.reactive_power_total, h.reactive_power_total AS reactive_power_kvar, h.apparent_power_total, h.power_factor_total, h.frequency", query);
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterCard> selectEnergyHistoryTrend(MeterCard query)
    {
        String sql = sampledHistorySql("meter_energy_history", "h.collect_time AS data_collect_time, h.collect_time AS last_collect_time, "
            + "h.forward_active_energy, h.reverse_active_energy, h.forward_reactive_energy, h.reverse_reactive_energy", query);
        return jdbcTemplate.query(sql, parameters(query), meterCardRowMapper());
    }

    public List<MeterEnergyReading> selectEnergyReadings(Date beginTime, Date endTime)
    {
        String sql = "SELECT h.meter_id, h.collect_time, h.forward_active_energy FROM meter_energy_history h "
            + "WHERE h.collect_time >= :beginTime AND h.collect_time <= :endTime AND h.forward_active_energy IS NOT NULL "
            + "ORDER BY h.meter_id, h.collect_time";
        MapSqlParameterSource parameters = new MapSqlParameterSource().addValue("beginTime", beginTime).addValue("endTime", endTime);
        return jdbcTemplate.query(sql, parameters, new RowMapper<MeterEnergyReading>()
        {
            @Override
            public MeterEnergyReading mapRow(ResultSet resultSet, int rowNum) throws SQLException
            {
                MeterEnergyReading reading = new MeterEnergyReading();
                reading.setMeterId(resultSet.getLong("meter_id"));
                reading.setCollectTime(resultSet.getTimestamp("collect_time"));
                reading.setForwardActiveEnergy(resultSet.getBigDecimal("forward_active_energy"));
                return reading;
            }
        });
    }

    private String sampledHistorySql(String table, String columns, MeterCard query)
    {
        return "WITH filtered AS (SELECT h.*, row_number() OVER (ORDER BY h.collect_time) AS row_number, "
            + "count(*) OVER () AS total_rows FROM " + table + " h WHERE h.meter_id = :meterId "
            + "AND h.collect_time >= :beginTime AND h.collect_time <= :endTime), sampled AS (SELECT *, "
            + "GREATEST(1, CEIL(total_rows / 720.0)::bigint) AS sample_step FROM filtered) SELECT " + columns + " FROM sampled h "
            + "WHERE MOD(h.row_number - 1, h.sample_step) = 0 OR h.row_number = h.total_rows ORDER BY h.collect_time";
    }

    private String historyJoins()
    {
        return "INNER JOIN meter m ON m.id = h.meter_id LEFT JOIN distribution_box b ON b.id = m.box_id "
            + "LEFT JOIN site s ON s.id = m.site_id ";
    }

    private String whereClause(MeterCard query, String meterAlias)
    {
        StringBuilder sql = new StringBuilder("WHERE ").append(meterAlias).append(".is_enabled = TRUE");
        if (query != null)
        {
            appendEquals(sql, query.getMeterId(), meterAlias + ".id", "meterId");
            appendLike(sql, query.getMeterName(), meterAlias + ".meter_name", "meterName");
            appendLike(sql, query.getMeterCode(), meterAlias + ".meter_code", "meterCode");
            appendLike(sql, query.getSiteCode(), "s.site_code", "siteCode");
            appendLike(sql, query.getSiteName(), "s.site_name", "siteName");
            appendLike(sql, query.getBoxCode(), "b.box_code", "boxCode");
            appendLike(sql, query.getBoxName(), "b.box_name", "boxName");
        }
        return sql.toString();
    }

    private String historyTimeClause(MeterCard query, String historyAlias)
    {
        StringBuilder sql = new StringBuilder();
        if (query != null && query.getBeginTime() != null)
        {
            sql.append(" AND ").append(historyAlias).append(".collect_time >= :beginTime");
        }
        if (query != null && query.getEndTime() != null)
        {
            sql.append(" AND ").append(historyAlias).append(".collect_time <= :endTime");
        }
        return sql.toString();
    }

    private void appendEquals(StringBuilder sql, Object value, String column, String parameter)
    {
        if (value != null)
        {
            sql.append(" AND ").append(column).append(" = :").append(parameter);
        }
    }

    private void appendLike(StringBuilder sql, String value, String column, String parameter)
    {
        if (value != null && !value.trim().isEmpty())
        {
            sql.append(" AND ").append(column).append(" ILIKE :").append(parameter);
        }
    }

    private MapSqlParameterSource parameters(MeterCard query)
    {
        MapSqlParameterSource parameters = new MapSqlParameterSource();
        if (query == null)
        {
            return parameters;
        }
        parameters.addValue("meterId", query.getMeterId());
        parameters.addValue("meterName", likeValue(query.getMeterName()));
        parameters.addValue("meterCode", likeValue(query.getMeterCode()));
        parameters.addValue("siteCode", likeValue(query.getSiteCode()));
        parameters.addValue("siteName", likeValue(query.getSiteName()));
        parameters.addValue("boxCode", likeValue(query.getBoxCode()));
        parameters.addValue("boxName", likeValue(query.getBoxName()));
        parameters.addValue("beginTime", query.getBeginTime());
        parameters.addValue("endTime", query.getEndTime());
        return parameters;
    }

    private String likeValue(String value)
    {
        return value == null || value.trim().isEmpty() ? null : "%" + value.trim() + "%";
    }

    private RowMapper<MeterCard> meterCardRowMapper()
    {
        return new RowMapper<MeterCard>()
        {
            @Override
            public MeterCard mapRow(ResultSet resultSet, int rowNum) throws SQLException
            {
                MeterCard card = new MeterCard();
                setLong(card, resultSet, "meter_id");
                card.setMeterCode(nullableString(resultSet, "meter_code"));
                card.setMeterName(nullableString(resultSet, "meter_name"));
                setInteger(card, resultSet, "slave_address");
                card.setBoxCode(nullableString(resultSet, "box_code"));
                card.setBoxName(nullableString(resultSet, "box_name"));
                card.setSiteCode(nullableString(resultSet, "site_code"));
                card.setSiteName(nullableString(resultSet, "site_name"));
                card.setIsToolbar(nullableBoolean(resultSet, "is_toolbar"));
                setFloat(card, resultSet, "voltage_a"); setFloat(card, resultSet, "voltage_b"); setFloat(card, resultSet, "voltage_c");
                setFloat(card, resultSet, "voltage_ab"); setFloat(card, resultSet, "voltage_bc"); setFloat(card, resultSet, "voltage_ca");
                setFloat(card, resultSet, "current_a"); setFloat(card, resultSet, "current_b"); setFloat(card, resultSet, "current_c");
                setFloat(card, resultSet, "active_power_total"); setFloat(card, resultSet, "reactive_power_total");
                setFloat(card, resultSet, "reactive_power_kvar"); setFloat(card, resultSet, "apparent_power_total");
                setFloat(card, resultSet, "power_factor_total"); setFloat(card, resultSet, "frequency"); setInteger(card, resultSet, "data_quality");
                setDate(card, resultSet, "data_collect_time"); setDate(card, resultSet, "last_collect_time"); setDate(card, resultSet, "last_publish_time");
                card.setMeterStatusCode(resultSet.getString("meter_status_code")); setInteger(card, resultSet, "is_online");
                setFloat(card, resultSet, "forward_active_energy"); setFloat(card, resultSet, "reverse_active_energy");
                setFloat(card, resultSet, "forward_reactive_energy"); setFloat(card, resultSet, "reverse_reactive_energy");
                setFloat(card, resultSet, "current_thd_a"); setFloat(card, resultSet, "current_thd_b"); setFloat(card, resultSet, "current_thd_c");
                setFloat(card, resultSet, "voltage_thd_a"); setFloat(card, resultSet, "voltage_thd_b"); setFloat(card, resultSet, "voltage_thd_c");
                setFloat(card, resultSet, "voltage_unbalance"); setFloat(card, resultSet, "current_unbalance");
                return card;
            }
        };
    }

    private void setLong(MeterCard card, ResultSet rs, String column) throws SQLException { Long value = nullableLong(rs, column); if ("meter_id".equals(column)) card.setMeterId(value); }
    private void setInteger(MeterCard card, ResultSet rs, String column) throws SQLException { Integer value = nullableInteger(rs, column); if ("slave_address".equals(column)) card.setSlaveAddress(value); else if ("data_quality".equals(column)) card.setDataQuality(value); else if ("is_online".equals(column)) card.setIsOnline(value); }
    private void setFloat(MeterCard card, ResultSet rs, String column) throws SQLException { Float value = nullableFloat(rs, column); if ("voltage_a".equals(column)) card.setVoltageA(value); else if ("voltage_b".equals(column)) card.setVoltageB(value); else if ("voltage_c".equals(column)) card.setVoltageC(value); else if ("voltage_ab".equals(column)) card.setVoltageAb(value); else if ("voltage_bc".equals(column)) card.setVoltageBc(value); else if ("voltage_ca".equals(column)) card.setVoltageCa(value); else if ("current_a".equals(column)) card.setCurrentA(value); else if ("current_b".equals(column)) card.setCurrentB(value); else if ("current_c".equals(column)) card.setCurrentC(value); else if ("active_power_total".equals(column)) card.setActivePowerTotal(value); else if ("reactive_power_total".equals(column)) card.setReactivePowerTotal(value); else if ("reactive_power_kvar".equals(column)) card.setReactivePowerKvar(value); else if ("apparent_power_total".equals(column)) card.setApparentPowerTotal(value); else if ("power_factor_total".equals(column)) card.setPowerFactorTotal(value); else if ("frequency".equals(column)) card.setFrequency(value); else if ("forward_active_energy".equals(column)) card.setForwardActiveEnergy(value); else if ("reverse_active_energy".equals(column)) card.setReverseActiveEnergy(value); else if ("forward_reactive_energy".equals(column)) card.setForwardReactiveEnergy(value); else if ("reverse_reactive_energy".equals(column)) card.setReverseReactiveEnergy(value); else if ("current_thd_a".equals(column)) card.setCurrentThdA(value); else if ("current_thd_b".equals(column)) card.setCurrentThdB(value); else if ("current_thd_c".equals(column)) card.setCurrentThdC(value); else if ("voltage_thd_a".equals(column)) card.setVoltageThdA(value); else if ("voltage_thd_b".equals(column)) card.setVoltageThdB(value); else if ("voltage_thd_c".equals(column)) card.setVoltageThdC(value); else if ("voltage_unbalance".equals(column)) card.setVoltageUnbalance(value); else if ("current_unbalance".equals(column)) card.setCurrentUnbalance(value); }
    private void setDate(MeterCard card, ResultSet rs, String column) throws SQLException { Date value = rs.getTimestamp(column); if ("data_collect_time".equals(column)) card.setDataCollectTime(value); else if ("last_collect_time".equals(column)) card.setLastCollectTime(value); else if ("last_publish_time".equals(column)) card.setLastPublishTime(value); }
    private Long nullableLong(ResultSet rs, String column) throws SQLException { long value = rs.getLong(column); return rs.wasNull() ? null : value; }
    private Integer nullableInteger(ResultSet rs, String column) throws SQLException { int value = rs.getInt(column); return rs.wasNull() ? null : value; }
    private Float nullableFloat(ResultSet rs, String column) throws SQLException { BigDecimal value = rs.getBigDecimal(column); return value == null ? null : value.floatValue(); }
    private String nullableString(ResultSet rs, String column) throws SQLException { return rs.getString(column); }
    private boolean nullableBoolean(ResultSet rs, String column) throws SQLException { boolean value = rs.getBoolean(column); return !rs.wasNull() && value; }
}
