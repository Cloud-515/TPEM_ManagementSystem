package com.ruoyi.system.mapper;

import java.util.List;
import com.ruoyi.system.domain.MeterTopologyDevice;
import com.ruoyi.system.domain.MeterTopologyRegion;

public interface MeterTopologyMapper
{
    List<MeterTopologyRegion> selectRegions();
    List<MeterTopologyDevice> selectDevices();
    int deleteRegions();
    int deleteDevices();
    int insertRegions(List<MeterTopologyRegion> regions);
    int insertDevices(List<MeterTopologyDevice> devices);
}
