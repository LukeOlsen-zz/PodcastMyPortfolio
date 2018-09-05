using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientMessageService
    {
        ClientMessage Get(int id);
        ClientMessage Get(string podcastId);
        void Update(ClientMessage clientMessage);
        ClientMessage Create(ClientMessage clientMessage);
        void Delete(int id);
        IEnumerable<ClientMessage> GetAllAvailableToClient(int clientId, bool? systemGenerated = null);
        IEnumerable<ClientMessageWithType> Get(int accountId, int pageNumber, int perPageQuantity);
        int GetCountInClient(int clientId);
        void MarkAsPublished(int id);
    }


    public class ClientMessageService : IClientMessageService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientMessageService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientMessage Create(ClientMessage clientMessage)
        {
            // Add new message for client. BUT there can only be ONE unreceived message per message type. We will need to check that first

            // Do we have an existing unreceived message for this message type for this client?
            var existing_UnreceivedClientMessage = _context.ClientMessages.FirstOrDefault(cm => cm.ClientId == clientMessage.ClientId && cm.ClientMessageTypeId == clientMessage.ClientMessageTypeId && cm.ReceivedByClient == false);

            if (existing_UnreceivedClientMessage == null)
            {
                clientMessage.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                clientMessage.PodcastId = Guid.NewGuid();
                _context.ClientMessages.Add(clientMessage);
                _context.SaveChanges();
                return clientMessage;
            }
            else
            {
                // Update existing
                existing_UnreceivedClientMessage.ExpiresOn = clientMessage.ExpiresOn;
                existing_UnreceivedClientMessage.Message = clientMessage.Message;
                existing_UnreceivedClientMessage.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;

                _context.Attach(existing_UnreceivedClientMessage);
                _context.SaveChanges();
                return existing_UnreceivedClientMessage;
            }
        }

        public void Delete(int id)
        {
            var cm = _context.ClientMessages.Find(id);
            if (cm != null)
            {
                _context.ClientMessages.Remove(cm);
                _context.SaveChanges();
            }
        }

        public ClientMessage Get(int id)
        {
            return _context.ClientMessages.Find(id);
        }

        public ClientMessage Get(string podcastId)
        {
            return _context.ClientMessages.SingleOrDefault(c => c.PodcastId == new Guid(podcastId));
        }

        public IEnumerable<ClientMessage> GetAllAvailableToClient(int clientId, bool? systemGenerated = null)
        {
            LocalDateTime now = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;

            // Messages available to a client are those (of a message type) that have not been received and that have not expired.
            var cms = (from cm in _context.ClientMessages
                       join cmt in _context.ClientMessageTypes on cm.ClientMessageTypeId equals cmt.Id
                       where cm.ClientId == clientId && cm.PublishedToClient == false && (cm.ExpiresOn == null || cm.ExpiresOn > now) 
                            && (systemGenerated.HasValue ? systemGenerated.Value == cm.SystemGeneratedMessage : true)
                       orderby cmt.Order
                       select cm).ToList();
            return cms;
        }

        public IEnumerable<ClientMessageWithType> Get(int clientId, int pageNumber, int perPageQuantity)
        {
            var q = (from cm in _context.ClientMessages
                     join cmt in _context.ClientMessageTypes on cm.ClientMessageTypeId equals cmt.Id
                     where cm.ClientId == clientId && cm.SystemGeneratedMessage == false
                     orderby cmt.Order, cm.AddedOn
                     select new ClientMessageWithType()
                     {
                         Id = cm.Id,
                         ClientId = cm.ClientId,
                         ClientMessage = cm.Message,
                         ClientMessageTypeId = cm.ClientMessageTypeId,
                         ExpiresOn = cm.ExpiresOn,
                         ReceivedByClient = cm.ReceivedByClient,
                         MessageTypeName = cmt.Name,
                         MessageTypeOrder = cmt.Order,
                         MessageTypeUploadCode = cmt.UploadCode
                     })
                     .Skip(pageNumber * perPageQuantity)
                     .Take(perPageQuantity)
                     .ToList();

            return q;
        }

        public int GetCountInClient(int clientId)
        {
            var q = _context.ClientMessages
                .Where(c => c.ClientId == clientId);
            var total = q.Count();
            return total;
        }

        public void Update(ClientMessage clientMessage)
        {
            clientMessage.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(clientMessage);
            _context.SaveChanges();
        }

        public void MarkAsPublished(int id)
        {
            var cm = _context.ClientMessages.Find(id);

            if (cm != null)
            {
                cm.PublishedToClient = true;
                _context.Attach(cm);
                _context.SaveChanges();
            }
        }
    }
}
