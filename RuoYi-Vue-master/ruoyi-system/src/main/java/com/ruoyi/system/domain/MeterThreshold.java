package com.ruoyi.system.domain;

import java.math.BigDecimal;

/**
 * 电表判定阈值（对应 meter_threshold 表的一行）。
 *
 * 对应《代码审查与改进清单.md》P1-1：此前合格/不合格判定在上位机、入库服务、
 * 本服务端、前端 meter-utils、前端模拟数据里各写了一套，阈值互不一致
 * （例如相电压：上位机 180~260V、服务端 198~242V）。现统一从本表读取。
 *
 * 作用域优先级：meter &gt; box &gt; site &gt; global。
 */
public class MeterThreshold
{
    /** global / site / box / meter */
    private String scope;

    /** global 为空串；site/box/meter 分别为 site_code / box_code / meter_code */
    private String scopeKey;

    private String metricCode;

    private String metricName;

    private String unit;

    private BigDecimal minValue;

    private BigDecimal maxValue;

    private Boolean alarmEnabled;

    public String getScope()
    {
        return scope;
    }

    public void setScope(String scope)
    {
        this.scope = scope;
    }

    public String getScopeKey()
    {
        return scopeKey;
    }

    public void setScopeKey(String scopeKey)
    {
        this.scopeKey = scopeKey;
    }

    public String getMetricCode()
    {
        return metricCode;
    }

    public void setMetricCode(String metricCode)
    {
        this.metricCode = metricCode;
    }

    public String getMetricName()
    {
        return metricName;
    }

    public void setMetricName(String metricName)
    {
        this.metricName = metricName;
    }

    public String getUnit()
    {
        return unit;
    }

    public void setUnit(String unit)
    {
        this.unit = unit;
    }

    public BigDecimal getMinValue()
    {
        return minValue;
    }

    public void setMinValue(BigDecimal minValue)
    {
        this.minValue = minValue;
    }

    public BigDecimal getMaxValue()
    {
        return maxValue;
    }

    public void setMaxValue(BigDecimal maxValue)
    {
        this.maxValue = maxValue;
    }

    public Boolean getAlarmEnabled()
    {
        return alarmEnabled;
    }

    public void setAlarmEnabled(Boolean alarmEnabled)
    {
        this.alarmEnabled = alarmEnabled;
    }

    /** 下限，取不到时返回给定默认值。 */
    public float minOr(float defaultValue)
    {
        return minValue == null ? defaultValue : minValue.floatValue();
    }

    /** 上限，取不到时返回给定默认值。 */
    public float maxOr(float defaultValue)
    {
        return maxValue == null ? defaultValue : maxValue.floatValue();
    }
}
