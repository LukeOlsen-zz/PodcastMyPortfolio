using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientManagement.Models
{
    public class Voice
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ServiceId { get; set; }
        public bool DefaultForNewFirms { get; set; }

    }
}
