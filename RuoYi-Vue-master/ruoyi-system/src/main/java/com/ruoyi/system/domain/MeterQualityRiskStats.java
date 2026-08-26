package com.ruoyi.system.domain;

public class MeterQualityRiskStats
{
    private long totalCount;
    private long riskCount;
    private long statusAbnormalCount;
    private long powerFactorLowCount;
    private long thdExceededCount;
    private long unbalanceExceededCount;

    public long getTotalCount()
    {
        return totalCount;
    }

    public void setTotalCount(long totalCount)
    {
        this.totalCount = totalCount;
    }

    public long getRiskCount()
    {
        return riskCount;
    }

    public void setRiskCount(long riskCount)
    {
        this.riskCount = riskCount;
    }

    public long getStatusAbnormalCount()
    {
        return statusAbnormalCount;
    }

    public void setStatusAbnormalCount(long statusAbnormalCount)
    {
        this.statusAbnormalCount = statusAbnormalCount;
    }

    public long getPowerFactorLowCount()
    {
        return powerFactorLowCount;
    }

    public void setPowerFactorLowCount(long powerFactorLowCount)
    {
        this.powerFactorLowCount = powerFactorLowCount;
    }

    public long getThdExceededCount()
    {
        return thdExceededCount;
    }

    public void setThdExceededCount(long thdExceededCount)
    {
        this.thdExceededCount = thdExceededCount;
    }

    public long getUnbalanceExceededCount()
    {
        return unbalanceExceededCount;
    }

    public void setUnbalanceExceededCount(long unbalanceExceededCount)
    {
        this.unbalanceExceededCount = unbalanceExceededCount;
    }
}
