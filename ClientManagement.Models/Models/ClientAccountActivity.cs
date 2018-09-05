using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientAccountActivity
    {
        public int Id { get; set; }
        
        public int AccountId { get; set; }
        public LocalDate ActivityDate { get; set; }
        public int ActivityTypeId { get; set; }
        public decimal ActivityAmount { get; set; }
        public string ActivityDescriptionOverride { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
    }


    public class ClientAccountActivityWithType
    {
        public int Id { get; set; }

        public int AccountId { get; set; }
        public LocalDate ActivityDate { get; set; }
        public int ActivityTypeId { get; set; }
        public decimal ActivityAmount { get; set; }
        public string ActivityDescriptionOverride { get; set; }
        public string ActivityTypeName { get; set; }
        public string ActivityTypeUploadCode { get; set; }
    }
}
