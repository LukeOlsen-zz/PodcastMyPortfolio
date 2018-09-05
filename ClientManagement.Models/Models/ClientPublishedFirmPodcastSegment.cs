using System;
using System.Collections.Generic;
using System.Text;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientPublishedFirmPodcastSegment
    {
        public int ClientId { get; set; }
        public int FirmPodcastSegmentId { get; set; }
        public LocalDateTime AddedOn { get; set; }
    }
}
