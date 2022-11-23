using System;
using System.Threading;
using Aliyun.MQ.Model;

namespace Aliyun.MQ.Sample
{
    public class ProducerSample
    {
        // 设置HTTP接入域名（此处以公共云生产环境为例）
        private const string _endpoint = "http://1917156927057806.mqrest.cn-hangzhou.aliyuncs.com";

        // AccessKey 阿里云身份验证，在阿里云服务器管理控制台创建
        private const string _accessKeyId = "ak";

        // SecretKey 阿里云身份验证，在阿里云服务器管理控制台创建
        private const string _secretAccessKey = "sk";

        // 所属的 Topic
        private const string _topicName = "lingchu_normal_topic";

        // Topic所属实例ID，默认实例为空
        private const string _instanceId = "MQ_INST_1917156927057806_BYR7WyCI";
        private static MQClient _client = new MQClient(_accessKeyId, _secretAccessKey, _endpoint);
        static MQProducer producer = _client.GetProducer(_instanceId, _topicName);

        static void Main(string[] args)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    TopicMessage sendMsg = new TopicMessage("dfadfadfadf");
                    // 设置KEY
                    sendMsg.MessageKey = "MessageKey";

                    TopicMessage result = producer.PublishMessage(sendMsg);
                    Console.WriteLine("publish message success:" + result);
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }
            }

            Thread.Sleep(999999999);
        }
    }
}