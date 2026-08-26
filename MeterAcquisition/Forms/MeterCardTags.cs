using System;
using System.Windows.Forms;

namespace MeterAcquisition
{
    /// <summary>
    /// 电表卡片上需要每秒刷新的三个控件引用（P2-6）。
    ///
    /// 改造前这里是一个匿名类型塞进 <see cref="Control.Tag"/>，再用 `dynamic` 取回：
    ///   - 没有编译期检查：改字段名不会报错，只会在运行时抛 RuntimeBinderException；
    ///   - 每次成员访问都要经过 DLR 绑定，而刷新路径是每秒 × 每张卡片；
    ///   - 别人读代码时无法知道 Tag 里到底有什么。
    /// 换成显式类型后，这三点同时解决。
    /// </summary>
    internal sealed class MeterCardTags
    {
        public MeterCardTags(Label powerLabel, Label currentLabel, Label statusLabel)
        {
            if (powerLabel == null)
            {
                throw new ArgumentNullException(nameof(powerLabel));
            }

            if (currentLabel == null)
            {
                throw new ArgumentNullException(nameof(currentLabel));
            }

            if (statusLabel == null)
            {
                throw new ArgumentNullException(nameof(statusLabel));
            }

            PowerLabel = powerLabel;
            CurrentLabel = currentLabel;
            StatusLabel = statusLabel;
        }

        public Label PowerLabel { get; private set; }

        public Label CurrentLabel { get; private set; }

        public Label StatusLabel { get; private set; }
    }
}
