package com.ruoyi.web.config;

import javax.sql.DataSource;

import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.jdbc.DataSourceBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

@Configuration
@ConditionalOnProperty(prefix = "meter-telemetry.datasource", name = "enabled", havingValue = "true")
public class MeterTelemetryDataSourceConfig
{
    @Bean(name = "meterTelemetryDataSource")
    @ConfigurationProperties(prefix = "meter-telemetry.datasource")
    public DataSource meterTelemetryDataSource()
    {
        return DataSourceBuilder.create().build();
    }

    @Bean(name = "meterTelemetryJdbcTemplate")
    public NamedParameterJdbcTemplate meterTelemetryJdbcTemplate(
        @Qualifier("meterTelemetryDataSource") DataSource dataSource)
    {
        return new NamedParameterJdbcTemplate(dataSource);
    }
}
