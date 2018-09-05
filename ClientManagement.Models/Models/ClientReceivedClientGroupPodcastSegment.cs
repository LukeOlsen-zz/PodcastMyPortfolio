using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientReceivedClientGroupPodcastSegment
    {
        public int ClientId { get; set; }
        public int ClientGroupPodcastSegmentId { get; set; }
        public LocalDateTime AddedOn { get; set; }
    }
}
