using System;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsulTestConsole.Models {
  public class ConsulHelper {
    public static async Task<WriteResult> ServiceRegister(ILogger logger,string ip,int port) {
      var client = new ConsulClient(t => {
        t.Address = new Uri($"{Program.ConsulIp}:{Program.ConsulPort}");
        t.Datacenter = Program.ConsulDataCenter;
      });
      
      var checkIntervalMinutes = Convert.ToInt32(Program.ConsulCheckIntervalMinutes);
      var deRegisterCriticalServiceAfterMinutes = Convert.ToInt32(Program.ConsulDeRegisterCriticalServiceAfterMinutes);

      logger.LogInformation($"服务健康检查间隔:{checkIntervalMinutes}分钟；失败取消注册分钟:{deRegisterCriticalServiceAfterMinutes}分钟。");

      //注册一个实例
      var agentServiceRegistration = new AgentServiceRegistration {
        //注册服务的IP
        Address = ip,
        //服务id唯一的
        ID = $"{Program.NameOfApiServer}-{port}",
        //服务名
        Name = Program.NameOfApiServer,
        //端口
        Port = port,
        Tags = null,
        Check = new AgentServiceCheck {
          //健康检查的API地址
          HTTP =
            $"http://{ip}:{port}/health",
          //间隔多少检查一次
          Interval = TimeSpan.FromMinutes(checkIntervalMinutes),
          //多久注销不健康的服务
          DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(deRegisterCriticalServiceAfterMinutes)
        }
      };
      return await client.Agent.ServiceRegister(agentServiceRegistration);
    }
  }
}