package com.ruoyi.system.domain;

import java.util.List;

/**
 * 单设备历史趋势响应。
 */
public class MeterHistoryTrend
{
    private String category;

    private List<MeterCard> points;

    public String getCategory()
    {
        return category;
    }

    public void setCategory(String category)
    {
        this.category = category;
    }

    public List<MeterCard> getPoints()
    {
        return points;
    }

    public void setPoints(List<MeterCard> points)
    {
        this.points = points;
    }
}
