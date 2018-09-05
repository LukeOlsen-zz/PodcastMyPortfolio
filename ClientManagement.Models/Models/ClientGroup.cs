using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientGroup
    {
        public int Id { get; set; }
        public int FirmId { get; set; }
        public string FirmGroupId { get; set; }
        public string Name { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }

    }
}
