using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.ClientSecret;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Client.Status;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

/// <summary>
/// Tests that Dictionary&lt;string, T&gt; settings are correctly bound via IOptions
/// through the full configuration provider pipeline.
/// </summary>
[TestFixture]
public class DictionaryConfigurationProviderTests
{
    private readonly Mock<IApiCommunicationHandler> _apiMock = new();
    private readonly Mock<ISettingStatusMonitor> _monitorMock = new();

    [SetUp]
    public void Setup()
    {
        _apiMock.Reset();
        _monitorMock.Reset();
        _monitorMock.Setup(m => m.Initialize());
        _monitorMock.Setup(m => m.AllowOfflineSettings).Returns(false);
        RegisteredProviders.Clear();
    }

    [Test]
    public void ShallBindDictionaryOfComplexTypeViaIOptions()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "primary",   ["MaxSize"] = 1024L, ["Region"] = "us-east" },
            new() { ["Key"] = "secondary", ["MaxSize"] = 512L,  ["Region"] = "eu-west" },
        };
        _apiMock.Setup(a => a.RequestConfiguration()).ReturnsAsync(new List<SettingDataContract>
        {
            new(nameof(DictSettings.Buckets), new DictionaryDataGridSettingDataContract(rows))
        });

        var configuration = BuildConfiguration<DictSettings>();

        var sp = new ServiceCollection()
            .Configure<DictSettings>(configuration)
            .BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<DictSettings>>().Value;

        Assert.That(options.Buckets, Is.Not.Null);
        Assert.That(options.Buckets.Count, Is.EqualTo(2));

        Assert.That(options.Buckets["primary"].MaxSize, Is.EqualTo(1024));
        Assert.That(options.Buckets["primary"].Region, Is.EqualTo("us-east"));
        Assert.That(options.Buckets["secondary"].MaxSize, Is.EqualTo(512));
        Assert.That(options.Buckets["secondary"].Region, Is.EqualTo("eu-west"));
    }

    [Test]
    public void ShallBindDictionaryOfPrimitiveTypeViaIOptions()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Key"] = "dev",  ["Value"] = 100L },
            new() { ["Key"] = "prod", ["Value"] = 500L },
        };
        _apiMock.Setup(a => a.RequestConfiguration()).ReturnsAsync(new List<SettingDataContract>
        {
            new(nameof(DictPrimitiveSettings.Thresholds), new DictionaryDataGridSettingDataContract(rows))
        });

        var configuration = BuildConfiguration<DictPrimitiveSettings>();

        var sp = new ServiceCollection()
            .Configure<DictPrimitiveSettings>(configuration)
            .BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<DictPrimitiveSettings>>().Value;

        Assert.That(options.Thresholds, Is.Not.Null);
        Assert.That(options.Thresholds.Count, Is.EqualTo(2));
        Assert.That(options.Thresholds["dev"], Is.EqualTo(100));
        Assert.That(options.Thresholds["prod"], Is.EqualTo(500));
    }

    [Test]
    public void ShallRegisterDictionarySettingWithIsDictionaryFlag()
    {
        _apiMock.Setup(a => a.RequestConfiguration()).ReturnsAsync(new List<SettingDataContract>());

        var source = CreateSource<DictSettings>();
        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Build();

        _apiMock.Verify(a => a.RegisterWithFigApi(
            It.Is<SettingsClientDefinitionDataContract>(d =>
                d.Settings.Any(s =>
                    s.Name == nameof(DictSettings.Buckets) &&
                    s.DataGridDefinition != null &&
                    s.DataGridDefinition.IsDictionary))),
            Times.Once);
    }

    [Test]
    public void ShallRegisterDictionarySettingWithKeyAsFirstColumn()
    {
        _apiMock.Setup(a => a.RequestConfiguration()).ReturnsAsync(new List<SettingDataContract>());

        var source = CreateSource<DictSettings>();
        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Build();

        _apiMock.Verify(a => a.RegisterWithFigApi(
            It.Is<SettingsClientDefinitionDataContract>(d =>
                d.Settings.Any(s =>
                    s.Name == nameof(DictSettings.Buckets) &&
                    s.DataGridDefinition != null &&
                    s.DataGridDefinition.Columns.First().Name == "Key"))),
            Times.Once);
    }

    // -------------------------------------------------------------------------

    private IConfigurationRoot BuildConfiguration<TSettings>() where TSettings : SettingsBase, new()
    {
        var source = CreateSource<TSettings>();
        var builder = new ConfigurationBuilder();
        builder.Add(source);
        return builder.Build();
    }

    private DictTestableSource CreateSource<TSettings>() where TSettings : SettingsBase, new()
    {
        return new DictTestableSource(_apiMock, _monitorMock)
        {
            ApiUris = ["http://localhost:5000"],
            PollIntervalMs = 30000,
            LiveReload = false,
            Instance = null,
            ClientName = "dict-test",
            AllowOfflineSettings = false,
            SettingsType = typeof(TSettings),
            ClientSecretProviders =
            [
                new InCodeClientSecretProvider(
                    Mock.Of<ILogger<InCodeClientSecretProvider>>(),
                    Guid.NewGuid().ToString())
            ]
        };
    }
}

// Test settings classes

public class DictSettings : SettingsBase
{
    public override string ClientDescription => "Settings with a Dictionary<string, ComplexType>";

    [Setting("Bucket configuration per name")]
    public Dictionary<string, DictBucketConfig> Buckets { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => Array.Empty<string>();
}

public class DictPrimitiveSettings : SettingsBase
{
    public override string ClientDescription => "Settings with a Dictionary<string, int>";

    [Setting("Threshold per environment")]
    public Dictionary<string, int> Thresholds { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => Array.Empty<string>();
}

public class DictBucketConfig
{
    public int MaxSize { get; set; }
    public string Region { get; set; } = string.Empty;
}

// Testable source (same pattern as in ConfigurationProviderTests.cs)
public class DictTestableSource : FigConfigurationSource
{
    private readonly Mock<IApiCommunicationHandler> _apiMock;
    private readonly Mock<ISettingStatusMonitor> _monitorMock;

    public DictTestableSource(Mock<IApiCommunicationHandler> apiMock, Mock<ISettingStatusMonitor> monitorMock)
    {
        _apiMock = apiMock;
        _monitorMock = monitorMock;
    }

    protected override IApiCommunicationHandler CreateCommunicationHandler(
        HttpClient httpClient,
        Fig.Common.NetStandard.IpAddress.IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider) => _apiMock.Object;

    protected override ISettingStatusMonitor CreateStatusMonitor(
        Fig.Common.NetStandard.IpAddress.IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        HttpClient httpClient) => _monitorMock.Object;

    protected override HttpClient CreateHttpClient(bool hasOfflineSettings) => new HttpClient();
}
