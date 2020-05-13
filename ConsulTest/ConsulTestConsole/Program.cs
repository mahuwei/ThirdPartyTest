using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using ConsulTestConsole.Models;
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

    private static readonly string[]
      Helps = { "start-api [count=<num>]:启动Api服务；count:启动数量(默认：1)；", "test-api:测试启动的Api", "help:查看帮助", "quit:退出" };

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
          case"start-api":
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
          case"test-api":
            WebApiServer.TestApi(ts.Token, LocalIp, LocalPort, _apiServerCount).Wait(ts.Token);
            break;
          case"help":
            Tools.PrintHelps(Helps);
            break;
          case"quit":
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
  }
}