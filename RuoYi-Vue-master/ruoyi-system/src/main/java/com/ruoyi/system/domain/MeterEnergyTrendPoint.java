package com.ruoyi.system.domain;

import java.math.BigDecimal;

/**
 * 仪表盘能耗趋势数据点。
 */
public class MeterEnergyTrendPoint
{
    private String label;

    private BigDecimal consumptionKwh;

    private Integer meterCount;

    public String getLabel()
    {
        return label;
    }

    public void setLabel(String label)
    {
        this.label = label;
    }

    public BigDecimal getConsumptionKwh()
    {
        return consumptionKwh;
    }

    public void setConsumptionKwh(BigDecimal consumptionKwh)
    {
        this.consumptionKwh = consumptionKwh;
    }

    public Integer getMeterCount()
    {
        return meterCount;
    }

    public void setMeterCount(Integer meterCount)
    {
        this.meterCount = meterCount;
    }
}
