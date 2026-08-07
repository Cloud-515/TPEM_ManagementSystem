package com.ruoyi.system.service.impl;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.ruoyi.common.annotation.DataSource;
import com.ruoyi.common.enums.DataSourceType;
import com.ruoyi.common.exception.ServiceException;
import com.ruoyi.common.utils.StringUtils;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterTopologyDevice;
import com.ruoyi.system.domain.MeterTopologyLayout;
import com.ruoyi.system.domain.MeterTopologyRegion;
import com.ruoyi.system.mapper.MeterTopologyMapper;
import com.ruoyi.system.service.IMeterCardService;
import com.ruoyi.system.service.IMeterTopologyService;

@Service
public class MeterTopologyServiceImpl implements IMeterTopologyService
{
    @Autowired
    private MeterTopologyMapper topologyMapper;

    @Autowired
    private IMeterCardService meterCardService;

    @Override
    @DataSource(DataSourceType.MASTER)
    public MeterTopologyLayout selectTopology()
    {
        List<MeterTopologyRegion> regions = topologyMapper.selectRegions();
        List<MeterTopologyDevice> assignments = topologyMapper.selectDevices();
        List<MeterCard> meters = meterCardService.listRealtimeMeters(new MeterCard());
        Map<Long, MeterCard> meterMap = new HashMap<Long, MeterCard>();
        for (MeterCard meter : meters)
        {
            meterMap.put(meter.getMeterId(), meter);
        }
        Map<Long, MeterTopologyRegion> regionMap = new HashMap<Long, MeterTopologyRegion>();
        for (MeterTopologyRegion region : regions)
        {
            region.setMeters(new ArrayList<MeterCard>());
            regionMap.put(region.getRegionId(), region);
        }
        Set<Long> assignedMeterIds = new HashSet<Long>();
        for (MeterTopologyDevice assignment : assignments)
        {
            MeterTopologyRegion region = regionMap.get(assignment.getRegionId());
            MeterCard meter = meterMap.get(assignment.getMeterId());
            if (region != null && meter != null)
            {
                region.getMeters().add(meter);
                assignedMeterIds.add(meter.getMeterId());
            }
        }
        List<MeterCard> unassignedMeters = new ArrayList<MeterCard>();
        for (MeterCard meter : meters)
        {
            if (!assignedMeterIds.contains(meter.getMeterId()))
            {
                unassignedMeters.add(meter);
            }
        }
        MeterTopologyLayout layout = new MeterTopologyLayout();
        layout.setRegions(regions);
        layout.setUnassignedMeters(unassignedMeters);
        return layout;
    }

    @Override
    @Transactional
    @DataSource(DataSourceType.MASTER)
    public int saveTopology(MeterTopologyLayout layout)
    {
        List<MeterTopologyRegion> regions = layout == null ? null : layout.getRegions();
        if (regions == null)
        {
            throw new ServiceException("拓扑区域不能为空");
        }
        Set<Long> regionIds = new HashSet<Long>();
        Set<Long> meterIds = new HashSet<Long>();
        List<MeterTopologyDevice> devices = new ArrayList<MeterTopologyDevice>();
        long nextRegionId = System.currentTimeMillis();
        for (int i = 0; i < regions.size(); i++)
        {
            MeterTopologyRegion region = regions.get(i);
            if (StringUtils.isEmpty(region.getRegionName()))
            {
                throw new ServiceException("区域名称不能为空");
            }
            region.setRegionName(region.getRegionName().trim());
            if (region.getRegionName().length() > 64)
            {
                throw new ServiceException("区域名称不能超过64个字符");
            }
            if (region.getRegionId() == null || region.getRegionId() <= 0)
            {
                region.setRegionId(nextRegionId++);
            }
            if (!regionIds.add(region.getRegionId()))
            {
                throw new ServiceException("区域标识重复");
            }
            region.setSortOrder(i + 1);
            List<MeterCard> meters = region.getMeters();
            if (meters == null)
            {
                continue;
            }
            for (int j = 0; j < meters.size(); j++)
            {
                MeterCard meter = meters.get(j);
                if (meter == null || meter.getMeterId() == null || !meterIds.add(meter.getMeterId()))
                {
                    throw new ServiceException("电表只能归属一个区域");
                }
                MeterTopologyDevice device = new MeterTopologyDevice();
                device.setRegionId(region.getRegionId());
                device.setMeterId(meter.getMeterId());
                device.setSortOrder(j + 1);
                devices.add(device);
            }
        }
        topologyMapper.deleteDevices();
        topologyMapper.deleteRegions();
        if (!regions.isEmpty())
        {
            topologyMapper.insertRegions(regions);
        }
        if (!devices.isEmpty())
        {
            topologyMapper.insertDevices(devices);
        }
        return 1;
    }
}
