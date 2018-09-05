using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class RecordImportResult
    {
        public int LineNumber { get; set; }
        public string Line { get; set; }
        public string Result { get; set; }
    }
}
