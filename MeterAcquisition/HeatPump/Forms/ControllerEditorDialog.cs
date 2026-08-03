using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class ControllerEditorDialog : Form
{
    private readonly NumericUpDown _nudSlaveId;
    private readonly TextBox _txtName;

    public ControllerEditorDialog(byte? initialSlaveId = null, string? initialName = null)
    {
        Text = initialSlaveId.HasValue ? "修改线控器组别" : "新增线控器组别";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 180);
        Font = new Font("微软雅黑", 10.5F);

        var lblSlaveId = new Label
        {
            Text = "线控器地址",
            AutoSize = true,
            Location = new Point(24, 26),
        };
        Controls.Add(lblSlaveId);

        _nudSlaveId = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 15,
            Value = initialSlaveId ?? 1,
            Location = new Point(128, 22),
            Size = new Size(90, 30),
        };
        Controls.Add(_nudSlaveId);

        var lblName = new Label
        {
            Text = "组别名称",
            AutoSize = true,
            Location = new Point(24, 74),
        };
        Controls.Add(lblName);

        _txtName = new TextBox
        {
            Text = initialName ?? string.Empty,
            Location = new Point(128, 70),
            Size = new Size(190, 30),
        };
        Controls.Add(_txtName);

        var btnOk = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(128, 120),
            Size = new Size(88, 34),
        };
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show(this, "请填写组别名称。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(230, 120),
            Size = new Size(88, 34),
        };
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    public byte SlaveId => (byte)_nudSlaveId.Value;

    public string GroupName => _txtName.Text.Trim();
}
