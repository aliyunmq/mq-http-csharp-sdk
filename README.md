# MQ HTTP C# SDK  
Aliyun MQ Documents: http://www.aliyun.com/product/ons

Aliyun MQ Console: https://ons.console.aliyun.com  

## Use

1. 下载最新版csharp sdk，解压后将工程导入到VisualStudio，其中Aliyun_MQ_SDK就是sdk所在的目录;
2. Sample中替换相关的参数

## Note
1. Http consumer only support timer msg (less than 3 days), no matter the msg is produced from http or tcp protocol.
2. Order is only supported at special server cluster.

## Samples (github)

[Publish Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/producer.cs)

[Consume Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/consumer.cs)

[Transaction Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/trans_producer.cs)

[Publish Order Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/order_producer.cs)

[Consume Order Message](https://github.com/aliyunmq/mq-http-samples/blob/master/csharp/order_consumer.cs)

### Sample (code.aliyun.com)

[Publish Message](https://code.aliyun.com/aliware_rocketmq/mq-http-samples/blob/master/csharp/producer.cs)

[Consume Message](https://code.aliyun.com/aliware_rocketmq/mq-http-samples/blob/master/csharp/consumer.cs)

[Transaction Message](https://code.aliyun.com/aliware_rocketmq/mq-http-samples/blob/master/csharp/trans_producer.cs)

[Publish Order Message](https://code.aliyun.com/aliware_rocketmq/mq-http-samples/blob/master/csharp/order_producer.cs)

[Consume Order Message](https://code.aliyun.com/aliware_rocketmq/mq-http-samples/blob/master/csharp/order_consumer.cs)