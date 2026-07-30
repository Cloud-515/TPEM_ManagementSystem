package com.ruoyi.web.config;

import javax.sql.DataSource;

import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.jdbc.DataSourceBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

@Configuration
public class HeatPumpDataSourceConfig
{
    @Bean(name = "heatPumpDataSource")
    @ConfigurationProperties(prefix = "heat-pump.datasource")
    public DataSource heatPumpDataSource()
    {
        return DataSourceBuilder.create().build();
    }

    @Bean(name = "heatPumpJdbcTemplate")
    public NamedParameterJdbcTemplate heatPumpJdbcTemplate(@Qualifier("heatPumpDataSource") DataSource dataSource)
    {
        return new NamedParameterJdbcTemplate(dataSource);
    }
}
