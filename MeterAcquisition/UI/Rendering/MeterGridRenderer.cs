using System.Windows.Forms;

namespace MeterAcquisition
{
    internal static class MeterGridRenderer
    {
        public static void InitializeRealTimeGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            grid.Rows.Add("A相电压", string.Empty, "V");
            grid.Rows.Add("B相电压", string.Empty, "V");
            grid.Rows.Add("C相电压", string.Empty, "V");
            grid.Rows.Add("AB线电压", string.Empty, "V");
            grid.Rows.Add("BC线电压", string.Empty, "V");
            grid.Rows.Add("CA线电压", string.Empty, "V");
            grid.Rows.Add("A相电流", string.Empty, "A");
            grid.Rows.Add("B相电流", string.Empty, "A");
            grid.Rows.Add("C相电流", string.Empty, "A");
            grid.Rows.Add("三相有功功率", string.Empty, "W");
            grid.Rows.Add("三相无功功率", string.Empty, "var");
            grid.Rows.Add("三相视在功率", string.Empty, "VA");
            grid.Rows.Add("总功率因数", string.Empty, string.Empty);
            grid.Rows.Add("频率", string.Empty, "Hz");
        }

        public static void InitializeEnergyGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            grid.Rows.Add("正向有功电能", string.Empty, "kWh");
            grid.Rows.Add("反向有功电能", string.Empty, "kWh");
            grid.Rows.Add("正向无功电能", string.Empty, "kvarh");
            grid.Rows.Add("反向无功电能", string.Empty, "kvarh");
        }

        public static void InitializeQualityGrid(DataGridView grid)
        {
            grid.Rows.Clear();
            grid.Rows.Add("A相电流THD", string.Empty, "%");
            grid.Rows.Add("B相电流THD", string.Empty, "%");
            grid.Rows.Add("C相电流THD", string.Empty, "%");
            grid.Rows.Add("A相电压THD", string.Empty, "%");
            grid.Rows.Add("B相电压THD", string.Empty, "%");
            grid.Rows.Add("C相电压THD", string.Empty, "%");
            grid.Rows.Add("电压不平衡度", string.Empty, "%");
            grid.Rows.Add("电流不平衡度", string.Empty, "%");
        }

        public static void UpdateRealTimeGrid(DataGridView grid, RealTimeData data)
        {
            if (grid.Rows.Count < 14)
                return;

            if (data == null)
            {
                ClearGridValues(grid);
                return;
            }

            grid.Rows[0].Cells[1].Value = Fmt(data.VoltageA, "F2");
            grid.Rows[1].Cells[1].Value = Fmt(data.VoltageB, "F2");
            grid.Rows[2].Cells[1].Value = Fmt(data.VoltageC, "F2");
            grid.Rows[3].Cells[1].Value = Fmt(data.VoltageAB, "F2");
            grid.Rows[4].Cells[1].Value = Fmt(data.VoltageBC, "F2");
            grid.Rows[5].Cells[1].Value = Fmt(data.VoltageCA, "F2");
            grid.Rows[6].Cells[1].Value = Fmt(data.CurrentA, "F2");
            grid.Rows[7].Cells[1].Value = Fmt(data.CurrentB, "F2");
            grid.Rows[8].Cells[1].Value = Fmt(data.CurrentC, "F2");
            grid.Rows[9].Cells[1].Value = Fmt(data.ActivePowerTotal, "F2");
            grid.Rows[10].Cells[1].Value = Fmt(data.ReactivePowerTotal, "F2");
            grid.Rows[11].Cells[1].Value = Fmt(data.ApparentPowerTotal, "F2");
            grid.Rows[12].Cells[1].Value = Fmt(data.PowerFactorTotal, "F3");
            grid.Rows[13].Cells[1].Value = Fmt(data.Frequency, "F2");
        }

        public static void UpdateEnergyGrid(DataGridView grid, EnergyData data)
        {
            if (grid.Rows.Count < 4)
                return;

            if (data == null)
            {
                ClearGridValues(grid);
                return;
            }

            grid.Rows[0].Cells[1].Value = Fmt(data.ForwardActiveEnergy, "F2");
            grid.Rows[1].Cells[1].Value = Fmt(data.ReverseActiveEnergy, "F2");
            grid.Rows[2].Cells[1].Value = Fmt(data.ForwardReactiveEnergy, "F2");
            grid.Rows[3].Cells[1].Value = Fmt(data.ReverseReactiveEnergy, "F2");
        }

        public static void UpdateQualityGrid(DataGridView grid, PowerQualityData data)
        {
            if (grid.Rows.Count < 8)
                return;

            if (data == null)
            {
                ClearGridValues(grid);
                return;
            }

            grid.Rows[0].Cells[1].Value = Fmt(data.CurrentTHDA, "F2");
            grid.Rows[1].Cells[1].Value = Fmt(data.CurrentTHDB, "F2");
            grid.Rows[2].Cells[1].Value = Fmt(data.CurrentTHDC, "F2");
            grid.Rows[3].Cells[1].Value = Fmt(data.VoltageTHDA, "F2");
            grid.Rows[4].Cells[1].Value = Fmt(data.VoltageTHDB, "F2");
            grid.Rows[5].Cells[1].Value = Fmt(data.VoltageTHDC, "F2");
            grid.Rows[6].Cells[1].Value = Fmt(data.VoltageUnbalance, "F2");
            grid.Rows[7].Cells[1].Value = Fmt(data.CurrentUnbalance, "F2");
        }

        /// <summary>
        /// P1-5：字段改为可空后，缺数据统一显示 "--"，不再把"没采到"渲染成 0。
        /// NaN / 无穷也按缺数据处理 —— 那是解析异常，不是测量值。
        /// </summary>
        private static string Fmt(float? value, string format)
        {
            if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            {
                return "--";
            }

            return value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void ClearGridValues(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
                row.Cells[1].Value = string.Empty;
        }
    }
}
