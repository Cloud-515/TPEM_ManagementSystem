package com.ruoyi.system.service;

import java.util.List;
import java.util.Map;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterEnergyAnalysis;
import com.ruoyi.system.domain.MeterEnergyAnalysis;
import com.ruoyi.system.domain.MeterEnergyTrendPoint;
import com.ruoyi.system.domain.MeterHistoryTrend;
import com.ruoyi.system.domain.MeterQualityRiskStats;
import com.ruoyi.system.domain.MeterTopologyLayout;

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

    /** 把电表改挂到另一个配电箱（同时把站点跟随到箱体所属站点）。见 MeterCardMapper.updateMeterBox。 */
    public int updateMeterBox(Long meterId, Long boxId);

    /**
     * 拓扑保存前校验箱体改动能不能落库（meter 表的两条唯一键）。
     * 必须由调用方在拓扑事务之外调用：拓扑表在 MASTER 库、meter 表在 SLAVE 库，
     * 而 MASTER 事务一旦开启连接就被钉住，事务里再换数据源是无效的。见 MeterCardController.saveTopology。
     */
    public void validateTopologyBoxChanges(MeterTopologyLayout layout);

    /** 按布局写回每台表的箱体归属（含站点跟随）。同样必须在拓扑事务之外调用。 */
    public int applyTopologyBoxChanges(MeterTopologyLayout layout);

    /** 目标配电箱里是否已有别的表占用该从站地址。见 MeterCardMapper.countBoxSlaveConflict。 */
    public int countBoxSlaveConflict(Long meterId, Long boxId, Integer slaveAddress);

    /** 目标箱体所属站点里是否已有别的表占用该设备编号。见 MeterCardMapper.countSiteMeterCodeConflict。 */
    public int countSiteMeterCodeConflict(Long meterId, Long boxId, String meterCode);

    public List<MeterEnergyTrendPoint> getEnergyTrend(String range);

    public MeterHistoryTrend getHistoryTrend(String category, MeterCard query);

    public MeterEnergyAnalysis getEnergyAnalysis(MeterCard query);
}
