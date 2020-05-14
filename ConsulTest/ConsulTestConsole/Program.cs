using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Consul;
using ConsulTestConsole.Models;
using Newtonsoft.Json;
using Serilog;

namespace ConsulTestConsole {
  internal class Program {
    public const string LocalIp = "192.168.1.129";
    public const int LocalPort = 5100;
    public const string ConsulIp = "http://192.168.1.51";
    public const int ConsulPort = 8500;
    public const string ConsulDataCenter = "dc1";
    public const int ConsulCheckIntervalMinutes = 1;
    public const int ConsulDeRegisterCriticalServiceAfterMinutes = 1;
    public const string NameOfApiServer = "TestConsul";
    public const string TempConfigName = "add/TempConfigKey";

    private static readonly string[]
      Helps = {
        "start-api [count=<num>]:启动Api服务；count:启动数量(默认：1)；", "get-kv [key]:获取Consul配置信息;key需要获取的配置，区分大小写（默认:ServerConfig)",
        "set-kv:Put Consul配置信息;", "test-api:测试启动的Api", "help:查看帮助", "quit:退出"
      };

    private static bool _isApiStarting;
    private static int _apiServerCount;

    private static void Main(string[] args) {
      Console.Title = "Consul 测试";
      var ts = new CancellationTokenSource();
      Log.Logger = Tools.CreateLogger();

      Tools.PrintHelps(Helps);

      Task.Run(() => {
      }, ts.Token);
      var isStop = false;
      do {
        var input = Console.ReadLine();
        var commands = Tools.HandleCommands(input);
        if (commands == null || commands.Any() == false) {
          continue;
        }

        switch (commands[0].Command) {
          case "start-api":
            if (_isApiStarting) {
              Log.Warning("api服务已启动");
              break;
            }

            _apiServerCount = 1;
            if (commands.Count > 1 && commands[1].Command == "count") {
              if (int.TryParse(commands[1].Argument, out _apiServerCount) == false) {
                Log.Warning($"输入命令格式不正确:{input}");
                break;
              }
            }

            Task.Run(() => {
              _isApiStarting = true;
              for (var i = 0; i < _apiServerCount; i++) {
                WebApiServer.StartWebApi(ts.Token, LocalIp, 5100 + i);
              }
            }, ts.Token);
            break;
          case "test-api":
            WebApiServer.TestApi(ts.Token, LocalIp, LocalPort, _apiServerCount).Wait(ts.Token);
            break;
          case "set-kv":
            SetConsulKv(ts.Token).Wait(ts.Token);
            break;
          case "get-kv":
            string key = null;
            if (commands.Count > 1) {
              key = commands[1].Command;
            }

            GetConsulKv(ts.Token, key).Wait(ts.Token);
            break;
          case "help":
            Tools.PrintHelps(Helps);
            break;
          case "quit":
            isStop = true;
            break;
          default:
            if (string.IsNullOrEmpty(input)) {
              break;
            }

            Log.Warning($"无效指令:{input}");
            break;
        }
      } while (!isStop);

      ts.Cancel();
      Console.WriteLine("按任意键退出...");
      Console.ReadKey();
    }

    private static async Task GetConsulKv(CancellationToken token, string key = null) {
      using var consulClient = CreateConsulClient();
      key ??= "ServerConfig";
      var ret = await consulClient.KV.Get(key, token);
      if (ret.StatusCode != HttpStatusCode.OK) {
        Log.Error($"consulClient.KV.Get(\"{key}\"): {ret.StatusCode}");
        return;
      }

      var str = Encoding.UTF8.GetString(ret.Response.Value);
      Log.Information($"Key:{key} = {{@str}}", str);
    }

    private static async Task SetConsulKv(CancellationToken token) {
      using var consulClient = CreateConsulClient();
      var key = TempConfigName;
      var tc = new TempConfig { Topic = $"topic-{DateTime.Now:hh:mm:ss}", Consumer = "temp-consumer" };

      // 为了可以用 .AddConsul("ServerConfig", options...)添加
      var tmp = new { TempConfig = tc };
      var kvPair = new KVPair(key) { Value = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tmp)) };
      var writeOption = WriteOptions.Default;

      var ret = await consulClient.KV.Put(kvPair, writeOption, token);
      if (ret.StatusCode != HttpStatusCode.OK) {
        Log.Error($"consulClient.KV.Put(\"{key}\"): {ret.StatusCode}");
        return;
      }

      Log.Information($"Put Key:{key} success");
    }

    private static ConsulClient CreateConsulClient() {
      var consulClient =
        new ConsulClient(configuration => configuration.Address = new Uri($"{ConsulIp}:{ConsulPort}"));
      return consulClient;
    }
  }


}