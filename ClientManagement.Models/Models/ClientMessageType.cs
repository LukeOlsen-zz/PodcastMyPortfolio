using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientManagement.Models
{
    public class ClientMessageType
    {
        public int Id { get; set; }
        public byte Order { get; set; }
        public string Name { get; set; }
        public string UploadCode { get; set; }
    }
}


