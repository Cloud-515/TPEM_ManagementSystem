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

            grid.Rows[0].Cells[1].Value = data.VoltageA.ToString("F2");
            grid.Rows[1].Cells[1].Value = data.VoltageB.ToString("F2");
            grid.Rows[2].Cells[1].Value = data.VoltageC.ToString("F2");
            grid.Rows[3].Cells[1].Value = data.VoltageAB.ToString("F2");
            grid.Rows[4].Cells[1].Value = data.VoltageBC.ToString("F2");
            grid.Rows[5].Cells[1].Value = data.VoltageCA.ToString("F2");
            grid.Rows[6].Cells[1].Value = data.CurrentA.ToString("F2");
            grid.Rows[7].Cells[1].Value = data.CurrentB.ToString("F2");
            grid.Rows[8].Cells[1].Value = data.CurrentC.ToString("F2");
            grid.Rows[9].Cells[1].Value = data.ActivePowerTotal.ToString("F2");
            grid.Rows[10].Cells[1].Value = data.ReactivePowerTotal.ToString("F2");
            grid.Rows[11].Cells[1].Value = data.ApparentPowerTotal.ToString("F2");
            grid.Rows[12].Cells[1].Value = data.PowerFactorTotal.ToString("F3");
            grid.Rows[13].Cells[1].Value = data.Frequency.ToString("F2");
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

            grid.Rows[0].Cells[1].Value = data.ForwardActiveEnergy.ToString("F2");
            grid.Rows[1].Cells[1].Value = data.ReverseActiveEnergy.ToString("F2");
            grid.Rows[2].Cells[1].Value = data.ForwardReactiveEnergy.ToString("F2");
            grid.Rows[3].Cells[1].Value = data.ReverseReactiveEnergy.ToString("F2");
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

            grid.Rows[0].Cells[1].Value = data.CurrentTHDA.ToString("F2");
            grid.Rows[1].Cells[1].Value = data.CurrentTHDB.ToString("F2");
            grid.Rows[2].Cells[1].Value = data.CurrentTHDC.ToString("F2");
            grid.Rows[3].Cells[1].Value = data.VoltageTHDA.ToString("F2");
            grid.Rows[4].Cells[1].Value = data.VoltageTHDB.ToString("F2");
            grid.Rows[5].Cells[1].Value = data.VoltageTHDC.ToString("F2");
            grid.Rows[6].Cells[1].Value = data.VoltageUnbalance.ToString("F2");
            grid.Rows[7].Cells[1].Value = data.CurrentUnbalance.ToString("F2");
        }

        private static void ClearGridValues(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
                row.Cells[1].Value = string.Empty;
        }
    }
}
