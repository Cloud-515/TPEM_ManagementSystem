package com.ruoyi.system.mapper;

import java.util.List;
import com.ruoyi.system.domain.MeterCard;

public interface MeterCardMapper
{
    public List<MeterCard> selectMeterCards();

    public List<MeterCard> selectRealtimeMeters(MeterCard query);

    public List<MeterCard> selectEnergyMeters(MeterCard query);

    public List<MeterCard> selectQualityMeters(MeterCard query);

    public List<MeterCard> selectRealtimeHistory(MeterCard query);

    public List<MeterCard> selectEnergyHistory(MeterCard query);

    public List<MeterCard> selectQualityHistory(MeterCard query);

    public MeterCard selectRealtimeDetail(Long meterId);
}
