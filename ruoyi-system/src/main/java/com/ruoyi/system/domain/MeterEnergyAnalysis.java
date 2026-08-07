package com.ruoyi.system.domain;

import java.util.List;

public class MeterEnergyAnalysis
{
    private MeterEnergyRangeSummary summary;
    private List<MeterCard> energyPoints;
    private List<MeterCard> powerPoints;

    public MeterEnergyRangeSummary getSummary()
    {
        return summary;
    }

    public void setSummary(MeterEnergyRangeSummary summary)
    {
        this.summary = summary;
    }

    public List<MeterCard> getEnergyPoints()
    {
        return energyPoints;
    }

    public void setEnergyPoints(List<MeterCard> energyPoints)
    {
        this.energyPoints = energyPoints;
    }

    public List<MeterCard> getPowerPoints()
    {
        return powerPoints;
    }

    public void setPowerPoints(List<MeterCard> powerPoints)
    {
        this.powerPoints = powerPoints;
    }
}
