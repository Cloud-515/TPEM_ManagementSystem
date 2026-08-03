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
        ClientSize = new Size(410, 360);

        var groupNameLabel = new Label { Text = "分组名称", AutoSize = true, Location = new Point(16, 18) };
        _groupNameTextBox = new TextBox { Location = new Point(88, 14), Width = 300 };
        var modulesLabel = new Label { Text = "模块成员", AutoSize = true, Location = new Point(16, 54) };
        _modulesList = new CheckedListBox
        {
            Location = new Point(16, 78),
            Size = new Size(372, 220),
            CheckOnClick = true,
            DisplayMember = nameof(ModuleItem.DisplayName)
        };
        var saveButton = new Button { Text = "保存", DialogResult = DialogResult.None, Location = new Point(232, 318), Width = 75 };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(313, 318), Width = 75 };
        saveButton.Click += SaveButton_Click;

        Controls.AddRange(new Control[] { groupNameLabel, _groupNameTextBox, modulesLabel, _modulesList, saveButton, cancelButton });
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
