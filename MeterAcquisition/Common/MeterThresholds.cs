using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Npgsql;
using Tpem.Diagnostics;

namespace Tpem.Thresholds
{
    /// <summary>
    /// 一条判定规则，对应 meter_threshold 表的一行。
    /// </summary>
    public sealed class ThresholdRule
    {
        public string MetricCode { get; set; }
        public string MetricName { get; set; }
        public string Unit { get; set; }
        public float? MinValue { get; set; }
        public float? MaxValue { get; set; }

        /// <summary>滞回余量。低限需回升到 Min + Margin 才恢复，高限需回落到 Max - Margin 才恢复。</summary>
        public float RecoverMargin { get; set; }

        /// <summary>连续命中多少次才置位报警。1 表示不去抖。</summary>
        public int DebounceCount { get; set; }

        /// <summary>是否要求"已带电"才参与判定。停电/空载时 PF 与电压天然为 0。</summary>
        public bool RequireEnergized { get; set; }

        public string AlarmCodeLow { get; set; }
        public string AlarmCodeHigh { get; set; }
        public string AlarmNameLow { get; set; }
        public string AlarmNameHigh { get; set; }
        public string AlarmLevel { get; set; }
        public bool IsAlarmEnabled { get; set; }

        public ThresholdRule()
        {
            DebounceCount = 1;
            RequireEnergized = true;
            AlarmLevel = "warning";
            IsAlarmEnabled = true;
        }
    }
    /// <summary>
    /// 某台电表最终生效的规则集合（已按 meter &gt; box &gt; site &gt; global 完成覆盖计算）。
    /// </summary>
    public sealed class ThresholdSet
    {
        public const string VoltagePhase = "voltage_phase";
        public const string PowerFactorTotal = "power_factor_total";
        public const string CurrentPhase = "current_phase";
        public const string VoltageThd = "voltage_thd";
        public const string CurrentThd = "current_thd";
        public const string VoltageUnbalance = "voltage_unbalance";
        public const string CurrentUnbalance = "current_unbalance";
        public const string Frequency = "frequency";
        public const string EnergizedVoltageMin = "energized_voltage_min";

        private readonly Dictionary<string, ThresholdRule> _rules;

        public ThresholdSet(IEnumerable<ThresholdRule> rules)
        {
            _rules = new Dictionary<string, ThresholdRule>(StringComparer.OrdinalIgnoreCase);
            if (rules == null)
            {
                return;
            }

            foreach (var rule in rules)
            {
                if (rule != null && !string.IsNullOrWhiteSpace(rule.MetricCode))
                {
                    _rules[rule.MetricCode] = rule;
                }
            }
        }

        public ThresholdRule Get(string metricCode)
        {
            ThresholdRule rule;
            return _rules.TryGetValue(metricCode ?? string.Empty, out rule) ? rule : null;
        }

        /// <summary>带电门槛。三相电压都低于它就认为未带电。取不到配置时用 50V。</summary>
        public float EnergizedThreshold
        {
            get
            {
                var rule = Get(EnergizedVoltageMin);
                return rule != null && rule.MinValue.HasValue ? rule.MinValue.Value : 50f;
            }
        }
    }
    /// <summary>
    /// 判定引擎的输入视图。上位机与入库服务各自把自己的 DTO 映射到这里，
    /// 保证两端喂给判定引擎的是同一种形状 —— 这是"判定口径唯一"的关键。
    /// 全部为可空：null 表示本次没有采到该值，不参与数值判定（与判定标准文档一致）。
    /// </summary>
    public sealed class MeterSampleView
    {
        public float? VoltageA { get; set; }
        public float? VoltageB { get; set; }
        public float? VoltageC { get; set; }
        public float? CurrentA { get; set; }
        public float? CurrentB { get; set; }
        public float? CurrentC { get; set; }
        public float? PowerFactorTotal { get; set; }
        public float? Frequency { get; set; }
        public float? VoltageThdA { get; set; }
        public float? VoltageThdB { get; set; }
        public float? VoltageThdC { get; set; }
        public float? CurrentThdA { get; set; }
        public float? CurrentThdB { get; set; }
        public float? CurrentThdC { get; set; }
        public float? VoltageUnbalance { get; set; }
        public float? CurrentUnbalance { get; set; }
    }

    /// <summary>一次判定命中。</summary>
    public sealed class ThresholdHit
    {
        public string MetricCode { get; set; }
        public string AlarmCode { get; set; }
        public string AlarmName { get; set; }
        public string AlarmLevel { get; set; }

        /// <summary>是否为 critical 级别。上位机据此把状态显示成红色而非橙色。</summary>
        public bool AlarmLevelIsCritical { get; set; }

        /// <summary>true = 超上限，false = 低于下限。</summary>
        public bool IsHigh { get; set; }

        public float Value { get; set; }
        public float Threshold { get; set; }
        public float RecoverMargin { get; set; }
        public int DebounceCount { get; set; }

        /// <summary>该项是否配置为产生报警事件。false 时只用于界面风险展示。</summary>
        public bool IsAlarmEnabled { get; set; }
    }
    /// <summary>
    /// 判定引擎（P1-1）。上位机的界面状态、入库服务的报警评估都调用这里，
    /// 阈值取自 meter_threshold 表，比较规则也只有这一份实现。
    ///
    /// 判定通则（与 RuoYi-Vue-master/docs/仪表监测判定标准.md 一致）：
    ///   - 严格比较：刚好等于阈值算合格，只有 &lt; min 或 &gt; max 才命中；
    ///   - null 不参与数值判定；
    ///   - 三相类指标取最不利的那一相作为命中值。
    /// </summary>
    public static class MeterQualityEvaluator
    {
        /// <summary>三相电压是否都低于带电门槛。停电/未接线时应跳过所有带电类判定。</summary>
        public static bool IsEnergized(ThresholdSet set, MeterSampleView sample)
        {
            if (set == null || sample == null)
            {
                return false;
            }

            var limit = set.EnergizedThreshold;
            return Above(sample.VoltageA, limit) || Above(sample.VoltageB, limit) || Above(sample.VoltageC, limit);
        }

        public static List<ThresholdHit> Evaluate(ThresholdSet set, MeterSampleView sample)
        {
            var hits = new List<ThresholdHit>();
            if (set == null || sample == null)
            {
                return hits;
            }

            var energized = IsEnergized(set, sample);

            EvaluateRange(hits, set, ThresholdSet.VoltagePhase, energized,
                sample.VoltageA, sample.VoltageB, sample.VoltageC);
            EvaluateRange(hits, set, ThresholdSet.CurrentPhase, energized,
                sample.CurrentA, sample.CurrentB, sample.CurrentC);
            EvaluateRange(hits, set, ThresholdSet.VoltageThd, energized,
                sample.VoltageThdA, sample.VoltageThdB, sample.VoltageThdC);
            EvaluateRange(hits, set, ThresholdSet.CurrentThd, energized,
                sample.CurrentThdA, sample.CurrentThdB, sample.CurrentThdC);
            EvaluateRange(hits, set, ThresholdSet.PowerFactorTotal, energized, sample.PowerFactorTotal);
            EvaluateRange(hits, set, ThresholdSet.Frequency, energized, sample.Frequency);
            EvaluateRange(hits, set, ThresholdSet.VoltageUnbalance, energized, sample.VoltageUnbalance);
            EvaluateRange(hits, set, ThresholdSet.CurrentUnbalance, energized, sample.CurrentUnbalance);

            return hits;
        }
        private static void EvaluateRange(
            List<ThresholdHit> hits,
            ThresholdSet set,
            string metricCode,
            bool energized,
            params float?[] values)
        {
            var rule = set.Get(metricCode);
            if (rule == null)
            {
                return;
            }

            if (rule.RequireEnergized && !energized)
            {
                // 未带电时不判定。否则停电会被误报成"电压过低 + 功率因数过低"。
                return;
            }

            // 三相取最不利的一相：低限看最小值，高限看最大值。
            float? minObserved = null;
            float? maxObserved = null;
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (!v.HasValue || float.IsNaN(v.Value) || float.IsInfinity(v.Value))
                {
                    continue;
                }

                if (!minObserved.HasValue || v.Value < minObserved.Value)
                {
                    minObserved = v.Value;
                }

                if (!maxObserved.HasValue || v.Value > maxObserved.Value)
                {
                    maxObserved = v.Value;
                }
            }

            if (rule.MinValue.HasValue && minObserved.HasValue && minObserved.Value < rule.MinValue.Value)
            {
                hits.Add(BuildHit(rule, false, minObserved.Value, rule.MinValue.Value));
            }

            if (rule.MaxValue.HasValue && maxObserved.HasValue && maxObserved.Value > rule.MaxValue.Value)
            {
                hits.Add(BuildHit(rule, true, maxObserved.Value, rule.MaxValue.Value));
            }
        }
        private static ThresholdHit BuildHit(ThresholdRule rule, bool isHigh, float value, float threshold)
        {
            var code = isHigh ? rule.AlarmCodeHigh : rule.AlarmCodeLow;
            var name = isHigh ? rule.AlarmNameHigh : rule.AlarmNameLow;
            return new ThresholdHit
            {
                MetricCode = rule.MetricCode,
                AlarmCode = string.IsNullOrWhiteSpace(code)
                    ? rule.MetricCode + (isHigh ? "_high" : "_low")
                    : code,
                AlarmName = string.IsNullOrWhiteSpace(name)
                    ? rule.MetricName + (isHigh ? "过高" : "过低")
                    : name,
                AlarmLevel = string.IsNullOrWhiteSpace(rule.AlarmLevel) ? "warning" : rule.AlarmLevel,
                AlarmLevelIsCritical = string.Equals(rule.AlarmLevel, "critical", StringComparison.OrdinalIgnoreCase),
                IsHigh = isHigh,
                Value = value,
                Threshold = threshold,
                RecoverMargin = rule.RecoverMargin,
                DebounceCount = rule.DebounceCount < 1 ? 1 : rule.DebounceCount,
                IsAlarmEnabled = rule.IsAlarmEnabled && !string.IsNullOrWhiteSpace(code)
            };
        }

        /// <summary>
        /// 报警恢复判定（滞回）。低限报警需回升到 threshold + margin 才恢复；
        /// 高限报警需回落到 threshold - margin 才恢复。
        /// 这是抑制"在阈值附近反复抖动 → 反复产生短命告警"的关键。
        /// </summary>
        public static bool IsRecovered(bool isHigh, float threshold, float margin, float? value)
        {
            if (!value.HasValue || float.IsNaN(value.Value) || float.IsInfinity(value.Value))
            {
                // 取不到值时不宣布恢复，避免"数据断了"被当成"故障消失了"。
                return false;
            }

            return isHigh
                ? value.Value <= threshold - margin
                : value.Value >= threshold + margin;
        }

        private static bool Above(float? value, float limit)
        {
            return value.HasValue && !float.IsNaN(value.Value) && !float.IsInfinity(value.Value) && value.Value > limit;
        }

        /// <summary>
        /// 取某个指标当前观测到的极值：wantMax=true 取最大（用于高限恢复判定），
        /// false 取最小（用于低限恢复判定）。三相类指标跨相取极值，单值指标直接返回该值。
        /// 供入库服务做滞回恢复判定使用。
        /// </summary>
        public static float? GetObservedExtreme(MeterSampleView sample, string metricCode, bool wantMax)
        {
            if (sample == null || string.IsNullOrWhiteSpace(metricCode))
            {
                return null;
            }

            float?[] values;
            switch (metricCode)
            {
                case ThresholdSet.VoltagePhase:
                    values = new[] { sample.VoltageA, sample.VoltageB, sample.VoltageC };
                    break;
                case ThresholdSet.CurrentPhase:
                    values = new[] { sample.CurrentA, sample.CurrentB, sample.CurrentC };
                    break;
                case ThresholdSet.VoltageThd:
                    values = new[] { sample.VoltageThdA, sample.VoltageThdB, sample.VoltageThdC };
                    break;
                case ThresholdSet.CurrentThd:
                    values = new[] { sample.CurrentThdA, sample.CurrentThdB, sample.CurrentThdC };
                    break;
                case ThresholdSet.PowerFactorTotal:
                    values = new[] { sample.PowerFactorTotal };
                    break;
                case ThresholdSet.Frequency:
                    values = new[] { sample.Frequency };
                    break;
                case ThresholdSet.VoltageUnbalance:
                    values = new[] { sample.VoltageUnbalance };
                    break;
                case ThresholdSet.CurrentUnbalance:
                    values = new[] { sample.CurrentUnbalance };
                    break;
                default:
                    return null;
            }

            float? result = null;
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (!v.HasValue || float.IsNaN(v.Value) || float.IsInfinity(v.Value))
                {
                    continue;
                }

                if (!result.HasValue || (wantMax ? v.Value > result.Value : v.Value < result.Value))
                {
                    result = v.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// 枚举阈值集合中所有会产生报警的 (alarmCode → 规则, 是否高限) 组合。
        /// 入库服务用它把"当前激活的告警"映射回对应指标，进而判定滞回恢复。
        /// </summary>
        public static List<AlarmCodeBinding> EnumerateAlarmCodes(ThresholdSet set, IEnumerable<string> metricCodes)
        {
            var result = new List<AlarmCodeBinding>();
            if (set == null)
            {
                return result;
            }

            foreach (var metricCode in metricCodes)
            {
                var rule = set.Get(metricCode);
                if (rule == null || !rule.IsAlarmEnabled)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rule.AlarmCodeLow) && rule.MinValue.HasValue)
                {
                    result.Add(new AlarmCodeBinding(rule.AlarmCodeLow, rule, false));
                }

                if (!string.IsNullOrWhiteSpace(rule.AlarmCodeHigh) && rule.MaxValue.HasValue)
                {
                    result.Add(new AlarmCodeBinding(rule.AlarmCodeHigh, rule, true));
                }
            }

            return result;
        }

        /// <summary>阈值集合中定义的全部指标代码。</summary>
        public static readonly string[] AllMetricCodes =
        {
            ThresholdSet.VoltagePhase,
            ThresholdSet.CurrentPhase,
            ThresholdSet.VoltageThd,
            ThresholdSet.CurrentThd,
            ThresholdSet.PowerFactorTotal,
            ThresholdSet.Frequency,
            ThresholdSet.VoltageUnbalance,
            ThresholdSet.CurrentUnbalance
        };
    }

    /// <summary>alarm_code 与其来源规则、方向的绑定关系。</summary>
    public sealed class AlarmCodeBinding
    {
        public AlarmCodeBinding(string alarmCode, ThresholdRule rule, bool isHigh)
        {
            AlarmCode = alarmCode;
            Rule = rule;
            IsHigh = isHigh;
        }

        public string AlarmCode { get; private set; }
        public ThresholdRule Rule { get; private set; }
        public bool IsHigh { get; private set; }

        public float Threshold
        {
            get { return IsHigh ? (Rule.MaxValue ?? 0f) : (Rule.MinValue ?? 0f); }
        }
    }
    /// <summary>
    /// 阈值提供者：从 meter_threshold 表读取并缓存，按 meter &gt; box &gt; site &gt; global 解析覆盖。
    ///
    /// 关键设计：数据库不可用时**不抛异常**，退回内置默认值继续工作。
    /// 上位机是现场采集程序，不能因为查不到阈值就停止判定或崩溃。
    /// </summary>
    public sealed class ThresholdProvider
    {
        private const string LoadSql = @"
SELECT scope, scope_key, metric_code, metric_name, unit,
       min_value, max_value, recover_margin, debounce_count, require_energized,
       alarm_code_low, alarm_code_high, alarm_name_low, alarm_name_high,
       alarm_level, is_alarm_enabled
FROM public.meter_threshold
ORDER BY metric_code;";

        private readonly string _connectionString;
        private readonly TimeSpan _refreshInterval;
        private readonly object _sync = new object();

        private List<ThresholdRule> _global = new List<ThresholdRule>();
        private Dictionary<string, List<ThresholdRule>> _byScope = new Dictionary<string, List<ThresholdRule>>(StringComparer.OrdinalIgnoreCase);
        private DateTime _loadedAtUtc = DateTime.MinValue;
        private bool _usingFallback = true;
        private bool _fallbackWarned;

        public ThresholdProvider(string connectionString, TimeSpan refreshInterval)
        {
            _connectionString = connectionString;
            _refreshInterval = refreshInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : refreshInterval;
            _global = BuildFallbackRules();
        }

        /// <summary>当前是否在用内置兜底阈值（表示数据库读取失败）。</summary>
        public bool UsingFallback
        {
            get { lock (_sync) { return _usingFallback; } }
        }

        public ThresholdSet Resolve(string siteCode, string boxCode, string meterCode)
        {
            EnsureLoaded();

            lock (_sync)
            {
                var merged = new Dictionary<string, ThresholdRule>(StringComparer.OrdinalIgnoreCase);
                Apply(merged, _global);
                Apply(merged, LookupScope("site", siteCode));
                Apply(merged, LookupScope("box", boxCode));
                Apply(merged, LookupScope("meter", meterCode));
                return new ThresholdSet(merged.Values);
            }
        }
        private List<ThresholdRule> LookupScope(string scope, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            List<ThresholdRule> rules;
            return _byScope.TryGetValue(scope + "|" + key.Trim(), out rules) ? rules : null;
        }

        private static void Apply(Dictionary<string, ThresholdRule> target, List<ThresholdRule> rules)
        {
            if (rules == null)
            {
                return;
            }

            foreach (var rule in rules)
            {
                target[rule.MetricCode] = rule;
            }
        }

        private void EnsureLoaded()
        {
            lock (_sync)
            {
                if (DateTime.UtcNow - _loadedAtUtc < _refreshInterval)
                {
                    return;
                }

                _loadedAtUtc = DateTime.UtcNow;
            }

            try
            {
                var global = new List<ThresholdRule>();
                var byScope = new Dictionary<string, List<ThresholdRule>>(StringComparer.OrdinalIgnoreCase);

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(LoadSql, connection))
                    {
                        command.CommandTimeout = 10;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var scope = reader.GetString(0);
                                var scopeKey = reader.GetString(1);
                                var rule = ReadRule(reader);

                                if (string.Equals(scope, "global", StringComparison.OrdinalIgnoreCase))
                                {
                                    global.Add(rule);
                                    continue;
                                }

                                var mapKey = scope + "|" + scopeKey;
                                if (!byScope.ContainsKey(mapKey))
                                {
                                    byScope[mapKey] = new List<ThresholdRule>();
                                }

                                byScope[mapKey].Add(rule);
                            }
                        }
                    }
                }

                if (global.Count == 0)
                {
                    throw new InvalidOperationException("meter_threshold 表中没有 global 作用域的记录。");
                }

                lock (_sync)
                {
                    _global = global;
                    _byScope = byScope;
                    var wasFallback = _usingFallback;
                    _usingFallback = false;
                    _fallbackWarned = false;
                    if (wasFallback)
                    {
                        AppLogger.Info("Threshold", "已从 meter_threshold 载入判定阈值，global " + global.Count + " 条，覆盖作用域 " + byScope.Count + " 组。");
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_sync)
                {
                    _usingFallback = true;
                    if (!_fallbackWarned)
                    {
                        _fallbackWarned = true;
                        AppLogger.Warn(
                            "Threshold",
                            "读取 meter_threshold 失败，暂时使用内置兜底阈值继续判定（不影响采集）。",
                            ex);
                    }
                }
            }
        }
        private static ThresholdRule ReadRule(NpgsqlDataReader reader)
        {
            return new ThresholdRule
            {
                MetricCode = reader.GetString(2),
                MetricName = reader.IsDBNull(3) ? reader.GetString(2) : reader.GetString(3),
                Unit = reader.IsDBNull(4) ? null : reader.GetString(4),
                MinValue = ReadNullableFloat(reader, 5),
                MaxValue = ReadNullableFloat(reader, 6),
                RecoverMargin = ReadNullableFloat(reader, 7) ?? 0f,
                DebounceCount = reader.IsDBNull(8) ? 1 : reader.GetInt32(8),
                RequireEnergized = !reader.IsDBNull(9) && reader.GetBoolean(9),
                AlarmCodeLow = reader.IsDBNull(10) ? null : reader.GetString(10),
                AlarmCodeHigh = reader.IsDBNull(11) ? null : reader.GetString(11),
                AlarmNameLow = reader.IsDBNull(12) ? null : reader.GetString(12),
                AlarmNameHigh = reader.IsDBNull(13) ? null : reader.GetString(13),
                AlarmLevel = reader.IsDBNull(14) ? "warning" : reader.GetString(14),
                IsAlarmEnabled = reader.IsDBNull(15) || reader.GetBoolean(15)
            };
        }

        private static float? ReadNullableFloat(NpgsqlDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            // numeric 在 Npgsql 里映射成 decimal，统一转 float 供判定使用。
            return Convert.ToSingle(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 内置兜底阈值，必须与 sql/migrations/003 的 global 种子数据保持一致。
        /// 仅在数据库读取失败时生效，让上位机在断库情况下仍能给出与平台一致的判定。
        /// </summary>
        private static List<ThresholdRule> BuildFallbackRules()
        {
            return new List<ThresholdRule>
            {
                Rule("voltage_phase", "相电压", "V", 198f, 242f, 2f, 3, true, "voltage_low", "voltage_high", "电压过低", "电压过高", "warning"),
                Rule("power_factor_total", "总功率因数", null, 0.85f, null, 0.02f, 5, true, "power_factor_low", null, "功率因数过低", null, "warning"),
                Rule("current_phase", "相电流", "A", null, 400f, 5f, 3, false, null, "current_high", null, "电流过高", "critical"),
                Rule("voltage_thd", "电压总谐波畸变率", "%", null, 5f, 0.5f, 3, true, null, "voltage_thd_high", null, "电压谐波超限", "warning"),
                Rule("current_thd", "电流总谐波畸变率", "%", null, 8f, 0.5f, 3, true, null, "current_thd_high", null, "电流谐波超限", "warning"),
                Rule("voltage_unbalance", "电压不平衡度", "%", null, 2f, 0.2f, 3, true, null, "voltage_unbalance_high", null, "电压不平衡超限", "warning"),
                Rule("current_unbalance", "电流不平衡度", "%", null, 3f, 0.3f, 3, true, null, "current_unbalance_high", null, "电流不平衡超限", "warning"),
                Rule("frequency", "频率", "Hz", 40f, 70f, 0.2f, 3, true, "frequency_low", "frequency_high", "频率过低", "频率过高", "warning"),
                Rule("energized_voltage_min", "带电判定电压下限", "V", 50f, null, 0f, 1, false, null, null, null, null, "info", false)
            };
        }

        private static ThresholdRule Rule(
            string metricCode, string metricName, string unit,
            float? min, float? max, float margin, int debounce, bool requireEnergized,
            string codeLow, string codeHigh, string nameLow, string nameHigh,
            string level, bool alarmEnabled = true)
        {
            return new ThresholdRule
            {
                MetricCode = metricCode,
                MetricName = metricName,
                Unit = unit,
                MinValue = min,
                MaxValue = max,
                RecoverMargin = margin,
                DebounceCount = debounce,
                RequireEnergized = requireEnergized,
                AlarmCodeLow = codeLow,
                AlarmCodeHigh = codeHigh,
                AlarmNameLow = nameLow,
                AlarmNameHigh = nameHigh,
                AlarmLevel = level,
                IsAlarmEnabled = alarmEnabled
            };
        }
    }
}
