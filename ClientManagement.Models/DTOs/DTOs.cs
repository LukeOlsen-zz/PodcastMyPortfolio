using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


/// <summary>
/// DTOs will be used at times to transfer data from CONTROLLERS to the UI
/// </summary>
namespace ClientManagement.DTOs
{
    public class UserLoginDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class FormExceptionDto
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }

    public class VoiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class FirmPodcastSegmentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string StartsOn { get; set; }
        public string EndsOn { get; set; }
        public string SegmentId { get; set; }
        public string SegmentURL { get; set; }
    }

    public class ClientGroupPodcastSegmentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string StartsOn { get; set; }
        public string EndsOn { get; set; }
        public string SegmentId { get; set; }
        public string SegmentURL { get; set; }
        public string ClientGroupName { get; set; }
        public int ClientGroupId { get; set; }
    }

    public class ClientGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FirmGroupId { get; set; }
    }

    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ClientGroupId { get; set; }
        public string FirmClientId { get; set; }
        public string EmailAddress { get; set; }
        public string ClientGroupName { get; set; }
        public string UserName { get; set; }       
    }

    public class ImportResultDto
    {
        public string Result { get; set; }
    }



    public class ClientAccountActivityWithTypeDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string ActivityDate { get; set; }
        public int ActivityTypeId { get; set; }
        public decimal ActivityAmount { get; set; }
        public string ActivityDescriptionOverride { get; set; }
        public string ActivityTypeName { get; set; }
        public string ActivityTypeUploadCode { get; set; }
    }

    public class ClientPeriodicDataDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string PeriodicDataAsOf { get; set; }
        public Decimal EndingBalance { get; set; }
    }

    public class ClientMessageWithTypeDto
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int ClientMessageTypeId { get; set; }
        public string ClientMessage { get; set; }
        public string ExpiresOn { get; set; }
        public string ReceivedByClient { get; set; }
        public byte MessageTypeOrder { get; set; }
        public string MessageTypeName { get; set; }
        public string MessageTypeUploadCode { get; set; }
    }
}
