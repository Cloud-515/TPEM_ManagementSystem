using System;
using System.Drawing;
using System.Windows.Forms;
using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class HeatPumpDeviceEditorDialog : Form
{
    private readonly WiredControllerInfo _controller;
    private readonly HeatPumpModuleInfo _module;
    private readonly TextBox _nameTextBox;
    private readonly CheckBox _enabledCheckBox;
    private readonly NumericUpDown _displayOrderInput;

    public HeatPumpDeviceEditorDialog(WiredControllerInfo controller, HeatPumpModuleInfo module = null)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _module = module;

        Text = module == null ? "控制器配置" : "模块配置";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = UiStyle.BodyFont;
        ClientSize = new Size(430, module == null ? 170 : 250);

        // 原来是绝对坐标：标签 x=16、输入框 x=90 写死，
        // "控制器站号"这种 5 字标签在雅黑 9.75 下宽 135px，直接压到文本框上（实测重叠 8px）。
        // 改成两列表单，左列宽度由标签内容决定。
        var grid = UiStyle.CreateFormGrid();
        UiStyle.AddFormRow(grid, "控制器站号", CreateReadOnlyBox(controller.SlaveId.ToString()));
        if (module != null)
        {
            UiStyle.AddFormRow(grid, "模块编号", CreateReadOnlyBox((module.ModuleIndex + 1).ToString()));
        }

        _nameTextBox = new TextBox { Text = module == null ? controller.Name : module.Name };
        UiStyle.AddFormRow(grid, "显示名称", _nameTextBox);

        if (module != null)
        {
            _enabledCheckBox = new CheckBox { Text = "启用模块", AutoSize = true, Checked = module.IsEnabled };
            UiStyle.AddFormRow(grid, string.Empty, _enabledCheckBox, false);

            _displayOrderInput = new NumericUpDown
            {
                Width = 100,
                Minimum = 0,
                Maximum = 10000,
                Value = Math.Max(0, module.DisplayOrder)
            };
            UiStyle.AddFormRow(grid, "显示排序", _displayOrderInput, false);
        }

        var saveButton = new Button { Text = "保存" };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        saveButton.Click += SaveButton_Click;
        Controls.Add(grid);
        Controls.Add(UiStyle.DialogButtonRow(saveButton, cancelButton));
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static TextBox CreateReadOnlyBox(string value) =>
        new TextBox { ReadOnly = true, Text = value, TabStop = false };

    private void SaveButton_Click(object sender, EventArgs e)
    {
        var name = _nameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "显示名称不能为空。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_module == null)
        {
            _controller.Name = name;
        }
        else
        {
            _module.Name = name;
            _module.IsEnabled = _enabledCheckBox.Checked;
            _module.DisplayOrder = decimal.ToInt32(_displayOrderInput.Value);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
