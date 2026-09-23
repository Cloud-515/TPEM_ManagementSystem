package com.ruoyi.system.domain;

import java.math.BigDecimal;
import java.util.Date;
import org.springframework.format.annotation.DateTimeFormat;
import com.fasterxml.jackson.annotation.JsonProperty;

public class MeterCard
{
    private Long meterId;
    private String meterCode;
    private String meterName;
    private Integer slaveAddress;
    private String boxCode;
    private String boxName;
    // 箱体主键。拓扑页把电表拖进另一个配电箱时要靠它写回 meter.box_id（箱体名可能重名，不能用名字定位）
    private Long boxId;
    private String siteCode;
    private String siteName;
    @JsonProperty("isToolbar")
    private boolean isToolbar;
    private Float voltageA;
    private Float voltageB;
    private Float voltageC;
    private Float voltageAb;
    private Float voltageBc;
    private Float voltageCa;
    private Float currentA;
    private Float currentB;
    private Float currentC;
    private Float activePowerTotal;
    private Float reactivePowerTotal;
    private Float reactivePowerKvar;
    private Float apparentPowerTotal;
    private Float powerFactorTotal;
    private Float frequency;
    private Integer dataQuality;
    private Date dataCollectTime;
    private Date lastCollectTime;
    private Date lastPublishTime;
    private String meterStatusCode;
    private Integer isOnline;
    @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private Date beginTime;
    @DateTimeFormat(pattern = "yyyy-MM-dd HH:mm:ss")
    private Date endTime;
    private Float forwardActiveEnergy;
    private Float reverseActiveEnergy;
    private Float forwardReactiveEnergy;
    private Float reverseReactiveEnergy;
    private Float currentThdA;
    private Float currentThdB;
    private Float currentThdC;
    private Float voltageThdA;
    private Float voltageThdB;
    private Float voltageThdC;
    private Float voltageUnbalance;
    private Float currentUnbalance;
    private BigDecimal activePowerKw;
    private BigDecimal maxCurrentA;
    private String statusCode;
    private String statusText;
    private Boolean riskOnly;
    private String[] qualityRiskCodes;

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

    public Integer getSlaveAddress()
    {
        return slaveAddress;
    }

    public void setSlaveAddress(Integer slaveAddress)
    {
        this.slaveAddress = slaveAddress;
    }

    public String getBoxCode()
    {
        return boxCode;
    }

    public void setBoxCode(String boxCode)
    {
        this.boxCode = boxCode;
    }

    public String getBoxName()
    {
        return boxName;
    }

    public void setBoxName(String boxName)
    {
        this.boxName = boxName;
    }

    public Long getBoxId()
    {
        return boxId;
    }

    public void setBoxId(Long boxId)
    {
        this.boxId = boxId;
    }

    public String getSiteCode()
    {
        return siteCode;
    }

    public void setSiteCode(String siteCode)
    {
        this.siteCode = siteCode;
    }

    public String getSiteName()
    {
        return siteName;
    }

    public void setSiteName(String siteName)
    {
        this.siteName = siteName;
    }

    public boolean getIsToolbar()
    {
        return isToolbar;
    }

    public void setIsToolbar(boolean isToolbar)
    {
        this.isToolbar = isToolbar;
    }

    public Float getVoltageA()
    {
        return voltageA;
    }

    public void setVoltageA(Float voltageA)
    {
        this.voltageA = voltageA;
    }

    public Float getVoltageB()
    {
        return voltageB;
    }

    public void setVoltageB(Float voltageB)
    {
        this.voltageB = voltageB;
    }

    public Float getVoltageC()
    {
        return voltageC;
    }

    public void setVoltageC(Float voltageC)
    {
        this.voltageC = voltageC;
    }

    public Float getVoltageAb()
    {
        return voltageAb;
    }

    public void setVoltageAb(Float voltageAb)
    {
        this.voltageAb = voltageAb;
    }

    public Float getVoltageBc()
    {
        return voltageBc;
    }

    public void setVoltageBc(Float voltageBc)
    {
        this.voltageBc = voltageBc;
    }

    public Float getVoltageCa()
    {
        return voltageCa;
    }

    public void setVoltageCa(Float voltageCa)
    {
        this.voltageCa = voltageCa;
    }

    public Float getCurrentA()
    {
        return currentA;
    }

    public void setCurrentA(Float currentA)
    {
        this.currentA = currentA;
    }

    public Float getCurrentB()
    {
        return currentB;
    }

    public void setCurrentB(Float currentB)
    {
        this.currentB = currentB;
    }

    public Float getCurrentC()
    {
        return currentC;
    }

    public void setCurrentC(Float currentC)
    {
        this.currentC = currentC;
    }

    public Float getActivePowerTotal()
    {
        return activePowerTotal;
    }

    public void setActivePowerTotal(Float activePowerTotal)
    {
        this.activePowerTotal = activePowerTotal;
    }

    public Float getReactivePowerTotal()
    {
        return reactivePowerTotal;
    }

    public void setReactivePowerTotal(Float reactivePowerTotal)
    {
        this.reactivePowerTotal = reactivePowerTotal;
    }

    public Float getReactivePowerKvar()
    {
        return reactivePowerKvar;
    }

    public void setReactivePowerKvar(Float reactivePowerKvar)
    {
        this.reactivePowerKvar = reactivePowerKvar;
    }

    public Float getApparentPowerTotal()
    {
        return apparentPowerTotal;
    }

    public void setApparentPowerTotal(Float apparentPowerTotal)
    {
        this.apparentPowerTotal = apparentPowerTotal;
    }

    public Float getPowerFactorTotal()
    {
        return powerFactorTotal;
    }

    public void setPowerFactorTotal(Float powerFactorTotal)
    {
        this.powerFactorTotal = powerFactorTotal;
    }

    public Float getFrequency()
    {
        return frequency;
    }

    public void setFrequency(Float frequency)
    {
        this.frequency = frequency;
    }

    public Integer getDataQuality()
    {
        return dataQuality;
    }

    public void setDataQuality(Integer dataQuality)
    {
        this.dataQuality = dataQuality;
    }

    public Date getDataCollectTime()
    {
        return dataCollectTime;
    }

    public void setDataCollectTime(Date dataCollectTime)
    {
        this.dataCollectTime = dataCollectTime;
    }

    public Date getLastCollectTime()
    {
        return lastCollectTime;
    }

    public void setLastCollectTime(Date lastCollectTime)
    {
        this.lastCollectTime = lastCollectTime;
    }

    public Date getLastPublishTime()
    {
        return lastPublishTime;
    }

    public void setLastPublishTime(Date lastPublishTime)
    {
        this.lastPublishTime = lastPublishTime;
    }

    public String getMeterStatusCode()
    {
        return meterStatusCode;
    }

    public void setMeterStatusCode(String meterStatusCode)
    {
        this.meterStatusCode = meterStatusCode;
    }

    public Integer getIsOnline()
    {
        return isOnline;
    }

    public void setIsOnline(Integer isOnline)
    {
        this.isOnline = isOnline;
    }

    public Date getBeginTime()
    {
        return beginTime;
    }

    public void setBeginTime(Date beginTime)
    {
        this.beginTime = beginTime;
    }

    public Date getEndTime()
    {
        return endTime;
    }

    public void setEndTime(Date endTime)
    {
        this.endTime = endTime;
    }

    public Float getForwardActiveEnergy()
    {
        return forwardActiveEnergy;
    }

    public void setForwardActiveEnergy(Float forwardActiveEnergy)
    {
        this.forwardActiveEnergy = forwardActiveEnergy;
    }

    public Float getReverseActiveEnergy()
    {
        return reverseActiveEnergy;
    }

    public void setReverseActiveEnergy(Float reverseActiveEnergy)
    {
        this.reverseActiveEnergy = reverseActiveEnergy;
    }

    public Float getForwardReactiveEnergy()
    {
        return forwardReactiveEnergy;
    }

    public void setForwardReactiveEnergy(Float forwardReactiveEnergy)
    {
        this.forwardReactiveEnergy = forwardReactiveEnergy;
    }

    public Float getReverseReactiveEnergy()
    {
        return reverseReactiveEnergy;
    }

    public void setReverseReactiveEnergy(Float reverseReactiveEnergy)
    {
        this.reverseReactiveEnergy = reverseReactiveEnergy;
    }

    public Float getCurrentThdA()
    {
        return currentThdA;
    }

    public void setCurrentThdA(Float currentThdA)
    {
        this.currentThdA = currentThdA;
    }

    public Float getCurrentThdB()
    {
        return currentThdB;
    }

    public void setCurrentThdB(Float currentThdB)
    {
        this.currentThdB = currentThdB;
    }

    public Float getCurrentThdC()
    {
        return currentThdC;
    }

    public void setCurrentThdC(Float currentThdC)
    {
        this.currentThdC = currentThdC;
    }

    public Float getVoltageThdA()
    {
        return voltageThdA;
    }

    public void setVoltageThdA(Float voltageThdA)
    {
        this.voltageThdA = voltageThdA;
    }

    public Float getVoltageThdB()
    {
        return voltageThdB;
    }

    public void setVoltageThdB(Float voltageThdB)
    {
        this.voltageThdB = voltageThdB;
    }

    public Float getVoltageThdC()
    {
        return voltageThdC;
    }

    public void setVoltageThdC(Float voltageThdC)
    {
        this.voltageThdC = voltageThdC;
    }

    public Float getVoltageUnbalance()
    {
        return voltageUnbalance;
    }

    public void setVoltageUnbalance(Float voltageUnbalance)
    {
        this.voltageUnbalance = voltageUnbalance;
    }

    public Float getCurrentUnbalance()
    {
        return currentUnbalance;
    }

    public void setCurrentUnbalance(Float currentUnbalance)
    {
        this.currentUnbalance = currentUnbalance;
    }

    public BigDecimal getActivePowerKw()
    {
        return activePowerKw;
    }

    public void setActivePowerKw(BigDecimal activePowerKw)
    {
        this.activePowerKw = activePowerKw;
    }

    public BigDecimal getMaxCurrentA()
    {
        return maxCurrentA;
    }

    public void setMaxCurrentA(BigDecimal maxCurrentA)
    {
        this.maxCurrentA = maxCurrentA;
    }

    public String getStatusCode()
    {
        return statusCode;
    }

    public void setStatusCode(String statusCode)
    {
        this.statusCode = statusCode;
    }

    public String getStatusText()
    {
        return statusText;
    }

    public void setStatusText(String statusText)
    {
        this.statusText = statusText;
    }

    public Boolean getRiskOnly()
    {
        return riskOnly;
    }

    public void setRiskOnly(Boolean riskOnly)
    {
        this.riskOnly = riskOnly;
    }

    public String[] getQualityRiskCodes()
    {
        return qualityRiskCodes;
    }

    public void setQualityRiskCodes(String[] qualityRiskCodes)
    {
        this.qualityRiskCodes = qualityRiskCodes;
    }
}
