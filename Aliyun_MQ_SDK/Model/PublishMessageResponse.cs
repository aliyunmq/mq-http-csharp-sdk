
using Aliyun.MQ.Runtime;

namespace Aliyun.MQ.Model
{
    public partial class PublishMessageResponse : WebServiceResponse
    {
        private string _messageBodyMD5;
        private string _messageId;

        public string MessageBodyMD5
        {
            get { return this._messageBodyMD5; }
            set { this._messageBodyMD5 = value; }
        }

        // Check to see if BodyMD5 property is set
        internal bool IsSetMessageBodyMD5()
        {
            return this._messageBodyMD5 != null;
        }

        public string MessageId
        {
            get { return this._messageId; }
            set { this._messageId = value; }
        }

        // Check to see if MessageId property is set
        internal bool IsSetMessageId()
        {
            return this._messageId != null;
        }

        public override string ToString()
        {
            return string.Format("(MessageId {0}, MessageBodyMD5 {1})", _messageId, _messageBodyMD5);
        }
    }
}
