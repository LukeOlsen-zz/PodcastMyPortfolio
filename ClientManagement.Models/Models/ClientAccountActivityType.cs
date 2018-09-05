using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class ClientAccountActivityType
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string UploadCode { get; set; }
    }
}
