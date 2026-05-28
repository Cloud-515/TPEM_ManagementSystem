function FloatToRegs($v) {
    $b = [BitConverter]::GetBytes([single]$v)
    [Array]::Reverse($b)
    @(($b[0] -shl 8) + $b[1], ($b[2] -shl 8) + $b[3])
}
function Int32ToRegs($v) {
    @(($v -shr 16) -band 0xFFFF, $v -band 0xFFFF)
}

# Meter data - SlaveID, V_A,V_B,V_C,V_AB,V_BC,V_CA,I_A,I_B,I_C,P_total,Q_total,S_total,PF,Freq, FwdEn,RevEn,FwdRe,RevRe, IT_A,IT_B,IT_C,VT_A,VT_B,VT_C,VU,IU
$data = @(
    @(95,  220.0,219.5,220.3,381.0,380.2,381.5,35.2,34.8,35.5,45500,15200,48000,0.95,50.0, 12345,0,5678,100, 0.028,0.032,0.025,0.015,0.018,0.012,0.005,0.030),
    @(100, 221.0,220.5,221.2,382.5,381.8,382.0,28.5,27.9,28.8,36500,12100,38500,0.93,49.98, 5678,0,2345,0, 0.035,0.038,0.030,0.012,0.015,0.010,0.004,0.035),
    @(101, 218.5,219.0,218.8,378.5,379.2,378.0,12.3,12.1,12.5,8200,3100,8800,0.91,50.02, 34567,0,12345,0, 0.045,0.042,0.048,0.020,0.022,0.018,0.006,0.050),
    @(102, 222.0,221.5,222.3,384.0,383.5,384.2,45.8,46.2,45.5,52000,18000,55000,0.94,49.95, 89012,0,34567,0, 0.025,0.028,0.022,0.010,0.012,0.009,0.003,0.028)
)

$R = "MB8_Register"
$rd = "dt:dt=`"int`""

$xml = [System.Text.StringBuilder]::new()
[void]$xml.Append('<?xml version="1.0" encoding="UTF-8"?>')
[void]$xml.Append("`n<Document Format=`"Modbus Slave Document`" Version=`"1.0`" xmlns:dt=`"urn:schemas-microsoft-com:datatypes`">")
[void]$xml.Append("`n  <MB8_SerialLine><TransmitMode dt:dt=`"string`">RTU</TransmitMode></MB8_SerialLine>")
[void]$xml.Append("`n  <MB8_SlaveList>")

foreach ($d in $data) {
    $sid = $d[0]
    [void]$xml.Append("`n    <MB8_Slave><SlaveId dt:dt=`"int`">$sid</SlaveId><MB8_FunctionList>")

    # ===== Real-time data: 0x1581, 58 regs =====
    [void]$xml.Append("`n      <MB8_Function><FunctionCode dt:dt=`"int`">3</FunctionCode><StartAddress dt:dt=`"hex`">1581</StartAddress><Quantity dt:dt=`"int`">58</Quantity><${R}_List>")
    for ($i = 1; $i -le 14; $i++) { $r = FloatToRegs($d[$i]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") }
    [void]$xml.Append("<${R} ${rd}>0</${R}><${R} ${rd}>0</${R}>") # skip avg V
    for ($i = 15; $i -le 17; $i++) { $r = FloatToRegs($d[$i]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") }
    [void]$xml.Append("<${R} ${rd}>0</${R}><${R} ${rd}>0</${R}>") # skip avg I
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip phase P
    $r = FloatToRegs($d[18]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") # P_total
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip phase Q
    $r = FloatToRegs($d[19]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") # Q_total
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip phase S
    $r = FloatToRegs($d[20]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") # S_total
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip phase PF
    $r = FloatToRegs($d[21]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") # PF
    $r = FloatToRegs($d[22]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") # Freq
    [void]$xml.Append("</${R}_List></MB8_Function>")

    # ===== Energy data: 0x01F4, 12 regs =====
    [void]$xml.Append("`n      <MB8_Function><FunctionCode dt:dt=`"int`">3</FunctionCode><StartAddress dt:dt=`"hex`">01F4</StartAddress><Quantity dt:dt=`"int`">12</Quantity><${R}_List>")
    $r = Int32ToRegs($d[23]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    $r = Int32ToRegs($d[24]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    [void]$xml.Append("<${R} ${rd}>0</${R}><${R} ${rd}>0</${R}>") # skip
    $r = Int32ToRegs($d[25]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    $r = Int32ToRegs($d[26]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    [void]$xml.Append("</${R}_List></MB8_Function>")

    # ===== Power quality: 0x0514, 60 regs =====
    [void]$xml.Append("`n      <MB8_Function><FunctionCode dt:dt=`"int`">3</FunctionCode><StartAddress dt:dt=`"hex`">0514</StartAddress><Quantity dt:dt=`"int`">60</Quantity><${R}_List>")
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip 3 distortion
    for ($i = 27; $i -le 29; $i++) { $r = FloatToRegs($d[$i]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") }
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 12) # skip 24 bytes
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 6)  # skip 12 bytes
    for ($i = 30; $i -le 32; $i++) { $r = FloatToRegs($d[$i]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>") }
    [void]$xml.Append("<${R} ${rd}>0</${R}>" * 20) # skip 40 bytes
    $r = FloatToRegs($d[33]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    $r = FloatToRegs($d[34]); [void]$xml.Append("<${R} ${rd}>$($r[0])</${R}><${R} ${rd}>$($r[1])</${R}>")
    [void]$xml.Append("</${R}_List></MB8_Function>")

    [void]$xml.Append("`n    </MB8_FunctionList></MB8_Slave>")
}
[void]$xml.Append("`n  </MB8_SlaveList>`n</Document>")

$out = Join-Path $PSScriptRoot "MetersSimulation.mbslave"
[System.IO.File]::WriteAllText($out, $xml.ToString(), [System.Text.UTF8Encoding]::new($true))
Write-Host "OK! $out"
