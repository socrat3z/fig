using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithDictionaries : TestSettingsBase
{
    public override string ClientName => "ClientWithDictionaries";
    public override string ClientDescription => "Client with dictionary settings";

    [Setting("Region configuration per name")]
    public Dictionary<string, RegionConfig> Regions { get; set; } = new();

    [Setting("Threshold per environment")]
    public Dictionary<string, int> Thresholds { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class RegionConfig
{
    public int MaxSize { get; set; }
    public string Location { get; set; } = string.Empty;
}
