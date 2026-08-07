package com.ruoyi.system.service;

import com.ruoyi.system.domain.MeterTopologyLayout;

public interface IMeterTopologyService
{
    MeterTopologyLayout selectTopology();
    int saveTopology(MeterTopologyLayout layout);
}
