using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastServer.Utilities
{
    public class AppSettings
    {
        public string AzureSpeechServicesKey {get;set;}
        public string AzureSpeechAPIEndPoint { get; set; }
        public string LogFile { get; set; }
        public string DefaultWelcomeMessage { get; set; }
        public string DefaultVoiceServiceId { get; set; }
        public string DefaultNotFoundMessage { get; set; }
        public string ClientUserPasswordKey { get; set; }
        public string PodcastServerEndPoint { get; set; }
    }
}
