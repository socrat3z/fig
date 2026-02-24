using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Configuration;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.ExtensionMethods;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.Settings;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class DictionarySettingTests
{
    private Mock<IDescriptionProvider> _descriptionProviderMock = null!;
    private Mock<IDataGridDefaultValueProvider> _dataGridDefaultValueProviderMock = null!;
    private SettingDefinitionFactory _factory = null!;
    private Mock<IIpAddressResolver> _ipAddressResolverMock = null!;

    [SetUp]
    public void Setup()
    {
        _descriptionProviderMock = new Mock<IDescriptionProvider>();
        _descriptionProviderMock.Setup(a => a.GetDescription(It.IsAny<string>())).Returns("Test description");

        _dataGridDefaultValueProviderMock = new Mock<IDataGridDefaultValueProvider>();
        _dataGridDefaultValueProviderMock
            .Setup(a => a.ConvertDictionary(It.IsAny<object?>(), It.IsAny<List<Fig.Contracts.SettingDefinitions.DataGridColumnDataContract>>()))
            .Returns((List<Dictionary<string, object?>>?)null);

        _factory = new SettingDefinitionFactory(_descriptionProviderMock.Object, _dataGridDefaultValueProviderMock.Object);

        _ipAddressResolverMock = new Mock<IIpAddressResolver>();
    }

    // -------------------------------------------------------------------------
    // SettingDefinitionFactory tests
    // -------------------------------------------------------------------------

    [Test]
    public void CreateDataContract_WithDictionaryOfComplexType_SetsDictionaryFlagOnDefinition()
    {
        var settings = new SettingsWithComplexDictionary();
        var contract = settings.CreateDataContract("TestClient");

        var setting = contract.Settings.Single(s => s.Name == nameof(settings.Buckets));
        Assert.That(setting.DataGridDefinition, Is.Not.Null);
        Assert.That(setting.DataGridDefinition!.IsDictionary, Is.True);
    }

    [Test]
    public void CreateDataContract_WithDictionaryOfComplexType_FirstColumnIsKey()
    {
        var settings = new SettingsWithComplexDictionary();
        var contract = settings.CreateDataContract("TestClient");

        var setting = contract.Settings.Single(s => s.Name == nameof(settings.Buckets));
        var firstColumn = setting.DataGridDefinition!.Columns.First();

        Assert.That(firstColumn.Name, Is.EqualTo("Key"));
        Assert.That(firstColumn.ValueType, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void CreateDataContract_WithDictionaryOfComplexType_RemainingColumnsMatchValueTypeProperties()
    {
        var settings = new SettingsWithComplexDictionary();
        var contract = settings.CreateDataContract("TestClient");

        var setting = contract.Settings.Single(s => s.Name == nameof(settings.Buckets));
        var columnNames = setting.DataGridDefinition!.Columns.Select(c => c.Name).ToList();

        Assert.That(columnNames, Does.Contain("Key"));
        Assert.That(columnNames, Does.Contain(nameof(BucketConfig.MaxSize)));
        Assert.That(columnNames, Does.Contain(nameof(BucketConfig.Region)));
        Assert.That(columnNames.Count, Is.EqualTo(3)); // Key + MaxSize + Region
    }

    [Test]
    public void CreateDataContract_WithDictionaryOfPrimitiveType_HasKeyAndValueColumns()
    {
        var settings = new SettingsWithPrimitiveDictionary();
        var contract = settings.CreateDataContract("TestClient");

        var setting = contract.Settings.Single(s => s.Name == nameof(settings.Thresholds));
        var columnNames = setting.DataGridDefinition!.Columns.Select(c => c.Name).ToList();

        Assert.That(columnNames, Is.EqualTo(new[] { "Key", "Value" }));
        Assert.That(setting.DataGridDefinition.IsDictionary, Is.True);
    }

    [Test]
    public void CreateDataContract_WithDictionarySetting_DoesNotThrow()
    {
        var settings = new SettingsWithComplexDictionary();
        Assert.DoesNotThrow(() => settings.CreateDataContract("TestClient"));
    }

    [Test]
    public void CreateDataContract_WithDictionarySetting_DefaultValueIsDictionaryDataGridContract()
    {
        // Arrange — provide a non-null default value so the factory wraps it
        var returnedRows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "primary", ["MaxSize"] = 100, ["Region"] = "us-east" }
        };
        _dataGridDefaultValueProviderMock
            .Setup(a => a.ConvertDictionary(It.IsAny<object?>(), It.IsAny<List<Fig.Contracts.SettingDefinitions.DataGridColumnDataContract>>()))
            .Returns(returnedRows);

        var settings = new SettingsWithComplexDictionary();
        var contract = settings.CreateDataContract("TestClient");

        var setting = contract.Settings.Single(s => s.Name == nameof(settings.Buckets));
        Assert.That(setting.DefaultValue, Is.InstanceOf<DictionaryDataGridSettingDataContract>());
    }

    // -------------------------------------------------------------------------
    // ToDataProviderFormat — dictionary path generation
    // -------------------------------------------------------------------------

    [Test]
    public void ToDataProviderFormat_WithDictionaryDataGrid_UsesKeyColumnAsPathSegment()
    {
        var settingName = "Buckets";
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "primary", ["MaxSize"] = 100, ["Region"] = "us-east" },
            new() { ["Key"] = "secondary", ["MaxSize"] = 50, ["Region"] = "eu-west" },
        };
        var settings = new List<SettingDataContract>
        {
            new(settingName, new DictionaryDataGridSettingDataContract(rows))
        };

        var result = settings.ToDataProviderFormat(_ipAddressResolverMock.Object, EmptySections());

        Assert.That(result, Does.ContainKey("Buckets:primary:MaxSize"));
        Assert.That(result, Does.ContainKey("Buckets:primary:Region"));
        Assert.That(result, Does.ContainKey("Buckets:secondary:MaxSize"));
        Assert.That(result, Does.ContainKey("Buckets:secondary:Region"));
    }

    [Test]
    public void ToDataProviderFormat_WithDictionaryDataGrid_KeyColumnIsNotEmittedAsProperty()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "primary", ["MaxSize"] = 100 }
        };
        var settings = new List<SettingDataContract>
        {
            new("Buckets", new DictionaryDataGridSettingDataContract(rows))
        };

        var result = settings.ToDataProviderFormat(_ipAddressResolverMock.Object, EmptySections());

        // "Key" column itself must not appear as a nested property value
        Assert.That(result.Keys, Has.None.EqualTo("Buckets:primary:Key"));
        Assert.That(result.Keys, Has.None.Matches<string>(k => k.EndsWith(":Key")));
    }

    [Test]
    public void ToDataProviderFormat_WithDictionaryDataGrid_CorrectValuesAreAssigned()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "primary", ["MaxSize"] = 100, ["Region"] = "us-east" }
        };
        var settings = new List<SettingDataContract>
        {
            new("Buckets", new DictionaryDataGridSettingDataContract(rows))
        };

        var result = settings.ToDataProviderFormat(_ipAddressResolverMock.Object, EmptySections());

        Assert.That(result["Buckets:primary:MaxSize"], Is.EqualTo("100"));
        Assert.That(result["Buckets:primary:Region"], Is.EqualTo("us-east"));
    }

    [Test]
    public void ToDataProviderFormat_WithNullDictionaryDataGrid_SetsNullForSettingName()
    {
        var settings = new List<SettingDataContract>
        {
            new("Buckets", new DictionaryDataGridSettingDataContract(null))
        };

        var result = settings.ToDataProviderFormat(_ipAddressResolverMock.Object, EmptySections());

        Assert.That(result, Does.ContainKey("Buckets"));
        Assert.That(result["Buckets"], Is.Null);
    }

    [Test]
    public void ToDataProviderFormat_WithRegularDataGrid_StillUsesNumericIndex()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "Ted", ["Value"] = "Doctor" },
            new() { ["Key"] = "Jill", ["Value"] = "Engineer" },
        };
        var settings = new List<SettingDataContract>
        {
            new("Employees", new DataGridSettingDataContract(rows))
        };

        var result = settings.ToDataProviderFormat(_ipAddressResolverMock.Object, EmptySections());

        // Regular DataGrid uses numeric row indices, NOT the "Key" column value
        Assert.That(result, Does.ContainKey("Employees:0:Key"));
        Assert.That(result, Does.ContainKey("Employees:0:Value"));
        Assert.That(result, Does.ContainKey("Employees:1:Key"));
        Assert.That(result["Employees:0:Key"], Is.EqualTo("Ted"));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Dictionary<string, List<CustomConfigurationSection>> EmptySections() =>
        new Dictionary<string, List<CustomConfigurationSection>>();
}

// Test settings classes

public class SettingsWithComplexDictionary : SettingsBase
{
    public override string ClientDescription => "Settings with a Dictionary<string, ComplexType> setting";

    [Setting("Bucket configuration per name")]
    public Dictionary<string, BucketConfig> Buckets { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => Array.Empty<string>();
}

public class SettingsWithPrimitiveDictionary : SettingsBase
{
    public override string ClientDescription => "Settings with a Dictionary<string, int> setting";

    [Setting("Threshold per environment")]
    public Dictionary<string, int> Thresholds { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => Array.Empty<string>();
}

public class BucketConfig
{
    public int MaxSize { get; set; }
    public string Region { get; set; } = string.Empty;
}
