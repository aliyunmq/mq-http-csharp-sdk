# MQ HTTP C# SDK  
Aliyun MQ Documents: http://www.aliyun.com/product/ons

Aliyun MQ Console: https://ons.console.aliyun.com  

## Use

1. 下载最新版csharp sdk，解压后将工程导入到VisualStudio，其中Aliyun_MQ_SDK就是sdk所在的目录;
2. Sample中替换相关的参数

## Samples

### V1.0.0 Samples
[Publish Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/producer.cs)

[Consume Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/consumer.cs)

### V1.0.1 Samples
[Publish Message](https://github.com/aliyunmq/mq-http-samples/tree/101-dev/csharp/producer.cs)

[Consume Message](https://github.com/aliyunmq/mq-http-samples/tree/101-dev/csharp/consumer.cs)

[Transaction Message](https://github.com/aliyunmq/mq-http-samples/tree/101-dev/csharp/trans_producer.cs)

### V1.0.3 Samples
[Publish Message](https://github.com/aliyunmq/mq-http-samples/tree/103-dev/csharp/producer.cs)

[Consume Message](https://github.com/aliyunmq/mq-http-samples/tree/103-dev/csharp/consumer.cs)

[Transaction Message](https://github.com/aliyunmq/mq-http-samples/tree/103-dev/csharp/trans_producer.cs)

[Publish Order Message](https://github.com/aliyunmq/mq-http-samples/tree/103-dev/csharp/order_producer.cs)

[Consume Order Message](https://github.com/aliyunmq/mq-http-samples/tree/103-dev/csharp/order_consumer.cs)

Note: Http consumer only support timer msg(less than 3 days), no matter the msg is produced from http or tcp protocol.
