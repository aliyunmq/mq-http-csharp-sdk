
using System;

namespace Aliyun.MQ.Model
{

    public partial class TopicMessage
    {
        private string _id;
        private string _bodyMD5;

        private string _body;
        private string _messageTag;


        public TopicMessage(string body) 
        {
            this._body = body;
        }

        public TopicMessage(string body, string messageTag)
        {
            this._body = body;
            this._messageTag = messageTag;
        }


        public string Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        // Check to see if Id property is set
        internal bool IsSetId()
        {
            return this._id != null;
        }

        public string Body
        {
            get { return this._body; }
        }

        public string MessageTag
        {
            get { return this._messageTag; }
            set { this._messageTag = value; }
        }

        public bool IsSetBody()
        {
            return this._body != null;
        }

        public string BodyMD5
        {
            get { return this._bodyMD5; }
            set { this._bodyMD5 = value; }
        }

        internal bool IsSetBodyMD5()
        {
            return this._bodyMD5 != null;
        }

    }
}
