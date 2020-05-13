using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Confluent.Kafka;
using Serilog;

namespace KafkaTest {
  internal class Program {
    public const string Kafka = "192.168.1.51:9092";
    public const string Topic = "test-topic";
    public const string ConsumerGroupName = "test-topic-consumer";

    private static readonly string[]
      Helps = { "start-consume:开始消费", "produce [count=10]:生产消息", "help:查看帮助", "quit:退出" };

    private static bool _isStartConsume;

    private static void Main(string[] args) {
      Log.Logger = Tools.CreateLogger();

      Console.Title = "Kafka测试";
      var ts = new CancellationTokenSource();

      Tools.PrintHelps(Helps);
      var isStop = false;
      do {
        var input = Console.ReadLine();
        var commands = Tools.HandleCommands(input);
        if (commands == null || commands.Any() == false) {
          continue;
        }

        switch (commands[0].Command) {
          case"start-consume":
            if (_isStartConsume) {
              break;
            }

            Task.Run(() => {
              ConsumeKafkaMessage(ts.Token);
              _isStartConsume = true;
            }, ts.Token);
            break;
          case"produce":
            if (commands.Count == 1 || commands[1].Command != "count") {
              ProduceMessages(10).Wait(ts.Token);
            }
            else {
              if (int.TryParse(commands[1].Argument, out var count)) {
                ProduceMessages(count).Wait(ts.Token);
              }
            }

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

            Console.WriteLine($"无效指令:{input}");
            break;
        }
      } while (!isStop);

      ts.Cancel();
      Console.WriteLine("按任意键退出...");
      Console.ReadKey();
    }

    private static async Task ProduceMessages(int count) {
      Log.Information("开始发送消息...");
      var stopWatch = new Stopwatch();
      stopWatch.Start();
      try {
        var conf = new ProducerConfig { BootstrapServers = Kafka, MessageTimeoutMs = 60000 };
        try {
          using var p = new ProducerBuilder<Null, string>(conf).Build();
          for (var i = 0; i < count; i++) {
            var message = $"{DateTime.Now:HH:mm:ss} - {i:0000}";
            await p.ProduceAsync(Topic, new Message<Null, string> { Value = message });
          }

          stopWatch.Stop();
          Log.Information($"发送：{count}条消息，用时：{stopWatch.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex) {
          Log.Logger.Error(ex, $"发送出错:{ex.Message}");
        }
      }
      catch (Exception ex) {
        Log.Logger.Error(ex, $"准备发送出错:{ex.Message}");
      }
    }

   

    private static void ConsumeKafkaMessage(in CancellationToken cancellationToken) {
      var conf = new ConsumerConfig {
        GroupId = ConsumerGroupName,
        BootstrapServers = Kafka,
        AutoCommitIntervalMs = 3000,
        EnableAutoCommit = true,
        SessionTimeoutMs = 6000,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnablePartitionEof = true
      };

      using var consumer = new ConsumerBuilder<Ignore, string>(conf).Build();
      try {
        consumer.Subscribe(Topic);
        while (true) {
          try {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var cr = consumer.Consume(cancellationToken);
            if (cr.IsPartitionEOF) {
              continue;
            }

            Log.Information($"获取到消息:{cr.Message.Value};" + "  {@topicPartition}; {@topicPartitionOffset}",
              cr.TopicPartition,
              cr.TopicPartitionOffset.Offset);
            //Thread.Sleep(500);
          }
          catch (ConsumeException e) {
            Log.Logger.Error(e, $"消费出错(ConsumeException):{e.Message}");
          }
          catch (TopicPartitionOffsetException e) {
            Log.Logger.Error(e, $"消费出错(TopicPartitionOffsetException):{e.Message}");
          }
          catch (KafkaException e) {
            Log.Logger.Error(e, $"消费出错(KafkaException):{e.Message}");
          }
          catch (OperationCanceledException e) {
            Log.Logger.Error(e, $"消费出错(OperationCanceledException):{e.Message}");
            break;
          }
          catch (Exception e) {
            Log.Logger.Error(e, $"消费出错(Exception):{e.Message}");
          }

          if (cancellationToken.IsCancellationRequested) {
            Log.Information("cancellationToken.IsCancellationRequested 退出线程。");
            break;
          }
        }
      }
      catch (Exception ex) {
        Log.Logger.Error(ex, $"订阅出错:{ex.Message}");
      }

      consumer.Close();
      Log.Information("消费线程结束。");
    }
  }
}