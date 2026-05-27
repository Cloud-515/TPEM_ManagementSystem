package com.ruoyi.system.service.impl;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import com.ruoyi.common.annotation.DataSource;
import com.ruoyi.common.enums.DataSourceType;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.mapper.MeterCardMapper;
import com.ruoyi.system.service.IMeterCardService;

@Service
public class MeterCardServiceImpl implements IMeterCardService
{
    private static final long NO_DATA_TIMEOUT_MILLIS = 5 * 60 * 1000L;
    private static final long FUTURE_TIME_TOLERANCE_MILLIS = 60 * 1000L;

    @Autowired
    private MeterCardMapper meterCardMapper;

    @Override
    @DataSource(DataSourceType.SLAVE)
    public Map<String, Object> getMeterCardGroups()
    {
        List<MeterCard> cards = meterCardMapper.selectMeterCards();
        List<MeterCard> toolbar = new ArrayList<>();
        List<MeterCard> dashboard = new ArrayList<>();

        for (MeterCard card : cards)
        {
            enrichCard(card);
            if (card.getIsToolbar())
            {
                toolbar.add(card);
            }
            else
            {
                dashboard.add(card);
            }
        }

        Map<String, Object> result = new LinkedHashMap<>();
        result.put("toolbar", toolbar);
        result.put("dashboard", dashboard);
        result.put("toolbarTitle", "外部串口（真实串口）");
        result.put("dashboardTitle", "仪表盘扫描串口");
        result.put("generatedAt", new Date());
        return result;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listRealtimeMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectRealtimeMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listEnergyMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectEnergyMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listQualityMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectQualityMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listRealtimeHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectRealtimeHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listEnergyHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectEnergyHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public List<MeterCard> listQualityHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectQualityHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    @DataSource(DataSourceType.SLAVE)
    public MeterCard getRealtimeDetail(Long meterId)
    {
        MeterCard card = meterCardMapper.selectRealtimeDetail(meterId);
        if (card != null)
        {
            enrichCard(card);
        }
        return card;
    }

    private void enrichCards(List<MeterCard> list)
    {
        for (MeterCard card : list)
        {
            enrichCard(card);
        }
    }

    private void enrichCard(MeterCard card)
    {
        card.setActivePowerKw(toKilowatts(card.getActivePowerTotal()));
        card.setMaxCurrentA(maxCurrent(card.getCurrentA(), card.getCurrentB(), card.getCurrentC()));
        card.setIsToolbar(resolveToolbar(card));
        applyStatus(card);
    }

    private BigDecimal toKilowatts(Float value)
    {
        if (value == null)
        {
            return null;
        }
        return BigDecimal.valueOf(value)
            .divide(BigDecimal.valueOf(1000), 2, RoundingMode.HALF_UP);
    }

    private BigDecimal maxCurrent(Float currentA, Float currentB, Float currentC)
    {
        Float max = null;
        for (Float current : new Float[] { currentA, currentB, currentC })
        {
            if (current == null)
            {
                continue;
            }
            float absolute = Math.abs(current);
            if (max == null || absolute > max)
            {
                max = absolute;
            }
        }

        if (max == null)
        {
            return null;
        }
        return BigDecimal.valueOf(max).setScale(2, RoundingMode.HALF_UP);
    }

    private boolean resolveToolbar(MeterCard card)
    {
        if (card.getIsToolbar())
        {
            return true;
        }
        if (card.getBoxCode() != null)
        {
            return "BOX-MAIN".equalsIgnoreCase(card.getBoxCode()) || card.getBoxCode().toUpperCase().contains("MAIN");
        }
        if (card.getBoxName() != null)
        {
            return card.getBoxName().contains("外部串口") || card.getBoxName().contains("真实串口");
        }
        return false;
    }

    private void applyStatus(MeterCard card)
    {
        long now = System.currentTimeMillis();
        Date collectTime = card.getLastCollectTime();
        Date publishTime = card.getLastPublishTime();

        if (collectTime == null)
        {
            card.setStatusCode("WAITING");
            card.setStatusText("等待数据");
            return;
        }

        if (isFault(card))
        {
            card.setStatusCode("FAULT");
            card.setStatusText("通信异常");
            return;
        }

        if (isOffline(card, now, collectTime, publishTime))
        {
            card.setStatusCode("NODATA");
            card.setStatusText("设备掉线");
            return;
        }

        if (isSevereAnomaly(card))
        {
            card.setStatusCode("FAULT");
            card.setStatusText("异常");
            return;
        }

        if (isVoltageAbnormal(card))
        {
            card.setStatusCode("VOLTAGE_BAD");
            card.setStatusText("电压异常");
            return;
        }

        if (card.getPowerFactorTotal() != null && card.getPowerFactorTotal() < 0.5f)
        {
            card.setStatusCode("PF_LOW");
            card.setStatusText("功率因数低");
            return;
        }

        card.setStatusCode("OK");
        card.setStatusText("运行正常");
    }

    private boolean isOffline(MeterCard card, long now, Date collectTime, Date publishTime)
    {
        if (collectTime.getTime() - now > FUTURE_TIME_TOLERANCE_MILLIS)
        {
            return true;
        }

        if (card.getIsOnline() != null && card.getIsOnline().intValue() == 0)
        {
            return true;
        }

        if (card.getMeterStatusCode() != null
            && !"online".equalsIgnoreCase(card.getMeterStatusCode())
            && !"comm_error".equalsIgnoreCase(card.getMeterStatusCode()))
        {
            return true;
        }

        if (publishTime == null)
        {
            return true;
        }

        return now - publishTime.getTime() > NO_DATA_TIMEOUT_MILLIS
            || now - collectTime.getTime() > NO_DATA_TIMEOUT_MILLIS;
    }

    private boolean isFault(MeterCard card)
    {
        return "comm_error".equalsIgnoreCase(card.getMeterStatusCode());
    }

    private boolean isSevereAnomaly(MeterCard card)
    {
        if (card.getFrequency() != null && (card.getFrequency() < 40f || card.getFrequency() > 70f))
        {
            return true;
        }

        return isLow(card.getVoltageA()) && isLow(card.getVoltageB()) && isLow(card.getVoltageC());
    }

    private boolean isVoltageAbnormal(MeterCard card)
    {
        return isOutOfRange(card.getVoltageA(), 180f, 260f)
            || isOutOfRange(card.getVoltageB(), 180f, 260f)
            || isOutOfRange(card.getVoltageC(), 180f, 260f);
    }

    private boolean isLow(Float value)
    {
        return value != null && value < 50f;
    }

    private boolean isOutOfRange(Float value, float min, float max)
    {
        return value != null && (value < min || value > max);
    }
}
