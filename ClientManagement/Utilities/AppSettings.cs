using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClientManagement.Utilities
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string AzureSpeechServicesKey {get;set;}
        public string AzureSpeechAPIEndPoint { get; set; }
        public string ClientUserPasswordKey { get; set; }
        public string SendGridAPIKey { get; set; }
    }
}
