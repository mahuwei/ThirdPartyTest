using System.Collections.Generic;
using ConsulTestConsole.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsulTestConsole.Controllers {
  [Produces("application/json")]
  [ApiController]
  [Route("[controller]")]
  public class ValuesController : ControllerBase {
    private readonly ILogger<ValuesController> _logger;
    private readonly IOptionsSnapshot<BaseConfig> _optionsSnapshot;

    public ValuesController(ILogger<ValuesController> logger, IOptionsSnapshot<BaseConfig> optionsSnapshot) {
      _logger = logger;
      _optionsSnapshot = optionsSnapshot;
    }

    [HttpGet]
    public List<string> Get() {
      return new List<string> { "value1", "value2" };
    }
  }
}