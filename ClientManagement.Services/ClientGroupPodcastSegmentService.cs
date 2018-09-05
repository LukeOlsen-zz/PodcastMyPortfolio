using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientGroupPodcastSegmentService
    {
        ClientGroupPodcastSegmentWithFirm Get(int id);
        ClientGroupPodcastSegment Get(string podcastId);
        void Update(ClientGroupPodcastSegment clientGroupPodcastSegment);
        IEnumerable<ClientGroupPodcastSegmentWithGroup> GetAllInFirm(int firmId);
        ClientGroupPodcastSegment Create(ClientGroupPodcastSegment clientGroupPodcastSegment);
        void Delete(int id);
        IEnumerable<ClientGroupPodcastSegment> GetAllAvailableForClient(int clientId);
        void MarkAsReceived(int clientId, int clientGroupPodcastSegmentId);
        void MarkAsPublished(int clientId, int clientGroupPodcastSegmentId);

    }
    public class ClientGroupPodcastSegmentService : IClientGroupPodcastSegmentService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();


        public ClientGroupPodcastSegmentService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientGroupPodcastSegment Create(ClientGroupPodcastSegment clientGroupPodcastSegment)
        {
            clientGroupPodcastSegment.PodcastId = Guid.NewGuid();
            _context.ClientGroupPodcastSegments.Add(clientGroupPodcastSegment);
            _context.SaveChanges();

            return clientGroupPodcastSegment;
        }

        public void Delete(int id)
        {
            var segment = _context.ClientGroupPodcastSegments.Find(id);
            if (segment != null)
            {
                _context.ClientGroupPodcastSegments.Remove(segment);
                _context.SaveChanges();
            }
        }

        public ClientGroupPodcastSegmentWithFirm Get(int id)
        {
            var q = (from cgps in _context.ClientGroupPodcastSegments
                     join cg in _context.ClientGroups on cgps.ClientGroupId equals cg.Id
                     where cgps.Id == id
                     select new ClientGroupPodcastSegmentWithFirm()
                     {
                         ClientGroupPodcastSegment = cgps,
                         FirmId = cg.FirmId
                     }).FirstOrDefault();

            return q;
        }

        public ClientGroupPodcastSegment Get(string podcastId)
        {
            ClientGroupPodcastSegment cgps = _context.ClientGroupPodcastSegments.SingleOrDefault(c => c.PodcastId == new Guid(podcastId));
            return cgps;
        }


        public IEnumerable<ClientGroupPodcastSegmentWithGroup> GetAllInFirm(int firmId)
        {
            var q = (from cgps in _context.ClientGroupPodcastSegments
                     join cg in _context.ClientGroups on cgps.ClientGroupId equals cg.Id
                     join f in _context.Firms on cg.FirmId equals f.Id
                     where f.Id == firmId
                     orderby cg.Name, cgps.Title
                     select new ClientGroupPodcastSegmentWithGroup()
                     {
                         ClientGroupPodcastSegment = cgps,
                         GroupName = cg.Name
                     }).ToList();
            return q;
        }

        public void Update(ClientGroupPodcastSegment clientGroupPodcastSegment)
        {
            clientGroupPodcastSegment.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(clientGroupPodcastSegment);
            _context.SaveChanges();
        }

        public int GetFirmIdOfClientGroupPodcastSegment(int id)
        {
            var q = (from cgps in _context.ClientGroupPodcastSegments
                     join cg in _context.ClientGroups on cgps.ClientGroupId equals cg.Id
                     where cgps.Id == id
                     select new
                     {
                         Id = cg.FirmId
                     }).FirstOrDefault();

            return q.Id;
        }

        public IEnumerable<ClientGroupPodcastSegment> GetAllAvailableForClient(int clientId)
        {
            LocalDateTime now = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            var q = (from c in _context.Clients
                     join cgps in _context.ClientGroupPodcastSegments on c.ClientGroupId equals cgps.ClientGroupId
                     where c.Id == clientId
                        && !_context.ClientPublishedClientGroupPodcastSegments.Any(r => r.ClientId == c.Id && r.ClientGroupPodcastSegmentId == cgps.Id)
                        && cgps.StartsOn <= now && now <= cgps.EndsOn 
                     select cgps
                     ).ToList();

            return q;
        }

        public void MarkAsReceived(int clientId, int clientGroupPodcastSegmentId)
        {
            // Did we already mark it as read? 
            var cpodseg = _context.ClientReceivedClientGroupPodcastSegments.Find(clientId, clientGroupPodcastSegmentId);

            if (cpodseg == null)
            {
                ClientReceivedClientGroupPodcastSegment crcgps = new ClientReceivedClientGroupPodcastSegment { ClientId = clientId, ClientGroupPodcastSegmentId = clientGroupPodcastSegmentId, AddedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime };
                _context.ClientReceivedClientGroupPodcastSegments.Add(crcgps);
                _context.SaveChanges();
            }
        }

        public void MarkAsPublished(int clientId, int clientGroupPodcastSegmentId)
        {
            // Did we already mark it as published? 
            var cpodseg = _context.ClientPublishedClientGroupPodcastSegments.Find(clientId, clientGroupPodcastSegmentId);

            if (cpodseg == null)
            {
                ClientPublishedClientGroupPodcastSegment crcgps = new ClientPublishedClientGroupPodcastSegment { ClientId = clientId, ClientGroupPodcastSegmentId = clientGroupPodcastSegmentId, AddedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime };
                _context.ClientPublishedClientGroupPodcastSegments.Add(crcgps);
                _context.SaveChanges();
            }
        }
    }
}
