using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientGroupPodcastSegment
    {
        public int Id { get; set; }
        public int ClientGroupId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string SegmentId { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
        public LocalDateTime? StartsOn { get; set; }
        public LocalDateTime? EndsOn { get; set; }
        public Guid PodcastId { get; set; }
    }


    public class ClientGroupPodcastSegmentWithFirm
    {
        public ClientGroupPodcastSegment ClientGroupPodcastSegment { get; set; }
        public int FirmId { get; set; }
    }

    public class ClientGroupPodcastSegmentWithGroup
    {
        public ClientGroupPodcastSegment ClientGroupPodcastSegment { get; set; }
        public string GroupName { get; set; }
        public int GroupId { get; set; }
    }
}
