using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaTest {
  internal class Program {
    public const string Kafka = "192.168.1.51:9092";
    public const string Topic = "test-topic";
    public const string ConsumerGroupName = "test-topic-consumer";

    private static readonly string[] Helps = { "proudce:生产消息", "quit:退出" };

    private static void Main(string[] args) {
      Console.Title = "Kafka测试";
      var ts = new CancellationTokenSource();
      Task.Run(() => {
        ConsumeKafkaMessage(ts.Token);
      }, ts.Token);

      PrintHelps();
      var isStop = false;
      do {
        var input = Console.ReadLine();
        switch (input) {
          case "produce":
            ProduceMessages(100).Wait(ts.Token);
            break;
          case "help":
            PrintHelps();
            break;
          case "quit":
            isStop = true;
            break;
          default:
            Console.WriteLine($"无效指令:{input}");
            break;
        }
      } while (!isStop);

      ts.Cancel();
      Console.WriteLine("按任意键退出...");
      Console.ReadKey();
    }

    private static async Task ProduceMessages(int count) {
      Console.WriteLine("开始发送消息");
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
          Console.WriteLine($"发送：{count}条消息，用时：{stopWatch.ElapsedMilliseconds / 1000}s.\n");
        }
        catch (Exception ex) {
          Console.WriteLine($"发送出错:{ex.Message}\n");
        }
      }
      catch (Exception exception) {
        Console.WriteLine($"准备发送出错:{exception.Message}\n");
      }
    }

    private static void PrintHelps() {
      Console.WriteLine("帮助信息:");
      foreach (var help in Helps) {
        Console.WriteLine($"  {help}");
      }

      Console.WriteLine();
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

            Console.WriteLine($"{DateTime.Now:HH:mm:ss} 获取到消息:{cr.Message.Value} TopicPartition:{cr.TopicPartition.Partition.Value} Offset:{cr.TopicPartitionOffset.Offset.Value}");
            //Thread.Sleep(500);
          }
          catch (ConsumeException e) {
            Console.WriteLine($"消费出错(ConsumeException):{e.Message}");
          }
          catch (TopicPartitionOffsetException e) {
            Console.WriteLine($"消费出错(TopicPartitionOffsetException):{e.Message}");
          }
          catch (KafkaException e) {
            Console.WriteLine($"消费出错(KafkaException):{e.Message}");
          }
          catch (OperationCanceledException e) {
            Console.WriteLine($"消费出错(OperationCanceledException):{e.Message}");
            break;
          }
          catch (Exception e) {
            Console.WriteLine($"消费出错(Exception):{e.Message}");
          }

          if (cancellationToken.IsCancellationRequested) {
            Console.WriteLine("cancellationToken.IsCancellationRequested 退出线程。");
            break;
          }
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"订阅出错:{ex.Message}");
      }

      consumer.Close();
      Console.WriteLine("消费线程结束。");
    }
  }
}