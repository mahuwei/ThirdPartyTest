using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace Common {
  public class Tools {
    /// <summary>
    ///   生成日志记录对象
    /// </summary>
    /// <returns></returns>
    public static ILogger CreateLogger() {
      return new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({ThreadId}) {Message}{NewLine}{Exception}")
        .CreateLogger();
    }

    public static List<InputCommand> HandleCommands(string input) {
      if (string.IsNullOrEmpty(input)) {
        return null;
      }

      var parts = input.Split(' ');
      return(from part in parts where !string.IsNullOrEmpty(part) select new InputCommand(part)).ToList();
    }

    public static void PrintHelps(string[] helps) {
      Console.WriteLine("帮助信息:");
      foreach (var help in helps) {
        Console.WriteLine($"  {help}");
      }

      Console.WriteLine();
    }
  }

  public class InputCommand {
    public InputCommand(string input) {
      var data = input.Trim();
      var index = data.IndexOf('=');
      if (index == -1) {
        Command = data;
      }
      else {
        var parts = data.Split('=');
        Command = parts[0];
        Argument = parts[1];
      }
    }

    public string Command { get; set; }
    public string Argument { get; set; }
  }
}