package com.ruoyi.system.service;

import java.util.List;
import java.util.Map;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterEnergyAnalysis;
import com.ruoyi.system.domain.MeterEnergyAnalysis;
import com.ruoyi.system.domain.MeterEnergyTrendPoint;
import com.ruoyi.system.domain.MeterHistoryTrend;
import com.ruoyi.system.domain.MeterQualityRiskStats;

public interface IMeterCardService
{
    public Map<String, Object> getMeterCardGroups();

    public List<MeterCard> listRealtimeMeters(MeterCard query);

    public List<MeterCard> listEnergyMeters(MeterCard query);

    public List<MeterCard> listQualityMeters(MeterCard query);

    public MeterQualityRiskStats getQualityRiskStats(MeterCard query);

    public List<MeterCard> listRealtimeHistory(MeterCard query);

    public List<MeterCard> listEnergyHistory(MeterCard query);

    public List<MeterCard> listQualityHistory(MeterCard query);

    public MeterCard getRealtimeDetail(Long meterId);

    public List<MeterEnergyTrendPoint> getEnergyTrend(String range);

    public MeterHistoryTrend getHistoryTrend(String category, MeterCard query);

    public MeterEnergyAnalysis getEnergyAnalysis(MeterCard query);
}
