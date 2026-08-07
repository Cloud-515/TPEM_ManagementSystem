package com.ruoyi.system.domain;

import java.math.BigDecimal;

public class MeterEnergyRangeSummary
{
    private BigDecimal startForwardActiveEnergy;
    private BigDecimal endForwardActiveEnergy;
    private BigDecimal intervalEnergy;
    private Long validReadingCount;
    private Boolean valid;
    private String reason;

    public BigDecimal getStartForwardActiveEnergy()
    {
        return startForwardActiveEnergy;
    }

    public void setStartForwardActiveEnergy(BigDecimal startForwardActiveEnergy)
    {
        this.startForwardActiveEnergy = startForwardActiveEnergy;
    }

    public BigDecimal getEndForwardActiveEnergy()
    {
        return endForwardActiveEnergy;
    }

    public void setEndForwardActiveEnergy(BigDecimal endForwardActiveEnergy)
    {
        this.endForwardActiveEnergy = endForwardActiveEnergy;
    }

    public BigDecimal getIntervalEnergy()
    {
        return intervalEnergy;
    }

    public void setIntervalEnergy(BigDecimal intervalEnergy)
    {
        this.intervalEnergy = intervalEnergy;
    }

    public Long getValidReadingCount()
    {
        return validReadingCount;
    }

    public void setValidReadingCount(Long validReadingCount)
    {
        this.validReadingCount = validReadingCount;
    }

    public Boolean getValid()
    {
        return valid;
    }

    public void setValid(Boolean valid)
    {
        this.valid = valid;
    }

    public String getReason()
    {
        return reason;
    }

    public void setReason(String reason)
    {
        this.reason = reason;
    }
}
