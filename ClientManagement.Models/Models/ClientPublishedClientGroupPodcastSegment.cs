using System;
using System.Collections.Generic;
using System.Text;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientPublishedClientGroupPodcastSegment
    {
        public int ClientId { get; set; }
        public int ClientGroupPodcastSegmentId { get; set; }
        public LocalDateTime AddedOn { get; set; }
    }
}
