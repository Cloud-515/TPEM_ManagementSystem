using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MeterAcquisition;
using Xunit;

namespace Tpem.Tests
{
    /// <summary>
    /// 界面规范的回归护栏（UI-15 ~ UI-20）。
    /// 这些断言对应的是"按钮大小错乱不一、同行控件对不齐"这类问题的可判定条件：
    /// 只要有人再往某个 Designer 里塞一份自己的按钮尺寸，或者把字段组拆回"标签 + 输入框"
    /// 两个独立控件，这里就会红。
    /// </summary>
    public class UiStyleTests
    {
        /// <summary>界面上真实用到的按钮文案（4 字及以内的那批，应当完全等宽）。</summary>
        private static readonly string[] ShortCaptions =
        {
            "连接", "断开", "查询", "设温", "扫描", "刷新", "上一页", "下一页", "清故障",
            "自动扫描", "新增电表", "读取参数", "写入参数", "刷新电表", "刷新目录", "开始对比",
            "查询趋势", "模块对比", "切换开关", "写入模式", "写入风速", "风机诊断", "探测地址",
        };

        [Fact]
        public void 标准按钮高度只有一个值()
        {
            var heights = ShortCaptions
                .Select(caption => UiStyle.CreateButton(caption).PreferredSize.Height)
                .Distinct()
                .ToList();

            Assert.Single(heights);
            Assert.Equal(UiStyle.RowHeight, heights[0]);
        }

        [Fact]
        public void 四字以内的按钮全部等宽()
        {
            var widths = ShortCaptions
                .Select(caption => UiStyle.CreateButton(caption).PreferredSize.Width)
                .Distinct()
                .ToList();

            Assert.Single(widths);
            Assert.Equal(UiStyle.ButtonMinWidth, widths[0]);
        }

        [Fact]
        public void 文案超长的按钮才允许变宽()
        {
            var longButton = UiStyle.CreateButton("电表：AMC96L-E4 多功能电表");

            Assert.True(longButton.PreferredSize.Width > UiStyle.ButtonMinWidth);
            // 变宽不变高：高度是全局统一的那一个值
            Assert.Equal(UiStyle.RowHeight, longButton.PreferredSize.Height);
        }

        [Fact]
        public void 按钮不参与拉伸()
        {
            var button = UiStyle.Apply(new Button
            {
                Text = "读取设备信息",
                Dock = DockStyle.Fill,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            });

            Assert.Equal(DockStyle.None, button.Dock);
            // 只锚一侧 = 单元格内垂直居中且不被拉伸
            Assert.Equal(AnchorStyles.Left, button.Anchor);
        }

        [Fact]
        public void 同页按钮宽度取齐且忽略超宽的那个()
        {
            var host = new Panel();
            var search = UiStyle.CreateButton("查询");
            var refresh = UiStyle.CreateButton("刷新电表");
            var scan = UiStyle.CreateButton("扫描线控器");
            var mode = UiStyle.CreateButton("电表：AMC96L-E4 多功能电表");
            host.Controls.AddRange(new Control[] { search, refresh, scan, mode });

            var wideBefore = mode.PreferredSize.Width;
            UiStyle.EqualizeWidths(host);

            Assert.Equal(scan.PreferredSize.Width, search.PreferredSize.Width);
            Assert.Equal(scan.PreferredSize.Width, refresh.PreferredSize.Width);
            Assert.True(search.PreferredSize.Width > UiStyle.ButtonMinWidth, "取齐后应当等于组内最宽的那个");
            Assert.Equal(wideBefore, mode.PreferredSize.Width);
        }

        [Fact]
        public void 紧凑档只有一档尺寸()
        {
            var buttons = new[] { "配置", "删除", "编辑分组", "配置控制器" }
                .Select(caption => UiStyle.CreateCompactButton(caption))
                .ToList();

            Assert.All(buttons, button => Assert.True(UiStyle.IsCompact(button)));
            Assert.Single(buttons.Select(button => button.PreferredSize.Height).Distinct());
            Assert.Equal(UiStyle.CompactRowHeight, buttons[0].PreferredSize.Height);
        }

        [Fact]
        public void 紧凑按钮不会被整树规范化覆盖()
        {
            var host = new Panel();
            var compact = UiStyle.CreateCompactButton("配置");
            var standard = new Button { Text = "查询" };
            host.Controls.AddRange(new Control[] { compact, standard });

            UiStyle.ApplyToTree(host);

            Assert.Equal(new Size(UiStyle.CompactButtonMinWidth, UiStyle.CompactRowHeight), compact.MinimumSize);
            Assert.Equal(new Size(UiStyle.ButtonMinWidth, UiStyle.RowHeight), standard.MinimumSize);
        }

        [Fact]
        public void 字段组高度与按钮一致且标签输入框不可分离()
        {
            var input = new ComboBox { Width = 160, Font = UiStyle.BodyFont };
            var field = UiStyle.Field("开始时间", input);
            var button = UiStyle.CreateButton("查询");

            // 工具条里每个条目高度相同 —— 这是同一行控件中线自然重合的前提
            Assert.Equal(UiStyle.RowHeight, field.PreferredSize.Height);
            Assert.Equal(field.PreferredSize.Height, button.PreferredSize.Height);

            // 标签和输入框在同一个容器里，FlowLayoutPanel 换行只能在条目之间断开
            Assert.Equal(2, field.Controls.Count);
            Assert.Same(field, input.Parent);
            Assert.All(field.Controls.Cast<Control>(), c => Assert.Equal(AnchorStyles.Left, c.Anchor));
        }

        [Fact]
        public void 工具条重排后条目间距统一()
        {
            var bar = new FlowLayoutPanel();
            var first = UiStyle.CreateButton("连接");
            var second = UiStyle.Cell(new Label { Text = "未连接", AutoSize = true, Font = UiStyle.BodyFont });
            UiStyle.RebuildToolbar(bar, first, second);

            Assert.True(bar.WrapContents);
            Assert.Equal(2, bar.Controls.Count);
            Assert.All(bar.Controls.Cast<Control>(), c => Assert.Equal(UiStyle.ItemMargin, c.Margin));
        }

        [Fact]
        public void 全项目只用一种正文字体()
        {
            var fonts = new[] { UiStyle.BodyFont, UiStyle.SectionFont, UiStyle.CardTitleFont, UiStyle.MetricFont, UiStyle.CompactFont }
                .Select(font => font.Name)
                .Distinct()
                .ToList();

            Assert.Single(fonts);
            Assert.Equal("微软雅黑", fonts[0]);
        }
    }
}
