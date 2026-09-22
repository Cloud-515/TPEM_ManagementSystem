package com.ruoyi.web.controller.system;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import com.ruoyi.common.annotation.Log;
import com.ruoyi.common.core.controller.BaseController;
import com.ruoyi.common.core.domain.AjaxResult;
import com.ruoyi.common.core.page.TableDataInfo;
import com.ruoyi.common.enums.BusinessType;
import com.ruoyi.common.utils.poi.ExcelUtil;
import com.ruoyi.system.domain.MeterCard;
import com.ruoyi.system.domain.MeterEnergyAnalysis;
import com.ruoyi.system.domain.MeterTopologyLayout;
import com.ruoyi.system.domain.MeterQualityRiskStats;
import com.ruoyi.system.service.IMeterCardService;
import com.ruoyi.system.service.IMeterTopologyService;

@RestController
@RequestMapping("/system/meter")
public class MeterCardController extends BaseController
{
    /** 单页上限（W-10）：pageSize 由请求参数直接决定，不封顶时可以用一个请求把整张表拉进内存。 */
    private static final int MAX_PAGE_SIZE = 200;

    @Autowired
    private IMeterCardService meterCardService;

    @Autowired
    private IMeterTopologyService meterTopologyService;

    @GetMapping("/cards")
    public AjaxResult getCards()
    {
        return AjaxResult.success(meterCardService.getMeterCardGroups());
    }

    @PreAuthorize("@ss.hasPermi('meter:topology:list')")
    @GetMapping("/topology")
    public AjaxResult topology()
    {
        return success(meterTopologyService.selectTopology());
    }

    @PreAuthorize("@ss.hasPermi('meter:topology:edit')")
    @Log(title = "设备拓扑图", businessType = BusinessType.UPDATE)
    @PutMapping("/topology")
    public AjaxResult saveTopology(@RequestBody MeterTopologyLayout layout)
    {
        return toAjax(meterTopologyService.saveTopology(layout));
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
    public TableDataInfo qualityPage(MeterCard query,
        @RequestParam(defaultValue = "1") int pageNum,
        @RequestParam(defaultValue = "20") int pageSize)
    {
        java.util.List<MeterCard> list = meterCardService.listQualityMeters(query);
        pageNum = Math.max(pageNum, 1);
        pageSize = Math.min(Math.max(pageSize, 1), MAX_PAGE_SIZE);

        // 用 long 计算下标：原来 (pageNum - 1) * pageSize 是 int 乘法，
        // ?pageNum=1000000&pageSize=1000000 会溢出成负数，subList(负数, …) 抛 IndexOutOfBoundsException，
        // 请求方拿到的就是一个 500（并且经全局异常处理器把内部信息带出去）。
        long fromIndex = Math.min((long)(pageNum - 1) * pageSize, list.size());
        long toIndex = Math.min(fromIndex + pageSize, list.size());

        TableDataInfo result = new TableDataInfo();
        result.setCode(200);
        result.setMsg("查询成功");
        result.setRows(list.subList((int) fromIndex, (int) toIndex));
        result.setTotal(list.size());
        return result;
    }

    @GetMapping("/quality/stats")
    public AjaxResult qualityStats(MeterCard query)
    {
        query.setRiskOnly(false);
        MeterQualityRiskStats stats = meterCardService.getQualityRiskStats(query);
        return success(stats);
    }

    @GetMapping("/history/{category}/trend")
    public AjaxResult historyTrend(@PathVariable String category, MeterCard query)
    {
        return success(meterCardService.getHistoryTrend(category, query));
    }

    @GetMapping("/energy/analysis")
    public AjaxResult energyAnalysis(MeterCard query)
    {
        MeterEnergyAnalysis analysis = meterCardService.getEnergyAnalysis(query);
        return success(analysis);
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
