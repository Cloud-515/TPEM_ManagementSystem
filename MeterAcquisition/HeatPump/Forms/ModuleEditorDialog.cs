using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class ModuleEditorDialog : Form
{
    private readonly TextBox _txtModuleName;

    public ModuleEditorDialog(HeatPumpModuleInfo module)
    {
        Module = module;
        Text = "编辑设备配置";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 160);
        Font = new Font("微软雅黑", 9F);

        var lblName = new Label
        {
            Text = "设备名称",
            AutoSize = true,
            Location = new Point(24, 26),
        };
        Controls.Add(lblName);

        _txtModuleName = new TextBox
        {
            Location = new Point(100, 22),
            Width = 240,
            Text = module.Name,
        };
        Controls.Add(_txtModuleName);

        var lblInfo = new Label
        {
            Text = $"线控器 {module.SlaveId}# / 模块 {module.ModuleIndex}#",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(24, 62),
        };
        Controls.Add(lblInfo);

        var btnOk = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            Location = new Point(184, 108),
            Width = 72,
        };
        btnOk.Click += (_, _) => Save();
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(268, 108),
            Width = 72,
        };
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    public HeatPumpModuleInfo Module { get; }

    private void Save()
    {
        var name = _txtModuleName.Text.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            Module.Name = name;
        }
    }
}
