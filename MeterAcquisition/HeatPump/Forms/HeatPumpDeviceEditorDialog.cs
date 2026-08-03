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
        ClientSize = new Size(410, module == null ? 180 : 265);

        var top = 16;
        Controls.Add(CreateReadOnlyField("控制器站号", controller.SlaveId.ToString(), top));
        top += 34;
        if (module != null)
        {
            Controls.Add(CreateReadOnlyField("模块编号", (module.ModuleIndex + 1).ToString(), top));
            top += 34;
        }

        Controls.Add(new Label { Text = "显示名称", AutoSize = true, Location = new Point(16, top + 4) });
        _nameTextBox = new TextBox { Location = new Point(90, top), Width = 288, Text = module == null ? controller.Name : module.Name };
        Controls.Add(_nameTextBox);
        top += 40;

        if (module != null)
        {
            _enabledCheckBox = new CheckBox { Text = "启用模块", AutoSize = true, Location = new Point(90, top), Checked = module.IsEnabled };
            Controls.Add(_enabledCheckBox);
            top += 30;

            Controls.Add(new Label { Text = "显示排序", AutoSize = true, Location = new Point(16, top + 4) });
            _displayOrderInput = new NumericUpDown
            {
                Location = new Point(90, top),
                Width = 100,
                Minimum = 0,
                Maximum = 10000,
                Value = Math.Max(0, module.DisplayOrder)
            };
            Controls.Add(_displayOrderInput);
            top += 40;
        }

        var saveButton = new Button { Text = "保存", Location = new Point(222, ClientSize.Height - 38), Width = 75 };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(303, ClientSize.Height - 38), Width = 75 };
        saveButton.Click += SaveButton_Click;
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static Control CreateReadOnlyField(string label, string value, int top)
    {
        var field = new TextBox { Location = new Point(90, 0), Width = 288, ReadOnly = true, Text = value, TabStop = false };
        var labelControl = new Label { Text = label, AutoSize = true, Location = new Point(16, 4) };
        var panel = new Panel { Location = new Point(0, top), Size = new Size(410, 28) };
        panel.Controls.Add(labelControl);
        panel.Controls.Add(field);
        return panel;
    }

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
