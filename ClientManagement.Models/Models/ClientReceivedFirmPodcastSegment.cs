using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientReceivedFirmPodcastSegment
    {
        public int ClientId { get; set; }
        public int FirmPodcastSegmentId { get; set; }
        public LocalDateTime AddedOn { get; set; }
    }
}
