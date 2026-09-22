using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class HeatPumpModuleGroupEditorDialog : Form
{
    private readonly WiredControllerInfo _controller;
    private readonly TextBox _groupNameTextBox;
    private readonly CheckedListBox _modulesList;

    public HeatPumpModuleGroupEditorDialog(WiredControllerInfo controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        Text = "模块分组 - " + controller.Name;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = UiStyle.BodyFont;
        ClientSize = new Size(430, 380);

        _groupNameTextBox = new TextBox();
        _modulesList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            DisplayMember = nameof(ModuleItem.DisplayName),
            Font = UiStyle.BodyFont,
        };

        // 原来全是绝对坐标（标签 x=16、输入框 x=88、列表 372×220、按钮 y=318），
        // 字体一变标签就压输入框、列表也不会跟着窗口走。改成两列表单 + 底部按钮行。
        var grid = UiStyle.CreateFormGrid();
        grid.AutoSize = false; // 列表行用百分比高度，容器不能再 AutoSize
        UiStyle.AddFormRow(grid, "分组名称", _groupNameTextBox);
        var listRow = grid.RowStyles.Count;
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        grid.RowCount = grid.RowStyles.Count;
        grid.Controls.Add(
            new Label
            {
                Text = "模块成员",
                AutoSize = true,
                Font = UiStyle.BodyFont,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, UiStyle.RowGap, UiStyle.LabelGap + UiStyle.Gap, 0),
            },
            0,
            listRow);
        grid.Controls.Add(_modulesList, 1, listRow);

        var saveButton = new Button { Text = "保存", DialogResult = DialogResult.None };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        saveButton.Click += SaveButton_Click;

        Controls.Add(grid);
        Controls.Add(UiStyle.DialogButtonRow(saveButton, cancelButton));
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        LoadModules();
    }

    private void LoadModules()
    {
        var groups = _controller.Modules
            .Where(module => !string.IsNullOrWhiteSpace(module.GroupName))
            .Select(module => module.GroupName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(groupName => groupName, StringComparer.Ordinal)
            .ToList();
        if (groups.Count > 0)
        {
            _groupNameTextBox.Text = groups[0];
        }

        foreach (var module in _controller.Modules.OrderBy(module => module.DisplayOrder).ThenBy(module => module.ModuleIndex))
        {
            var item = new ModuleItem(module);
            var isInSelectedGroup = !string.IsNullOrWhiteSpace(_groupNameTextBox.Text) &&
                string.Equals(module.GroupName, _groupNameTextBox.Text, StringComparison.Ordinal);
            _modulesList.Items.Add(item, isInSelectedGroup);
        }
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        var groupName = _groupNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            MessageBox.Show(this, "请输入分组名称。", "模块分组", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedModules = _modulesList.CheckedItems.Cast<ModuleItem>().Select(item => item.Module).ToHashSet();
        if (selectedModules.Count == 0)
        {
            MessageBox.Show(this, "请至少选择一个模块。", "模块分组", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (var module in _controller.Modules)
        {
            if (selectedModules.Contains(module))
            {
                module.GroupName = groupName;
            }
            else if (string.Equals(module.GroupName, groupName, StringComparison.Ordinal))
            {
                module.GroupName = string.Empty;
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed class ModuleItem
    {
        public ModuleItem(HeatPumpModuleInfo module)
        {
            Module = module;
            DisplayName = string.Format("模块 {0}: {1}", module.ModuleIndex + 1, module.Name);
        }

        public HeatPumpModuleInfo Module { get; }

        public string DisplayName { get; }
    }
}
