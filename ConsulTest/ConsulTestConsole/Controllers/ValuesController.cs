using System.Collections.Generic;
using Consul;
using ConsulTestConsole.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ConsulTestConsole.Controllers {
  [Produces("application/json")]
  [ApiController]
  [Route("[controller]")]
  public class ValuesController : ControllerBase {
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ValuesController> _logger;
    private readonly IOptionsSnapshot<BaseConfig> _optionsSnapshot;

    public ValuesController(ILogger<ValuesController> logger,
      IOptionsSnapshot<BaseConfig> optionsSnapshot,
      IConsulClient consulClient) {
      _logger = logger;
      _optionsSnapshot = optionsSnapshot;
      _consulClient = consulClient;
    }

    [HttpGet]
    public List<string> Get() {
      return new List<string> { "value1", "value2", JsonConvert.SerializeObject(_optionsSnapshot.Value) };
    }
  }
}