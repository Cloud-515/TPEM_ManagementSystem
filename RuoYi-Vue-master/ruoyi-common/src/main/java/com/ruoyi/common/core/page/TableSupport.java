package com.ruoyi.common.core.page;

import com.ruoyi.common.core.text.Convert;
import com.ruoyi.common.utils.ServletUtils;

/**
 * 表格数据处理
 * 
 * @author ruoyi
 */
public class TableSupport
{
    /**
     * 当前记录起始索引
     */
    public static final String PAGE_NUM = "pageNum";

    /**
     * 每页显示记录数
     */
    public static final String PAGE_SIZE = "pageSize";

    /**
     * 排序列
     */
    public static final String ORDER_BY_COLUMN = "orderByColumn";

    /**
     * 排序的方向 "desc" 或者 "asc".
     */
    public static final String IS_ASC = "isAsc";

    /**
     * 分页参数合理化
     */
    public static final String REASONABLE = "reasonable";

    /**
     * 单页条数上限（W-10）。
     * pageSize 直接来自请求参数，原本既没有上限也没有下限：
     * ?pageSize=1000000 可以用一个请求把整张表拉进 JVM，pageNum/pageSize 为 0 或负数时
     * 还会让 PageHelper 产生意料外的行为。这里统一夹取，分页接口一律受此约束。
     */
    private static final int MAX_PAGE_SIZE = 500;

    /**
     * 封装分页对象
     */
    public static PageDomain getPageDomain()
    {
        PageDomain pageDomain = new PageDomain();
        pageDomain.setPageNum(Math.max(Convert.toInt(ServletUtils.getParameter(PAGE_NUM), 1), 1));
        pageDomain.setPageSize(
            Math.min(Math.max(Convert.toInt(ServletUtils.getParameter(PAGE_SIZE), 10), 1), MAX_PAGE_SIZE));
        pageDomain.setOrderByColumn(ServletUtils.getParameter(ORDER_BY_COLUMN));
        pageDomain.setIsAsc(ServletUtils.getParameter(IS_ASC));
        pageDomain.setReasonable(ServletUtils.getParameterToBool(REASONABLE));
        return pageDomain;
    }

    public static PageDomain buildPageRequest()
    {
        return getPageDomain();
    }
}
