using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestProto
{
    class ChannelMessage
    {
        private MessageRegistry messageId;
        private byte[] body;

        public ChannelMessage(MessageRegistry messageId, byte[] body)
        {
            this.messageId = messageId;
            this.body = body;
        }

        public MessageRegistry GetMessageId()
        {
            return this.messageId;
        }

        public byte[] GetBody()
        {
            return body;
        }
    }
}
