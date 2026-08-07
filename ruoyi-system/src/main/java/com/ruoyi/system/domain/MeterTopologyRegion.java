package com.ruoyi.system.domain;

import java.util.ArrayList;
import java.util.List;

public class MeterTopologyRegion
{
    private Long regionId;
    private String regionName;
    private Integer sortOrder;
    private List<MeterCard> meters = new ArrayList<>();

    public Long getRegionId() { return regionId; }
    public void setRegionId(Long regionId) { this.regionId = regionId; }
    public String getRegionName() { return regionName; }
    public void setRegionName(String regionName) { this.regionName = regionName; }
    public Integer getSortOrder() { return sortOrder; }
    public void setSortOrder(Integer sortOrder) { this.sortOrder = sortOrder; }
    public List<MeterCard> getMeters() { return meters; }
    public void setMeters(List<MeterCard> meters) { this.meters = meters; }
}
