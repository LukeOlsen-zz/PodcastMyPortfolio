using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class FirmPodcastSettings
    {

        public int FirmId { get; set; }
        public LocalDateTime AddedOn { get; }
        public int PodcastVoiceId { get; set; }
        public string PodcastWelcomeMessage { get; set; }
        public string PodcastNotFoundMessage { get; set; }
        public LocalTime PodcastPublishingTime { get; set; }
        public bool PodcastPublishingMonday { get; set; }
        public bool PodcastPublishingTuesday { get; set; }
        public bool PodcastPublishingWednesday { get; set; }
        public bool PodcastPublishingThursday { get; set; }
        public bool PodcastPublishingFriday { get; set; }
        public bool PodcastPublishingSaturday { get; set; }
        public bool PodcastPublishingSunday { get; set; }
        public string PodcastFirmSiteURL { get; set; }
        public string PodcastFirmLogoId { get; set; }
        public LocalDateTime UpdatedOn { get; set; }
        public Guid PodcastId { get; set; }
        public string PodcastContactEmail { get; set; }
        public string PodcastContactName { get; set; }
        public string PodcastDescription { get; set; }



        public LocalDate? MostRecentDateAllowedForPodcastPublishing(LocalDateTime referenceDateTime)
        {
            LocalDate? mostRecentDateAllowedForPublishing = null;

            // We need to find the last date publishing was allowed
            int offset = -1;
            for (int i = 0; i < 7; i++)
            {
                LocalDate hypotheticalPublishingDate = referenceDateTime.Date.PlusDays(-i);
                IsoDayOfWeek hypotheticalPublishingDayOfWeek = hypotheticalPublishingDate.DayOfWeek;

                // Is publishing allowed on this day?
                switch (hypotheticalPublishingDayOfWeek)
                {
                    case IsoDayOfWeek.Sunday:
                        if (PodcastPublishingSunday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Saturday:
                        if (PodcastPublishingSaturday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Friday:
                        if (PodcastPublishingFriday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Thursday:
                        if (PodcastPublishingThursday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Wednesday:
                        if (PodcastPublishingWednesday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Tuesday:
                        if (PodcastPublishingTuesday)
                            offset = i;
                        break;
                    case IsoDayOfWeek.Monday:
                        if (PodcastPublishingMonday)
                            offset = i;
                        break;
                }

                // WAIT! If offset is 0 this means we are on a publishing date and we need to further check the time
                // If we are good on time let it pass otherwise this day doesn't count
                if (offset == 0)
                {
                    if (referenceDateTime.TimeOfDay < PodcastPublishingTime)
                        offset = -1;
                }

                if (offset > -1)
                {
                    mostRecentDateAllowedForPublishing = referenceDateTime.Date.PlusDays(-offset);
                    break;
                }
            }

            return mostRecentDateAllowedForPublishing;
        }

        public bool NewPodcastAllowed(LocalDateTime podcastDataDate, LocalDateTime referenceDate)
        {
            bool allowed = false;
            LocalDate? mostRecentDateAllowedForPublishing = MostRecentDateAllowedForPodcastPublishing(referenceDate);

            if (mostRecentDateAllowedForPublishing.HasValue && podcastDataDate.Date <= mostRecentDateAllowedForPublishing.Value)
                allowed = true;

            return allowed;
        }
    }

    public class FirmPodcastSettingsWithVoice
    {
        public FirmPodcastSettings FirmPodcastSettings { get; set; }
        public Voice Voice { get; set; }
    }
}
