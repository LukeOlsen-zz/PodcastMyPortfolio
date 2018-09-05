using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientAccountPeriodicData
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public LocalDate PeriodicDataAsOf { get; set; }
        public Decimal EndingBalance { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
    }
}
