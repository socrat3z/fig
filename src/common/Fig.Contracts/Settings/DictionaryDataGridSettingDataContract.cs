using System.Collections.Generic;

namespace Fig.Contracts.Settings;

/// <summary>
/// Value contract for settings backed by Dictionary&lt;string, T&gt;.
/// Stored internally as a list of rows where the first column ("Key") is the dictionary key.
/// Distinct from <see cref="DataGridSettingDataContract"/> so consumers can pattern-match
/// without extra runtime tracking.
/// </summary>
public class DictionaryDataGridSettingDataContract : SettingValueBaseDataContract
{
    public DictionaryDataGridSettingDataContract(List<Dictionary<string, object?>>? value)
    {
        Value = value;
    }

    public List<Dictionary<string, object?>>? Value { get; set; }

    public override object? GetValue() => Value;
}
