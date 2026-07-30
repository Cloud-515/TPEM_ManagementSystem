package com.ruoyi.web.controller.system;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import com.ruoyi.common.core.controller.BaseController;
import com.ruoyi.common.core.domain.AjaxResult;
import com.ruoyi.common.core.page.TableDataInfo;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.service.IMeterCardService;

@RestController
@RequestMapping("/system/meter")
public class MeterCardController extends BaseController
{
    @Autowired
    private IMeterCardService meterCardService;

    @GetMapping("/cards")
    public AjaxResult getCards()
    {
        return AjaxResult.success(meterCardService.getMeterCardGroups());
    }

    @GetMapping("/dashboard/energy-trend")
    public AjaxResult energyTrend(@RequestParam(defaultValue = "24h") String range)
    {
        return success(meterCardService.getEnergyTrend(range));
    }

    @GetMapping("/realtime/page")
    public TableDataInfo realtimePage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listRealtimeMeters(query));
    }

    @GetMapping("/energy/page")
    public TableDataInfo energyPage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listEnergyMeters(query));
    }

    @GetMapping("/quality/page")
    public TableDataInfo qualityPage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listQualityMeters(query));
    }

    @GetMapping("/history/{category}/trend")
    public AjaxResult historyTrend(@PathVariable String category, MeterCard query)
    {
        return success(meterCardService.getHistoryTrend(category, query));
    }

    @GetMapping("/history/realtime/page")
    public TableDataInfo realtimeHistoryPage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listRealtimeHistory(query));
    }

    @GetMapping("/history/energy/page")
    public TableDataInfo energyHistoryPage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listEnergyHistory(query));
    }

    @GetMapping("/history/quality/page")
    public TableDataInfo qualityHistoryPage(MeterCard query)
    {
        startPage();
        return getDataTable(meterCardService.listQualityHistory(query));
    }

    @GetMapping("/realtime/detail/{meterId}")
    public AjaxResult realtimeDetail(@PathVariable Long meterId)
    {
        return success(meterCardService.getRealtimeDetail(meterId));
    }
}
