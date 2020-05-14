using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ConsulTestConsole.Models {
  public class TestService : IHostedService {
    private readonly ILogger<TestService> _logger;
    private BaseConfig _config;

    public TestService(ILogger<TestService> logger, IOptionsMonitor<BaseConfig> options, IOptionsMonitor<TempConfig> optionsTempConfig) {
      _logger = logger;

      _logger.LogDebug("config setting:{@config}",
        JsonConvert.SerializeObject(options.CurrentValue, Formatting.Indented));
      _config = options.CurrentValue;

      optionsTempConfig.OnChange(op => {
        _logger.LogInformation("HostSample Config change:{@newConfig}",
          JsonConvert.SerializeObject(optionsTempConfig.CurrentValue, Formatting.Indented));
      });

      options.OnChange(op => {
        _logger.LogInformation("HostSample Config change:{@newConfig}",
          JsonConvert.SerializeObject(options.CurrentValue, Formatting.Indented));

        _logger.LogInformation(_config.Kafka != options.CurrentValue.Kafka
          ? $"Kafka发生变化:{_config.Kafka} ==> {options.CurrentValue.Kafka}"
          : $"Kafka没有发生变化:{_config.Kafka} ==> {options.CurrentValue.Kafka}");

        _config = options.CurrentValue;
      });
    }

    public Task StartAsync(CancellationToken cancellationToken) {
      _logger.LogDebug("StartAsync...");
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
      _logger.LogDebug("StopAsync...");
      return Task.CompletedTask;
    }
  }
}