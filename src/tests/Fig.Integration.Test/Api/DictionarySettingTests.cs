using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class DictionarySettingTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterDictionarySettingWithIsDictionaryFlag()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var regionsSetting = client.Settings.Single(s => s.Name == nameof(settings.CurrentValue.Regions));
        Assert.That(regionsSetting.DataGridDefinition, Is.Not.Null);
        Assert.That(regionsSetting.DataGridDefinition!.IsDictionary, Is.True);
    }

    [Test]
    public async Task ShallRegisterDictionarySettingWithKeyAsFirstColumn()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var regionsSetting = client.Settings.Single(s => s.Name == nameof(settings.CurrentValue.Regions));
        Assert.That(regionsSetting.DataGridDefinition!.Columns.First().Name, Is.EqualTo("Key"));
    }

    [Test]
    public async Task ShallRegisterComplexDictionaryWithValueTypePropertiesAsColumns()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var regionsSetting = client.Settings.Single(s => s.Name == nameof(settings.CurrentValue.Regions));
        var columnNames = regionsSetting.DataGridDefinition!.Columns.Select(c => c.Name).ToList();

        Assert.That(columnNames, Does.Contain("Key"));
        Assert.That(columnNames, Does.Contain(nameof(RegionConfig.MaxSize)));
        Assert.That(columnNames, Does.Contain(nameof(RegionConfig.Location)));
        Assert.That(columnNames.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallRegisterPrimitiveDictionaryWithKeyAndValueColumns()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var thresholdsSetting = client.Settings.Single(s => s.Name == nameof(settings.CurrentValue.Thresholds));
        var columnNames = thresholdsSetting.DataGridDefinition!.Columns.Select(c => c.Name).ToList();

        Assert.That(columnNames, Is.EqualTo(new[] { "Key", "Value" }));
        Assert.That(thresholdsSetting.DataGridDefinition.IsDictionary, Is.True);
    }

    [Test]
    public async Task ShallUpdateComplexDictionaryAndReloadCorrectly()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var updatedRows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "us-east", ["MaxSize"] = 1024L, ["Location"] = "Virginia" },
            new() { ["Key"] = "eu-west", ["MaxSize"] = 512L, ["Location"] = "Dublin" },
        };

        await SetSettings(settings.CurrentValue.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Regions), new DictionaryDataGridSettingDataContract(updatedRows))
        });

        configuration.Reload();

        Assert.That(settings.CurrentValue.Regions, Is.Not.Null);
        Assert.That(settings.CurrentValue.Regions.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.Regions["us-east"].MaxSize, Is.EqualTo(1024));
        Assert.That(settings.CurrentValue.Regions["us-east"].Location, Is.EqualTo("Virginia"));
        Assert.That(settings.CurrentValue.Regions["eu-west"].MaxSize, Is.EqualTo(512));
        Assert.That(settings.CurrentValue.Regions["eu-west"].Location, Is.EqualTo("Dublin"));
    }

    [Test]
    public async Task ShallUpdatePrimitiveDictionaryAndReloadCorrectly()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithDictionaries>(secret);

        var updatedRows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "dev", ["Value"] = 100L },
            new() { ["Key"] = "prod", ["Value"] = 500L },
        };

        await SetSettings(settings.CurrentValue.ClientName, new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Thresholds), new DictionaryDataGridSettingDataContract(updatedRows))
        });

        configuration.Reload();

        Assert.That(settings.CurrentValue.Thresholds, Is.Not.Null);
        Assert.That(settings.CurrentValue.Thresholds.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.Thresholds["dev"], Is.EqualTo(100));
        Assert.That(settings.CurrentValue.Thresholds["prod"], Is.EqualTo(500));
    }
}
