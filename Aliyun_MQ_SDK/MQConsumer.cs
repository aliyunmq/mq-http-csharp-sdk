﻿
using Aliyun.MQ.Model;
using Aliyun.MQ.Model.Internal.MarshallTransformations;
using Aliyun.MQ.Runtime;
using Aliyun.MQ.Util;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aliyun.MQ
{
    public partial class MQConsumer
    {
        private string _instanceId;
        private string _topicName;
        private string _consumer;
        private string _messageTag;
        private readonly HttpClientBasedAliyunServiceClient _serviceClient;

        public MQConsumer(string instanceId, string topicName, string consumer, string messageTag, HttpClientBasedAliyunServiceClient serviceClient)
        {
            this._instanceId = instanceId;
            this._topicName = topicName;
            this._consumer = consumer;
            if (messageTag != null)
            {
                this._messageTag = AliyunSDKUtils.UrlEncode(messageTag, false);
            }
            this._serviceClient = serviceClient;
        }

        public string IntanceId
        {
            get { return this._instanceId; }
        }

        public bool IsSetInstance()
        {
            return !string.IsNullOrEmpty(this._instanceId);
        }

        public string TopicName
        {
            get { return this._topicName; }
        }

        public bool IsSetTopicName()
        {
            return this._topicName != null;
        }

        public string Consumer
        {
            get { return this._consumer; }
        }

        public bool IsSetConsumer()
        {
            return this._consumer != null;
        }

        public string MessageTag
        {
            get { return this._messageTag; }
        }

        public bool IsSetMessageTag()
        {
            return this._messageTag != null;
        }

        public AckMessageResponse AckMessage(List<string> receiptHandles)
        {
            var request = new AckMessageRequest(this._topicName, this._consumer, receiptHandles);
            request.IntanceId = this._instanceId;
            var marshaller = new AckMessageRequestMarshaller();
            var unmarshaller = AckMessageResponseUnmarshaller.Instance;

            return _serviceClient.Invoke<AckMessageRequest, AckMessageResponse>(request, marshaller, unmarshaller);
        }

        public async Task<AckMessageResponse> AckMessageAsync(List<string> receiptHandles)
        {
            var request = new AckMessageRequest(this._topicName, this._consumer, receiptHandles);
            request.IntanceId = this._instanceId;
            var marshaller = new AckMessageRequestMarshaller();
            var unmarshaller = AckMessageResponseUnmarshaller.Instance;

            var ackMessageResponse = await _serviceClient.InvokeAsync<AckMessageRequest, AckMessageResponse>(request, marshaller, unmarshaller, default(CancellationToken));
            return ackMessageResponse;
        }

        public List<Message> ConsumeMessage(uint batchSize)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            ConsumeMessageResponse result = _serviceClient.Invoke<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller);

            return result.Messages;
        }

        public async Task<List<Message>> ConsumeMessageAsync(uint batchSize)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            var consumeMessageResponse = await _serviceClient.InvokeAsync<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller,
                unmarshaller, default(CancellationToken));
            return consumeMessageResponse.Messages;
        }

        public List<Message> ConsumeMessageOrderly(uint batchSize)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.Trasaction = "order";
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            ConsumeMessageResponse result = _serviceClient.Invoke<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller);
            return result.Messages;
        }

        public async Task<List<Message>> ConsumeMessageOrderlyAsync(uint batchSize)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.Trasaction = "order";
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;
            
            var consumeMessageResponse = await _serviceClient.InvokeAsync<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller,
                unmarshaller, default(CancellationToken));
            return consumeMessageResponse.Messages;
        }

        public List<Message> ConsumeMessage(uint batchSize, uint waitSeconds)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.WaitSeconds = waitSeconds;
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            ConsumeMessageResponse result = _serviceClient.Invoke<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller);

            return result.Messages;
        }

        public async Task<List<Message>> ConsumeMessageAsync(uint batchSize, uint waitSeconds)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.WaitSeconds = waitSeconds;
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            var consumeMessageResponse = await _serviceClient.InvokeAsync<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller, default(CancellationToken));
            return consumeMessageResponse.Messages;
        }

        public List<Message> ConsumeMessageOrderly(uint batchSize, uint waitSeconds)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.WaitSeconds = waitSeconds;
            request.Trasaction = "order";
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            ConsumeMessageResponse result = _serviceClient.Invoke<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller);

            return result.Messages;
        }

        public async Task<List<Message>> ConsumeMessageOrderlyAsync(uint batchSize, uint waitSeconds)
        {
            var request = new ConsumeMessageRequest(this._topicName, this._consumer, this._messageTag);
            request.IntanceId = this._instanceId;
            request.BatchSize = batchSize;
            request.WaitSeconds = waitSeconds;
            request.Trasaction = "order";
            var marshaller = ConsumeMessageRequestMarshaller.Instance;
            var unmarshaller = ConsumeMessageResponseUnmarshaller.Instance;

            var consumeMessageResponse = await _serviceClient.InvokeAsync<ConsumeMessageRequest, ConsumeMessageResponse>(request, marshaller, unmarshaller, default(CancellationToken));
            return consumeMessageResponse.Messages;
        }
    }
}
