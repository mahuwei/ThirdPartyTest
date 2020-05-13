using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ConsulTestConsole.Models {
  public class WebApiServer {
    public static void StartWebApi(CancellationToken token, string ip, int port) {
      Log.Information($"测试启动WebApi（{ip}:{port}）...");
      var hostBuilder = Host.CreateDefaultBuilder();
      var host = hostBuilder.UseSerilog()
        .ConfigureAppConfiguration((hostingContext, config) => {
          config.AddCommandLine(source => source.Args = new[] { $"ip={ip}", $"port={port}" });
        })
        .ConfigureWebHostDefaults(webBuilder => {
          webBuilder.UseKestrel(options => {
            options.Listen(IPAddress.Parse(ip), port);
          });
          webBuilder.UseStartup<Startup>();
        })
        .Build();
      host.RunAsync(token);
    }

    public static async Task TestApi(CancellationToken tsToken, string ip, int port, int apiServerCount) {
      Log.Information("测试Web Api Server...");
      using var httpClient = new HttpClient();
      for (var i = 0; i < apiServerCount; i++) {
        var requestUri = $"http://{ip}:{port + i}/values";
        Log.Information($"Get {requestUri}");
        var response = await httpClient.GetAsync(requestUri, tsToken);
        if (response.IsSuccessStatusCode == false) {
          Log.Error(response.ReasonPhrase);
          continue;
        }

        var ret = await response.Content.ReadAsStringAsync();
        Log.Information($"{requestUri}" + "，返回值：{@ret}", ret);
      }
    }
  }

  public class Startup {
    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services) {
      services.AddControllers();
      services.AddHealthChecks();

      //services.AddSingleton(Program.MessageCenterTestCurrent);
      //services.AddSingleton(Program.BankClientTestCurrent);
    }

    public void Configure(IApplicationBuilder app, ILogger<Startup> logger) {
      app.UseRouting();
      // app.UseCors(builder =>
      //   builder.AllowAnyHeader()
      //     .AllowAnyMethod()
      //     .SetIsOriginAllowed(_ => true)
      //     .AllowCredentials());

      // app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
        endpoints.MapHealthChecks("/health");
      });

      var ip = Configuration["ip"];
      var port = Convert.ToInt32(Configuration["port"]);

      try {
        var result = ConsulHelper.ServiceRegister(logger, ip, port).Result;
        if (result.StatusCode != HttpStatusCode.OK) {
          logger.LogError("注册服务失败。Result:{@result}", result);
        }
        else {
          logger.LogInformation("注册服务成功。Result:{@result}", result);
        }
      }
      catch (Exception ex) {
        logger.LogError(ex, "注册服务发生错误。");
      }
    }
  }
}