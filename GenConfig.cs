using System;
using System.IO;
using System.Text;

class GenConfig
{
    static byte[] FloatBE(float v)
    {
        byte[] b = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    static int B16(byte[] b, int off) { return (b[off] << 8) | b[off + 1]; }

    static void AddF(StringBuilder sb, string indent, float val)
    {
        var be = FloatBE(val);
        sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">" + B16(be, 0) + "</MB8_Register>");
        sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">" + B16(be, 2) + "</MB8_Register>");
    }

    static void AddI(StringBuilder sb, string indent, int val)
    {
        sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">" + val + "</MB8_Register>");
    }

    static void AddI32(StringBuilder sb, string indent, int val)
    {
        int high = (val >> 16) & 0xFFFF;
        int low = val & 0xFFFF;
        sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">" + high + "</MB8_Register>");
        sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">" + low + "</MB8_Register>");
    }

    static void Add0(StringBuilder sb, string indent, int count)
    {
        for (int i = 0; i < count; i++)
            sb.AppendLine(indent + "<MB8_Register dt:dt=\"int\">0</MB8_Register>");
    }

    static void WriteSlave(StringBuilder sb, int sid, string name, float[] f, int[] rtCols, float pt, float qt, float st, float pf, float fq, int[] enV, int[] qCols, float[] qV)
    {
        string ind2 = "            ";
        sb.AppendLine("    <MB8_Slave>");
        sb.AppendLine("      <SlaveId dt:dt=\"int\">" + sid + "</SlaveId>");
        sb.AppendLine("      <Name dt:dt=\"string\">" + name + "</Name>");
        sb.AppendLine("      <MB8_FunctionList>");

        // Real-time: 0x1581, 58 regs
        sb.AppendLine("        <MB8_Function>");
        sb.AppendLine("          <FunctionCode dt:dt=\"int\">3</FunctionCode>");
        sb.AppendLine("          <StartAddress dt:dt=\"hex\">1581</StartAddress>");
        sb.AppendLine("          <Quantity dt:dt=\"int\">58</Quantity>");
        sb.AppendLine("          <MB8_RegisterList>");
        for (int i = 0; i < 3; i++) AddF(sb, ind2, f[rtCols[i]]);    // Va,Vb,Vc
        Add0(sb, ind2, 2);                                            // avg V placeholder
        for (int i = 3; i < 6; i++) AddF(sb, ind2, f[rtCols[i]]);    // Iab,Ibc,Ica
        Add0(sb, ind2, 2);                                            // avg I placeholder
        for (int i = 6; i < 9; i++) AddF(sb, ind2, f[rtCols[i]]);    // Wa,Wb,Wc
        Add0(sb, ind2, 6);                                            // W total etc.
        AddF(sb, ind2, pt);                                           // Pt total
        Add0(sb, ind2, 6);
        AddF(sb, ind2, qt);
        Add0(sb, ind2, 6);
        AddF(sb, ind2, st);
        Add0(sb, ind2, 6);
        AddF(sb, ind2, pf);
        AddF(sb, ind2, fq);
        sb.AppendLine("          </MB8_RegisterList>");
        sb.AppendLine("        </MB8_Function>");

        // Energy: 0x01F4, 12 regs
        sb.AppendLine("        <MB8_Function>");
        sb.AppendLine("          <FunctionCode dt:dt=\"int\">3</FunctionCode>");
        sb.AppendLine("          <StartAddress dt:dt=\"hex\">01F4</StartAddress>");
        sb.AppendLine("          <Quantity dt:dt=\"int\">12</Quantity>");
        sb.AppendLine("          <MB8_RegisterList>");
        for (int i = 0; i < enV.Length; i++) AddI32(sb, ind2, enV[i]);
        Add0(sb, ind2, 4);
        sb.AppendLine("          </MB8_RegisterList>");
        sb.AppendLine("        </MB8_Function>");

        // Quality: 0x0514, 60 regs
        sb.AppendLine("        <MB8_Function>");
        sb.AppendLine("          <FunctionCode dt:dt=\"int\">3</FunctionCode>");
        sb.AppendLine("          <StartAddress dt:dt=\"hex\">0514</StartAddress>");
        sb.AppendLine("          <Quantity dt:dt=\"int\">60</Quantity>");
        sb.AppendLine("          <MB8_RegisterList>");
        Add0(sb, ind2, 6);
        for (int i = 0; i < 3; i++) AddF(sb, ind2, qV[qCols[i]]);
        Add0(sb, ind2, 18);
        for (int i = 3; i < 6; i++) AddF(sb, ind2, qV[qCols[i]]);
        Add0(sb, ind2, 20);
        for (int i = 6; i < 8; i++) AddF(sb, ind2, qV[qCols[i]]);
        sb.AppendLine("          </MB8_RegisterList>");
        sb.AppendLine("        </MB8_Function>");

        sb.AppendLine("      </MB8_FunctionList>");
        sb.AppendLine("    </MB8_Slave>");
    }

    static void Main()
    {
        // f[] layout: 0-13 real-time floats, 14-17 energy(i32), 18-25 quality floats
        var m95 = new float[] { 220.0f,219.5f,220.3f,381.0f,380.2f,381.5f,35.2f,34.8f,35.5f,45500f,15200f,48000f,0.95f,50.0f, 12345,0,5678,100, 0.028f,0.032f,0.025f,0.015f,0.018f,0.012f,0.005f,0.030f };
        var m100 = new float[] { 221.0f,220.5f,221.2f,382.5f,381.8f,382.0f,28.5f,27.9f,28.8f,36500f,12100f,38500f,0.93f,49.98f, 5678,0,2345,0, 0.035f,0.038f,0.030f,0.012f,0.015f,0.010f,0.004f,0.035f };
        var m101 = new float[] { 218.5f,219.0f,218.8f,378.5f,379.2f,378.0f,12.3f,12.1f,12.5f,8200f,3100f,8800f,0.91f,50.02f, 34567,0,12345,0, 0.045f,0.042f,0.048f,0.020f,0.022f,0.018f,0.006f,0.050f };
        var m102 = new float[] { 222.0f,221.5f,222.3f,384.0f,383.5f,384.2f,45.8f,46.2f,45.5f,52000f,18000f,55000f,0.94f,49.95f, 89012,0,34567,0, 0.025f,0.028f,0.022f,0.010f,0.012f,0.009f,0.003f,0.028f };

        int[] rtCols = { 0,1,2, 3,4,5, 6,7,8 };
        int[] qCols = { 18,19,20, 21,22,23, 24,25 };

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<Document Format=\"Modbus Slave Document\" Version=\"1.0\" xmlns:dt=\"urn:schemas-microsoft-com:datatypes\">");
        sb.AppendLine("  <MB8_SerialLine>");
        sb.AppendLine("    <TransmitMode dt:dt=\"string\">RTU</TransmitMode>");
        sb.AppendLine("  </MB8_SerialLine>");
        sb.AppendLine("  <MB8_SlaveList>");

        WriteSlave(sb, 95, "一号主水泵", m95, rtCols, 45500f, 15200f, 48000f, 0.95f, 50.0f, new int[] {12345,0,5678,100}, qCols, m95);
        WriteSlave(sb, 100, "二号主水泵", m100, rtCols, 36500f, 12100f, 38500f, 0.93f, 49.98f, new int[] {5678,0,2345,0}, qCols, m100);
        WriteSlave(sb, 101, "照明回路", m101, rtCols, 8200f, 3100f, 8800f, 0.91f, 50.02f, new int[] {34567,0,12345,0}, qCols, m101);
        WriteSlave(sb, 102, "空调机组", m102, rtCols, 52000f, 18000f, 55000f, 0.94f, 49.95f, new int[] {89012,0,34567,0}, qCols, m102);

        sb.AppendLine("  </MB8_SlaveList>");
        sb.AppendLine("</Document>");

        File.WriteAllText("MetersSimulation.mbslave", sb.ToString(), Encoding.UTF8);
        Console.WriteLine("OK! " + Path.GetFullPath("MetersSimulation.mbslave"));
    }
}
