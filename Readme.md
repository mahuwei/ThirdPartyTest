# 说明
用于测试第三方组件库

## 1. Common 通用配置
- 创建控制台日志输出对象
- 分拆控制台输入命令信息；格式：command p1=pv p2=pv...

## 2. KafkaTest：Kafka测试
- 测试kafka组件：Confluent.Kafka
- 生成消息和消费消息
- 当前使用的是单机Kafka: create-network.sh 和 docker-compose-single-broker.yml
```sh
echo "create docker network:zookeeper_network"
docker network create --driver bridge --subnet 172.23.0.0/25 --gateway 172.23.0.1  zookeeper_network

# 修改添加执行权限
# chmod a+x create-network.sh
# 执行
# ./create-network.sh
```

```yml
version: '3.4'

services:
  zookeeper:
    image: wurstmeister/zookeeper
    restart: always
    ports:
      - 2181:2181
    volumes:
      - ./single/zoo/data:/data
      - ./single/zoo/datalog:/datalog
      - ./single/zoo/logs:/logs
    networks:
      default:
        ipv4_address: 172.23.0.30

  kafka:
    image: wurstmeister/kafka:2.12-2.5.0
    restart: always
    ports:
      - "9092:9092"
    environment:
      KAFKA_ADVERTISED_HOST_NAME: 192.168.1.51
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
    volumes:
      - ./single/kafka/logs:/kafka
    networks:
      default:
        ipv4_address: 172.23.0.31

networks:
  default:
    external:
      name: zookeeper_network
```

## 3. ConsulTest：服务发现和配置管理
- docker启动
```yml
docker run -itd --name  cs1 -p 8500:8500  -v /consul/data:/consul/data consul:1.7.2 agent -server -bind 127.0.0.1 -node consul-server-1 -bootstrap-expect 1 -client 0.0.0.0 -ui
```

- 注册服务
  - 注册
  - 健康检查
  - 重复注册
 
- 配置信息
  - 添加
  - 读取
  - 热更新
  - 导入
  - 导出

- 集群配置