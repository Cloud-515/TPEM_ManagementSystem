package com.ruoyi.system.domain;

import java.math.BigDecimal;
import java.util.Date;

/**
 * 仪表历史正向有功电能读数。
 */
public class MeterEnergyReading
{
    private Long meterId;

    private Date collectTime;

    private BigDecimal forwardActiveEnergy;

    public Long getMeterId()
    {
        return meterId;
    }

    public void setMeterId(Long meterId)
    {
        this.meterId = meterId;
    }

    public Date getCollectTime()
    {
        return collectTime;
    }

    public void setCollectTime(Date collectTime)
    {
        this.collectTime = collectTime;
    }

    public BigDecimal getForwardActiveEnergy()
    {
        return forwardActiveEnergy;
    }

    public void setForwardActiveEnergy(BigDecimal forwardActiveEnergy)
    {
        this.forwardActiveEnergy = forwardActiveEnergy;
    }
}
