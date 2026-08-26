package com.ruoyi.system.mapper;

import java.math.BigDecimal;
import java.math.BigDecimal;
import java.util.Date;
import java.util.List;
import org.apache.ibatis.annotations.Param;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterEnergyReading;
import com.ruoyi.system.domain.MeterThreshold;

public interface MeterCardMapper
{
    public List<MeterCard> selectMeterCards();

    /**
     * P1-1：读取判定阈值。与上位机、入库服务读的是同一张 meter_threshold 表，
     * 保证三端对同一台表给出一致的合格/不合格结论。
     */
    public List<MeterThreshold> selectMeterThresholds();

    public List<MeterCard> selectRealtimeHistoryTrend(MeterCard query);

    public List<MeterCard> selectEnergyHistoryTrend(MeterCard query);

    public BigDecimal selectFirstForwardActiveEnergy(MeterCard query);

    public BigDecimal selectLastForwardActiveEnergy(MeterCard query);

    public Long countForwardActiveEnergyReadings(MeterCard query);

    public List<MeterCard> selectQualityHistoryTrend(MeterCard query);

    public List<MeterCard> selectRealtimeMeters(MeterCard query);

    public List<MeterCard> selectEnergyMeters(MeterCard query);

    public List<MeterCard> selectQualityMeters(MeterCard query);

    public List<MeterCard> selectRealtimeHistory(MeterCard query);

    public List<MeterCard> selectEnergyHistory(MeterCard query);

    public List<MeterCard> selectQualityHistory(MeterCard query);

    public MeterCard selectRealtimeDetail(Long meterId);

    /**
     * 查询仪表正向有功累计电能读数。
     *
     * @param beginTime 开始时间
     * @param endTime 结束时间
     * @return 历史读数
     */
    public List<MeterEnergyReading> selectEnergyReadings(@Param("beginTime") Date beginTime, @Param("endTime") Date endTime);
}
