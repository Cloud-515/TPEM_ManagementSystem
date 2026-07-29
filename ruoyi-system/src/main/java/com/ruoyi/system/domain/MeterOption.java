package com.ruoyi.system.domain;

/**
 * 仪表选择项。
 */
public class MeterOption
{
    private Long meterId;

    private String meterCode;

    private String meterName;

    private String siteName;

    private String boxName;

    private String slaveAddress;

    public Long getMeterId()
    {
        return meterId;
    }

    public void setMeterId(Long meterId)
    {
        this.meterId = meterId;
    }

    public String getMeterCode()
    {
        return meterCode;
    }

    public void setMeterCode(String meterCode)
    {
        this.meterCode = meterCode;
    }

    public String getMeterName()
    {
        return meterName;
    }

    public void setMeterName(String meterName)
    {
        this.meterName = meterName;
    }

    public String getSiteName()
    {
        return siteName;
    }

    public void setSiteName(String siteName)
    {
        this.siteName = siteName;
    }

    public String getBoxName()
    {
        return boxName;
    }

    public void setBoxName(String boxName)
    {
        this.boxName = boxName;
    }

    public String getSlaveAddress()
    {
        return slaveAddress;
    }

    public void setSlaveAddress(String slaveAddress)
    {
        this.slaveAddress = slaveAddress;
    }
}
