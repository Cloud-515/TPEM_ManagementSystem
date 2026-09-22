using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

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

        // 注意（UI-15）：本文件里按钮的 Size / Padding / Margin 已经不是最终值。
        // 构造函数末尾的 MainForm.ApplyLayoutStandard() 会用 UiStyle 统一覆盖
        // 按钮尺寸、内外边距和工具条排布 —— 要调按钮外观请改 UI/UiStyle.cs，
        // 在这里改（例如把 Padding 改成 12,6,12,6）不会生效。
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.tabDashboard = new System.Windows.Forms.TabPage();
            this.tlpDashboardRoot = new System.Windows.Forms.TableLayoutPanel();
            this.flpDashboardToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSecondPortCaption = new System.Windows.Forms.Label();
            this.cmbSecondPort = new System.Windows.Forms.ComboBox();
            this.btnSecondConnect = new System.Windows.Forms.Button();
            this.btnSecondDisconnect = new System.Windows.Forms.Button();
            this.lblScanRangeCaption = new System.Windows.Forms.Label();
            this.nudScanStart = new System.Windows.Forms.NumericUpDown();
            this.lblScanRangeSeparator = new System.Windows.Forms.Label();
            this.nudScanEnd = new System.Windows.Forms.NumericUpDown();
            this.btnAutoScanDashboard = new System.Windows.Forms.Button();
            this.btnAddMeter = new System.Windows.Forms.Button();
            this.lblSecondStatusDashboard = new System.Windows.Forms.Label();
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
            this.tabDataQuery = new System.Windows.Forms.TabPage();
            this.tabHistoryTabs = new System.Windows.Forms.TabControl();
            this.tabQuickHistory = new System.Windows.Forms.TabPage();
            this.tlpQuickRoot = new System.Windows.Forms.TableLayoutPanel();
            this.flpQuickTop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblQuickMeterCaption = new System.Windows.Forms.Label();
            this.cmbQuickMeter = new System.Windows.Forms.ComboBox();
            this.lblQuickMetricCaption = new System.Windows.Forms.Label();
            this.cmbQuickMetric = new System.Windows.Forms.ComboBox();
            this.lblQuickStartCaption = new System.Windows.Forms.Label();
            this.dtQuickStart = new System.Windows.Forms.DateTimePicker();
            this.lblQuickEndCaption = new System.Windows.Forms.Label();
            this.dtQuickEnd = new System.Windows.Forms.DateTimePicker();
            this.btnQuickSearch = new System.Windows.Forms.Button();
            this.btnQuickRefresh = new System.Windows.Forms.Button();
            this.chartQuickHistory = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.dgvQuickHistory = new System.Windows.Forms.DataGridView();
            this.colQuickCollectTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuickMetricValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuickMetricUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuickMetricSource = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpQuickPaging = new System.Windows.Forms.FlowLayoutPanel();
            this.btnQuickPrev = new System.Windows.Forms.Button();
            this.btnQuickNext = new System.Windows.Forms.Button();
            this.lblQuickPage = new System.Windows.Forms.Label();
            this.lblQuickStatus = new System.Windows.Forms.Label();
            this.tabCompareHistory = new System.Windows.Forms.TabPage();
            this.splitCompareRoot = new System.Windows.Forms.SplitContainer();
            this.tlpCompareLeft = new System.Windows.Forms.TableLayoutPanel();
            this.flpCompareLeftTop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCompareTreeCaption = new System.Windows.Forms.Label();
            this.btnCompareRefresh = new System.Windows.Forms.Button();
            this.tvCompareMeters = new System.Windows.Forms.TreeView();
            this.lblCompareTreeStatus = new System.Windows.Forms.Label();
            this.tlpCompareRight = new System.Windows.Forms.TableLayoutPanel();
            this.flpCompareRightTop = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCompareMetricCaption = new System.Windows.Forms.Label();
            this.cmbCompareMetric = new System.Windows.Forms.ComboBox();
            this.lblCompareStartCaption = new System.Windows.Forms.Label();
            this.dtCompareStart = new System.Windows.Forms.DateTimePicker();
            this.lblCompareEndCaption = new System.Windows.Forms.Label();
            this.dtCompareEnd = new System.Windows.Forms.DateTimePicker();
            this.btnCompareSearch = new System.Windows.Forms.Button();
            this.chartCompareHistory = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.lblCompareStatus = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnDiscover = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnMeterProtocol = new System.Windows.Forms.Button();
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
            this.tlpDashboardRoot.SuspendLayout();
            this.flpDashboardToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanEnd)).BeginInit();
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
            this.tabDataQuery.SuspendLayout();
            this.tabHistoryTabs.SuspendLayout();
            this.tabQuickHistory.SuspendLayout();
            this.tlpQuickRoot.SuspendLayout();
            this.flpQuickTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartQuickHistory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuickHistory)).BeginInit();
            this.flpQuickPaging.SuspendLayout();
            this.tabCompareHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitCompareRoot)).BeginInit();
            this.splitCompareRoot.Panel1.SuspendLayout();
            this.splitCompareRoot.Panel2.SuspendLayout();
            this.splitCompareRoot.SuspendLayout();
            this.tlpCompareLeft.SuspendLayout();
            this.flpCompareLeftTop.SuspendLayout();
            this.tlpCompareRight.SuspendLayout();
            this.flpCompareRightTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartCompareHistory)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.tlpTop.SuspendLayout();
            this.tlpMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabDashboard
            // 
            this.tabDashboard.Controls.Add(this.tlpDashboardRoot);
            this.tabDashboard.Location = new System.Drawing.Point(4, 30);
            this.tabDashboard.Margin = new System.Windows.Forms.Padding(4);
            this.tabDashboard.Name = "tabDashboard";
            this.tabDashboard.Padding = new System.Windows.Forms.Padding(4);
            this.tabDashboard.Size = new System.Drawing.Size(1169, 588);
            this.tabDashboard.TabIndex = 0;
            this.tabDashboard.Text = "概览仪表盘";
            this.tabDashboard.UseVisualStyleBackColor = true;
            // 
            // tlpDashboardRoot
            // 
            this.tlpDashboardRoot.ColumnCount = 1;
            this.tlpDashboardRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDashboardRoot.Controls.Add(this.flpDashboardToolbar, 0, 0);
            this.tlpDashboardRoot.Controls.Add(this.flpDashboard, 0, 1);
            this.tlpDashboardRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpDashboardRoot.Location = new System.Drawing.Point(4, 4);
            this.tlpDashboardRoot.Margin = new System.Windows.Forms.Padding(0);
            this.tlpDashboardRoot.Name = "tlpDashboardRoot";
            this.tlpDashboardRoot.RowCount = 2;
            this.tlpDashboardRoot.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpDashboardRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDashboardRoot.Size = new System.Drawing.Size(1161, 580);
            this.tlpDashboardRoot.TabIndex = 0;
            // 
            // flpDashboardToolbar
            // 
            this.flpDashboardToolbar.AutoSize = true;
            this.flpDashboardToolbar.Controls.Add(this.lblSecondPortCaption);
            this.flpDashboardToolbar.Controls.Add(this.cmbSecondPort);
            this.flpDashboardToolbar.Controls.Add(this.btnSecondConnect);
            this.flpDashboardToolbar.Controls.Add(this.btnSecondDisconnect);
            this.flpDashboardToolbar.Controls.Add(this.lblScanRangeCaption);
            this.flpDashboardToolbar.Controls.Add(this.nudScanStart);
            this.flpDashboardToolbar.Controls.Add(this.lblScanRangeSeparator);
            this.flpDashboardToolbar.Controls.Add(this.nudScanEnd);
            this.flpDashboardToolbar.Controls.Add(this.btnAutoScanDashboard);
            this.flpDashboardToolbar.Controls.Add(this.btnAddMeter);
            this.flpDashboardToolbar.Controls.Add(this.lblSecondStatusDashboard);
            this.flpDashboardToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDashboardToolbar.Location = new System.Drawing.Point(4, 4);
            this.flpDashboardToolbar.Margin = new System.Windows.Forms.Padding(4);
            this.flpDashboardToolbar.Name = "flpDashboardToolbar";
            this.flpDashboardToolbar.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.flpDashboardToolbar.Size = new System.Drawing.Size(1153, 61);
            this.flpDashboardToolbar.TabIndex = 0;
            // 
            // lblSecondPortCaption
            // 
            this.lblSecondPortCaption.AutoSize = true;
            this.lblSecondPortCaption.Location = new System.Drawing.Point(14, 16);
            this.lblSecondPortCaption.Margin = new System.Windows.Forms.Padding(6, 10, 2, 0);
            this.lblSecondPortCaption.Name = "lblSecondPortCaption";
            this.lblSecondPortCaption.Size = new System.Drawing.Size(95, 23);
            this.lblSecondPortCaption.TabIndex = 0;
            this.lblSecondPortCaption.Text = "仪表盘串口";
            // 
            // cmbSecondPort
            // 
            this.cmbSecondPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSecondPort.FormattingEnabled = true;
            this.cmbSecondPort.Location = new System.Drawing.Point(115, 10);
            this.cmbSecondPort.Margin = new System.Windows.Forms.Padding(4);
            this.cmbSecondPort.Name = "cmbSecondPort";
            this.cmbSecondPort.Size = new System.Drawing.Size(140, 29);
            this.cmbSecondPort.TabIndex = 1;
            // 
            // btnSecondConnect
            // 
            this.btnSecondConnect.AutoSize = true;
            this.btnSecondConnect.Location = new System.Drawing.Point(263, 8);
            this.btnSecondConnect.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.btnSecondConnect.Name = "btnSecondConnect";
            this.btnSecondConnect.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.btnSecondConnect.Size = new System.Drawing.Size(74, 45);
            this.btnSecondConnect.TabIndex = 2;
            this.btnSecondConnect.Text = "连接";
            this.btnSecondConnect.UseVisualStyleBackColor = true;
            this.btnSecondConnect.Click += new System.EventHandler(this.BtnSecondConnect_Click);
            // 
            // btnSecondDisconnect
            // 
            this.btnSecondDisconnect.AutoSize = true;
            this.btnSecondDisconnect.Enabled = false;
            this.btnSecondDisconnect.Location = new System.Drawing.Point(345, 8);
            this.btnSecondDisconnect.Margin = new System.Windows.Forms.Padding(4, 2, 10, 2);
            this.btnSecondDisconnect.Name = "btnSecondDisconnect";
            this.btnSecondDisconnect.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.btnSecondDisconnect.Size = new System.Drawing.Size(74, 45);
            this.btnSecondDisconnect.TabIndex = 3;
            this.btnSecondDisconnect.Text = "断开";
            this.btnSecondDisconnect.UseVisualStyleBackColor = true;
            this.btnSecondDisconnect.Click += new System.EventHandler(this.BtnSecondDisconnect_Click);
            // 
            // lblScanRangeCaption
            // 
            this.lblScanRangeCaption.AutoSize = true;
            this.lblScanRangeCaption.Location = new System.Drawing.Point(433, 16);
            this.lblScanRangeCaption.Margin = new System.Windows.Forms.Padding(4, 10, 2, 0);
            this.lblScanRangeCaption.Name = "lblScanRangeCaption";
            this.lblScanRangeCaption.Size = new System.Drawing.Size(78, 23);
            this.lblScanRangeCaption.TabIndex = 4;
            this.lblScanRangeCaption.Text = "扫描范围";
            // 
            // nudScanStart
            // 
            this.nudScanStart.Location = new System.Drawing.Point(517, 10);
            this.nudScanStart.Margin = new System.Windows.Forms.Padding(4);
            this.nudScanStart.Maximum = new decimal(new int[] {
            247,
            0,
            0,
            0});
            this.nudScanStart.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudScanStart.Name = "nudScanStart";
            this.nudScanStart.Size = new System.Drawing.Size(70, 29);
            this.nudScanStart.TabIndex = 5;
            this.nudScanStart.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblScanRangeSeparator
            // 
            this.lblScanRangeSeparator.AutoSize = true;
            this.lblScanRangeSeparator.Location = new System.Drawing.Point(595, 16);
            this.lblScanRangeSeparator.Margin = new System.Windows.Forms.Padding(4, 10, 4, 0);
            this.lblScanRangeSeparator.Name = "lblScanRangeSeparator";
            this.lblScanRangeSeparator.Size = new System.Drawing.Size(23, 23);
            this.lblScanRangeSeparator.TabIndex = 6;
            this.lblScanRangeSeparator.Text = "~";
            // 
            // nudScanEnd
            // 
            this.nudScanEnd.Location = new System.Drawing.Point(626, 10);
            this.nudScanEnd.Margin = new System.Windows.Forms.Padding(4);
            this.nudScanEnd.Maximum = new decimal(new int[] {
            247,
            0,
            0,
            0});
            this.nudScanEnd.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudScanEnd.Name = "nudScanEnd";
            this.nudScanEnd.Size = new System.Drawing.Size(70, 29);
            this.nudScanEnd.TabIndex = 7;
            this.nudScanEnd.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // btnAutoScanDashboard
            // 
            this.btnAutoScanDashboard.AutoSize = true;
            this.btnAutoScanDashboard.Location = new System.Drawing.Point(704, 8);
            this.btnAutoScanDashboard.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.btnAutoScanDashboard.Name = "btnAutoScanDashboard";
            this.btnAutoScanDashboard.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.btnAutoScanDashboard.Size = new System.Drawing.Size(108, 45);
            this.btnAutoScanDashboard.TabIndex = 8;
            this.btnAutoScanDashboard.Text = "自动扫描";
            this.btnAutoScanDashboard.UseVisualStyleBackColor = true;
            this.btnAutoScanDashboard.Click += new System.EventHandler(this.BtnAutoScan_Click);
            // 
            // btnAddMeter
            // 
            this.btnAddMeter.AutoSize = true;
            this.btnAddMeter.Location = new System.Drawing.Point(820, 8);
            this.btnAddMeter.Margin = new System.Windows.Forms.Padding(4, 2, 10, 2);
            this.btnAddMeter.Name = "btnAddMeter";
            this.btnAddMeter.Padding = new System.Windows.Forms.Padding(10, 6, 10, 6);
            this.btnAddMeter.Size = new System.Drawing.Size(108, 45);
            this.btnAddMeter.TabIndex = 9;
            this.btnAddMeter.Text = "新增电表";
            this.btnAddMeter.UseVisualStyleBackColor = true;
            this.btnAddMeter.Click += new System.EventHandler(this.BtnAddBox_Click);
            //
            // lblSecondStatusDashboard
            // 
            this.lblSecondStatusDashboard.AutoSize = true;
            this.lblSecondStatusDashboard.ForeColor = System.Drawing.Color.Red;
            this.lblSecondStatusDashboard.Location = new System.Drawing.Point(942, 16);
            this.lblSecondStatusDashboard.Margin = new System.Windows.Forms.Padding(4, 10, 4, 0);
            this.lblSecondStatusDashboard.Name = "lblSecondStatusDashboard";
            this.lblSecondStatusDashboard.Size = new System.Drawing.Size(61, 23);
            this.lblSecondStatusDashboard.TabIndex = 10;
            this.lblSecondStatusDashboard.Text = "未连接";
            // 
            // flpDashboard
            // 
            this.flpDashboard.AutoScroll = true;
            this.flpDashboard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this.flpDashboard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpDashboard.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDashboard.Location = new System.Drawing.Point(0, 69);
            this.flpDashboard.Margin = new System.Windows.Forms.Padding(0);
            this.flpDashboard.Name = "flpDashboard";
            this.flpDashboard.Padding = new System.Windows.Forms.Padding(10, 8, 10, 10);
            this.flpDashboard.Size = new System.Drawing.Size(1161, 511);
            this.flpDashboard.TabIndex = 1;
            this.flpDashboard.WrapContents = false;
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
            this.tabControlMain.Controls.Add(this.tabDataQuery);
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
            // tabDataQuery
            // 
            this.tabDataQuery.Controls.Add(this.tabHistoryTabs);
            this.tabDataQuery.Location = new System.Drawing.Point(4, 30);
            this.tabDataQuery.Margin = new System.Windows.Forms.Padding(4);
            this.tabDataQuery.Name = "tabDataQuery";
            this.tabDataQuery.Padding = new System.Windows.Forms.Padding(4);
            this.tabDataQuery.Size = new System.Drawing.Size(1169, 588);
            this.tabDataQuery.TabIndex = 6;
            this.tabDataQuery.Text = "数据查询";
            this.tabDataQuery.UseVisualStyleBackColor = true;
            // 
            // tabHistoryTabs
            // 
            this.tabHistoryTabs.Controls.Add(this.tabQuickHistory);
            this.tabHistoryTabs.Controls.Add(this.tabCompareHistory);
            this.tabHistoryTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabHistoryTabs.Font = new System.Drawing.Font("微软雅黑", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabHistoryTabs.Location = new System.Drawing.Point(4, 4);
            this.tabHistoryTabs.Margin = new System.Windows.Forms.Padding(4);
            this.tabHistoryTabs.Name = "tabHistoryTabs";
            this.tabHistoryTabs.SelectedIndex = 0;
            this.tabHistoryTabs.Size = new System.Drawing.Size(1161, 580);
            this.tabHistoryTabs.TabIndex = 0;
            // 
            // tabQuickHistory
            // 
            this.tabQuickHistory.Controls.Add(this.tlpQuickRoot);
            this.tabQuickHistory.Location = new System.Drawing.Point(4, 30);
            this.tabQuickHistory.Margin = new System.Windows.Forms.Padding(4);
            this.tabQuickHistory.Name = "tabQuickHistory";
            this.tabQuickHistory.Padding = new System.Windows.Forms.Padding(6);
            this.tabQuickHistory.Size = new System.Drawing.Size(1153, 546);
            this.tabQuickHistory.TabIndex = 0;
            this.tabQuickHistory.Text = "快速数据追踪";
            this.tabQuickHistory.UseVisualStyleBackColor = true;
            // 
            // tlpQuickRoot
            // 
            this.tlpQuickRoot.ColumnCount = 1;
            this.tlpQuickRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpQuickRoot.Controls.Add(this.flpQuickTop, 0, 0);
            this.tlpQuickRoot.Controls.Add(this.chartQuickHistory, 0, 1);
            this.tlpQuickRoot.Controls.Add(this.dgvQuickHistory, 0, 2);
            this.tlpQuickRoot.Controls.Add(this.flpQuickPaging, 0, 3);
            this.tlpQuickRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpQuickRoot.Location = new System.Drawing.Point(6, 6);
            this.tlpQuickRoot.Margin = new System.Windows.Forms.Padding(4);
            this.tlpQuickRoot.Name = "tlpQuickRoot";
            this.tlpQuickRoot.RowCount = 4;
            this.tlpQuickRoot.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpQuickRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48F));
            this.tlpQuickRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F));
            this.tlpQuickRoot.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpQuickRoot.Size = new System.Drawing.Size(1141, 534);
            this.tlpQuickRoot.TabIndex = 0;
            // 
            // flpQuickTop
            // 
            this.flpQuickTop.AutoSize = true;
            this.flpQuickTop.Controls.Add(this.lblQuickMeterCaption);
            this.flpQuickTop.Controls.Add(this.cmbQuickMeter);
            this.flpQuickTop.Controls.Add(this.lblQuickMetricCaption);
            this.flpQuickTop.Controls.Add(this.cmbQuickMetric);
            this.flpQuickTop.Controls.Add(this.lblQuickStartCaption);
            this.flpQuickTop.Controls.Add(this.dtQuickStart);
            this.flpQuickTop.Controls.Add(this.lblQuickEndCaption);
            this.flpQuickTop.Controls.Add(this.dtQuickEnd);
            this.flpQuickTop.Controls.Add(this.btnQuickSearch);
            this.flpQuickTop.Controls.Add(this.btnQuickRefresh);
            this.flpQuickTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpQuickTop.Location = new System.Drawing.Point(4, 4);
            this.flpQuickTop.Margin = new System.Windows.Forms.Padding(4);
            this.flpQuickTop.Name = "flpQuickTop";
            this.flpQuickTop.Padding = new System.Windows.Forms.Padding(4);
            this.flpQuickTop.Size = new System.Drawing.Size(1133, 94);
            this.flpQuickTop.TabIndex = 0;
            // 
            // lblQuickMeterCaption
            // 
            this.lblQuickMeterCaption.AutoSize = true;
            this.lblQuickMeterCaption.Location = new System.Drawing.Point(10, 13);
            this.lblQuickMeterCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblQuickMeterCaption.Name = "lblQuickMeterCaption";
            this.lblQuickMeterCaption.Size = new System.Drawing.Size(44, 23);
            this.lblQuickMeterCaption.TabIndex = 0;
            this.lblQuickMeterCaption.Text = "电表";
            // 
            // cmbQuickMeter
            // 
            this.cmbQuickMeter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuickMeter.DropDownWidth = 460;
            this.cmbQuickMeter.FormattingEnabled = true;
            this.cmbQuickMeter.Location = new System.Drawing.Point(60, 8);
            this.cmbQuickMeter.Margin = new System.Windows.Forms.Padding(4);
            this.cmbQuickMeter.Name = "cmbQuickMeter";
            this.cmbQuickMeter.Size = new System.Drawing.Size(230, 29);
            this.cmbQuickMeter.TabIndex = 1;
            // 
            // lblQuickMetricCaption
            // 
            this.lblQuickMetricCaption.AutoSize = true;
            this.lblQuickMetricCaption.Location = new System.Drawing.Point(350, 13);
            this.lblQuickMetricCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblQuickMetricCaption.Name = "lblQuickMetricCaption";
            this.lblQuickMetricCaption.Size = new System.Drawing.Size(44, 23);
            this.lblQuickMetricCaption.TabIndex = 2;
            this.lblQuickMetricCaption.Text = "参数";
            // 
            // cmbQuickMetric
            // 
            this.cmbQuickMetric.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuickMetric.DropDownWidth = 240;
            this.cmbQuickMetric.FormattingEnabled = true;
            this.cmbQuickMetric.Location = new System.Drawing.Point(400, 8);
            this.cmbQuickMetric.Margin = new System.Windows.Forms.Padding(4);
            this.cmbQuickMetric.Name = "cmbQuickMetric";
            this.cmbQuickMetric.Size = new System.Drawing.Size(156, 29);
            this.cmbQuickMetric.TabIndex = 3;
            // 
            // lblQuickStartCaption
            // 
            this.lblQuickStartCaption.AutoSize = true;
            this.lblQuickStartCaption.Location = new System.Drawing.Point(590, 13);
            this.lblQuickStartCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblQuickStartCaption.Name = "lblQuickStartCaption";
            this.lblQuickStartCaption.Size = new System.Drawing.Size(78, 23);
            this.lblQuickStartCaption.TabIndex = 4;
            this.lblQuickStartCaption.Text = "开始时间";
            // 
            // dtQuickStart
            // 
            this.dtQuickStart.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtQuickStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtQuickStart.Location = new System.Drawing.Point(674, 8);
            this.dtQuickStart.Margin = new System.Windows.Forms.Padding(4);
            this.dtQuickStart.Name = "dtQuickStart";
            this.dtQuickStart.ShowUpDown = true;
            this.dtQuickStart.Size = new System.Drawing.Size(190, 29);
            this.dtQuickStart.TabIndex = 5;
            // 
            // lblQuickEndCaption
            // 
            this.lblQuickEndCaption.AutoSize = true;
            this.lblQuickEndCaption.Location = new System.Drawing.Point(874, 13);
            this.lblQuickEndCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblQuickEndCaption.Name = "lblQuickEndCaption";
            this.lblQuickEndCaption.Size = new System.Drawing.Size(78, 23);
            this.lblQuickEndCaption.TabIndex = 6;
            this.lblQuickEndCaption.Text = "结束时间";
            // 
            // dtQuickEnd
            // 
            this.dtQuickEnd.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtQuickEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtQuickEnd.Location = new System.Drawing.Point(8, 45);
            this.dtQuickEnd.Margin = new System.Windows.Forms.Padding(4);
            this.dtQuickEnd.Name = "dtQuickEnd";
            this.dtQuickEnd.ShowUpDown = true;
            this.dtQuickEnd.Size = new System.Drawing.Size(190, 29);
            this.dtQuickEnd.TabIndex = 7;
            // 
            // btnQuickSearch
            // 
            this.btnQuickSearch.AutoSize = true;
            this.btnQuickSearch.Location = new System.Drawing.Point(208, 43);
            this.btnQuickSearch.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnQuickSearch.Name = "btnQuickSearch";
            this.btnQuickSearch.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnQuickSearch.Size = new System.Drawing.Size(78, 45);
            this.btnQuickSearch.TabIndex = 8;
            this.btnQuickSearch.Text = "查询";
            this.btnQuickSearch.UseVisualStyleBackColor = true;
            this.btnQuickSearch.Click += new System.EventHandler(this.QuickSearchButton_Click);
            // 
            // btnQuickRefresh
            // 
            this.btnQuickRefresh.AutoSize = true;
            this.btnQuickRefresh.Location = new System.Drawing.Point(298, 43);
            this.btnQuickRefresh.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnQuickRefresh.Name = "btnQuickRefresh";
            this.btnQuickRefresh.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnQuickRefresh.Size = new System.Drawing.Size(112, 45);
            this.btnQuickRefresh.TabIndex = 9;
            this.btnQuickRefresh.Text = "刷新电表";
            this.btnQuickRefresh.UseVisualStyleBackColor = true;
            this.btnQuickRefresh.Click += new System.EventHandler(this.QuickRefreshButton_Click);
            // 
            // chartQuickHistory
            // 
            chartArea1.AxisX.IntervalAutoMode = System.Windows.Forms.DataVisualization.Charting.IntervalAutoMode.VariableCount;
            chartArea1.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea1.Name = "MainArea";
            this.chartQuickHistory.ChartAreas.Add(chartArea1);
            this.chartQuickHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Alignment = System.Drawing.StringAlignment.Center;
            legend1.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            legend1.Enabled = false;
            legend1.Name = "MainLegend";
            this.chartQuickHistory.Legends.Add(legend1);
            this.chartQuickHistory.Location = new System.Drawing.Point(4, 106);
            this.chartQuickHistory.Margin = new System.Windows.Forms.Padding(4);
            this.chartQuickHistory.Name = "chartQuickHistory";
            this.chartQuickHistory.Size = new System.Drawing.Size(1133, 168);
            this.chartQuickHistory.TabIndex = 1;
            this.chartQuickHistory.Text = "chartQuickHistory";
            // 
            // dgvQuickHistory
            // 
            this.dgvQuickHistory.AllowUserToAddRows = false;
            this.dgvQuickHistory.AllowUserToDeleteRows = false;
            this.dgvQuickHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvQuickHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvQuickHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colQuickCollectTime,
            this.colQuickMetricValue,
            this.colQuickMetricUnit,
            this.colQuickMetricSource});
            this.dgvQuickHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvQuickHistory.Location = new System.Drawing.Point(4, 282);
            this.dgvQuickHistory.Margin = new System.Windows.Forms.Padding(4);
            this.dgvQuickHistory.MultiSelect = false;
            this.dgvQuickHistory.Name = "dgvQuickHistory";
            this.dgvQuickHistory.ReadOnly = true;
            this.dgvQuickHistory.RowHeadersVisible = false;
            this.dgvQuickHistory.RowHeadersWidth = 51;
            this.dgvQuickHistory.RowTemplate.Height = 23;
            this.dgvQuickHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvQuickHistory.Size = new System.Drawing.Size(1133, 182);
            this.dgvQuickHistory.TabIndex = 2;
            // 
            // colQuickCollectTime
            // 
            this.colQuickCollectTime.HeaderText = "采集时间";
            this.colQuickCollectTime.MinimumWidth = 6;
            this.colQuickCollectTime.Name = "colQuickCollectTime";
            this.colQuickCollectTime.ReadOnly = true;
            // 
            // colQuickMetricValue
            // 
            this.colQuickMetricValue.HeaderText = "数值";
            this.colQuickMetricValue.MinimumWidth = 6;
            this.colQuickMetricValue.Name = "colQuickMetricValue";
            this.colQuickMetricValue.ReadOnly = true;
            // 
            // colQuickMetricUnit
            // 
            this.colQuickMetricUnit.HeaderText = "单位";
            this.colQuickMetricUnit.MinimumWidth = 6;
            this.colQuickMetricUnit.Name = "colQuickMetricUnit";
            this.colQuickMetricUnit.ReadOnly = true;
            // 
            // colQuickMetricSource
            // 
            this.colQuickMetricSource.HeaderText = "来源";
            this.colQuickMetricSource.MinimumWidth = 6;
            this.colQuickMetricSource.Name = "colQuickMetricSource";
            this.colQuickMetricSource.ReadOnly = true;
            // 
            // flpQuickPaging
            // 
            this.flpQuickPaging.AutoSize = true;
            this.flpQuickPaging.Controls.Add(this.btnQuickPrev);
            this.flpQuickPaging.Controls.Add(this.btnQuickNext);
            this.flpQuickPaging.Controls.Add(this.lblQuickPage);
            this.flpQuickPaging.Controls.Add(this.lblQuickStatus);
            this.flpQuickPaging.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpQuickPaging.Location = new System.Drawing.Point(4, 472);
            this.flpQuickPaging.Margin = new System.Windows.Forms.Padding(4);
            this.flpQuickPaging.Name = "flpQuickPaging";
            this.flpQuickPaging.Padding = new System.Windows.Forms.Padding(4);
            this.flpQuickPaging.Size = new System.Drawing.Size(1133, 58);
            this.flpQuickPaging.TabIndex = 3;
            // 
            // btnQuickPrev
            // 
            this.btnQuickPrev.AutoSize = true;
            this.btnQuickPrev.Location = new System.Drawing.Point(10, 6);
            this.btnQuickPrev.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnQuickPrev.Name = "btnQuickPrev";
            this.btnQuickPrev.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnQuickPrev.Size = new System.Drawing.Size(95, 45);
            this.btnQuickPrev.TabIndex = 0;
            this.btnQuickPrev.Text = "上一页";
            this.btnQuickPrev.UseVisualStyleBackColor = true;
            this.btnQuickPrev.Click += new System.EventHandler(this.QuickPrevButton_Click);
            // 
            // btnQuickNext
            // 
            this.btnQuickNext.AutoSize = true;
            this.btnQuickNext.Location = new System.Drawing.Point(117, 6);
            this.btnQuickNext.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnQuickNext.Name = "btnQuickNext";
            this.btnQuickNext.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnQuickNext.Size = new System.Drawing.Size(95, 45);
            this.btnQuickNext.TabIndex = 1;
            this.btnQuickNext.Text = "下一页";
            this.btnQuickNext.UseVisualStyleBackColor = true;
            this.btnQuickNext.Click += new System.EventHandler(this.QuickNextButton_Click);
            // 
            // lblQuickPage
            // 
            this.lblQuickPage.AutoSize = true;
            this.lblQuickPage.Location = new System.Drawing.Point(224, 13);
            this.lblQuickPage.Margin = new System.Windows.Forms.Padding(6, 9, 6, 0);
            this.lblQuickPage.Name = "lblQuickPage";
            this.lblQuickPage.Size = new System.Drawing.Size(135, 23);
            this.lblQuickPage.TabIndex = 2;
            this.lblQuickPage.Text = "第 0 页 / 共 0 条";
            // 
            // lblQuickStatus
            // 
            this.lblQuickStatus.AutoSize = true;
            this.lblQuickStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblQuickStatus.Location = new System.Drawing.Point(381, 13);
            this.lblQuickStatus.Margin = new System.Windows.Forms.Padding(16, 9, 6, 0);
            this.lblQuickStatus.Name = "lblQuickStatus";
            this.lblQuickStatus.Size = new System.Drawing.Size(78, 23);
            this.lblQuickStatus.TabIndex = 3;
            this.lblQuickStatus.Text = "等待查询";
            // 
            // tabCompareHistory
            // 
            this.tabCompareHistory.Controls.Add(this.splitCompareRoot);
            this.tabCompareHistory.Location = new System.Drawing.Point(4, 30);
            this.tabCompareHistory.Margin = new System.Windows.Forms.Padding(4);
            this.tabCompareHistory.Name = "tabCompareHistory";
            this.tabCompareHistory.Padding = new System.Windows.Forms.Padding(6);
            this.tabCompareHistory.Size = new System.Drawing.Size(1153, 546);
            this.tabCompareHistory.TabIndex = 1;
            this.tabCompareHistory.Text = "关联对比分析";
            this.tabCompareHistory.UseVisualStyleBackColor = true;
            // 
            // splitCompareRoot
            // 
            this.splitCompareRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitCompareRoot.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitCompareRoot.Location = new System.Drawing.Point(6, 6);
            this.splitCompareRoot.Margin = new System.Windows.Forms.Padding(4);
            this.splitCompareRoot.Name = "splitCompareRoot";
            // 
            // splitCompareRoot.Panel1
            // 
            this.splitCompareRoot.Panel1.Controls.Add(this.tlpCompareLeft);
            // 
            // splitCompareRoot.Panel2
            // 
            this.splitCompareRoot.Panel2.Controls.Add(this.tlpCompareRight);
            this.splitCompareRoot.Size = new System.Drawing.Size(1141, 534);
            this.splitCompareRoot.SplitterDistance = 300;
            this.splitCompareRoot.TabIndex = 0;
            // 
            // tlpCompareLeft
            // 
            this.tlpCompareLeft.ColumnCount = 1;
            this.tlpCompareLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCompareLeft.Controls.Add(this.flpCompareLeftTop, 0, 0);
            this.tlpCompareLeft.Controls.Add(this.tvCompareMeters, 0, 1);
            this.tlpCompareLeft.Controls.Add(this.lblCompareTreeStatus, 0, 2);
            this.tlpCompareLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpCompareLeft.Location = new System.Drawing.Point(0, 0);
            this.tlpCompareLeft.Margin = new System.Windows.Forms.Padding(4);
            this.tlpCompareLeft.Name = "tlpCompareLeft";
            this.tlpCompareLeft.RowCount = 3;
            this.tlpCompareLeft.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpCompareLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCompareLeft.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpCompareLeft.Size = new System.Drawing.Size(300, 534);
            this.tlpCompareLeft.TabIndex = 0;
            // 
            // flpCompareLeftTop
            // 
            this.flpCompareLeftTop.AutoSize = true;
            this.flpCompareLeftTop.Controls.Add(this.lblCompareTreeCaption);
            this.flpCompareLeftTop.Controls.Add(this.btnCompareRefresh);
            this.flpCompareLeftTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCompareLeftTop.Location = new System.Drawing.Point(4, 4);
            this.flpCompareLeftTop.Margin = new System.Windows.Forms.Padding(4);
            this.flpCompareLeftTop.Name = "flpCompareLeftTop";
            this.flpCompareLeftTop.Padding = new System.Windows.Forms.Padding(4);
            this.flpCompareLeftTop.Size = new System.Drawing.Size(292, 57);
            this.flpCompareLeftTop.TabIndex = 0;
            // 
            // lblCompareTreeCaption
            // 
            this.lblCompareTreeCaption.AutoSize = true;
            this.lblCompareTreeCaption.Location = new System.Drawing.Point(10, 13);
            this.lblCompareTreeCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblCompareTreeCaption.Name = "lblCompareTreeCaption";
            this.lblCompareTreeCaption.Size = new System.Drawing.Size(61, 23);
            this.lblCompareTreeCaption.TabIndex = 0;
            this.lblCompareTreeCaption.Text = "电表树";
            // 
            // btnCompareRefresh
            // 
            this.btnCompareRefresh.AutoSize = true;
            this.btnCompareRefresh.Location = new System.Drawing.Point(79, 6);
            this.btnCompareRefresh.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnCompareRefresh.Name = "btnCompareRefresh";
            this.btnCompareRefresh.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnCompareRefresh.Size = new System.Drawing.Size(112, 45);
            this.btnCompareRefresh.TabIndex = 1;
            this.btnCompareRefresh.Text = "刷新目录";
            this.btnCompareRefresh.UseVisualStyleBackColor = true;
            this.btnCompareRefresh.Click += new System.EventHandler(this.CompareRefreshButton_Click);
            // 
            // tvCompareMeters
            // 
            this.tvCompareMeters.CheckBoxes = true;
            this.tvCompareMeters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvCompareMeters.Font = new System.Drawing.Font("微软雅黑", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tvCompareMeters.HideSelection = false;
            this.tvCompareMeters.Location = new System.Drawing.Point(4, 69);
            this.tvCompareMeters.Margin = new System.Windows.Forms.Padding(4);
            this.tvCompareMeters.Name = "tvCompareMeters";
            this.tvCompareMeters.Size = new System.Drawing.Size(292, 426);
            this.tvCompareMeters.TabIndex = 1;
            this.tvCompareMeters.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.CompareTree_AfterCheck);
            // 
            // lblCompareTreeStatus
            // 
            this.lblCompareTreeStatus.AutoSize = true;
            this.lblCompareTreeStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCompareTreeStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblCompareTreeStatus.Location = new System.Drawing.Point(4, 499);
            this.lblCompareTreeStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCompareTreeStatus.Name = "lblCompareTreeStatus";
            this.lblCompareTreeStatus.Padding = new System.Windows.Forms.Padding(6);
            this.lblCompareTreeStatus.Size = new System.Drawing.Size(292, 35);
            this.lblCompareTreeStatus.TabIndex = 2;
            this.lblCompareTreeStatus.Text = "最多勾选 4 台电表";
            // 
            // tlpCompareRight
            // 
            this.tlpCompareRight.ColumnCount = 1;
            this.tlpCompareRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCompareRight.Controls.Add(this.flpCompareRightTop, 0, 0);
            this.tlpCompareRight.Controls.Add(this.chartCompareHistory, 0, 1);
            this.tlpCompareRight.Controls.Add(this.lblCompareStatus, 0, 2);
            this.tlpCompareRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpCompareRight.Location = new System.Drawing.Point(0, 0);
            this.tlpCompareRight.Margin = new System.Windows.Forms.Padding(4);
            this.tlpCompareRight.Name = "tlpCompareRight";
            this.tlpCompareRight.RowCount = 3;
            this.tlpCompareRight.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpCompareRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpCompareRight.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpCompareRight.Size = new System.Drawing.Size(837, 534);
            this.tlpCompareRight.TabIndex = 0;
            // 
            // flpCompareRightTop
            // 
            this.flpCompareRightTop.AutoSize = true;
            this.flpCompareRightTop.Controls.Add(this.lblCompareMetricCaption);
            this.flpCompareRightTop.Controls.Add(this.cmbCompareMetric);
            this.flpCompareRightTop.Controls.Add(this.lblCompareStartCaption);
            this.flpCompareRightTop.Controls.Add(this.dtCompareStart);
            this.flpCompareRightTop.Controls.Add(this.lblCompareEndCaption);
            this.flpCompareRightTop.Controls.Add(this.dtCompareEnd);
            this.flpCompareRightTop.Controls.Add(this.btnCompareSearch);
            this.flpCompareRightTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCompareRightTop.Location = new System.Drawing.Point(4, 4);
            this.flpCompareRightTop.Margin = new System.Windows.Forms.Padding(4);
            this.flpCompareRightTop.Name = "flpCompareRightTop";
            this.flpCompareRightTop.Padding = new System.Windows.Forms.Padding(4);
            this.flpCompareRightTop.Size = new System.Drawing.Size(829, 94);
            this.flpCompareRightTop.TabIndex = 0;
            // 
            // lblCompareMetricCaption
            // 
            this.lblCompareMetricCaption.AutoSize = true;
            this.lblCompareMetricCaption.Location = new System.Drawing.Point(10, 13);
            this.lblCompareMetricCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblCompareMetricCaption.Name = "lblCompareMetricCaption";
            this.lblCompareMetricCaption.Size = new System.Drawing.Size(78, 23);
            this.lblCompareMetricCaption.TabIndex = 0;
            this.lblCompareMetricCaption.Text = "对比参数";
            // 
            // cmbCompareMetric
            // 
            this.cmbCompareMetric.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCompareMetric.DropDownWidth = 240;
            this.cmbCompareMetric.FormattingEnabled = true;
            this.cmbCompareMetric.Location = new System.Drawing.Point(94, 8);
            this.cmbCompareMetric.Margin = new System.Windows.Forms.Padding(4);
            this.cmbCompareMetric.Name = "cmbCompareMetric";
            this.cmbCompareMetric.Size = new System.Drawing.Size(156, 29);
            this.cmbCompareMetric.TabIndex = 1;
            // 
            // lblCompareStartCaption
            // 
            this.lblCompareStartCaption.AutoSize = true;
            this.lblCompareStartCaption.Location = new System.Drawing.Point(284, 13);
            this.lblCompareStartCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblCompareStartCaption.Name = "lblCompareStartCaption";
            this.lblCompareStartCaption.Size = new System.Drawing.Size(78, 23);
            this.lblCompareStartCaption.TabIndex = 2;
            this.lblCompareStartCaption.Text = "开始时间";
            // 
            // dtCompareStart
            // 
            this.dtCompareStart.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtCompareStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtCompareStart.Location = new System.Drawing.Point(368, 8);
            this.dtCompareStart.Margin = new System.Windows.Forms.Padding(4);
            this.dtCompareStart.Name = "dtCompareStart";
            this.dtCompareStart.ShowUpDown = true;
            this.dtCompareStart.Size = new System.Drawing.Size(190, 29);
            this.dtCompareStart.TabIndex = 3;
            // 
            // lblCompareEndCaption
            // 
            this.lblCompareEndCaption.AutoSize = true;
            this.lblCompareEndCaption.Location = new System.Drawing.Point(568, 13);
            this.lblCompareEndCaption.Margin = new System.Windows.Forms.Padding(6, 9, 2, 0);
            this.lblCompareEndCaption.Name = "lblCompareEndCaption";
            this.lblCompareEndCaption.Size = new System.Drawing.Size(78, 23);
            this.lblCompareEndCaption.TabIndex = 4;
            this.lblCompareEndCaption.Text = "结束时间";
            // 
            // dtCompareEnd
            // 
            this.dtCompareEnd.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dtCompareEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtCompareEnd.Location = new System.Drawing.Point(8, 45);
            this.dtCompareEnd.Margin = new System.Windows.Forms.Padding(4);
            this.dtCompareEnd.Name = "dtCompareEnd";
            this.dtCompareEnd.ShowUpDown = true;
            this.dtCompareEnd.Size = new System.Drawing.Size(190, 29);
            this.dtCompareEnd.TabIndex = 5;
            // 
            // btnCompareSearch
            // 
            this.btnCompareSearch.AutoSize = true;
            this.btnCompareSearch.Location = new System.Drawing.Point(208, 43);
            this.btnCompareSearch.Margin = new System.Windows.Forms.Padding(6, 2, 6, 2);
            this.btnCompareSearch.Name = "btnCompareSearch";
            this.btnCompareSearch.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.btnCompareSearch.Size = new System.Drawing.Size(112, 45);
            this.btnCompareSearch.TabIndex = 6;
            this.btnCompareSearch.Text = "开始对比";
            this.btnCompareSearch.UseVisualStyleBackColor = true;
            this.btnCompareSearch.Click += new System.EventHandler(this.CompareSearchButton_Click);
            // 
            // chartCompareHistory
            // 
            chartArea2.AxisX.IntervalAutoMode = System.Windows.Forms.DataVisualization.Charting.IntervalAutoMode.VariableCount;
            chartArea2.AxisX.LabelStyle.Format = "MM-dd HH:mm";
            chartArea2.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            chartArea2.Name = "MainArea";
            this.chartCompareHistory.ChartAreas.Add(chartArea2);
            this.chartCompareHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Alignment = System.Drawing.StringAlignment.Center;
            legend2.Docking = System.Windows.Forms.DataVisualization.Charting.Docking.Bottom;
            legend2.Name = "MainLegend";
            this.chartCompareHistory.Legends.Add(legend2);
            this.chartCompareHistory.Location = new System.Drawing.Point(4, 106);
            this.chartCompareHistory.Margin = new System.Windows.Forms.Padding(4);
            this.chartCompareHistory.Name = "chartCompareHistory";
            this.chartCompareHistory.Size = new System.Drawing.Size(829, 389);
            this.chartCompareHistory.TabIndex = 1;
            this.chartCompareHistory.Text = "chartCompareHistory";
            // 
            // lblCompareStatus
            // 
            this.lblCompareStatus.AutoSize = true;
            this.lblCompareStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCompareStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblCompareStatus.Location = new System.Drawing.Point(4, 499);
            this.lblCompareStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCompareStatus.Name = "lblCompareStatus";
            this.lblCompareStatus.Padding = new System.Windows.Forms.Padding(6);
            this.lblCompareStatus.Size = new System.Drawing.Size(829, 35);
            this.lblCompareStatus.TabIndex = 2;
            this.lblCompareStatus.Text = "等待查询";
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
            // btnMeterProtocol
            //
            this.btnMeterProtocol.AutoSize = true;
            this.btnMeterProtocol.Location = new System.Drawing.Point(0, 0);
            this.btnMeterProtocol.Name = "btnMeterProtocol";
            this.btnMeterProtocol.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnMeterProtocol.Size = new System.Drawing.Size(130, 29);
            this.btnMeterProtocol.TabIndex = 9;
            this.btnMeterProtocol.UseVisualStyleBackColor = true;
            this.btnMeterProtocol.Click += new System.EventHandler(this.btnMeterProtocol_Click);
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
            this.tlpTop.ColumnCount = 10;
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpTop.Controls.Add(this.btnMeterProtocol, 9, 0);
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
            this.tlpDashboardRoot.ResumeLayout(false);
            this.tlpDashboardRoot.PerformLayout();
            this.flpDashboardToolbar.ResumeLayout(false);
            this.flpDashboardToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudScanEnd)).EndInit();
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
            this.tabDataQuery.ResumeLayout(false);
            this.tabHistoryTabs.ResumeLayout(false);
            this.tabQuickHistory.ResumeLayout(false);
            this.tlpQuickRoot.ResumeLayout(false);
            this.tlpQuickRoot.PerformLayout();
            this.flpQuickTop.ResumeLayout(false);
            this.flpQuickTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartQuickHistory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQuickHistory)).EndInit();
            this.flpQuickPaging.ResumeLayout(false);
            this.flpQuickPaging.PerformLayout();
            this.tabCompareHistory.ResumeLayout(false);
            this.splitCompareRoot.Panel1.ResumeLayout(false);
            this.splitCompareRoot.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitCompareRoot)).EndInit();
            this.splitCompareRoot.ResumeLayout(false);
            this.tlpCompareLeft.ResumeLayout(false);
            this.tlpCompareLeft.PerformLayout();
            this.flpCompareLeftTop.ResumeLayout(false);
            this.flpCompareLeftTop.PerformLayout();
            this.tlpCompareRight.ResumeLayout(false);
            this.tlpCompareRight.PerformLayout();
            this.flpCompareRightTop.ResumeLayout(false);
            this.flpCompareRightTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartCompareHistory)).EndInit();
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
        private Button btnMeterProtocol;
        private TextBox txtAddr;
        private Label lblAddr;
        private ComboBox cmbBaud;
        private Label lblBaud;
        private ComboBox cmbPort;
        private Label lblPort;
        private ToolStripStatusLabel tslTime;
        private StatusStrip statusStrip;
        private System.Windows.Forms.Timer clockTimer;
        private TableLayoutPanel tlpTop;
        private TableLayoutPanel tlpMain;
        private TabPage tabDashboard;
        private TableLayoutPanel tlpDashboardRoot;
        private FlowLayoutPanel flpDashboardToolbar;
        private Label lblSecondPortCaption;
        private ComboBox cmbSecondPort;
        private Button btnSecondConnect;
        private Button btnSecondDisconnect;
        private Label lblScanRangeCaption;
        private NumericUpDown nudScanStart;
        private Label lblScanRangeSeparator;
        private NumericUpDown nudScanEnd;
        private Button btnAutoScanDashboard;
        private Button btnAddMeter;
        private Label lblSecondStatusDashboard;
        private FlowLayoutPanel flpDashboard;
        private TabPage tabDataQuery;
        private TabControl tabHistoryTabs;
        private TabPage tabQuickHistory;
        private TableLayoutPanel tlpQuickRoot;
        private FlowLayoutPanel flpQuickTop;
        private Label lblQuickMeterCaption;
        private ComboBox cmbQuickMeter;
        private Label lblQuickMetricCaption;
        private ComboBox cmbQuickMetric;
        private Label lblQuickStartCaption;
        private DateTimePicker dtQuickStart;
        private Label lblQuickEndCaption;
        private DateTimePicker dtQuickEnd;
        private Button btnQuickSearch;
        private Button btnQuickRefresh;
        private Chart chartQuickHistory;
        private DataGridView dgvQuickHistory;
        private DataGridViewTextBoxColumn colQuickCollectTime;
        private DataGridViewTextBoxColumn colQuickMetricValue;
        private DataGridViewTextBoxColumn colQuickMetricUnit;
        private DataGridViewTextBoxColumn colQuickMetricSource;
        private FlowLayoutPanel flpQuickPaging;
        private Button btnQuickPrev;
        private Button btnQuickNext;
        private Label lblQuickPage;
        private Label lblQuickStatus;
        private TabPage tabCompareHistory;
        private SplitContainer splitCompareRoot;
        private TableLayoutPanel tlpCompareLeft;
        private FlowLayoutPanel flpCompareLeftTop;
        private Label lblCompareTreeCaption;
        private Button btnCompareRefresh;
        private TreeView tvCompareMeters;
        private Label lblCompareTreeStatus;
        private TableLayoutPanel tlpCompareRight;
        private FlowLayoutPanel flpCompareRightTop;
        private Label lblCompareMetricCaption;
        private ComboBox cmbCompareMetric;
        private Label lblCompareStartCaption;
        private DateTimePicker dtCompareStart;
        private Label lblCompareEndCaption;
        private DateTimePicker dtCompareEnd;
        private Button btnCompareSearch;
        private Chart chartCompareHistory;
        private Label lblCompareStatus;
    }
}
