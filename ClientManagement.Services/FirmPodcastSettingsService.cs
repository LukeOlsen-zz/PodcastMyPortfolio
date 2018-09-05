using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IFirmPodcastSettingsService
    {
        FirmPodcastSettings Get(int id);
        FirmPodcastSettingsWithVoice GetWithVoice(int id);
        FirmPodcastSettingsWithVoice GetWithVoice(Guid podcastId);
        void Update(FirmPodcastSettings firmPodcastSettings);
        FirmPodcastSettings Create(FirmPodcastSettings firmPodcastSettings);
    }


    public class FirmPodcastSettingsService : IFirmPodcastSettingsService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public FirmPodcastSettingsService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public FirmPodcastSettings Get(int firmId)
        {
            var fps = _context.FirmPodcastSettings.Find(firmId);
            if (fps == null)
            {
                FirmPodcastSettings firmPodcastSettings = new FirmPodcastSettings();
                firmPodcastSettings.FirmId = firmId;
                fps = Create(firmPodcastSettings);
            }
            return fps;
        }

        public FirmPodcastSettingsWithVoice GetWithVoice(int firmId)
        {
            var fpsv = (from fs in _context.FirmPodcastSettings
                     join v in _context.Voices on fs.PodcastVoiceId equals v.Id
                     where fs.FirmId == firmId
                     select new FirmPodcastSettingsWithVoice()
                     {
                         FirmPodcastSettings = fs,
                         Voice = v
                     })
                     .SingleOrDefault();

            return fpsv;
        }

        public FirmPodcastSettingsWithVoice GetWithVoice(Guid podcastId)
        {
            var q = (from fs in _context.FirmPodcastSettings
                     join v in _context.Voices on fs.PodcastVoiceId equals v.Id
                     where fs.PodcastId == podcastId
                     select new FirmPodcastSettingsWithVoice()
                     {
                         FirmPodcastSettings = fs,
                         Voice = v
                     })
                     .SingleOrDefault();

            return q;
        }

        public FirmPodcastSettings Create(FirmPodcastSettings firmPodcastSettings)
        {
            try
            {
                // We need to assign a default voice if one is not assigned
                if (firmPodcastSettings.PodcastVoiceId == 0)
                {
                    var defaultVoice = _context.Voices.FirstOrDefault(c => c.DefaultForNewFirms == true);
                    firmPodcastSettings.PodcastVoiceId = defaultVoice.Id;
                }

                firmPodcastSettings.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                firmPodcastSettings.PodcastId = System.Guid.NewGuid();

                _context.FirmPodcastSettings.Add(firmPodcastSettings);
                _context.SaveChanges();
                return firmPodcastSettings;
            }
            catch (Exception)
            {
                _context.Entry(firmPodcastSettings).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Update(FirmPodcastSettings firmPodcastSettings)
        {
            firmPodcastSettings.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(firmPodcastSettings);
            _context.SaveChanges(); 
        }

    }
}
