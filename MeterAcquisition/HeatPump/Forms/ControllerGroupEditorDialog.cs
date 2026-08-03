using System.ComponentModel;
using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Forms;

public sealed class ControllerGroupEditorDialog : Form
{
    private readonly WiredControllerInfo _controller;
    private readonly TextBox _txtGroupName;
    private readonly NumericUpDown _nudSlaveId;
    private readonly CheckBox _chkControllerLock;
    private readonly NumericUpDown _nudDifferential;
    private readonly DataGridView _gridModules;
    private readonly BindingList<HeatPumpModuleInfo> _modules;

    public ControllerGroupEditorDialog(WiredControllerInfo controller)
    {
        _controller = controller;
        _modules = new BindingList<HeatPumpModuleInfo>(controller.Modules.OrderBy(module => module.DisplayOrder).ThenBy(module => module.ModuleIndex).ToList());

        Text = "线控器与模块列表编辑";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        MinimumSize = new Size(860, 560);
        ClientSize = new Size(860, 560);
        Font = new Font("微软雅黑", 9F);

        var lblGroup = new Label
        {
            Text = "组别名称",
            AutoSize = true,
            Location = new Point(18, 22),
        };
        Controls.Add(lblGroup);

        _txtGroupName = new TextBox
        {
            Location = new Point(88, 18),
            Width = 180,
            Text = controller.Name,
        };
        Controls.Add(_txtGroupName);

        var lblSlaveId = new Label
        {
            Text = "SlaveId",
            AutoSize = true,
            Location = new Point(286, 22),
        };
        Controls.Add(lblSlaveId);

        _nudSlaveId = new NumericUpDown
        {
            Location = new Point(344, 18),
            Width = 64,
            Minimum = 0,
            Maximum = 15,
            Value = controller.SlaveId,
        };
        Controls.Add(_nudSlaveId);

        _chkControllerLock = new CheckBox
        {
            Text = "上位机锁定",
            AutoSize = true,
            Location = new Point(430, 20),
            Checked = controller.ControllerLockEnabled,
        };
        Controls.Add(_chkControllerLock);

        var lblDiff = new Label
        {
            Text = "回差",
            AutoSize = true,
            Location = new Point(560, 22),
        };
        Controls.Add(lblDiff);

        _nudDifferential = new NumericUpDown
        {
            Location = new Point(598, 18),
            Width = 58,
            Minimum = 2,
            Maximum = 5,
            Value = Math.Clamp(controller.Differential, 2, 5),
        };
        Controls.Add(_nudDifferential);

        var btnAddModule = new Button
        {
            Text = "新增模块",
            Location = new Point(22, 58),
            Size = new Size(92, 32),
        };
        btnAddModule.Click += (_, _) => AddModule();
        Controls.Add(btnAddModule);

        var btnRemoveModule = new Button
        {
            Text = "删除模块",
            Location = new Point(126, 58),
            Size = new Size(92, 32),
        };
        btnRemoveModule.Click += (_, _) => RemoveSelectedModule();
        Controls.Add(btnRemoveModule);

        var btnMoveUp = new Button
        {
            Text = "上移",
            Location = new Point(230, 58),
            Size = new Size(70, 32),
        };
        btnMoveUp.Click += (_, _) => MoveSelectedModule(-1);
        Controls.Add(btnMoveUp);

        var btnMoveDown = new Button
        {
            Text = "下移",
            Location = new Point(310, 58),
            Size = new Size(70, 32),
        };
        btnMoveDown.Click += (_, _) => MoveSelectedModule(1);
        Controls.Add(btnMoveDown);

        _gridModules = new DataGridView
        {
            Location = new Point(22, 100),
            Size = new Size(814, 350),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
        };
        _gridModules.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "模块编号",
            DataPropertyName = nameof(HeatPumpModuleInfo.ModuleIndex),
            Width = 90,
            ReadOnly = true,
        });
        _gridModules.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "设备名称",
            DataPropertyName = nameof(HeatPumpModuleInfo.Name),
            Width = 360,
        });
        _gridModules.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "启用",
            DataPropertyName = nameof(HeatPumpModuleInfo.IsEnabled),
            Width = 80,
        });
        _gridModules.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "显示顺序",
            DataPropertyName = nameof(HeatPumpModuleInfo.DisplayOrder),
            Width = 90,
        });
        _gridModules.DataSource = new BindingSource { DataSource = _modules };
        Controls.Add(_gridModules);

        var btnOk = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            Location = new Point(680, 476),
            Width = 72,
        };
        btnOk.Click += (_, _) => Save();
        Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(764, 476),
            Width = 72,
        };
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    public byte SlaveId => (byte)_nudSlaveId.Value;

    public bool ControllerLockEnabled => _chkControllerLock.Checked;

    public int Differential => (int)_nudDifferential.Value;

    private void AddModule()
    {
        var nextIndex = Enumerable.Range(0, 16)
            .Select(index => (byte)index)
            .FirstOrDefault(index => _modules.All(module => module.ModuleIndex != index));

        if (_modules.Any(module => module.ModuleIndex == nextIndex))
        {
            MessageBox.Show(this, "模块编号 0-15 已全部占用，无法继续新增。", "新增模块", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _modules.Add(new HeatPumpModuleInfo
        {
            Id = Guid.NewGuid().ToString("N"),
            SlaveId = _controller.SlaveId,
            ModuleIndex = nextIndex,
            Name = nextIndex == 0 ? "0#总机" : $"模块 {nextIndex}#",
            IsEnabled = true,
            DisplayOrder = _modules.Count,
        });
    }

    private void RemoveSelectedModule()
    {
        if (_gridModules.CurrentRow?.DataBoundItem is not HeatPumpModuleInfo module)
        {
            return;
        }

        var confirm = MessageBox.Show(this, $"确认删除模块 {module.ModuleIndex}# ({module.Name})?", "删除模块", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (confirm == DialogResult.OK)
        {
            _modules.Remove(module);
            NormalizeDisplayOrder();
        }
    }

    private void MoveSelectedModule(int direction)
    {
        if (_gridModules.CurrentRow?.DataBoundItem is not HeatPumpModuleInfo module)
        {
            return;
        }

        var index = _modules.IndexOf(module);
        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= _modules.Count)
        {
            return;
        }

        _modules.RemoveAt(index);
        _modules.Insert(targetIndex, module);
        NormalizeDisplayOrder();
        _gridModules.ClearSelection();
        _gridModules.Rows[targetIndex].Selected = true;
        _gridModules.CurrentCell = _gridModules.Rows[targetIndex].Cells[0];
    }

    private void NormalizeDisplayOrder()
    {
        for (var index = 0; index < _modules.Count; index++)
        {
            _modules[index].DisplayOrder = index;
        }

        _gridModules.Refresh();
    }

    private void Save()
    {
        var name = _txtGroupName.Text.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            _controller.Name = name;
        }

        _controller.SlaveId = (byte)_nudSlaveId.Value;
        _controller.ControllerLockEnabled = _chkControllerLock.Checked;
        _controller.Differential = (int)_nudDifferential.Value;

        NormalizeDisplayOrder();
        foreach (var module in _modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                module.Name = module.ModuleIndex == 0 ? "0#总机" : $"模块 {module.ModuleIndex}#";
            }

            module.SlaveId = _controller.SlaveId;
        }

        _controller.Modules.Clear();
        _controller.Modules.AddRange(_modules.OrderBy(module => module.DisplayOrder));
    }
}
