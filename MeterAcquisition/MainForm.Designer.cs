using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MeterAcquisition
{
    partial class MainForm
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabDashboard = new System.Windows.Forms.TabPage();
            this.flpDashboard = new System.Windows.Forms.FlowLayoutPanel();
            this.tabDeviceInfo = new System.Windows.Forms.TabPage();
            this.panelDeviceInfo = new System.Windows.Forms.Panel();
            this.gbInfo = new System.Windows.Forms.GroupBox();
            this.lblDeviceType = new System.Windows.Forms.Label();
            this.dgvDeviceInfo = new System.Windows.Forms.DataGridView();
            this.colDeviceInfoItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDeviceInfoValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnReadInfo = new System.Windows.Forms.Button();
            this.tabQuality = new System.Windows.Forms.TabPage();
            this.dgvQuality = new System.Windows.Forms.DataGridView();
            this.colQualityParameter = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQualityValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQualityUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabEnergy = new System.Windows.Forms.TabPage();
            this.dgvEnergy = new System.Windows.Forms.DataGridView();
            this.colEnergyParameter = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnergyValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEnergyUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabRealTime = new System.Windows.Forms.TabPage();
            this.dgvRealTime = new System.Windows.Forms.DataGridView();
            this.colRealTimeParameter = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRealTimeValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRealTimeUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabParams = new System.Windows.Forms.TabPage();
            this.panelParams = new System.Windows.Forms.TableLayoutPanel();
            this.lblCommTitle = new System.Windows.Forms.Label();
            this.lblEmpty1 = new System.Windows.Forms.Label();
            this.lblParamAddr = new System.Windows.Forms.Label();
            this.txtParamAddr = new System.Windows.Forms.TextBox();
            this.lblParamBaud = new System.Windows.Forms.Label();
            this.cmbParamBaud = new System.Windows.Forms.ComboBox();
            this.lblParamParity = new System.Windows.Forms.Label();
            this.cmbParamParity = new System.Windows.Forms.ComboBox();
            this.lblBasicTitle = new System.Windows.Forms.Label();
            this.lblEmpty2 = new System.Windows.Forms.Label();
            this.lblWiring = new System.Windows.Forms.Label();
            this.cmbWiring = new System.Windows.Forms.ComboBox();
            this.lblPT = new System.Windows.Forms.Label();
            this.txtPT = new System.Windows.Forms.TextBox();
            this.lblCT = new System.Windows.Forms.Label();
            this.txtCT = new System.Windows.Forms.TextBox();
            this.btnPanelParams = new System.Windows.Forms.FlowLayoutPanel();
            this.btnReadParams = new System.Windows.Forms.Button();
            this.btnWriteParams = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabPage();
            this.panelControl = new System.Windows.Forms.TableLayoutPanel();
            this.gbDO1 = new System.Windows.Forms.GroupBox();
            this.btnDO1On = new System.Windows.Forms.Button();
            this.btnDO1Off = new System.Windows.Forms.Button();
            this.gbDO2 = new System.Windows.Forms.GroupBox();
            this.btnDO2On = new System.Windows.Forms.Button();
            this.btnDO2Off = new System.Windows.Forms.Button();
            this.gbOperation = new System.Windows.Forms.GroupBox();
            this.btnClearEnergy = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnDiscover = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.txtAddr = new System.Windows.Forms.TextBox();
            this.lblAddr = new System.Windows.Forms.Label();
            this.cmbBaud = new System.Windows.Forms.ComboBox();
            this.lblBaud = new System.Windows.Forms.Label();
            this.cmbPort = new System.Windows.Forms.ComboBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.tslTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.clockTimer = new System.Windows.Forms.Timer(this.components);
            this.tlpTop = new System.Windows.Forms.TableLayoutPanel();
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.tabDashboard.SuspendLayout();
            this.tabDeviceInfo.SuspendLayout();
            this.panelDeviceInfo.SuspendLayout();
            this.gbInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeviceInfo)).BeginInit();
            this.tabQuality.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuality)).BeginInit();
            this.tabEnergy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEnergy)).BeginInit();
            this.tabRealTime.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRealTime)).BeginInit();
            this.tabControlMain.SuspendLayout();
            this.tabParams.SuspendLayout();
            this.panelParams.SuspendLayout();
            this.btnPanelParams.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.panelControl.SuspendLayout();
            this.gbDO1.SuspendLayout();
            this.gbDO2.SuspendLayout();
            this.gbOperation.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.tlpTop.SuspendLayout();
            this.tlpMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabDashboard
            // 
            this.tabDashboard.Controls.Add(this.flpDashboard);
            this.tabDashboard.Location = new System.Drawing.Point(4, 30);
            this.tabDashboard.Margin = new System.Windows.Forms.Padding(4);
            this.tabDashboard.Name = "tabDashboard";
            this.tabDashboard.Padding = new System.Windows.Forms.Padding(4);
            this.tabDashboard.Size = new System.Drawing.Size(1169, 588);
            this.tabDashboard.TabIndex = 0;
            this.tabDashboard.Text = "概览仪表盘";
            this.tabDashboard.UseVisualStyleBackColor = true;
            // 
            // flpDashboard
            // 
            this.flpDashboard.AutoScroll = true;
            this.flpDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.flpDashboard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDashboard.Location = new System.Drawing.Point(4, 4);
            this.flpDashboard.Name = "flpDashboard";
            this.flpDashboard.Padding = new System.Windows.Forms.Padding(10);
            this.flpDashboard.Size = new System.Drawing.Size(1161, 580);
            this.flpDashboard.TabIndex = 0;
            // 
            // tabDeviceInfo
            // 
            this.tabDeviceInfo.Controls.Add(this.panelDeviceInfo);
            this.tabDeviceInfo.Location = new System.Drawing.Point(4, 30);
            this.tabDeviceInfo.Margin = new System.Windows.Forms.Padding(4);
            this.tabDeviceInfo.Name = "tabDeviceInfo";
            this.tabDeviceInfo.Padding = new System.Windows.Forms.Padding(4);
            this.tabDeviceInfo.Size = new System.Drawing.Size(1169, 588);
            this.tabDeviceInfo.TabIndex = 3;
            this.tabDeviceInfo.Text = "设备信息";
            this.tabDeviceInfo.UseVisualStyleBackColor = true;
            // 
            // panelDeviceInfo
            // 
            this.panelDeviceInfo.Controls.Add(this.gbInfo);
            this.panelDeviceInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDeviceInfo.Location = new System.Drawing.Point(4, 4);
            this.panelDeviceInfo.Margin = new System.Windows.Forms.Padding(4);
            this.panelDeviceInfo.Name = "panelDeviceInfo";
            this.panelDeviceInfo.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.panelDeviceInfo.Size = new System.Drawing.Size(1161, 580);
            this.panelDeviceInfo.TabIndex = 0;
            // 
            // gbInfo
            // 
            this.gbInfo.Controls.Add(this.lblDeviceType);
            this.gbInfo.Controls.Add(this.dgvDeviceInfo);
            this.gbInfo.Controls.Add(this.btnReadInfo);
            this.gbInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbInfo.Location = new System.Drawing.Point(13, 12);
            this.gbInfo.Margin = new System.Windows.Forms.Padding(4);
            this.gbInfo.Name = "gbInfo";
            this.gbInfo.Padding = new System.Windows.Forms.Padding(4);
            this.gbInfo.Size = new System.Drawing.Size(1135, 556);
            this.gbInfo.TabIndex = 0;
            this.gbInfo.TabStop = false;
            this.gbInfo.Text = "设备信息";
            // 
            // lblDeviceType
            // 
            this.lblDeviceType.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDeviceType.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDeviceType.Location = new System.Drawing.Point(4, 26);
            this.lblDeviceType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDeviceType.Name = "lblDeviceType";
            this.lblDeviceType.Size = new System.Drawing.Size(1127, 31);
            this.lblDeviceType.TabIndex = 0;
            this.lblDeviceType.Text = "未读取";
            // 
            // dgvDeviceInfo
            // 
            this.dgvDeviceInfo.AllowUserToAddRows = false;
            this.dgvDeviceInfo.AllowUserToDeleteRows = false;
            this.dgvDeviceInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDeviceInfo.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvDeviceInfo.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDeviceInfo.ColumnHeadersVisible = false;
            this.dgvDeviceInfo.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colDeviceInfoItem,
            this.colDeviceInfoValue});
            this.dgvDeviceInfo.Location = new System.Drawing.Point(4, 61);
            this.dgvDeviceInfo.Margin = new System.Windows.Forms.Padding(4);
            this.dgvDeviceInfo.Name = "dgvDeviceInfo";
            this.dgvDeviceInfo.ReadOnly = true;
            this.dgvDeviceInfo.RowHeadersVisible = false;
            this.dgvDeviceInfo.RowHeadersWidth = 51;
            this.dgvDeviceInfo.RowTemplate.Height = 23;
            this.dgvDeviceInfo.Size = new System.Drawing.Size(1123, 446);
            this.dgvDeviceInfo.TabIndex = 1;
            // 
            // colDeviceInfoItem
            // 
            this.colDeviceInfoItem.MinimumWidth = 6;
            this.colDeviceInfoItem.Name = "colDeviceInfoItem";
            this.colDeviceInfoItem.ReadOnly = true;
            // 
            // colDeviceInfoValue
            // 
            this.colDeviceInfoValue.MinimumWidth = 6;
            this.colDeviceInfoValue.Name = "colDeviceInfoValue";
            this.colDeviceInfoValue.ReadOnly = true;
            // 
            // btnReadInfo
            // 
            this.btnReadInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnReadInfo.Location = new System.Drawing.Point(4, 514);
            this.btnReadInfo.Margin = new System.Windows.Forms.Padding(4);
            this.btnReadInfo.Name = "btnReadInfo";
            this.btnReadInfo.Size = new System.Drawing.Size(1127, 38);
            this.btnReadInfo.TabIndex = 2;
            this.btnReadInfo.Text = "读取设备信息";
            this.btnReadInfo.UseVisualStyleBackColor = true;
            this.btnReadInfo.Click += new System.EventHandler(this.BtnReadDeviceInfo_Click);
            // 
            // tabQuality
            // 
            this.tabQuality.Controls.Add(this.dgvQuality);
            this.tabQuality.Location = new System.Drawing.Point(4, 30);
            this.tabQuality.Margin = new System.Windows.Forms.Padding(4);
            this.tabQuality.Name = "tabQuality";
            this.tabQuality.Padding = new System.Windows.Forms.Padding(4);
            this.tabQuality.Size = new System.Drawing.Size(1169, 588);
            this.tabQuality.TabIndex = 2;
            this.tabQuality.Text = "电能质量";
            this.tabQuality.UseVisualStyleBackColor = true;
            // 
            // dgvQuality
            // 
            this.dgvQuality.AllowUserToAddRows = false;
            this.dgvQuality.AllowUserToDeleteRows = false;
            this.dgvQuality.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvQuality.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvQuality.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colQualityParameter,
            this.colQualityValue,
            this.colQualityUnit});
            this.dgvQuality.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvQuality.Location = new System.Drawing.Point(4, 4);
            this.dgvQuality.Margin = new System.Windows.Forms.Padding(4);
            this.dgvQuality.Name = "dgvQuality";
            this.dgvQuality.ReadOnly = true;
            this.dgvQuality.RowHeadersVisible = false;
            this.dgvQuality.RowHeadersWidth = 51;
            this.dgvQuality.RowTemplate.Height = 23;
            this.dgvQuality.Size = new System.Drawing.Size(1161, 580);
            this.dgvQuality.TabIndex = 0;
            // 
            // colQualityParameter
            // 
            this.colQualityParameter.HeaderText = "参数";
            this.colQualityParameter.MinimumWidth = 6;
            this.colQualityParameter.Name = "colQualityParameter";
            this.colQualityParameter.ReadOnly = true;
            // 
            // colQualityValue
            // 
            this.colQualityValue.HeaderText = "数值";
            this.colQualityValue.MinimumWidth = 6;
            this.colQualityValue.Name = "colQualityValue";
            this.colQualityValue.ReadOnly = true;
            // 
            // colQualityUnit
            // 
            this.colQualityUnit.HeaderText = "单位";
            this.colQualityUnit.MinimumWidth = 6;
            this.colQualityUnit.Name = "colQualityUnit";
            this.colQualityUnit.ReadOnly = true;
            // 
            // tabEnergy
            // 
            this.tabEnergy.Controls.Add(this.dgvEnergy);
            this.tabEnergy.Location = new System.Drawing.Point(4, 30);
            this.tabEnergy.Margin = new System.Windows.Forms.Padding(4);
            this.tabEnergy.Name = "tabEnergy";
            this.tabEnergy.Padding = new System.Windows.Forms.Padding(4);
            this.tabEnergy.Size = new System.Drawing.Size(1169, 588);
            this.tabEnergy.TabIndex = 1;
            this.tabEnergy.Text = "电能数据";
            this.tabEnergy.UseVisualStyleBackColor = true;
            // 
            // dgvEnergy
            // 
            this.dgvEnergy.AllowUserToAddRows = false;
            this.dgvEnergy.AllowUserToDeleteRows = false;
            this.dgvEnergy.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvEnergy.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEnergy.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colEnergyParameter,
            this.colEnergyValue,
            this.colEnergyUnit});
            this.dgvEnergy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvEnergy.Location = new System.Drawing.Point(4, 4);
            this.dgvEnergy.Margin = new System.Windows.Forms.Padding(4);
            this.dgvEnergy.Name = "dgvEnergy";
            this.dgvEnergy.ReadOnly = true;
            this.dgvEnergy.RowHeadersVisible = false;
            this.dgvEnergy.RowHeadersWidth = 51;
            this.dgvEnergy.RowTemplate.Height = 23;
            this.dgvEnergy.Size = new System.Drawing.Size(1161, 580);
            this.dgvEnergy.TabIndex = 0;
            // 
            // colEnergyParameter
            // 
            this.colEnergyParameter.HeaderText = "参数";
            this.colEnergyParameter.MinimumWidth = 6;
            this.colEnergyParameter.Name = "colEnergyParameter";
            this.colEnergyParameter.ReadOnly = true;
            // 
            // colEnergyValue
            // 
            this.colEnergyValue.HeaderText = "数值";
            this.colEnergyValue.MinimumWidth = 6;
            this.colEnergyValue.Name = "colEnergyValue";
            this.colEnergyValue.ReadOnly = true;
            // 
            // colEnergyUnit
            // 
            this.colEnergyUnit.HeaderText = "单位";
            this.colEnergyUnit.MinimumWidth = 6;
            this.colEnergyUnit.Name = "colEnergyUnit";
            this.colEnergyUnit.ReadOnly = true;
            // 
            // tabRealTime
            // 
            this.tabRealTime.Controls.Add(this.dgvRealTime);
            this.tabRealTime.Location = new System.Drawing.Point(4, 30);
            this.tabRealTime.Margin = new System.Windows.Forms.Padding(4);
            this.tabRealTime.Name = "tabRealTime";
            this.tabRealTime.Padding = new System.Windows.Forms.Padding(4);
            this.tabRealTime.Size = new System.Drawing.Size(1169, 588);
            this.tabRealTime.TabIndex = 0;
            this.tabRealTime.Text = "实时数据";
            this.tabRealTime.UseVisualStyleBackColor = true;
            // 
            // dgvRealTime
            // 
            this.dgvRealTime.AllowUserToAddRows = false;
            this.dgvRealTime.AllowUserToDeleteRows = false;
            this.dgvRealTime.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvRealTime.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRealTime.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRealTimeParameter,
            this.colRealTimeValue,
            this.colRealTimeUnit});
            this.dgvRealTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRealTime.Location = new System.Drawing.Point(4, 4);
            this.dgvRealTime.Margin = new System.Windows.Forms.Padding(4);
            this.dgvRealTime.Name = "dgvRealTime";
            this.dgvRealTime.ReadOnly = true;
            this.dgvRealTime.RowHeadersVisible = false;
            this.dgvRealTime.RowHeadersWidth = 51;
            this.dgvRealTime.RowTemplate.Height = 23;
            this.dgvRealTime.Size = new System.Drawing.Size(1161, 580);
            this.dgvRealTime.TabIndex = 0;
            // 
            // colRealTimeParameter
            // 
            this.colRealTimeParameter.HeaderText = "参数";
            this.colRealTimeParameter.MinimumWidth = 6;
            this.colRealTimeParameter.Name = "colRealTimeParameter";
            this.colRealTimeParameter.ReadOnly = true;
            // 
            // colRealTimeValue
            // 
            this.colRealTimeValue.HeaderText = "数值";
            this.colRealTimeValue.MinimumWidth = 6;
            this.colRealTimeValue.Name = "colRealTimeValue";
            this.colRealTimeValue.ReadOnly = true;
            // 
            // colRealTimeUnit
            // 
            this.colRealTimeUnit.HeaderText = "单位";
            this.colRealTimeUnit.MinimumWidth = 6;
            this.colRealTimeUnit.Name = "colRealTimeUnit";
            this.colRealTimeUnit.ReadOnly = true;
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabDashboard);
            this.tabControlMain.Controls.Add(this.tabRealTime);
            this.tabControlMain.Controls.Add(this.tabEnergy);
            this.tabControlMain.Controls.Add(this.tabQuality);
            this.tabControlMain.Controls.Add(this.tabDeviceInfo);
            this.tabControlMain.Controls.Add(this.tabParams);
            this.tabControlMain.Controls.Add(this.tabControl);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControlMain.Location = new System.Drawing.Point(4, 110);
            this.tabControlMain.Margin = new System.Windows.Forms.Padding(4);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1177, 622);
            this.tabControlMain.TabIndex = 1;
            // 
            // tabParams
            // 
            this.tabParams.Controls.Add(this.panelParams);
            this.tabParams.Location = new System.Drawing.Point(4, 30);
            this.tabParams.Margin = new System.Windows.Forms.Padding(4);
            this.tabParams.Name = "tabParams";
            this.tabParams.Padding = new System.Windows.Forms.Padding(4);
            this.tabParams.Size = new System.Drawing.Size(1169, 588);
            this.tabParams.TabIndex = 4;
            this.tabParams.Text = "参数设置";
            this.tabParams.UseVisualStyleBackColor = true;
            // 
            // panelParams
            // 
            this.panelParams.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.panelParams.ColumnCount = 2;
            this.panelParams.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.2497F));
            this.panelParams.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 82.75031F));
            this.panelParams.Controls.Add(this.lblCommTitle, 0, 0);
            this.panelParams.Controls.Add(this.lblEmpty1, 1, 0);
            this.panelParams.Controls.Add(this.lblParamAddr, 0, 1);
            this.panelParams.Controls.Add(this.txtParamAddr, 1, 1);
            this.panelParams.Controls.Add(this.lblParamBaud, 0, 2);
            this.panelParams.Controls.Add(this.cmbParamBaud, 1, 2);
            this.panelParams.Controls.Add(this.lblParamParity, 0, 3);
            this.panelParams.Controls.Add(this.cmbParamParity, 1, 3);
            this.panelParams.Controls.Add(this.lblBasicTitle, 0, 4);
            this.panelParams.Controls.Add(this.lblEmpty2, 1, 4);
            this.panelParams.Controls.Add(this.lblWiring, 0, 5);
            this.panelParams.Controls.Add(this.cmbWiring, 1, 5);
            this.panelParams.Controls.Add(this.lblPT, 0, 6);
            this.panelParams.Controls.Add(this.txtPT, 1, 6);
            this.panelParams.Controls.Add(this.lblCT, 0, 7);
            this.panelParams.Controls.Add(this.txtCT, 1, 7);
            this.panelParams.Controls.Add(this.btnPanelParams, 1, 8);
            this.panelParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelParams.Location = new System.Drawing.Point(4, 4);
            this.panelParams.Margin = new System.Windows.Forms.Padding(4);
            this.panelParams.Name = "panelParams";
            this.panelParams.Padding = new System.Windows.Forms.Padding(27, 25, 27, 25);
            this.panelParams.RowCount = 9;
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.panelParams.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.panelParams.Size = new System.Drawing.Size(1161, 580);
            this.panelParams.TabIndex = 0;
            // 
            // lblCommTitle
            // 
            this.lblCommTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCommTitle.AutoSize = true;
            this.lblCommTitle.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCommTitle.Location = new System.Drawing.Point(32, 34);
            this.lblCommTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCommTitle.Name = "lblCommTitle";
            this.lblCommTitle.Size = new System.Drawing.Size(92, 27);
            this.lblCommTitle.TabIndex = 0;
            this.lblCommTitle.Text = "通讯参数";
            // 
            // lblEmpty1
            // 
            this.lblEmpty1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEmpty1.AutoSize = true;
            this.lblEmpty1.Location = new System.Drawing.Point(223, 36);
            this.lblEmpty1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEmpty1.Name = "lblEmpty1";
            this.lblEmpty1.Size = new System.Drawing.Size(0, 23);
            this.lblEmpty1.TabIndex = 1;
            // 
            // lblParamAddr
            // 
            this.lblParamAddr.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblParamAddr.AutoSize = true;
            this.lblParamAddr.Location = new System.Drawing.Point(132, 81);
            this.lblParamAddr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblParamAddr.Name = "lblParamAddr";
            this.lblParamAddr.Size = new System.Drawing.Size(82, 23);
            this.lblParamAddr.TabIndex = 2;
            this.lblParamAddr.Text = "设备地址:";
            // 
            // txtParamAddr
            // 
            this.txtParamAddr.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtParamAddr.Location = new System.Drawing.Point(223, 78);
            this.txtParamAddr.Margin = new System.Windows.Forms.Padding(4);
            this.txtParamAddr.Name = "txtParamAddr";
            this.txtParamAddr.Size = new System.Drawing.Size(132, 29);
            this.txtParamAddr.TabIndex = 3;
            this.txtParamAddr.Text = "100";
            // 
            // lblParamBaud
            // 
            this.lblParamBaud.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblParamBaud.AutoSize = true;
            this.lblParamBaud.Location = new System.Drawing.Point(149, 126);
            this.lblParamBaud.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblParamBaud.Name = "lblParamBaud";
            this.lblParamBaud.Size = new System.Drawing.Size(65, 23);
            this.lblParamBaud.TabIndex = 4;
            this.lblParamBaud.Text = "波特率:";
            // 
            // cmbParamBaud
            // 
            this.cmbParamBaud.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbParamBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbParamBaud.FormattingEnabled = true;
            this.cmbParamBaud.Items.AddRange(new object[] {
            "1200",
            "2400",
            "4800",
            "9600",
            "19200",
            "38400"});
            this.cmbParamBaud.Location = new System.Drawing.Point(223, 126);
            this.cmbParamBaud.Margin = new System.Windows.Forms.Padding(4);
            this.cmbParamBaud.Name = "cmbParamBaud";
            this.cmbParamBaud.Size = new System.Drawing.Size(132, 29);
            this.cmbParamBaud.TabIndex = 5;
            // 
            // lblParamParity
            // 
            this.lblParamParity.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblParamParity.AutoSize = true;
            this.lblParamParity.Location = new System.Drawing.Point(132, 171);
            this.lblParamParity.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblParamParity.Name = "lblParamParity";
            this.lblParamParity.Size = new System.Drawing.Size(82, 23);
            this.lblParamParity.TabIndex = 6;
            this.lblParamParity.Text = "校验方式:";
            // 
            // cmbParamParity
            // 
            this.cmbParamParity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbParamParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbParamParity.FormattingEnabled = true;
            this.cmbParamParity.Items.AddRange(new object[] {
            "8N2",
            "8O1",
            "8E1",
            "8N1",
            "8O2",
            "8E2"});
            this.cmbParamParity.Location = new System.Drawing.Point(223, 171);
            this.cmbParamParity.Margin = new System.Windows.Forms.Padding(4);
            this.cmbParamParity.Name = "cmbParamParity";
            this.cmbParamParity.Size = new System.Drawing.Size(132, 29);
            this.cmbParamParity.TabIndex = 7;
            // 
            // lblBasicTitle
            // 
            this.lblBasicTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblBasicTitle.AutoSize = true;
            this.lblBasicTitle.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblBasicTitle.Location = new System.Drawing.Point(32, 214);
            this.lblBasicTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblBasicTitle.Name = "lblBasicTitle";
            this.lblBasicTitle.Size = new System.Drawing.Size(92, 27);
            this.lblBasicTitle.TabIndex = 8;
            this.lblBasicTitle.Text = "基本参数";
            // 
            // lblEmpty2
            // 
            this.lblEmpty2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblEmpty2.AutoSize = true;
            this.lblEmpty2.Location = new System.Drawing.Point(223, 216);
            this.lblEmpty2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEmpty2.Name = "lblEmpty2";
            this.lblEmpty2.Size = new System.Drawing.Size(0, 23);
            this.lblEmpty2.TabIndex = 9;
            // 
            // lblWiring
            // 
            this.lblWiring.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblWiring.AutoSize = true;
            this.lblWiring.Location = new System.Drawing.Point(132, 261);
            this.lblWiring.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblWiring.Name = "lblWiring";
            this.lblWiring.Size = new System.Drawing.Size(82, 23);
            this.lblWiring.TabIndex = 10;
            this.lblWiring.Text = "接线方式:";
            // 
            // cmbWiring
            // 
            this.cmbWiring.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cmbWiring.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWiring.FormattingEnabled = true;
            this.cmbWiring.Items.AddRange(new object[] {
            "DEMO",
            "3P3W",
            "3P4W"});
            this.cmbWiring.Location = new System.Drawing.Point(223, 261);
            this.cmbWiring.Margin = new System.Windows.Forms.Padding(4);
            this.cmbWiring.Name = "cmbWiring";
            this.cmbWiring.Size = new System.Drawing.Size(132, 29);
            this.cmbWiring.TabIndex = 11;
            // 
            // lblPT
            // 
            this.lblPT.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblPT.AutoSize = true;
            this.lblPT.Location = new System.Drawing.Point(146, 306);
            this.lblPT.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPT.Name = "lblPT";
            this.lblPT.Size = new System.Drawing.Size(68, 23);
            this.lblPT.TabIndex = 12;
            this.lblPT.Text = "PT变比:";
            // 
            // txtPT
            // 
            this.txtPT.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtPT.Location = new System.Drawing.Point(223, 303);
            this.txtPT.Margin = new System.Windows.Forms.Padding(4);
            this.txtPT.Name = "txtPT";
            this.txtPT.Size = new System.Drawing.Size(132, 29);
            this.txtPT.TabIndex = 13;
            this.txtPT.Text = "1";
            // 
            // lblCT
            // 
            this.lblCT.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblCT.AutoSize = true;
            this.lblCT.Location = new System.Drawing.Point(145, 351);
            this.lblCT.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCT.Name = "lblCT";
            this.lblCT.Size = new System.Drawing.Size(69, 23);
            this.lblCT.TabIndex = 14;
            this.lblCT.Text = "CT变比:";
            // 
            // txtCT
            // 
            this.txtCT.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.txtCT.Location = new System.Drawing.Point(223, 348);
            this.txtCT.Margin = new System.Windows.Forms.Padding(4);
            this.txtCT.Name = "txtCT";
            this.txtCT.Size = new System.Drawing.Size(132, 29);
            this.txtCT.TabIndex = 15;
            this.txtCT.Text = "1";
            // 
            // btnPanelParams
            // 
            this.btnPanelParams.AutoSize = true;
            this.btnPanelParams.Controls.Add(this.btnReadParams);
            this.btnPanelParams.Controls.Add(this.btnWriteParams);
            this.btnPanelParams.Location = new System.Drawing.Point(223, 390);
            this.btnPanelParams.Margin = new System.Windows.Forms.Padding(4);
            this.btnPanelParams.Name = "btnPanelParams";
            this.btnPanelParams.Size = new System.Drawing.Size(282, 46);
            this.btnPanelParams.TabIndex = 16;
            // 
            // btnReadParams
            // 
            this.btnReadParams.Location = new System.Drawing.Point(4, 4);
            this.btnReadParams.Margin = new System.Windows.Forms.Padding(4);
            this.btnReadParams.Name = "btnReadParams";
            this.btnReadParams.Size = new System.Drawing.Size(133, 38);
            this.btnReadParams.TabIndex = 0;
            this.btnReadParams.Text = "读取参数";
            this.btnReadParams.UseVisualStyleBackColor = true;
            this.btnReadParams.Click += new System.EventHandler(this.BtnReadParams_Click);
            // 
            // btnWriteParams
            // 
            this.btnWriteParams.Location = new System.Drawing.Point(145, 4);
            this.btnWriteParams.Margin = new System.Windows.Forms.Padding(4);
            this.btnWriteParams.Name = "btnWriteParams";
            this.btnWriteParams.Size = new System.Drawing.Size(133, 38);
            this.btnWriteParams.TabIndex = 1;
            this.btnWriteParams.Text = "写入参数";
            this.btnWriteParams.UseVisualStyleBackColor = true;
            this.btnWriteParams.Click += new System.EventHandler(this.BtnWriteParams_Click);
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.panelControl);
            this.tabControl.Location = new System.Drawing.Point(4, 30);
            this.tabControl.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Windows.Forms.Padding(4);
            this.tabControl.Size = new System.Drawing.Size(1169, 588);
            this.tabControl.TabIndex = 5;
            this.tabControl.Text = "远程控制";
            this.tabControl.UseVisualStyleBackColor = true;
            // 
            // panelControl
            // 
            this.panelControl.ColumnCount = 2;
            this.panelControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelControl.Controls.Add(this.gbDO1, 0, 0);
            this.panelControl.Controls.Add(this.gbDO2, 1, 0);
            this.panelControl.Controls.Add(this.gbOperation, 0, 1);
            this.panelControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl.Location = new System.Drawing.Point(4, 4);
            this.panelControl.Margin = new System.Windows.Forms.Padding(4);
            this.panelControl.Name = "panelControl";
            this.panelControl.Padding = new System.Windows.Forms.Padding(27, 25, 27, 25);
            this.panelControl.RowCount = 2;
            this.panelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelControl.Size = new System.Drawing.Size(1161, 580);
            this.panelControl.TabIndex = 0;
            // 
            // gbDO1
            // 
            this.gbDO1.Controls.Add(this.btnDO1On);
            this.gbDO1.Controls.Add(this.btnDO1Off);
            this.gbDO1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbDO1.Location = new System.Drawing.Point(31, 29);
            this.gbDO1.Margin = new System.Windows.Forms.Padding(4);
            this.gbDO1.Name = "gbDO1";
            this.gbDO1.Padding = new System.Windows.Forms.Padding(4);
            this.gbDO1.Size = new System.Drawing.Size(545, 257);
            this.gbDO1.TabIndex = 0;
            this.gbDO1.TabStop = false;
            this.gbDO1.Text = "DO1 控制";
            // 
            // btnDO1On
            // 
            this.btnDO1On.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnDO1On.Location = new System.Drawing.Point(55, 69);
            this.btnDO1On.Margin = new System.Windows.Forms.Padding(4);
            this.btnDO1On.Name = "btnDO1On";
            this.btnDO1On.Size = new System.Drawing.Size(160, 50);
            this.btnDO1On.TabIndex = 0;
            this.btnDO1On.Text = "DO1 合闸";
            this.btnDO1On.UseVisualStyleBackColor = true;
            this.btnDO1On.Click += new System.EventHandler(this.BtnDO1On_Click);
            // 
            // btnDO1Off
            // 
            this.btnDO1Off.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnDO1Off.Location = new System.Drawing.Point(55, 138);
            this.btnDO1Off.Margin = new System.Windows.Forms.Padding(4);
            this.btnDO1Off.Name = "btnDO1Off";
            this.btnDO1Off.Size = new System.Drawing.Size(160, 50);
            this.btnDO1Off.TabIndex = 1;
            this.btnDO1Off.Text = "DO1 分闸";
            this.btnDO1Off.UseVisualStyleBackColor = true;
            this.btnDO1Off.Click += new System.EventHandler(this.BtnDO1Off_Click);
            // 
            // gbDO2
            // 
            this.gbDO2.Controls.Add(this.btnDO2On);
            this.gbDO2.Controls.Add(this.btnDO2Off);
            this.gbDO2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbDO2.Location = new System.Drawing.Point(584, 29);
            this.gbDO2.Margin = new System.Windows.Forms.Padding(4);
            this.gbDO2.Name = "gbDO2";
            this.gbDO2.Padding = new System.Windows.Forms.Padding(4);
            this.gbDO2.Size = new System.Drawing.Size(546, 257);
            this.gbDO2.TabIndex = 1;
            this.gbDO2.TabStop = false;
            this.gbDO2.Text = "DO2 控制";
            // 
            // btnDO2On
            // 
            this.btnDO2On.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnDO2On.Location = new System.Drawing.Point(56, 69);
            this.btnDO2On.Margin = new System.Windows.Forms.Padding(4);
            this.btnDO2On.Name = "btnDO2On";
            this.btnDO2On.Size = new System.Drawing.Size(160, 50);
            this.btnDO2On.TabIndex = 0;
            this.btnDO2On.Text = "DO2 合闸";
            this.btnDO2On.UseVisualStyleBackColor = true;
            this.btnDO2On.Click += new System.EventHandler(this.BtnDO2On_Click);
            // 
            // btnDO2Off
            // 
            this.btnDO2Off.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnDO2Off.Location = new System.Drawing.Point(56, 138);
            this.btnDO2Off.Margin = new System.Windows.Forms.Padding(4);
            this.btnDO2Off.Name = "btnDO2Off";
            this.btnDO2Off.Size = new System.Drawing.Size(160, 50);
            this.btnDO2Off.TabIndex = 1;
            this.btnDO2Off.Text = "DO2 分闸";
            this.btnDO2Off.UseVisualStyleBackColor = true;
            this.btnDO2Off.Click += new System.EventHandler(this.BtnDO2Off_Click);
            // 
            // gbOperation
            // 
            this.gbOperation.Controls.Add(this.btnClearEnergy);
            this.gbOperation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbOperation.Location = new System.Drawing.Point(31, 294);
            this.gbOperation.Margin = new System.Windows.Forms.Padding(4);
            this.gbOperation.Name = "gbOperation";
            this.gbOperation.Padding = new System.Windows.Forms.Padding(4);
            this.gbOperation.Size = new System.Drawing.Size(545, 257);
            this.gbOperation.TabIndex = 2;
            this.gbOperation.TabStop = false;
            this.gbOperation.Text = "操作";
            // 
            // btnClearEnergy
            // 
            this.btnClearEnergy.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnClearEnergy.Location = new System.Drawing.Point(55, 103);
            this.btnClearEnergy.Margin = new System.Windows.Forms.Padding(4);
            this.btnClearEnergy.Name = "btnClearEnergy";
            this.btnClearEnergy.Size = new System.Drawing.Size(160, 50);
            this.btnClearEnergy.TabIndex = 0;
            this.btnClearEnergy.Text = "清除总电能";
            this.btnClearEnergy.UseVisualStyleBackColor = true;
            this.btnClearEnergy.Click += new System.EventHandler(this.BtnClearEnergy_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpTop.SetColumnSpan(this.lblStatus, 9);
            this.lblStatus.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(4, 57);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1169, 32);
            this.lblStatus.TabIndex = 9;
            this.lblStatus.Text = "未连接";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnDiscover
            // 
            this.btnDiscover.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDiscover.AutoSize = true;
            this.btnDiscover.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDiscover.Location = new System.Drawing.Point(1003, 4);
            this.btnDiscover.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnDiscover.Name = "btnDiscover";
            this.btnDiscover.Size = new System.Drawing.Size(167, 41);
            this.btnDiscover.TabIndex = 8;
            this.btnDiscover.Text = "探测地址";
            this.btnDiscover.UseVisualStyleBackColor = true;
            this.btnDiscover.Click += new System.EventHandler(this.BtnDiscover_Click);
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDisconnect.AutoSize = true;
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDisconnect.Location = new System.Drawing.Point(881, 4);
            this.btnDisconnect.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(108, 41);
            this.btnDisconnect.TabIndex = 7;
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.BtnDisconnect_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnect.AutoSize = true;
            this.btnConnect.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnConnect.Location = new System.Drawing.Point(759, 4);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(108, 41);
            this.btnConnect.TabIndex = 6;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.BtnConnect_Click);
            // 
            // txtAddr
            // 
            this.txtAddr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAddr.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtAddr.Location = new System.Drawing.Point(576, 10);
            this.txtAddr.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.txtAddr.Name = "txtAddr";
            this.txtAddr.Size = new System.Drawing.Size(169, 29);
            this.txtAddr.TabIndex = 5;
            this.txtAddr.Text = "95";
            // 
            // lblAddr
            // 
            this.lblAddr.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblAddr.AutoSize = true;
            this.lblAddr.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblAddr.Location = new System.Drawing.Point(514, 13);
            this.lblAddr.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.lblAddr.Name = "lblAddr";
            this.lblAddr.Size = new System.Drawing.Size(48, 23);
            this.lblAddr.TabIndex = 4;
            this.lblAddr.Text = "地址:";
            // 
            // cmbBaud
            // 
            this.cmbBaud.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBaud.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbBaud.FormattingEnabled = true;
            this.cmbBaud.Items.AddRange(new object[] {
            "1200",
            "2400",
            "4800",
            "9600",
            "19200",
            "38400"});
            this.cmbBaud.Location = new System.Drawing.Point(331, 9);
            this.cmbBaud.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.cmbBaud.Name = "cmbBaud";
            this.cmbBaud.Size = new System.Drawing.Size(169, 29);
            this.cmbBaud.TabIndex = 3;
            // 
            // lblBaud
            // 
            this.lblBaud.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblBaud.AutoSize = true;
            this.lblBaud.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblBaud.Location = new System.Drawing.Point(252, 13);
            this.lblBaud.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.lblBaud.Name = "lblBaud";
            this.lblBaud.Size = new System.Drawing.Size(65, 23);
            this.lblBaud.TabIndex = 2;
            this.lblBaud.Text = "波特率:";
            // 
            // cmbPort
            // 
            this.cmbPort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPort.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbPort.FormattingEnabled = true;
            this.cmbPort.Location = new System.Drawing.Point(69, 9);
            this.cmbPort.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.cmbPort.Name = "cmbPort";
            this.cmbPort.Size = new System.Drawing.Size(169, 29);
            this.cmbPort.TabIndex = 1;
            // 
            // lblPort
            // 
            this.lblPort.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblPort.AutoSize = true;
            this.lblPort.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPort.Location = new System.Drawing.Point(7, 13);
            this.lblPort.Margin = new System.Windows.Forms.Padding(7, 4, 7, 4);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(48, 23);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "串口:";
            // 
            // tslTime
            // 
            this.tslTime.Name = "tslTime";
            this.tslTime.Size = new System.Drawing.Size(0, 16);
            // 
            // statusStrip
            // 
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslTime});
            this.statusStrip.Location = new System.Drawing.Point(0, 736);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip.Size = new System.Drawing.Size(1185, 22);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // clockTimer
            // 
            this.clockTimer.Interval = 1000;
            this.clockTimer.Tick += new System.EventHandler(this.ClockTimer_Tick);
            // 
            // tlpTop
            // 
            this.tlpTop.AutoSize = true;
            this.tlpTop.ColumnCount = 9;
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.Controls.Add(this.lblPort, 0, 0);
            this.tlpTop.Controls.Add(this.lblStatus, 0, 1);
            this.tlpTop.Controls.Add(this.btnDiscover, 8, 0);
            this.tlpTop.Controls.Add(this.btnDisconnect, 7, 0);
            this.tlpTop.Controls.Add(this.btnConnect, 6, 0);
            this.tlpTop.Controls.Add(this.txtAddr, 5, 0);
            this.tlpTop.Controls.Add(this.lblAddr, 4, 0);
            this.tlpTop.Controls.Add(this.cmbBaud, 3, 0);
            this.tlpTop.Controls.Add(this.lblBaud, 2, 0);
            this.tlpTop.Controls.Add(this.cmbPort, 1, 0);
            this.tlpTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpTop.Location = new System.Drawing.Point(4, 4);
            this.tlpTop.Margin = new System.Windows.Forms.Padding(4);
            this.tlpTop.Name = "tlpTop";
            this.tlpTop.RowCount = 2;
            this.tlpTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpTop.Size = new System.Drawing.Size(1177, 98);
            this.tlpTop.TabIndex = 10;
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 1;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.Controls.Add(this.tlpTop, 0, 0);
            this.tlpMain.Controls.Add(this.statusStrip, 0, 2);
            this.tlpMain.Controls.Add(this.tabControlMain, 0, 1);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Margin = new System.Windows.Forms.Padding(4);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 3;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.Size = new System.Drawing.Size(1185, 758);
            this.tlpMain.TabIndex = 11;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1185, 758);
            this.Controls.Add(this.tlpMain);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(980, 680);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PMC-S723-A 电表监控系统 v1.0";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabDashboard.ResumeLayout(false);
            this.tabDeviceInfo.ResumeLayout(false);
            this.panelDeviceInfo.ResumeLayout(false);
            this.gbInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeviceInfo)).EndInit();
            this.tabQuality.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuality)).EndInit();
            this.tabEnergy.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEnergy)).EndInit();
            this.tabRealTime.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRealTime)).EndInit();
            this.tabControlMain.ResumeLayout(false);
            this.tabParams.ResumeLayout(false);
            this.panelParams.ResumeLayout(false);
            this.panelParams.PerformLayout();
            this.btnPanelParams.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.panelControl.ResumeLayout(false);
            this.gbDO1.ResumeLayout(false);
            this.gbDO2.ResumeLayout(false);
            this.gbOperation.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.tlpTop.ResumeLayout(false);
            this.tlpTop.PerformLayout();
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private TabPage tabDeviceInfo;
        private Panel panelDeviceInfo;
        private GroupBox gbInfo;
        private Label lblDeviceType;
        private DataGridView dgvDeviceInfo;
        private DataGridViewTextBoxColumn colDeviceInfoItem;
        private DataGridViewTextBoxColumn colDeviceInfoValue;
        private Button btnReadInfo;
        private TabPage tabQuality;
        private DataGridView dgvQuality;
        private DataGridViewTextBoxColumn colQualityParameter;
        private DataGridViewTextBoxColumn colQualityValue;
        private DataGridViewTextBoxColumn colQualityUnit;
        private TabPage tabEnergy;
        private DataGridView dgvEnergy;
        private DataGridViewTextBoxColumn colEnergyParameter;
        private DataGridViewTextBoxColumn colEnergyValue;
        private DataGridViewTextBoxColumn colEnergyUnit;
        private TabPage tabRealTime;
        private DataGridView dgvRealTime;
        private DataGridViewTextBoxColumn colRealTimeParameter;
        private DataGridViewTextBoxColumn colRealTimeValue;
        private DataGridViewTextBoxColumn colRealTimeUnit;
        private TabControl tabControlMain;
        private TabPage tabParams;
        private TableLayoutPanel panelParams;
        private Label lblCommTitle;
        private Label lblEmpty1;
        private Label lblParamAddr;
        private TextBox txtParamAddr;
        private Label lblParamBaud;
        private ComboBox cmbParamBaud;
        private Label lblParamParity;
        private ComboBox cmbParamParity;
        private Label lblBasicTitle;
        private Label lblEmpty2;
        private Label lblWiring;
        private ComboBox cmbWiring;
        private Label lblPT;
        private TextBox txtPT;
        private Label lblCT;
        private TextBox txtCT;
        private FlowLayoutPanel btnPanelParams;
        private Button btnReadParams;
        private Button btnWriteParams;
        private TabPage tabControl;
        private TableLayoutPanel panelControl;
        private GroupBox gbDO1;
        private Button btnDO1On;
        private Button btnDO1Off;
        private GroupBox gbDO2;
        private Button btnDO2On;
        private Button btnDO2Off;
        private GroupBox gbOperation;
        private Button btnClearEnergy;
        private Label lblStatus;
        private Button btnDiscover;
        private Button btnDisconnect;
        private Button btnConnect;
        private TextBox txtAddr;
        private Label lblAddr;
        private ComboBox cmbBaud;
        private Label lblBaud;
        private ComboBox cmbPort;
        private Label lblPort;
        private ToolStripStatusLabel tslTime;
        private StatusStrip statusStrip;
        private Timer clockTimer;
        private TableLayoutPanel tlpTop;
        private TableLayoutPanel tlpMain;
        private TabPage tabDashboard;
        private FlowLayoutPanel flpDashboard;

        // ===== 仪表盘页面控件（自定义，非 Designer 生成） =====
        /// <summary>仪表盘串口选择下拉框</summary>
        private ComboBox _cmbSecondPort;
        /// <summary>仪表盘串口连接按钮</summary>
        private Button _btnSecondConnect;
        /// <summary>仪表盘串口断开按钮</summary>
        private Button _btnSecondDisconnect;
        /// <summary>仪表盘串口连接状态标签</summary>
        private Label _lblSecondStatus;
        /// <summary>添加配电箱按钮</summary>
        private Button _btnAddBox;
        /// <summary>自动扫描按钮</summary>
        private Button _btnAutoScan;
        /// <summary>扫描起始地址输入框</summary>
        private NumericUpDown _nudScanStart;
        /// <summary>扫描结束地址输入框</summary>
        private NumericUpDown _nudScanEnd;
        /// <summary>配电箱与电表卡片的垂直容器</summary>
        private FlowLayoutPanel _flpBoxContainer;

        // ===== 仪表盘页面初始化方法 =====

        /// <summary>
        /// 初始化仪表盘顶栏（串口选择、连接断开、添加配电箱、自动扫描按钮）
        /// 以及配电箱卡片容器（复用设计器的 flpDashboard）
        /// </summary>
        private void InitDashboardControls()
        {
            tabDashboard.SuspendLayout();

            flpDashboard.Dock = DockStyle.Fill;
            flpDashboard.FlowDirection = FlowDirection.TopDown;
            flpDashboard.WrapContents = false;
            flpDashboard.AutoScroll = true;
            flpDashboard.BackColor = Color.FromArgb(245, 245, 245);
            flpDashboard.Padding = new Padding(10, 8, 10, 10);
            flpDashboard.Controls.Clear();
            _flpBoxContainer = flpDashboard;

            var toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 10, 10, 8),
                BackColor = Color.WhiteSmoke,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
            };

            toolbarPanel.Controls.Add(new Label
            {
                Text = "仪表盘串口:",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0),
            });

            _cmbSecondPort = new ComboBox
            {
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 10),
                Margin = new Padding(0, 2, 6, 0),
            };
            toolbarPanel.Controls.Add(_cmbSecondPort);

            _btnSecondConnect = new Button
            {
                Text = "连接",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(72, 30),
                Font = new Font("微软雅黑", 9),
                Margin = new Padding(0, 0, 6, 0),
            };
            _btnSecondConnect.Click += BtnSecondConnect_Click;
            toolbarPanel.Controls.Add(_btnSecondConnect);

            _btnSecondDisconnect = new Button
            {
                Text = "断开",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(72, 30),
                Font = new Font("微软雅黑", 9),
                Margin = new Padding(0, 0, 12, 0),
                Enabled = false,
            };
            _btnSecondDisconnect.Click += BtnSecondDisconnect_Click;
            toolbarPanel.Controls.Add(_btnSecondDisconnect);

            _lblSecondStatus = new Label
            {
                Text = "● 未连接",
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Red,
                AutoSize = true,
                Margin = new Padding(0, 6, 15, 0),
            };
            toolbarPanel.Controls.Add(_lblSecondStatus);

            _btnAddBox = new Button
            {
                Text = "+ 添加电表",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(110, 30),
                Font = new Font("微软雅黑", 9),
                Margin = new Padding(0, 0, 6, 0),
            };
            _btnAddBox.Click += BtnAddBox_Click;
            toolbarPanel.Controls.Add(_btnAddBox);

            toolbarPanel.Controls.Add(new Label
            {
                Text = "扫描范围:",
                Font = new Font("微软雅黑", 9),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0),
            });

            _nudScanStart = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 247,
                Value = 95,
                Width = 70,
                Font = new Font("微软雅黑", 9),
                Margin = new Padding(0, 2, 4, 0),
            };
            toolbarPanel.Controls.Add(_nudScanStart);

            toolbarPanel.Controls.Add(new Label
            {
                Text = "~",
                Font = new Font("微软雅黑", 9),
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0),
            });

            _nudScanEnd = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 247,
                Value = 120,
                Width = 70,
                Font = new Font("微软雅黑", 9),
                Margin = new Padding(0, 2, 8, 0),
            };
            toolbarPanel.Controls.Add(_nudScanEnd);

            _btnAutoScan = new Button
            {
                Text = "自动扫描",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(110, 30),
                Font = new Font("微软雅黑", 9),
            };
            _btnAutoScan.Click += BtnAutoScan_Click;
            toolbarPanel.Controls.Add(_btnAutoScan);

            tabDashboard.Controls.Add(toolbarPanel);
            tabDashboard.Controls.SetChildIndex(toolbarPanel, 0);

            _flpBoxContainer.Resize += (s, e) => ResizeDashboardPanels();

            tabDashboard.ResumeLayout(false);
            tabDashboard.PerformLayout();
        }

        /// <summary>
        /// 构建一张电表卡片（纯 UI 布局）
        /// </summary>
        /// <param name="meter">电表信息，决定卡片标题、默认 ID</param>
        /// <returns>包含完整子控件的 Panel</returns>
        internal Panel BuildMeterCard(MeterInfo meter)
        {
            var card = new Panel
            {
                Width = 320,
                Height = 220,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Margin = new Padding(6),
                Padding = new Padding(14, 12, 14, 12),
                Name = meter.Id,
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.Controls.Add(layout);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var txtName = new TextBox
            {
                Text = meter.Name,
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 0),
                BackColor = Color.White,
                MinimumSize = new Size(0, 24),
                Tag = meter,
            };
            txtName.TextChanged += (s, ev) =>
            {
                var tb = (TextBox)s;
                var m = (MeterInfo)tb.Tag;
                var name = tb.Text.Trim();
                m.Name = string.IsNullOrWhiteSpace(name) ? "未命名" : name;
            };
            header.Controls.Add(txtName, 0, 0);

            var lblId = new Label
            {
                Text = "ID:",
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 6, 0),
            };
            header.Controls.Add(lblId, 1, 0);

            var cmbId = new ComboBox
            {
                Width = 62,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9),
                Tag = meter,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };
            for (int i = 1; i <= 247; i++)
                cmbId.Items.Add(i);
            cmbId.SelectedItem = (int)meter.SlaveAddress;
            cmbId.SelectedIndexChanged += (s, ev) =>
            {
                var cb = (ComboBox)s;
                var m = (MeterInfo)cb.Tag;
                if (cb.SelectedItem != null)
                    m.SlaveAddress = (byte)(int)cb.SelectedItem;
            };
            header.Controls.Add(cmbId, 2, 0);
            layout.Controls.Add(header, 0, 0);

            header.Click += MeterCard_Click;
            lblId.Click += MeterCard_Click;

            var line = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = Color.FromArgb(230, 230, 230),
                Margin = new Padding(0, 10, 0, 10),
            };
            layout.Controls.Add(line, 0, 1);

            var metrics = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblPowerTitle = new Label
            {
                Text = "三相功率",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4),
            };
            metrics.Controls.Add(lblPowerTitle, 0, 0);

            var lblPower = new Label
            {
                Text = "-- kW",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 140, 255),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14),
            };
            metrics.Controls.Add(lblPower, 0, 1);
            metrics.SetColumnSpan(lblPower, 2);

            var lblCurrentTitle = new Label
            {
                Text = "最大电流",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 2, 8, 0),
            };
            metrics.Controls.Add(lblCurrentTitle, 0, 2);

            var lblCurrent = new Label
            {
                Text = "-- A",
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleRight,
            };
            metrics.Controls.Add(lblCurrent, 1, 2);
            layout.Controls.Add(metrics, 0, 2);

            var bottomRow = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                ColumnCount = meter.IsToolbar ? 1 : 2,
                Margin = new Padding(0, 12, 0, 0),
                Padding = new Padding(0),
            };
            bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (!meter.IsToolbar)
                bottomRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var lblStatus = new Label
            {
                Text = "● 等待数据",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 0, 0),
            };
            bottomRow.Controls.Add(lblStatus, 0, 0);

            if (!meter.IsToolbar)
            {
                var btnDelete = new Button
                {
                    Text = "删除",
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    MinimumSize = new Size(58, 28),
                    Font = new Font("微软雅黑", 8),
                    ForeColor = Color.Red,
                    Tag = meter,
                    Margin = new Padding(8, 0, 0, 0),
                    Anchor = AnchorStyles.Right,
                };
                btnDelete.Click += DeleteMeterCard_Click;
                bottomRow.Controls.Add(btnDelete, 1, 0);
            }

            layout.Controls.Add(bottomRow, 0, 3);

            card.Click += MeterCard_Click;
            layout.Click += MeterCard_Click;
            metrics.Click += MeterCard_Click;
            lblPowerTitle.Click += MeterCard_Click;
            lblPower.Click += MeterCard_Click;
            lblCurrentTitle.Click += MeterCard_Click;
            lblCurrent.Click += MeterCard_Click;
            lblStatus.Click += MeterCard_Click;
            line.Click += MeterCard_Click;

            card.Tag = new { PowerLabel = lblPower, CurrentLabel = lblCurrent, StatusLabel = lblStatus };
            return card;
        }
    }
}
