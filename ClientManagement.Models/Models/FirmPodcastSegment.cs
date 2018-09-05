using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class FirmPodcastSegment
    {
        public int Id { get; set; }
        public int FirmId { get; set; }
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
}
