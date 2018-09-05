using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientMessage
    {
        /// <summary>
        /// Each client can have ONE non-recevied message per message type. Succeeding uploads will overwrite non-received messages of the same type for the client in question. 
        /// e.g. A new balance message will overwrite an older one if has not been received (why hear an old balance?). 
        /// A message will always be available until received or until it expires.
        /// </summary>
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int ClientMessageTypeId { get; set; }
        public string Message { get; set; }
        public LocalDateTime? ExpiresOn { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
        public bool ReceivedByClient { get; set; }
        public LocalDateTime? ReceivedByClientOn { get; set; }
        public Guid PodcastId { get; set; }
        public bool SystemGeneratedMessage { get; set; }

        public bool PublishedToClient { get; set; }

        public ClientMessage()
        {
        }

        public ClientMessage(int clientId, int clientMessageTypeId, string message, LocalDateTime? expiresOn, bool systemGeneratedMessage)
        {
            ClientId = clientId;
            ClientMessageTypeId = clientMessageTypeId;
            Message = message;
            ExpiresOn = expiresOn;
            SystemGeneratedMessage = systemGeneratedMessage;

            PodcastId = System.Guid.NewGuid();
        }
    }


    public class ClientMessageWithType
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int ClientMessageTypeId { get; set; }
        public string ClientMessage { get; set; }
        public LocalDateTime? ExpiresOn { get; set; }
        public bool ReceivedByClient { get; set; }
        public LocalDateTime? ReceivedByClientOn { get; set; }
        public byte MessageTypeOrder { get; set; }
        public string MessageTypeName { get; set; }
        public string MessageTypeUploadCode { get; set; }
    }
}
