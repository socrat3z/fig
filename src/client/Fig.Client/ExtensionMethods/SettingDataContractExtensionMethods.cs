using System;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fig.Client.Configuration;
using Fig.Client.Parsers;
using Microsoft.Extensions.Configuration;
using Fig.Common.NetStandard.IpAddress;
using Newtonsoft.Json.Linq;

namespace Fig.Client.ExtensionMethods;

internal static class SettingDataContractExtensionMethods
{
    public static Dictionary<string, string?> ToDataProviderFormat(
        this List<SettingDataContract> settings,
        IIpAddressResolver ipAddressResolver,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections)
    {
        var dictionary = new Dictionary<string, string?>();

        foreach (var setting in settings.Where(IsSimpleValue))
        {
            var settingValue = setting.Value switch
            {
                StringSettingDataContract stringSetting => stringSetting.Value.ReplaceConstants(ipAddressResolver),
                BoolSettingDataContract boolSetting => boolSetting.Value.ToString(),
                DateTimeSettingDataContract dateTimeSetting => dateTimeSetting.Value?.ToString("o"),
                DoubleSettingDataContract doubleSetting => doubleSetting.Value.ToString(CultureInfo.InvariantCulture),
                LongSettingDataContract longSetting => longSetting.Value.ToString(CultureInfo.InvariantCulture),
                IntSettingDataContract intSetting => intSetting.Value.ToString(CultureInfo.InvariantCulture),
                TimeSpanSettingDataContract timeSpanSetting => timeSpanSetting.Value?.ToString(),
                _ => null
            };

            var simplifiedName = setting.Name.Split([Constants.SettingPathSeparator], StringSplitOptions.RemoveEmptyEntries).Last();
            dictionary[simplifiedName] = settingValue;

            var joinedName = setting.Name.Replace(Constants.SettingPathSeparator, ":");
            dictionary[joinedName] = settingValue;

            if (configurationSections.TryGetValue(setting.Name, out var sections) && sections != null)
            {
                foreach (var section in sections)
                {
                    var sectionSettingName = section.SettingNameOverride ?? simplifiedName;
                    if (!string.IsNullOrEmpty(section.SectionName))
                        dictionary[$"{section.SectionName}:{sectionSettingName}"] = settingValue;
                    else
                        dictionary[sectionSettingName] = settingValue;
                }
            }
        }

        // List-backed DataGrids: rows indexed 0, 1, 2, …
        foreach (var setting in settings.Where(IsDataGrid))
        {
            var value = ((DataGridSettingDataContract)setting.Value!).Value;
            if (value is null) { dictionary[setting.Name] = null; continue; }

            GetSections(setting.Name, configurationSections, out var sections);
            var isBaseTypeList = value.FirstOrDefault()?.Count == 1 && value.First().First().Key == "Values";
            var rowIndex = 0;

            foreach (var row in value)
            {
                WriteDataGridRow(dictionary, ipAddressResolver, setting.Name, row, rowIndex.ToString(), isBaseTypeList, sections);
                rowIndex++;
            }
        }

        // Dictionary-backed DataGrids: rows keyed by the "Key" column value.
        foreach (var setting in settings.Where(IsDictionaryDataGrid))
        {
            var value = ((DictionaryDataGridSettingDataContract)setting.Value!).Value;
            if (value is null) { dictionary[setting.Name] = null; continue; }

            GetSections(setting.Name, configurationSections, out var sections);
            var rowIndex = 0;

            foreach (var row in value)
            {
                var keySegment = row.TryGetValue("Key", out var keyObj)
                    ? keyObj?.ToString() ?? rowIndex.ToString()
                    : rowIndex.ToString();

                // Emit every column except the synthetic "Key" — that IS the path segment.
                var dataRow = row.Where(kvp => kvp.Key != "Key")
                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // For primitive-value dicts (single "Value" column), emit {Name}:{key} = value
                // rather than {Name}:{key}:Value = value, matching IConfiguration binding expectations.
                var isPrimitiveValueDict = dataRow.Count == 1 && dataRow.ContainsKey("Value");
                WriteDataGridRow(dictionary, ipAddressResolver, setting.Name, dataRow, keySegment, isBaseTypeList: isPrimitiveValueDict, sections);
                rowIndex++;
            }
        }

        foreach (var setting in settings.Where(IsJson))
        {
            var value = ((JsonSettingDataContract)setting.Value!).Value;
            if (value is not null)
            {
                var parser = new JsonValueParser();
                var parsedValues = parser.ParseJsonValue(value);

                foreach (var kvp in parsedValues)
                {
                    var key = ConfigurationPath.Combine(setting.Name, kvp.Key)
                        .Replace(Constants.SettingPathSeparator, ":");
                    dictionary[key] = kvp.Value.ReplaceConstants(ipAddressResolver);
                }

                if (configurationSections.TryGetValue(setting.Name, out var sections) && sections != null)
                {
                    foreach (var section in sections)
                    {
                        if (!string.IsNullOrEmpty(section.SectionName))
                        {
                            var sectionSettingName = section.SettingNameOverride ?? setting.Name;
                            foreach (var kvp in parsedValues)
                            {
                                var key = ConfigurationPath.Combine(section.SectionName!, sectionSettingName, kvp.Key)
                                    .Replace(Constants.SettingPathSeparator, ":");
                                dictionary[key] = kvp.Value.ReplaceConstants(ipAddressResolver);
                            }
                        }
                    }
                }
            }
        }

        return dictionary;
    }

    private static void WriteDataGridRow(
        Dictionary<string, string?> dictionary,
        IIpAddressResolver ipAddressResolver,
        string settingName,
        Dictionary<string, object?> row,
        string rowSegment,
        bool isBaseTypeList,
        List<CustomConfigurationSection> sections)
    {
        var name = settingName.Replace(Constants.SettingPathSeparator, ":");

        foreach (var kvp in row)
        {
            var path = isBaseTypeList
                ? ConfigurationPath.Combine(name, rowSegment)
                : ConfigurationPath.Combine(name, rowSegment, kvp.Key);

            WriteValue(dictionary, ipAddressResolver, path, kvp.Value);

            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section.SectionName)) continue;

                var sectionName = !string.IsNullOrWhiteSpace(section.SettingNameOverride)
                    ? section.SettingNameOverride
                    : name;

                var sectionPath = isBaseTypeList
                    ? ConfigurationPath.Combine(sectionName!, rowSegment)
                    : ConfigurationPath.Combine(sectionName!, rowSegment, kvp.Key);

                sectionPath = ConfigurationPath.Combine(section.SectionName!, sectionPath);
                WriteValue(dictionary, ipAddressResolver, sectionPath, kvp.Value);
            }
        }
    }

    private static void WriteValue(Dictionary<string, string?> dictionary, IIpAddressResolver ipAddressResolver, string path, object? value)
    {
        if (value is JArray arr)
        {
            for (var i = 0; i < arr.Count; i++)
                dictionary[ConfigurationPath.Combine(path, i.ToString())] = arr[i].ToString();
        }
        else
        {
            dictionary[path] = Convert.ToString(value, CultureInfo.InvariantCulture)?.ReplaceConstants(ipAddressResolver);
        }
    }

    private static void GetSections(string settingName, Dictionary<string, List<CustomConfigurationSection>> configurationSections, out List<CustomConfigurationSection> sections)
    {
        sections = configurationSections.TryGetValue(settingName, out var s) && s != null
            ? s
            : new List<CustomConfigurationSection>();
    }

    private static bool IsSimpleValue(SettingDataContract s) =>
        s.Value is not DataGridSettingDataContract
               and not DictionaryDataGridSettingDataContract
               and not JsonSettingDataContract;

    private static bool IsDataGrid(SettingDataContract s) =>
        s.Value is DataGridSettingDataContract;

    private static bool IsDictionaryDataGrid(SettingDataContract s) =>
        s.Value is DictionaryDataGridSettingDataContract;

    private static bool IsJson(SettingDataContract s) =>
        s.Value is JsonSettingDataContract;
}
