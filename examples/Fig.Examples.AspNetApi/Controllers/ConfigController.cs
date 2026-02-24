using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fig.Examples.AspNetApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<Settings> _settings;

    public ConfigController(IConfiguration configuration, IOptionsMonitor<Settings> settings)
    {
        _configuration = configuration;
        _settings = settings;
    }

    [HttpGet("dump")]
    public IActionResult Dump()
    {
        var entries = _configuration.AsEnumerable()
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        return Ok(entries);
    }

    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        return Ok(_settings.CurrentValue);
    }
}
