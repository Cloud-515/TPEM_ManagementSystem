namespace MeterAcquisition.HeatPump.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel tlpDashboardRoot = null!;
    private FlowLayoutPanel flpDashboardToolbar = null!;
    private ComboBox cmbPort = null!;
    private NumericUpDown nudSlaveAddress = null!;
    private ComboBox cmbBaudRate = null!;
    private ComboBox cmbParity = null!;
    private ComboBox cmbStopBits = null!;
    private Button btnConnect = null!;
    private Button btnDisconnect = null!;
    private NumericUpDown nudAddressStart = null!;
    private NumericUpDown nudAddressEnd = null!;
    private Button btnAutoScan = null!;
    private Button btnAddUnit = null!;
    private Label lblStatusDashboard = null!;
    private Label lblControlScopeCaption = null!;
    private ComboBox cmbControlScope = null!;
    private Label lblControlModeCaption = null!;
    private ComboBox cmbControlMode = null!;
    private Label lblControlTempCaption = null!;
    private NumericUpDown nudControlTemp = null!;
    private Button btnApplyControl = null!;
    private Button btnClearFault = null!;
    private Panel pnlControlMessage = null!;
    private Label lblControlMessage = null!;
    private FlowLayoutPanel flpDashboard = null!;
    private System.Windows.Forms.Timer dashboardRefreshTimer = null!;
    private Label lblPortCaption = null!;
    private Label lblSlaveAddressCaption = null!;
    private Label lblBaudRateCaption = null!;
    private Label lblParityCaption = null!;
    private Label lblStopBitsCaption = null!;
    private Label lblAddressRangeCaption = null!;
    private Label lblAddressRangeSeparator = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tlpDashboardRoot = new TableLayoutPanel();
        flpDashboardToolbar = new FlowLayoutPanel();
        lblPortCaption = new Label();
        cmbPort = new ComboBox();
        lblSlaveAddressCaption = new Label();
        nudSlaveAddress = new NumericUpDown();
        lblBaudRateCaption = new Label();
        cmbBaudRate = new ComboBox();
        lblParityCaption = new Label();
        cmbParity = new ComboBox();
        lblStopBitsCaption = new Label();
        cmbStopBits = new ComboBox();
        btnConnect = new Button();
        btnDisconnect = new Button();
        lblAddressRangeCaption = new Label();
        nudAddressStart = new NumericUpDown();
        lblAddressRangeSeparator = new Label();
        nudAddressEnd = new NumericUpDown();
        btnAutoScan = new Button();
        btnAddUnit = new Button();
        lblStatusDashboard = new Label();
        lblControlScopeCaption = new Label();
        cmbControlScope = new ComboBox();
        lblControlModeCaption = new Label();
        cmbControlMode = new ComboBox();
        lblControlTempCaption = new Label();
        nudControlTemp = new NumericUpDown();
        btnApplyControl = new Button();
        btnClearFault = new Button();
        pnlControlMessage = new Panel();
        lblControlMessage = new Label();
        flpDashboard = new FlowLayoutPanel();
        dashboardRefreshTimer = new System.Windows.Forms.Timer(components);
        tlpDashboardRoot.SuspendLayout();
        flpDashboardToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudSlaveAddress).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAddressStart).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudAddressEnd).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudControlTemp).BeginInit();
        pnlControlMessage.SuspendLayout();
        SuspendLayout();
        // 
        // tlpDashboardRoot
        // 
        tlpDashboardRoot.ColumnCount = 1;
        tlpDashboardRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tlpDashboardRoot.Controls.Add(flpDashboardToolbar, 0, 0);
        tlpDashboardRoot.Controls.Add(pnlControlMessage, 0, 1);
        tlpDashboardRoot.Controls.Add(flpDashboard, 0, 2);
        tlpDashboardRoot.Dock = DockStyle.Fill;
        tlpDashboardRoot.Location = new Point(0, 0);
        tlpDashboardRoot.Margin = new Padding(0);
        tlpDashboardRoot.Name = "tlpDashboardRoot";
        tlpDashboardRoot.RowCount = 3;
        tlpDashboardRoot.RowStyles.Add(new RowStyle());
        tlpDashboardRoot.RowStyles.Add(new RowStyle());
        tlpDashboardRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpDashboardRoot.Size = new Size(1440, 820);
        tlpDashboardRoot.TabIndex = 0;
        // 
        // flpDashboardToolbar
        // 
        flpDashboardToolbar.AutoSize = true;
        flpDashboardToolbar.Controls.Add(lblPortCaption);
        flpDashboardToolbar.Controls.Add(cmbPort);
        flpDashboardToolbar.Controls.Add(lblSlaveAddressCaption);
        flpDashboardToolbar.Controls.Add(nudSlaveAddress);
        flpDashboardToolbar.Controls.Add(lblBaudRateCaption);
        flpDashboardToolbar.Controls.Add(cmbBaudRate);
        flpDashboardToolbar.Controls.Add(lblParityCaption);
        flpDashboardToolbar.Controls.Add(cmbParity);
        flpDashboardToolbar.Controls.Add(lblStopBitsCaption);
        flpDashboardToolbar.Controls.Add(cmbStopBits);
        flpDashboardToolbar.Controls.Add(btnConnect);
        flpDashboardToolbar.Controls.Add(btnDisconnect);
        flpDashboardToolbar.Controls.Add(lblAddressRangeCaption);
        flpDashboardToolbar.Controls.Add(nudAddressStart);
        flpDashboardToolbar.Controls.Add(lblAddressRangeSeparator);
        flpDashboardToolbar.Controls.Add(nudAddressEnd);
        flpDashboardToolbar.Controls.Add(btnAutoScan);
        flpDashboardToolbar.Controls.Add(btnAddUnit);
        flpDashboardToolbar.Controls.Add(lblStatusDashboard);
        flpDashboardToolbar.Controls.Add(lblControlScopeCaption);
        flpDashboardToolbar.Controls.Add(cmbControlScope);
        flpDashboardToolbar.Controls.Add(lblControlModeCaption);
        flpDashboardToolbar.Controls.Add(cmbControlMode);
        flpDashboardToolbar.Controls.Add(lblControlTempCaption);
        flpDashboardToolbar.Controls.Add(nudControlTemp);
        flpDashboardToolbar.Controls.Add(btnApplyControl);
        flpDashboardToolbar.Controls.Add(btnClearFault);
        flpDashboardToolbar.Dock = DockStyle.Fill;
        flpDashboardToolbar.Location = new Point(4, 4);
        flpDashboardToolbar.Margin = new Padding(4);
        flpDashboardToolbar.Name = "flpDashboardToolbar";
        flpDashboardToolbar.Padding = new Padding(8, 6, 8, 6);
        flpDashboardToolbar.Size = new Size(1432, 114);
        flpDashboardToolbar.TabIndex = 0;
        // 
        // lblPortCaption
        // 
        lblPortCaption.AutoSize = true;
        lblPortCaption.Location = new Point(14, 13);
        lblPortCaption.Margin = new Padding(6, 7, 2, 0);
        lblPortCaption.Name = "lblPortCaption";
        lblPortCaption.Size = new Size(64, 24);
        lblPortCaption.TabIndex = 0;
        lblPortCaption.Text = "通讯口";
        // 
        // cmbPort
        // 
        cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPort.FormattingEnabled = true;
        cmbPort.Location = new Point(84, 10);
        cmbPort.Margin = new Padding(4);
        cmbPort.Name = "cmbPort";
        cmbPort.Size = new Size(130, 31);
        cmbPort.TabIndex = 1;
        // 
        // lblSlaveAddressCaption
        // 
        lblSlaveAddressCaption.AutoSize = true;
        lblSlaveAddressCaption.Location = new Point(222, 13);
        lblSlaveAddressCaption.Margin = new Padding(4, 7, 2, 0);
        lblSlaveAddressCaption.Name = "lblSlaveAddressCaption";
        lblSlaveAddressCaption.Size = new Size(100, 24);
        lblSlaveAddressCaption.TabIndex = 2;
        lblSlaveAddressCaption.Text = "线控器地址";
        // 
        // nudSlaveAddress
        // 
        nudSlaveAddress.Location = new Point(328, 10);
        nudSlaveAddress.Margin = new Padding(4);
        nudSlaveAddress.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
        nudSlaveAddress.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudSlaveAddress.Name = "nudSlaveAddress";
        nudSlaveAddress.Size = new Size(66, 31);
        nudSlaveAddress.TabIndex = 3;
        nudSlaveAddress.Value = new decimal(new int[] { 1, 0, 0, 0 });
        // 
        // lblBaudRateCaption
        // 
        lblBaudRateCaption.AutoSize = true;
        lblBaudRateCaption.Location = new Point(402, 13);
        lblBaudRateCaption.Margin = new Padding(4, 7, 2, 0);
        lblBaudRateCaption.Name = "lblBaudRateCaption";
        lblBaudRateCaption.Size = new Size(64, 24);
        lblBaudRateCaption.TabIndex = 4;
        lblBaudRateCaption.Text = "波特率";
        // 
        // cmbBaudRate
        // 
        cmbBaudRate.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbBaudRate.FormattingEnabled = true;
        cmbBaudRate.Location = new Point(472, 10);
        cmbBaudRate.Margin = new Padding(4);
        cmbBaudRate.Name = "cmbBaudRate";
        cmbBaudRate.Size = new Size(110, 31);
        cmbBaudRate.TabIndex = 5;
        // 
        // lblParityCaption
        // 
        lblParityCaption.AutoSize = true;
        lblParityCaption.Location = new Point(590, 13);
        lblParityCaption.Margin = new Padding(4, 7, 2, 0);
        lblParityCaption.Name = "lblParityCaption";
        lblParityCaption.Size = new Size(64, 24);
        lblParityCaption.TabIndex = 6;
        lblParityCaption.Text = "校验位";
        // 
        // cmbParity
        // 
        cmbParity.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbParity.FormattingEnabled = true;
        cmbParity.Location = new Point(660, 10);
        cmbParity.Margin = new Padding(4);
        cmbParity.Name = "cmbParity";
        cmbParity.Size = new Size(90, 31);
        cmbParity.TabIndex = 7;
        // 
        // lblStopBitsCaption
        // 
        lblStopBitsCaption.AutoSize = true;
        lblStopBitsCaption.Location = new Point(758, 13);
        lblStopBitsCaption.Margin = new Padding(4, 7, 2, 0);
        lblStopBitsCaption.Name = "lblStopBitsCaption";
        lblStopBitsCaption.Size = new Size(64, 24);
        lblStopBitsCaption.TabIndex = 8;
        lblStopBitsCaption.Text = "停止位";
        // 
        // cmbStopBits
        // 
        cmbStopBits.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbStopBits.FormattingEnabled = true;
        cmbStopBits.Location = new Point(828, 10);
        cmbStopBits.Margin = new Padding(4);
        cmbStopBits.Name = "cmbStopBits";
        cmbStopBits.Size = new Size(76, 31);
        cmbStopBits.TabIndex = 9;
        // 
        // btnConnect
        // 
        btnConnect.AutoSize = true;
        btnConnect.Location = new Point(912, 8);
        btnConnect.Margin = new Padding(4, 2, 4, 2);
        btnConnect.Name = "btnConnect";
        btnConnect.Padding = new Padding(10, 6, 10, 6);
        btnConnect.Size = new Size(76, 46);
        btnConnect.TabIndex = 10;
        btnConnect.Text = "连接";
        btnConnect.UseVisualStyleBackColor = true;
        btnConnect.Click += BtnConnect_Click;
        // 
        // btnDisconnect
        // 
        btnDisconnect.AutoSize = true;
        btnDisconnect.Enabled = false;
        btnDisconnect.Location = new Point(996, 8);
        btnDisconnect.Margin = new Padding(4, 2, 10, 2);
        btnDisconnect.Name = "btnDisconnect";
        btnDisconnect.Padding = new Padding(10, 6, 10, 6);
        btnDisconnect.Size = new Size(76, 46);
        btnDisconnect.TabIndex = 11;
        btnDisconnect.Text = "断开";
        btnDisconnect.UseVisualStyleBackColor = true;
        btnDisconnect.Click += BtnDisconnect_Click;
        // 
        // lblAddressRangeCaption
        // 
        lblAddressRangeCaption.AutoSize = true;
        lblAddressRangeCaption.Location = new Point(1086, 13);
        lblAddressRangeCaption.Margin = new Padding(4, 7, 2, 0);
        lblAddressRangeCaption.Name = "lblAddressRangeCaption";
        lblAddressRangeCaption.Size = new Size(82, 24);
        lblAddressRangeCaption.TabIndex = 12;
        lblAddressRangeCaption.Text = "模块范围";
        // 
        // nudAddressStart
        // 
        nudAddressStart.Location = new Point(1174, 10);
        nudAddressStart.Margin = new Padding(4);
        nudAddressStart.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
        nudAddressStart.Name = "nudAddressStart";
        nudAddressStart.Size = new Size(58, 31);
        nudAddressStart.TabIndex = 13;
        // 
        // lblAddressRangeSeparator
        // 
        lblAddressRangeSeparator.AutoSize = true;
        lblAddressRangeSeparator.Location = new Point(1240, 13);
        lblAddressRangeSeparator.Margin = new Padding(4, 7, 4, 0);
        lblAddressRangeSeparator.Name = "lblAddressRangeSeparator";
        lblAddressRangeSeparator.Size = new Size(23, 24);
        lblAddressRangeSeparator.TabIndex = 14;
        lblAddressRangeSeparator.Text = "~";
        // 
        // nudAddressEnd
        // 
        nudAddressEnd.Location = new Point(1271, 10);
        nudAddressEnd.Margin = new Padding(4);
        nudAddressEnd.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
        nudAddressEnd.Name = "nudAddressEnd";
        nudAddressEnd.Size = new Size(58, 31);
        nudAddressEnd.TabIndex = 15;
        nudAddressEnd.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // btnAutoScan
        // 
        btnAutoScan.AutoSize = true;
        btnAutoScan.Location = new Point(14, 60);
        btnAutoScan.Margin = new Padding(6, 4, 4, 2);
        btnAutoScan.Name = "btnAutoScan";
        btnAutoScan.Padding = new Padding(10, 6, 10, 6);
        btnAutoScan.Size = new Size(112, 46);
        btnAutoScan.TabIndex = 16;
        btnAutoScan.Text = "自动扫描";
        btnAutoScan.UseVisualStyleBackColor = true;
        btnAutoScan.Click += BtnAutoScan_Click;
        // 
        // btnAddUnit
        // 
        btnAddUnit.AutoSize = true;
        btnAddUnit.Location = new Point(134, 60);
        btnAddUnit.Margin = new Padding(4, 4, 10, 2);
        btnAddUnit.Name = "btnAddUnit";
        btnAddUnit.Padding = new Padding(10, 6, 10, 6);
        btnAddUnit.Size = new Size(130, 46);
        btnAddUnit.TabIndex = 17;
        btnAddUnit.Text = "新增线控器";
        btnAddUnit.UseVisualStyleBackColor = true;
        btnAddUnit.Click += BtnAddUnit_Click;
        // 
        // lblStatusDashboard
        // 
        lblStatusDashboard.AutoSize = true;
        lblStatusDashboard.ForeColor = Color.Red;
        lblStatusDashboard.Location = new Point(278, 65);
        lblStatusDashboard.Margin = new Padding(4, 9, 4, 0);
        lblStatusDashboard.Name = "lblStatusDashboard";
        lblStatusDashboard.Size = new Size(64, 24);
        lblStatusDashboard.TabIndex = 18;
        lblStatusDashboard.Text = "未连接";
        // 
        // lblControlScopeCaption
        // 
        lblControlScopeCaption.AutoSize = true;
        lblControlScopeCaption.Location = new Point(354, 65);
        lblControlScopeCaption.Margin = new Padding(8, 9, 2, 0);
        lblControlScopeCaption.Name = "lblControlScopeCaption";
        lblControlScopeCaption.Size = new Size(46, 24);
        lblControlScopeCaption.TabIndex = 19;
        lblControlScopeCaption.Text = "范围";
        // 
        // cmbControlScope
        // 
        cmbControlScope.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbControlScope.FormattingEnabled = true;
        cmbControlScope.Location = new Point(406, 60);
        cmbControlScope.Margin = new Padding(4, 4, 8, 2);
        cmbControlScope.Name = "cmbControlScope";
        cmbControlScope.Size = new Size(104, 31);
        cmbControlScope.TabIndex = 20;
        // 
        // lblControlModeCaption
        // 
        lblControlModeCaption.AutoSize = true;
        lblControlModeCaption.Location = new Point(526, 65);
        lblControlModeCaption.Margin = new Padding(8, 9, 2, 0);
        lblControlModeCaption.Name = "lblControlModeCaption";
        lblControlModeCaption.Size = new Size(46, 24);
        lblControlModeCaption.TabIndex = 21;
        lblControlModeCaption.Text = "模式";
        // 
        // cmbControlMode
        // 
        cmbControlMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbControlMode.FormattingEnabled = true;
        cmbControlMode.Location = new Point(578, 60);
        cmbControlMode.Margin = new Padding(4, 4, 8, 2);
        cmbControlMode.Name = "cmbControlMode";
        cmbControlMode.Size = new Size(96, 31);
        cmbControlMode.TabIndex = 22;
        // 
        // lblControlTempCaption
        // 
        lblControlTempCaption.AutoSize = true;
        lblControlTempCaption.Location = new Point(690, 65);
        lblControlTempCaption.Margin = new Padding(8, 9, 2, 0);
        lblControlTempCaption.Name = "lblControlTempCaption";
        lblControlTempCaption.Size = new Size(46, 24);
        lblControlTempCaption.TabIndex = 23;
        lblControlTempCaption.Text = "设温";
        // 
        // nudControlTemp
        // 
        nudControlTemp.Location = new Point(742, 60);
        nudControlTemp.Margin = new Padding(4, 4, 8, 2);
        nudControlTemp.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        nudControlTemp.Name = "nudControlTemp";
        nudControlTemp.Size = new Size(70, 31);
        nudControlTemp.TabIndex = 24;
        nudControlTemp.Value = new decimal(new int[] { 45, 0, 0, 0 });
        // 
        // btnApplyControl
        // 
        btnApplyControl.AutoSize = true;
        btnApplyControl.Location = new Point(952, 58);
        btnApplyControl.Margin = new Padding(4, 2, 4, 2);
        btnApplyControl.Name = "btnApplyControl";
        btnApplyControl.Padding = new Padding(10, 6, 10, 6);
        btnApplyControl.Size = new Size(112, 46);
        btnApplyControl.TabIndex = 27;
        btnApplyControl.Text = "写入控制";
        btnApplyControl.UseVisualStyleBackColor = true;
        btnApplyControl.Click += BtnApplyControl_Click;
        // 
        // btnClearFault
        // 
        btnClearFault.AutoSize = true;
        btnClearFault.Location = new Point(1208, 58);
        btnClearFault.Margin = new Padding(4, 2, 4, 2);
        btnClearFault.Name = "btnClearFault";
        btnClearFault.Padding = new Padding(10, 6, 10, 6);
        btnClearFault.Size = new Size(112, 46);
        btnClearFault.TabIndex = 28;
        btnClearFault.Text = "清除故障";
        btnClearFault.UseVisualStyleBackColor = true;
        btnClearFault.Click += BtnClearFault_Click;
        // 
        // pnlControlMessage
        // 
        pnlControlMessage.BackColor = Color.FromArgb(255, 252, 235);
        pnlControlMessage.Controls.Add(lblControlMessage);
        pnlControlMessage.Dock = DockStyle.Fill;
        pnlControlMessage.Location = new Point(0, 122);
        pnlControlMessage.Margin = new Padding(0);
        pnlControlMessage.Name = "pnlControlMessage";
        pnlControlMessage.Padding = new Padding(12, 8, 12, 8);
        pnlControlMessage.Size = new Size(1440, 40);
        pnlControlMessage.TabIndex = 1;
        // 
        // lblControlMessage
        // 
        lblControlMessage.AutoSize = true;
        lblControlMessage.ForeColor = Color.FromArgb(120, 90, 20);
        lblControlMessage.Location = new Point(24, 16);
        lblControlMessage.Name = "lblControlMessage";
        lblControlMessage.Size = new Size(226, 24);
        lblControlMessage.TabIndex = 0;
        lblControlMessage.Text = "请选择线控器后再下发控制";
        // 
        // flpDashboard
        // 
        flpDashboard.AutoScroll = true;
        flpDashboard.BackColor = Color.FromArgb(245, 245, 245);
        flpDashboard.Dock = DockStyle.Fill;
        flpDashboard.FlowDirection = FlowDirection.TopDown;
        flpDashboard.Location = new Point(0, 162);
        flpDashboard.Margin = new Padding(0);
        flpDashboard.Name = "flpDashboard";
        flpDashboard.Padding = new Padding(10, 8, 10, 10);
        flpDashboard.Size = new Size(1440, 658);
        flpDashboard.TabIndex = 1;
        flpDashboard.WrapContents = false;
        flpDashboard.SizeChanged += FlpDashboard_SizeChanged;
        // 
        // dashboardRefreshTimer
        // 
        dashboardRefreshTimer.Interval = 1500;
        dashboardRefreshTimer.Tick += DashboardRefreshTimer_Tick;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(10F, 23F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 820);
        Controls.Add(tlpDashboardRoot);
        Font = new Font("微软雅黑", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
        MinimumSize = new Size(1260, 760);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "热泵线控器管理系统";
        Load += MainForm_Load;
        tlpDashboardRoot.ResumeLayout(false);
        tlpDashboardRoot.PerformLayout();
        flpDashboardToolbar.ResumeLayout(false);
        flpDashboardToolbar.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudSlaveAddress).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAddressStart).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudAddressEnd).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudControlTemp).EndInit();
        pnlControlMessage.ResumeLayout(false);
        pnlControlMessage.PerformLayout();
        ResumeLayout(false);
    }
}
