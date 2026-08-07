package com.ruoyi.system.domain;

import java.util.ArrayList;
import java.util.List;

public class MeterTopologyLayout
{
    private List<MeterTopologyRegion> regions = new ArrayList<>();
    private List<MeterCard> unassignedMeters = new ArrayList<>();

    public List<MeterTopologyRegion> getRegions() { return regions; }
    public void setRegions(List<MeterTopologyRegion> regions) { this.regions = regions; }
    public List<MeterCard> getUnassignedMeters() { return unassignedMeters; }
    public void setUnassignedMeters(List<MeterCard> unassignedMeters) { this.unassignedMeters = unassignedMeters; }
}
