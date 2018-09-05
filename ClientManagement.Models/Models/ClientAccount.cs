using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientAccount
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public string FirmClientAccountId { get; set; }
        public string Name { get; set; }
        public string CommonName { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
        public int FirmId { get; set; }
    }
}
