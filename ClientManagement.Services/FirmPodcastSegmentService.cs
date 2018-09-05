using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IFirmPodcastSegmentService
    {
        FirmPodcastSegment Get(int id);
        FirmPodcastSegment Get(string podcastId);
        void Update(FirmPodcastSegment firmPodcastSegment);
        IEnumerable<FirmPodcastSegment> GetAllInFirm(int firmId);
        FirmPodcastSegment Create(FirmPodcastSegment firmPodcastSegment);
        void Delete(int id);
        IEnumerable<FirmPodcastSegment> GetAllAvailableForClient(int clientId);
        void MarkAsReceived(int clientId, int firmPodcastSegmentId);
        void MarkAsPublished(int clientId, int firmPodcastSegmentId);
    }


    public class FirmPodcastSegmentService : IFirmPodcastSegmentService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public FirmPodcastSegmentService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public FirmPodcastSegment Create(FirmPodcastSegment firmPodcastSegment)
        {
            firmPodcastSegment.PodcastId = Guid.NewGuid();
            _context.FirmPodcastSegments.Add(firmPodcastSegment);
            _context.SaveChanges();

            return firmPodcastSegment;
        }

        public void Delete(int id)
        {
            var segment = _context.FirmPodcastSegments.Find(id);
            if (segment != null)
            {
                _context.FirmPodcastSegments.Remove(segment);
                _context.SaveChanges();
            }
        }

        public FirmPodcastSegment Get(int id)
        {
            return _context.FirmPodcastSegments.Find(id);
        }

        public FirmPodcastSegment Get(string podcastId)
        {
            FirmPodcastSegment fps = _context.FirmPodcastSegments.SingleOrDefault(c => c.PodcastId == new Guid(podcastId));
            return fps;
        }

        public IEnumerable<FirmPodcastSegment> GetAllInFirm(int firmId)
        {
            return _context.FirmPodcastSegments.Where(c => c.FirmId == firmId).ToList();
        }

        public void Update(FirmPodcastSegment firmPodcastSegment)
        {
            firmPodcastSegment.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(firmPodcastSegment);
            _context.SaveChanges();
        }

        public IEnumerable<FirmPodcastSegment> GetAllAvailableForClient(int clientId)
        {
            LocalDateTime now = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            var q = (from c in _context.Clients
                     join fps in _context.FirmPodcastSegments on c.FirmId equals fps.FirmId
                     where c.Id == clientId 
                        && !_context.ClientPublishedFirmPodcastSegments.Any(r => r.ClientId == c.Id && r.FirmPodcastSegmentId == fps.Id)
                        && fps.StartsOn <= now && now <= fps.EndsOn 
            select fps
                     ).ToList();

            return q;
        }

        public void MarkAsReceived(int clientId, int firmPodcastSegmentId)
        {
            // Did we already mark it as read? 
            var fpodseg = _context.ClientReceivedFirmPodcastSegments.Find(clientId, firmPodcastSegmentId);

            if (fpodseg == null)
            {
                ClientReceivedFirmPodcastSegment crfps = new ClientReceivedFirmPodcastSegment { ClientId = clientId, FirmPodcastSegmentId = firmPodcastSegmentId, AddedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime };
                _context.ClientReceivedFirmPodcastSegments.Add(crfps);
                _context.SaveChanges();
            }
        }

        public void MarkAsPublished(int clientId, int firmPodcastSegmentId)
        {
            // Did we already mark it as published? 
            var fpodseg = _context.ClientPublishedFirmPodcastSegments.Find(clientId, firmPodcastSegmentId);

            if (fpodseg == null)
            {
                ClientPublishedFirmPodcastSegment crfps = new ClientPublishedFirmPodcastSegment { ClientId = clientId, FirmPodcastSegmentId = firmPodcastSegmentId, AddedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime };
                _context.ClientPublishedFirmPodcastSegments.Add(crfps);
                _context.SaveChanges();
            }
        }






    }
}
