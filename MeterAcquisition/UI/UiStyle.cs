using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MeterAcquisition
{
    /// <summary>
    /// 界面规范（UI-15 ~ UI-20）：字体、行高、按钮尺寸、控件间距只在这里定义一次，各页面不再各写一套。
    ///
    /// 为什么需要它：审计探针（<c>artifacts/UiAudit</c>）在 1400×900 下量到 38 个按钮有 **17 种尺寸**、
    /// 8 种高度（29/33/38/41/42/45/50/52），两种字体，4 条工具条换行把标签和输入框拆到两行，
    /// 15 处同一行控件垂直中线差 4~11px。根因是三类重复实现：
    ///   1. 每个 Designer 自己写 <c>MinimumSize</c>/<c>Padding</c>（92×26、82×26、76×32、88×30、Width=75…）；
    ///   2. 按钮 <c>AutoSize</c> 后宽度随文字长短变化，同一行里 2 字和 4 字按钮差 40px；
    ///   3. "标签 + 输入框"有四份各自写死 <c>Margin.Top</c> 的假垂直居中实现。
    ///
    /// 规范本身：
    ///   · 一行的标准高度 <see cref="RowHeight"/> = 32，按钮、字段组统一这个高度，同行必然对齐；
    ///   · 按钮最小宽 <see cref="ButtonMinWidth"/> = 100（够放 4 个汉字），文字更长才增长；
    ///   · 同一页内按钮宽度取齐（<see cref="EqualizeWidths"/>），超过 <see cref="MaxEqualizedWidth"/> 的不参与，
    ///     免得一个"电表：原有电表"把整页按钮都撑成 150px；
    ///   · 按钮永不参与拉伸（<c>Dock=Fill</c> 会把按钮拉成 1324×38，见 UI-16）；
    ///   · 卡片内的按钮走 <see cref="ApplyCompact"/> 紧凑档（76×26），只有两档，不是 17 档。
    /// </summary>
    internal static class UiStyle
    {
        /// <summary>GB2312 字符集，和现有 Designer 里 <c>GraphicsUnit.Point, 134</c> 的写法保持一致。</summary>
        private const byte GbkCharSet = 134;

        /// <summary>正文字体。取现状里占比最高的一种（微软雅黑 9.75），避免宋体/雅黑混排（UI-10）。</summary>
        public static readonly Font BodyFont =
            new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, GbkCharSet);

        /// <summary>分区标题（"DO1 控制""告警事件"这类）。</summary>
        public static readonly Font SectionFont =
            new Font("微软雅黑", 9.75F, FontStyle.Bold, GraphicsUnit.Point, GbkCharSet);

        /// <summary>卡片标题。</summary>
        public static readonly Font CardTitleFont =
            new Font("微软雅黑", 10.5F, FontStyle.Bold, GraphicsUnit.Point, GbkCharSet);

        /// <summary>汇总卡片的大号数值。原来写的是 <c>Segoe UI</c>，中文全靠字体回退（UI-10）。</summary>
        public static readonly Font MetricFont =
            new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, GbkCharSet);

        /// <summary>卡片里的紧凑按钮字体。</summary>
        public static readonly Font CompactFont =
            new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, GbkCharSet);

        /// <summary>
        /// 工具条一行的标准高度：按钮、字段组都是这个高，同行控件的垂直中线自然重合。
        /// 34 = 微软雅黑 9.75 的按钮首选高度（33）再留 1px，保证按钮放进
        /// <see cref="Cell"/> 的固定行里不会被挤掉一像素。
        /// </summary>
        public const int RowHeight = 34;

        /// <summary>标准按钮最小宽度：够放 4 个汉字，绝大多数按钮因此正好等宽。</summary>
        public const int ButtonMinWidth = 100;

        /// <summary>参与"同页取齐"的宽度上限。超过它的按钮（文字特别长）保持自身宽度，不拖着整页变宽。</summary>
        public const int MaxEqualizedWidth = 140;

        /// <summary>紧凑档（卡片内）行高：微软雅黑 9pt 按钮的首选高度（实测 32）。</summary>
        public const int CompactRowHeight = 32;

        /// <summary>紧凑档最小宽度。</summary>
        public const int CompactButtonMinWidth = 76;

        /// <summary>同行相邻控件的横向间距。</summary>
        public const int Gap = 8;

        /// <summary>行与行之间的纵向间距（工具条换行时生效）。</summary>
        public const int RowGap = 4;

        /// <summary>标签与它的输入框之间的间距。</summary>
        public const int LabelGap = 6;

        /// <summary>工具条自身的内边距。</summary>
        public const int ToolbarPad = 8;

        /// <summary>工具条内每个条目的统一外边距：右侧留间距、上下留换行间距。</summary>
        public static Padding ItemMargin => new Padding(0, RowGap, Gap, RowGap);

        /// <summary>按钮内边距：只给左右，纵向留给 <see cref="RowHeight"/> 决定（原来给了 6~8 的纵向内边距，高度被顶到 45~52）。</summary>
        public static Padding ButtonPadding => new Padding(Gap, 0, Gap, 0);

        /// <summary>被标记为紧凑档的按钮。<see cref="ApplyToTree"/> 会跳过它们，不把卡片里的小按钮撑成 100×32。</summary>
        private static readonly ConditionalWeakTable<Button, object> CompactButtons =
            new ConditionalWeakTable<Button, object>();

        /// <summary>由 <see cref="Cell"/> 建出来的条目容器。它内部的间距已经排好，<see cref="Apply"/> 不再覆盖其子控件的外边距。</summary>
        private static readonly ConditionalWeakTable<Control, object> StyledCells =
            new ConditionalWeakTable<Control, object>();

        /// <summary>按标准档规范一个按钮。</summary>
        public static TButton Apply<TButton>(TButton button) where TButton : Button
        {
            if (button == null)
            {
                return null;
            }

            button.Font = BodyFont;
            button.AutoSize = true;
            // 必须是 GrowAndShrink：GrowOnly 只允许在**当前 Size 基础上变大**，
            // 设计器里写过 Size = (78,45) 的按钮会一直保持 45px 高，规范怎么设都不生效
            // （实测就是这样：Padding/Margin 都改了，尺寸却还是 78×45）。
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(ButtonMinWidth, RowHeight);
            button.Padding = ButtonPadding;
            if (!IsInStyledCell(button))
            {
                // 条目容器内部的间距由 Cell 排好，这里不要再盖回通用外边距，
                // 否则按钮多出 4px 上下边距，会顶出固定行高的条目（实测溢出 4px）。
                button.Margin = ItemMargin;
            }
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = true;
            NoStretch(button);
            return button;
        }

        /// <summary>按紧凑档规范一个按钮（卡片、列表行内）。外边距交给调用方，卡片布局各有各的贴边要求。</summary>
        public static TButton ApplyCompact<TButton>(TButton button) where TButton : Button
        {
            if (button == null)
            {
                return null;
            }

            button.Font = CompactFont;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(CompactButtonMinWidth, CompactRowHeight);
            button.Padding = new Padding(LabelGap, 0, LabelGap, 0);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = true;
            CompactButtons.Remove(button);
            CompactButtons.Add(button, null);
            return button;
        }

        /// <summary>新建一个标准按钮。</summary>
        public static Button CreateButton(string text, EventHandler onClick = null)
        {
            var button = Apply(new Button { Text = text });
            if (onClick != null)
            {
                button.Click += onClick;
            }

            return button;
        }

        /// <summary>新建一个紧凑按钮。</summary>
        public static Button CreateCompactButton(string text, EventHandler onClick = null)
        {
            var button = ApplyCompact(new Button { Text = text });
            if (onClick != null)
            {
                button.Click += onClick;
            }

            return button;
        }

        /// <summary>
        /// 让控件不再被容器拉伸：去掉会横向拉满的停靠，只保留一个水平锚点。
        /// 表格单元格里只锚左（或只锚右）的控件会**垂直居中**，这是同行对齐最省事的写法；
        /// 左右都锚 = 被拉满整格宽（"读取设备信息"因此变成 1324×38，UI-16）。
        /// <c>Dock=Top/Bottom</c> 保留不动 —— 那种按钮是靠停靠参与父容器分层的，
        /// 直接去掉会让它掉回 Location 处被别的控件盖住（实测按钮整个消失）。
        /// 需要收窄这类按钮时用 <see cref="WrapInToolbar"/> 给它一个宿主工具条。
        /// </summary>
        private static void NoStretch(Control control)
        {
            // 注意：给控件赋 Anchor 会把 Dock 清成 None（两者互斥）。
            // 所以 Top/Bottom 停靠的控件在这里直接放过 —— 它们靠停靠参与父容器分层，
            // 一旦被清成 None 就掉回 Location 处被别的控件盖住（实测"读取设备信息"整个消失）。
            // 需要把这类按钮收窄时用 WrapInToolbar 给它一个宿主工具条。
            if (control.Dock == DockStyle.Top || control.Dock == DockStyle.Bottom)
            {
                return;
            }

            control.Dock = DockStyle.None;
            var horizontal = control.Anchor & (AnchorStyles.Left | AnchorStyles.Right);
            if (horizontal == (AnchorStyles.Left | AnchorStyles.Right) || horizontal == AnchorStyles.None)
            {
                horizontal = AnchorStyles.Left;
            }

            control.Anchor = horizontal;
        }

        /// <summary>
        /// 把一个原本靠 <c>Dock=Top/Bottom</c> 独占一行的控件塞进同样停靠的标准工具条里，
        /// 位置（父容器里的索引）保持不变。这样按钮回到标准尺寸，又不会从布局里消失。
        /// </summary>
        public static FlowLayoutPanel WrapInToolbar(Control content)
        {
            var parent = content.Parent;
            if (parent == null)
            {
                return null;
            }

            var index = parent.Controls.GetChildIndex(content);
            var dock = content.Dock == DockStyle.Bottom ? DockStyle.Bottom : DockStyle.Top;
            var bar = new FlowLayoutPanel { Dock = dock };
            content.Dock = DockStyle.None;
            RebuildToolbar(bar, content);
            parent.Controls.Add(bar);
            parent.Controls.SetChildIndex(bar, index);
            return bar;
        }

        /// <summary>是否已被标记为紧凑档。</summary>
        public static bool IsCompact(Button button) => CompactButtons.TryGetValue(button, out _);

        /// <summary>控件是否位于 <see cref="Cell"/> 建出来的条目容器内。</summary>
        private static bool IsInStyledCell(Control control) =>
            control.Parent != null && StyledCells.TryGetValue(control.Parent, out _);

        /// <summary>把 <paramref name="root"/> 下所有未标记紧凑档的按钮按标准档规范一遍。</summary>
        public static void ApplyToTree(Control root)
        {
            foreach (var button in Descendants<Button>(root))
            {
                if (!IsCompact(button))
                {
                    Apply(button);
                }
            }
        }

        /// <summary>
        /// 把一个范围（通常是一个页签）内的标准按钮宽度取齐到其中最宽的那个，
        /// 让"查询"和"刷新电表"这类同页按钮不再一个 78 一个 112。
        /// 宽度超过 <see cref="MaxEqualizedWidth"/> 的按钮不参与，保持自身宽度。
        /// </summary>
        public static void EqualizeWidths(Control scope)
        {
            var buttons = Descendants<Button>(scope).Where(b => !IsCompact(b)).ToList();
            if (buttons.Count < 2)
            {
                return;
            }

            // 先量一遍再统一写，避免边改边量。
            var measured = buttons.Select(b => new { Button = b, Width = b.PreferredSize.Width }).ToList();
            var candidates = measured.Where(m => m.Width <= MaxEqualizedWidth).ToList();
            if (candidates.Count < 2)
            {
                return;
            }

            var target = Math.Max(ButtonMinWidth, candidates.Max(m => m.Width));
            foreach (var item in candidates)
            {
                item.Button.MinimumSize = new Size(target, RowHeight);
            }
        }

        /// <summary>
        /// 把若干控件打包成一个**高度恒为 <see cref="RowHeight"/> 的条目**：
        /// 内部是 1 行 N 列的表格，每个控件只锚左 → 由表格负责垂直居中，
        /// 不再靠手写 <c>Margin = new Padding(0, 8, 4, 0)</c> 凑（那种写法换字体/换 DPI 立刻歪，UI-17）。
        /// 打包后的条目在工具条里是一个整体，换行只会在条目之间断开，
        /// 不会再出现"开始时间"留在上一行、日期框跑到下一行的情况（UI-18）。
        /// </summary>
        public static TableLayoutPanel Cell(params Control[] contents)
        {
            var cell = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = contents.Length,
                RowCount = 1,
                Margin = ItemMargin,
                Padding = Padding.Empty,
            };
            cell.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
            StyledCells.Add(cell, null);

            for (var i = 0; i < contents.Length; i++)
            {
                cell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                var content = contents[i];
                content.Dock = DockStyle.None;
                content.Anchor = AnchorStyles.Left;
                content.Margin = i == contents.Length - 1
                    ? Padding.Empty
                    : new Padding(0, 0, LabelGap, 0);
                cell.Controls.Add(content, i, 0);
            }

            return cell;
        }

        /// <summary>"标签 + 输入框"字段组。</summary>
        public static TableLayoutPanel Field(Label caption, Control input)
        {
            caption.AutoSize = true;
            caption.Font = BodyFont;
            caption.TextAlign = ContentAlignment.MiddleLeft;
            return Cell(caption, input);
        }

        /// <summary>"标签 + 输入框"字段组（新建标签）。</summary>
        public static TableLayoutPanel Field(string caption, Control input) =>
            Field(new Label { Text = caption }, input);

        /// <summary>
        /// 用统一规格重建一条工具条：条目顺序不变，间距/内边距/换行策略统一。
        /// 传进来的条目应当是按钮或 <see cref="Cell"/>/<see cref="Field"/> 打包过的组，
        /// 这样每个条目高度都是 <see cref="RowHeight"/>，同行控件自然对齐。
        /// </summary>
        public static void RebuildToolbar(FlowLayoutPanel bar, params Control[] items)
        {
            bar.SuspendLayout();
            bar.FlowDirection = FlowDirection.LeftToRight;
            bar.WrapContents = true;
            bar.AutoSize = true;
            bar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            bar.Padding = new Padding(ToolbarPad, RowGap, ToolbarPad, RowGap);
            bar.Controls.Clear();
            foreach (var item in items)
            {
                item.Margin = ItemMargin;
                bar.Controls.Add(item);
            }

            bar.ResumeLayout(true);
        }

        /// <summary>新建一条标准工具条（默认停靠在容器顶部）。</summary>
        public static FlowLayoutPanel CreateToolbar(params Control[] items)
        {
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top };
            RebuildToolbar(bar, items);
            return bar;
        }

        /// <summary>
        /// 对话框用的两列表单：左列标签按内容自适应，右列输入框占满剩余宽度。
        /// 原来这几个对话框是绝对坐标（标签 x=16、输入框 x=90 写死），
        /// 换个字体标签就压在输入框上（实测"控制器站号"和文本框重叠 8px）。
        /// </summary>
        public static TableLayoutPanel CreateFormGrid()
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(ToolbarPad + 4, ToolbarPad, ToolbarPad + 4, ToolbarPad),
                GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return grid;
        }

        /// <summary>往 <see cref="CreateFormGrid"/> 建出来的表单追加一行。</summary>
        public static void AddFormRow(TableLayoutPanel grid, string caption, Control input, bool stretch = true)
        {
            var row = grid.RowStyles.Count;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight + RowGap));
            grid.RowCount = grid.RowStyles.Count;
            grid.Controls.Add(
                new Label
                {
                    Text = caption,
                    AutoSize = true,
                    Font = BodyFont,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(0, 0, LabelGap + Gap, 0),
                },
                0,
                row);
            input.Font = BodyFont;
            input.Margin = Padding.Empty;
            input.Anchor = stretch ? AnchorStyles.Left | AnchorStyles.Right : AnchorStyles.Left;
            grid.Controls.Add(input, 1, row);
        }

        /// <summary>对话框底部的按钮行：右对齐、统一尺寸与间距。按传入顺序从左到右显示。</summary>
        public static FlowLayoutPanel DialogButtonRow(params Button[] buttons)
        {
            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(ToolbarPad, RowGap, ToolbarPad + 4, ToolbarPad),
            };
            for (var i = buttons.Length - 1; i >= 0; i--)
            {
                Apply(buttons[i]);
                buttons[i].Margin = new Padding(Gap, 0, 0, 0);
                row.Controls.Add(buttons[i]);
            }

            return row;
        }

        /// <summary>深度优先枚举后代控件（含自身的直接子控件）。</summary>
        public static IEnumerable<T> Descendants<T>(Control root) where T : Control
        {
            foreach (Control child in root.Controls)
            {
                if (child is T typed)
                {
                    yield return typed;
                }

                foreach (var nested in Descendants<T>(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
