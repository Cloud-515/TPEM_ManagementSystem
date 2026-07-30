package com.ruoyi.system.service.impl;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import com.ruoyi.common.annotation.DataSource;
import com.ruoyi.common.enums.DataSourceType;
import com.ruoyi.system.domain.MeterEnergyReading;
import com.ruoyi.system.domain.MeterHistoryTrend;
import com.ruoyi.system.domain.MeterEnergyTrendPoint;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.mapper.MeterCardMapper;
import com.ruoyi.system.service.IMeterCardService;

@Service
@DataSource(DataSourceType.SLAVE)
public class MeterCardServiceImpl implements IMeterCardService
{
    private static final long NO_DATA_TIMEOUT_MILLIS = 5 * 60 * 1000L;
    private static final long FUTURE_TIME_TOLERANCE_MILLIS = 60 * 1000L;
    private static final String RANGE_7D = "7d";
    private static final String RANGE_30D = "30d";

    @Autowired
    private MeterCardMapper meterCardMapper;

    @Override
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
    public List<MeterCard> listRealtimeMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectRealtimeMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    public List<MeterCard> listEnergyMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectEnergyMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    public List<MeterCard> listQualityMeters(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectQualityMeters(query);
        enrichCards(list);
        return list;
    }

    @Override
    public List<MeterCard> listRealtimeHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectRealtimeHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    public List<MeterCard> listEnergyHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectEnergyHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    public List<MeterCard> listQualityHistory(MeterCard query)
    {
        List<MeterCard> list = meterCardMapper.selectQualityHistory(query);
        enrichCards(list);
        return list;
    }

    @Override
    public MeterCard getRealtimeDetail(Long meterId)
    {
        MeterCard card = meterCardMapper.selectRealtimeDetail(meterId);
        if (card != null)
        {
            enrichCard(card);
        }
        return card;
    }

    @Override
    public MeterHistoryTrend getHistoryTrend(String category, MeterCard query)
    {
        validateHistoryTrendQuery(query);
        List<MeterCard> points;
        if ("energy".equals(category))
        {
            points = meterCardMapper.selectEnergyHistoryTrend(query);
        }
        else
        {
            category = "realtime";
            points = meterCardMapper.selectRealtimeHistoryTrend(query);
        }
        MeterHistoryTrend trend = new MeterHistoryTrend();
        trend.setCategory(category);
        trend.setPoints(points);
        return trend;
    }

    private void validateHistoryTrendQuery(MeterCard query)
    {
        if (query == null || query.getMeterId() == null || query.getBeginTime() == null || query.getEndTime() == null)
        {
            throw new IllegalArgumentException("设备和时间范围不能为空");
        }
        if (query.getBeginTime().after(query.getEndTime()))
        {
            throw new IllegalArgumentException("开始时间不能晚于结束时间");
        }
        if (query.getEndTime().after(new Date(System.currentTimeMillis() + FUTURE_TIME_TOLERANCE_MILLIS)))
        {
            throw new IllegalArgumentException("结束时间不能晚于当前时间");
        }
    }

    @Override
    public List<MeterEnergyTrendPoint> getEnergyTrend(String range)
    {
        TrendRange trendRange = resolveTrendRange(range);
        List<MeterEnergyTrendPoint> points = createTrendPoints(trendRange);
        List<MeterEnergyReading> readings = meterCardMapper.selectEnergyReadings(trendRange.beginTime, trendRange.endTime);
        Map<Long, Map<Integer, MeterEnergyReading>> firstReadings = new LinkedHashMap<>();
        Map<Long, Map<Integer, MeterEnergyReading>> lastReadings = new LinkedHashMap<>();

        for (MeterEnergyReading reading : readings)
        {
            if (reading == null)
            {
                continue;
            }
            Integer bucketIndex = getBucketIndex(reading.getCollectTime(), trendRange);
            if (bucketIndex == null)
            {
                continue;
            }
            firstReadings.computeIfAbsent(reading.getMeterId(), key -> new LinkedHashMap<>()).putIfAbsent(bucketIndex, reading);
            lastReadings.computeIfAbsent(reading.getMeterId(), key -> new LinkedHashMap<>()).put(bucketIndex, reading);
        }

        for (Map.Entry<Long, Map<Integer, MeterEnergyReading>> meterEntry : firstReadings.entrySet())
        {
            Map<Integer, MeterEnergyReading> firstByBucket = meterEntry.getValue();
            Map<Integer, MeterEnergyReading> lastByBucket = lastReadings.get(meterEntry.getKey());
            for (Map.Entry<Integer, MeterEnergyReading> bucketEntry : firstByBucket.entrySet())
            {
                MeterEnergyReading first = bucketEntry.getValue();
                MeterEnergyReading last = lastByBucket.get(bucketEntry.getKey());
                if (first == last || first.getForwardActiveEnergy() == null || last.getForwardActiveEnergy() == null)
                {
                    continue;
                }
                BigDecimal consumption = last.getForwardActiveEnergy().subtract(first.getForwardActiveEnergy());
                if (consumption.signum() < 0)
                {
                    continue;
                }
                MeterEnergyTrendPoint point = points.get(bucketEntry.getKey());
                BigDecimal total = point.getConsumptionKwh() == null ? BigDecimal.ZERO : point.getConsumptionKwh();
                point.setConsumptionKwh(total.add(consumption));
                point.setMeterCount(point.getMeterCount() + 1);
            }
        }

        for (MeterEnergyTrendPoint point : points)
        {
            if (point.getMeterCount() > 0)
            {
                point.setConsumptionKwh(point.getConsumptionKwh().setScale(2, RoundingMode.HALF_UP));
            }
        }
        return points;
    }

    private TrendRange resolveTrendRange(String range)
    {
        Calendar calendar = Calendar.getInstance();
        Date endTime = calendar.getTime();
        if (RANGE_7D.equals(range) || RANGE_30D.equals(range))
        {
            int bucketCount = RANGE_7D.equals(range) ? 7 : 30;
            calendar.set(Calendar.HOUR_OF_DAY, 0);
            calendar.set(Calendar.MINUTE, 0);
            calendar.set(Calendar.SECOND, 0);
            calendar.set(Calendar.MILLISECOND, 0);
            calendar.add(Calendar.DAY_OF_YEAR, -(bucketCount - 1));
            return new TrendRange(calendar.getTime(), endTime, bucketCount, Calendar.DAY_OF_YEAR, "MM-dd");
        }
        calendar.set(Calendar.MINUTE, 0);
        calendar.set(Calendar.SECOND, 0);
        calendar.set(Calendar.MILLISECOND, 0);
        calendar.add(Calendar.HOUR_OF_DAY, -23);
        return new TrendRange(calendar.getTime(), endTime, 24, Calendar.HOUR_OF_DAY, "HH:00");
    }

    private List<MeterEnergyTrendPoint> createTrendPoints(TrendRange trendRange)
    {
        List<MeterEnergyTrendPoint> points = new ArrayList<>();
        Calendar calendar = Calendar.getInstance();
        calendar.setTime(trendRange.beginTime);
        SimpleDateFormat labelFormat = new SimpleDateFormat(trendRange.labelPattern, Locale.getDefault());
        for (int index = 0; index < trendRange.bucketCount; index++)
        {
            MeterEnergyTrendPoint point = new MeterEnergyTrendPoint();
            point.setLabel(labelFormat.format(calendar.getTime()));
            point.setMeterCount(0);
            points.add(point);
            calendar.add(trendRange.calendarField, 1);
        }
        return points;
    }

    private Integer getBucketIndex(Date collectTime, TrendRange trendRange)
    {
        if (collectTime == null || collectTime.before(trendRange.beginTime) || collectTime.after(trendRange.endTime))
        {
            return null;
        }
        Calendar bucketStart = Calendar.getInstance();
        bucketStart.setTime(trendRange.beginTime);
        for (int index = 0; index < trendRange.bucketCount; index++)
        {
            Calendar bucketEnd = (Calendar) bucketStart.clone();
            bucketEnd.add(trendRange.calendarField, 1);
            if (!collectTime.before(bucketStart.getTime()) && collectTime.before(bucketEnd.getTime()))
            {
                return index;
            }
            bucketStart = bucketEnd;
        }
        return null;
    }

    private static class TrendRange
    {
        private final Date beginTime;
        private final Date endTime;
        private final int bucketCount;
        private final int calendarField;
        private final String labelPattern;

        private TrendRange(Date beginTime, Date endTime, int bucketCount, int calendarField, String labelPattern)
        {
            this.beginTime = beginTime;
            this.endTime = endTime;
            this.bucketCount = bucketCount;
            this.calendarField = calendarField;
            this.labelPattern = labelPattern;
        }
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
