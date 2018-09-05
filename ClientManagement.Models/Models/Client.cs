using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace ClientManagement.Models
{
    public class Client
    {
        public int Id { get; set; }
        public int ClientGroupId { get; set; }
        public string Name { get; set; }
        public LocalDateTime AddedOn { get; }
        public LocalDateTime UpdatedOn { get; set; }
        public int FirmId { get; set; }
        public string FirmClientId { get; set; }
        public string EmailAddress { get; set; }
        public bool AccountActivityNew { get; set; }
        public LocalDateTime? AccountActivityImportedOn { get; set; }
        public bool PeriodicDataNew { get; set; }
        public LocalDateTime? PeriodicDataImportedOn { get; set; }
        public int? PodcastVoiceId { get; set; }
        public LocalDateTime? PodcastLastAccessOn { get; set; }
        public string Podcast { get; set; }
        public LocalDateTime? PodcastCreatedOn { get; set; }
        public Guid PodcastId { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public LocalDateTime? UserCredentialsSentOn { get; set; }
    }

    public class ClientWithGroup
    {
        public Client Client { get; set; }
        public string GroupName { get; set; }
    }
}
