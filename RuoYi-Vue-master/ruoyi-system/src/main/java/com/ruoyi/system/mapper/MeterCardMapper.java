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
     * 把电表挂到另一个配电箱。拓扑页允许把电表拖进别的配电箱，那本质是设备自身的箱体归属变了，
     * 而箱体归属被实时运行 / 电能 / 质量等页面共用，所以只能写 meter.box_id 这一个来源。
     * 箱体所属站点一并跟随，避免出现站点与箱体互相矛盾。box_id 没变时不产生写操作。
     */
    public int updateMeterBox(@Param("meterId") Long meterId, @Param("boxId") Long boxId);

    /**
     * 目标配电箱里是否已经有别的表占用这个从站地址（uk_box_slave）。
     * 走数据库而不是只比对布局里的表，是因为已停用（is_enabled = 0）的表不在拓扑布局里，但仍然占着地址。
     */
    public int countBoxSlaveConflict(@Param("meterId") Long meterId, @Param("boxId") Long boxId, @Param("slaveAddress") Integer slaveAddress);

    /** 目标箱体所属站点里是否已经有别的表占用这个设备编号（uk_site_meter）。 */
    public int countSiteMeterCodeConflict(@Param("meterId") Long meterId, @Param("boxId") Long boxId, @Param("meterCode") String meterCode);

    /**
     * 查询仪表正向有功累计电能读数。
     *
     * @param beginTime 开始时间
     * @param endTime 结束时间
     * @return 历史读数
     */
    public List<MeterEnergyReading> selectEnergyReadings(@Param("beginTime") Date beginTime, @Param("endTime") Date endTime);
}
