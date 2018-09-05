using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class InvalidAccessAttempt
    {
        public string IPAddress { get; set; }
        public int HitCount { get; set; }
        public LocalDateTime LastHitOn { get; set; }
        public LocalDateTime AddedOn { get; }
    }
}
