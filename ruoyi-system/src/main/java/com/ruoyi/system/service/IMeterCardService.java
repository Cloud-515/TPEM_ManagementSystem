package com.ruoyi.system.service;

import java.util.List;
import java.util.Map;
import com.ruoyi.system.domain.MeterCard;

public interface IMeterCardService
{
    public Map<String, Object> getMeterCardGroups();

    public List<MeterCard> listRealtimeMeters(MeterCard query);

    public List<MeterCard> listEnergyMeters(MeterCard query);

    public List<MeterCard> listQualityMeters(MeterCard query);

    public List<MeterCard> listRealtimeHistory(MeterCard query);

    public List<MeterCard> listEnergyHistory(MeterCard query);

    public List<MeterCard> listQualityHistory(MeterCard query);

    public MeterCard getRealtimeDetail(Long meterId);
}
