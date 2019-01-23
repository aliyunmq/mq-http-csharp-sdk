
using Aliyun.MQ.Model;
using Aliyun.MQ.Model.Internal.MarshallTransformations;
using Aliyun.MQ.Runtime;

namespace Aliyun.MQ
{
    public partial class MQProducer
    {

        private string _topicName;
        private string _instanceId;
        private readonly AliyunServiceClient _serviceClient;

        public MQProducer(string instanceId, string topicName, AliyunServiceClient serviceClient)
        {
            this._instanceId = instanceId;
            this._topicName = topicName;
            this._serviceClient = serviceClient;
        }

        public string TopicName
        {
            get { return this._topicName; }
        }

        public bool IsSetTopicName()
        {
            return this._topicName != null;
        }

        public string IntanceId
        {
            get { return this._instanceId; }
        }

        public bool IsSetInstance()
        {
            return !string.IsNullOrEmpty(this._instanceId);
        }

        public TopicMessage PublishMessage(TopicMessage topicMessage)
        {
            var request = new PublishMessageRequest(topicMessage.Body, topicMessage.MessageTag);
            request.TopicName = this._topicName;
            request.IntanceId = this._instanceId;

            var marshaller = PublishMessageRequestMarshaller.Instance;
            var unmarshaller = PublishMessageResponseUnmarshaller.Instance;

            PublishMessageResponse result = _serviceClient.Invoke<PublishMessageRequest, PublishMessageResponse>(request, marshaller, unmarshaller);

            TopicMessage retMsg = new TopicMessage(null);
            retMsg.Id = result.MessageId;
            retMsg.BodyMD5 = result.MessageBodyMD5;

            return retMsg;
        }

    }
}
