using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using ClientManagement.Data;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Services;
using ClientManagement.DTOs;
using ClientManagement.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net;
using Serilog;
using NodaTime;
using System.Globalization;
using System.Collections.Specialized;
using System.Net.Http;
using System.Xml;
using PodcastServer.Utilities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;



namespace ClientManagement.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PodcastController : ControllerBase
    {
        private IConfiguration _configuration;
        private IInvalidAccessAttemptService _invalidAccessAttemptService;
        private IClientService _clientService;
        private IClientMessageService _clientMessageService;
        private IClientAccountActivityTypeService _clientAccountActivityTypeService;
        private IClientAccountService _clientAccountService;
        private IClientAccountActivityService _clientAccountActivityService;
        private IClientAccountPeriodicDataService _clientAccountPeriodicDataService;
        private IFirmService _firmService;
        private IFirmPodcastSettingsService _firmPodcastSettingsService;
        private IFirmPodcastSegmentService _firmPodcastSegmentService;
        private IClientGroupPodcastSegmentService _clientGroupPodcastSegmentService;
        private IHttpContextAccessor _httpContextAccessor;
        private IClock _clock;
        private readonly AppSettings _appSettings;

        private const int MAXIMUM_DAYS_FOR_ACCOUNT_ACTIVITY = 120;     
        private const int MAXIMUM_DAYS_FOR_ACCOUNT_DATA_PERIODS = 120;

        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public PodcastController(IConfiguration configuration, IInvalidAccessAttemptService invalidAccessAttemptService, IClientService clientService, IClientAccountActivityTypeService clientAccountActivityTypeService, IClientAccountService clientAccountService, IClientMessageService clientMessageService, IClientAccountActivityService clientAccountActivityService, IHttpContextAccessor httpContextAccessor, IClock clock, IFirmPodcastSettingsService firmPodcastSettingsService, IFirmService firmService, IClientAccountPeriodicDataService clientAccountPeriodicDataService, IFirmPodcastSegmentService firmPodcastSegmentService, IClientGroupPodcastSegmentService clientGroupPodcastSegmentService, IOptions<AppSettings> appSettings)
        {
            _configuration = configuration;
            _clientService = clientService;
            _clientAccountActivityTypeService = clientAccountActivityTypeService;
            _clientAccountService = clientAccountService;
            _clientAccountActivityService = clientAccountActivityService;
            _clientAccountPeriodicDataService = clientAccountPeriodicDataService;
            _clientMessageService = clientMessageService;
            _firmPodcastSettingsService = firmPodcastSettingsService;
            _firmService = firmService;
            _firmPodcastSegmentService = firmPodcastSegmentService;
            _clientGroupPodcastSegmentService = clientGroupPodcastSegmentService;
            _invalidAccessAttemptService = invalidAccessAttemptService;
            _httpContextAccessor = httpContextAccessor;
            _appSettings = appSettings.Value;
            _clock = clock;
        }

        /// <summary>
        /// This will return the podcast menu
        /// </summary>
        /// <returns></returns>
        [Produces("text/xml")]
        [HttpGet()]
        public ActionResult<string> Get()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get IP address of user. If the IP is on the BLOCK list do not let them in further
            string remoteIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            InvalidAccessAttempt invalidAccessAttempt = _invalidAccessAttemptService.Get(remoteIpAddress);

            if (invalidAccessAttempt != null && invalidAccessAttempt.HitCount >= 3)
                // Bump the user off
                return BadRequest();

            string podcast = string.Empty;

            if (!string.IsNullOrEmpty(userId))
                if (int.TryParse(userId, out int uid))
                    podcast = GetPodcast(uid);


            // Does podcast ID exist? If not record a bad ip
            if (podcast == string.Empty)
            {
                // Did this IP already try?
                if (invalidAccessAttempt == null)
                {
                    InvalidAccessAttempt a = new InvalidAccessAttempt { IPAddress = remoteIpAddress };
                    _invalidAccessAttemptService.Create(a);
                }
                else
                    _invalidAccessAttemptService.Update(invalidAccessAttempt);

                return BadRequest();
            }
            else
            {
                // Clear the invalid accesses
                _invalidAccessAttemptService.Clear(remoteIpAddress);

                // Get client podcast
                return Content(podcast, "text/xml", System.Text.Encoding.UTF8);
            }
        }

        public string GetPodcast(int clientUserId)
        {
            Client client = _clientService.Get(clientUserId);

            if (client != null)
            {
                // Get firm podcast settings
                FirmPodcastSettings firmPodcastSettings = _firmPodcastSettingsService.Get(client.FirmId);

                if (firmPodcastSettings != null)
                {
                    LocalDateTime now = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                    string podcast = string.Empty;

                    IEnumerable<ClientMessage> clientMessagesDirect = _clientMessageService.GetAllAvailableToClient(client.Id, false);
                    IEnumerable<FirmPodcastSegment> firmPodcastSegmentsForClient = _firmPodcastSegmentService.GetAllAvailableForClient(client.Id);
                    IEnumerable<ClientGroupPodcastSegment> clientGroupPodcastSegmentsForClient = _clientGroupPodcastSegmentService.GetAllAvailableForClient(client.Id);

                    if (client.AccountActivityNew || client.PeriodicDataNew || firmPodcastSegmentsForClient.Count() > 0 || clientGroupPodcastSegmentsForClient.Count() > 0 || clientMessagesDirect.Count() > 0 || String.IsNullOrEmpty(client.Podcast))
                    {
                        podcast = CreateClientPodcast(client, firmPodcastSettings, firmPodcastSegmentsForClient, clientGroupPodcastSegmentsForClient, clientMessagesDirect);

                        client.Podcast = podcast;
                        client.PodcastCreatedOn = now;
                        client.PodcastLastAccessOn = now;
                        client.PeriodicDataNew = false;
                        client.AccountActivityNew = false;
                        _clientService.Update(client);
                    }
                    else
                    {
                        // Get previously saved podcast
                        podcast = client.Podcast;

                        client.PodcastLastAccessOn = now;
                        _clientService.Update(client);
                    }

                    return podcast;
                }
                else
                    // No firm podcast settings found
                    return String.Empty;
            }
            else
                // No client found
                return String.Empty;        
        }

        /// <summary>
        /// Describe a previous date in terms like "last week", "yesterday", etc...
        /// </summary>
        /// <param name="startingDate"></param>
        /// <param name="endingDate"></param>
        /// <returns></returns>
        private string DaysPastDescription(LocalDate startingDate, LocalDate endingDate)
        {
            int days = LocalDate.Subtract(endingDate, startingDate).Days;
            string description = string.Empty;

            if (days == 0)
                description = "Since earlier today there has been";
            else
                if (days == 1)
                    description = "Since yesterday there has been";
                else
                {
                    Period period = NodaTime.Period.Between(startingDate, endingDate);
                    int weeksBetween = period.Weeks;
                    if (weeksBetween == 0)
                        description = String.Format("Over the past {0} days there has been", days);
                    else
                        if (weeksBetween == 1)
                            description = "Since last week there has been";
                        else
                        {
                            int monthsBetween = period.Months;
                            if (monthsBetween == 0)
                                description = String.Format("Over the past {0} weeks there has been", weeksBetween);
                            else
                                if (monthsBetween == 1)
                                    description = "Since last month there has been";
                                else
                                    description = String.Format("Over the past {0} months there has been", monthsBetween);
                        }
                }

            return description;
        }

        private PodcastItemDescription CreateActivityDescription(Client client, IEnumerable<ClientAccount> clientAccounts)
        {
            int clientAccountCount = clientAccounts.Count();

            LocalDate currentDate = _clock.GetCurrentInstant().InZone(_tz).Date;
            string activityDescription = string.Empty;

            // We need a description of activity over x days on all accounts
            var activityData = _clientAccountActivityService.GetActivityAvailableToClient(client.Id, MAXIMUM_DAYS_FOR_ACCOUNT_ACTIVITY);

            if (activityData.Count() > 0)
            {
                LocalDate earliestDate = currentDate;   // Default to today

                List<string> explicitActivities = new List<string>();
                OrderedDictionary regularActivities = new OrderedDictionary();
                foreach (var activity in activityData)
                {
                    if (activity.ActivityDescriptionOverride != null && activity.ActivityDescriptionOverride.Length > 0)
                        explicitActivities.Add(activity.ActivityDescriptionOverride);
                    else
                    {
                        if (regularActivities.Contains((object)activity.ActivityTypeId))
                        {
                            decimal previousAmount = ((Tuple<string, decimal>)regularActivities[(object)activity.ActivityTypeId]).Item2;
                            regularActivities[(object)activity.ActivityTypeId] = new Tuple<string, decimal>(activity.ActivityTypeName, activity.ActivityAmount + previousAmount);
                        }
                        else
                            regularActivities.Add((object)activity.ActivityTypeId, new Tuple<string, decimal>(activity.ActivityTypeName, activity.ActivityAmount));
                    }

                    if (activity.ActivityDate < earliestDate)
                        earliestDate = activity.ActivityDate;
                }

                // Determine how many days we looked at
                activityDescription += DaysPastDescription(earliestDate, currentDate) + " ";
                // We will treat one activity different from multiple activities
                if (regularActivities.Count > 1)
                {
                    for (int i = 0; i < regularActivities.Count; i++)
                    {
                        Tuple<string, decimal> activity = (Tuple<string, decimal>)regularActivities[i];
                        if (i < regularActivities.Count - 1)
                            activityDescription += "a " + activity.Item1.ToLower() + " of " + TranslateCurrencyIntoText(activity.Item2) + ", ";
                        else
                            activityDescription += "and a " + activity.Item1.ToLower() + " of " + TranslateCurrencyIntoText(activity.Item2) + "  ";
                    }
                }
                else
                {
                    Tuple<string, decimal> activity = (Tuple<string, decimal>)regularActivities[0];
                    activityDescription += "a " + activity.Item1.ToLower() + " of " + TranslateCurrencyIntoText(activity.Item2) + " ";
                }

                // Account or accounts
                if (clientAccountCount > 1)
                {
                    if (clientAccountCount == 2)
                        activityDescription += "over both of your accounts. ";
                    else
                        activityDescription += String.Format("over all {0} of your accounts. ", clientAccountCount);
                }


                // Any activity overrides should be explicity stated at the end of the summary
                if (explicitActivities.Count > 0)
                {
                    activityDescription += "Additionally ";
                    foreach (string explicitActivity in explicitActivities)
                        activityDescription += explicitActivity + ". ";
                }
            }
            else
            {
                if (clientAccountCount == 1)
                    activityDescription = String.Format("There has been no activity on your account during the past {0} days.", MAXIMUM_DAYS_FOR_ACCOUNT_ACTIVITY);
                else
                    activityDescription = String.Format("There has been no activity on all your accounts during the past {0} days.", MAXIMUM_DAYS_FOR_ACCOUNT_ACTIVITY);
            }

            PodcastItemDescription podcastItemDescription = null;
            if (activityDescription != string.Empty)
            {
                podcastItemDescription = new PodcastItemDescription
                {
                    Title = String.Format("Your activity summary as of {0:M/d/yyyy}", currentDate),
                    SubTitle = string.Empty,
                    Description = activityDescription,
                    Summary = string.Empty
                };
            }
            return podcastItemDescription;
        }


        private PodcastItemDescription CreatePeriodicDataDescription(Client client, IEnumerable<ClientAccount> clientAccounts)
        {
            int clientAccountCount = clientAccounts.Count();
            string periodicDataDescription = String.Empty;

            LocalDate currentDate = _clock.GetCurrentInstant().InZone(_tz).Date;

            // We only want two dates; the most recent and previous month's date or sooner.
            var mostRecentDate = _clientAccountPeriodicDataService.GetMostCurrentDateAcrossAllClientAccounts(client.Id);

            if (mostRecentDate.HasValue)
            {
                var previousDate = _clientAccountPeriodicDataService.GetPreviousDateGivenByTimeSpanAcrossAllClientAccounts(client.Id, mostRecentDate.Value, MAXIMUM_DAYS_FOR_ACCOUNT_DATA_PERIODS);

                List<LocalDate> dataDates = new List<LocalDate> { mostRecentDate.Value };
                if (previousDate.HasValue)
                    dataDates.Add(previousDate.Value);

                var periodicData = _clientAccountPeriodicDataService.Get(client.Id, dataDates);

                decimal currentBalance = 0.00m;
                decimal previousBalance = 0.00m;
                foreach (ClientAccountPeriodicData capd in periodicData)
                {
                    if (capd.PeriodicDataAsOf == dataDates[0])
                        currentBalance += capd.EndingBalance;
                    if (dataDates.Count > 1 && capd.PeriodicDataAsOf == dataDates[1])
                        previousBalance += capd.EndingBalance;
                }

                // Create language
                // Your total balance [today|as of yesterday|as of n days ago] [is|was] $
                string currentBalanceRelativeTime = String.Empty;
                string currentBalanceRelativeState = "was";
                if (Period.Between(dataDates[0], currentDate, PeriodUnits.Days).Days == 0)
                {
                    currentBalanceRelativeTime = "today";
                    currentBalanceRelativeState = "is";
                }
                else
                    if (Period.Between(dataDates[0], currentDate, PeriodUnits.Days).Days == 1)
                    currentBalanceRelativeTime = "as of yesterday";
                else
                    currentBalanceRelativeTime = String.Format("as of {0} days ago", (Period.Between(dataDates[0], currentDate, PeriodUnits.Days).Days));
                periodicDataDescription += String.Format("Your total balance {0} {1} {2}. ", currentBalanceRelativeTime, currentBalanceRelativeState, TranslateCurrencyIntoText(currentBalance));

                // This has been an [increase | decrease] from your previous balance of $ [yesterday | n days ago] (only if previous exists and balance is different)
                // This is the same as your previous balance [yesterday | n days ago] (only if previous exists and balance is same)
                if (dataDates.Count > 1)
                {
                    string previousBalanceRelativeTime = String.Empty;
                    if (Period.Between(dataDates[1], currentDate, PeriodUnits.Days).Days == 1)
                        previousBalanceRelativeTime = "yesterday";
                    else
                        previousBalanceRelativeTime = String.Format("{0} days ago", (Period.Between(dataDates[1], currentDate, PeriodUnits.Days).Days));

                    if (currentBalance == previousBalance)
                        periodicDataDescription += "This is the same as your previous balance " + previousBalanceRelativeTime + ". ";
                    else
                    {
                        string currentBalanceDirection = String.Empty;
                        if (currentBalance > previousBalance)
                            currentBalanceDirection = "an increase";
                        else
                            currentBalanceDirection = "a decrease";

                        periodicDataDescription += "This has been " + currentBalanceDirection + String.Format(" from your previous balance of {0} ", TranslateCurrencyIntoText(currentBalance - previousBalance)) + " " + previousBalanceRelativeTime + ". ";
                    }
                }


                // This total is for all your accounts. (only if client has more than one account)
                if (clientAccountCount > 1)
                {
                    if (clientAccountCount == 2)
                        periodicDataDescription += "This total is based on both of your accounts.";
                    else
                        periodicDataDescription += String.Format("This total is based on all {0} of your accounts.", clientAccountCount);
                }
            }

            PodcastItemDescription podcastItemDescription = null;
            if (periodicDataDescription != string.Empty)
            {
                podcastItemDescription = new PodcastItemDescription
                {
                    Title = String.Format("Your data summary as of {0:M/d/yyyy}", currentDate),
                    SubTitle = string.Empty,
                    Description = periodicDataDescription,
                    Summary = string.Empty
                };
            }
            return podcastItemDescription;
        }

        private string GetFormattedDateTime(LocalDateTime localDateTime)
        {
            // e.g. Thu, 21 Jun 2019 21:37:00 -0200 
            //ddd, dd MMM yyy HH:mm:ss zzz
            return localDateTime.ToString("ddd, dd MMM yyyy HH:mm:ss +0000", CultureInfo.InvariantCulture);
        }

        private string CreateClientPodcast(Client client, FirmPodcastSettings firmPodcastSettings,  IEnumerable<FirmPodcastSegment> firmPodcastSegmentsForClient, IEnumerable<ClientGroupPodcastSegment> clientGroupPodcastSegmentsForClient, IEnumerable<ClientMessage> clientMessagesDirect)
        {
            Firm firm = _firmService.Get(client.FirmId);

            // Firm info
            string firmPodcastLogoURL = String.Empty;
            if (!String.IsNullOrWhiteSpace(firmPodcastSettings.PodcastFirmLogoId))
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("AzureStorage"));
                firmPodcastLogoURL = storageAccount.BlobStorageUri.PrimaryUri.ToString() + "podcastlogos/" + firmPodcastSettings.PodcastFirmLogoId + "?" + firmPodcastSettings.UpdatedOn.TickOfSecond;
            }
            

            string formattedBuildDateTime = GetFormattedDateTime(_clock.GetCurrentInstant().InZone(_tz).LocalDateTime);

            // Cache bust id
            string cacheId = System.Guid.NewGuid().ToString();

            string atomNamespace = "http://www.w3.org/2005/Atom";
            string iTunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter xw = XmlWriter.Create(ms))
                {
                    xw.WriteStartDocument();
                    xw.WriteStartElement("rss");
                    xw.WriteAttributeString("xmlns", "atom", null, atomNamespace);
                    xw.WriteAttributeString("xmlns", "itunes", null, iTunesNamespace);
                    xw.WriteAttributeString("version", "2.0");
                    xw.WriteStartElement("channel");

                    xw.WriteStartElement("link", atomNamespace);
                    xw.WriteAttributeString("href", _appSettings + "/podcast");
                    xw.WriteAttributeString("rel", "self");
                    xw.WriteAttributeString("type", "application/rss+xml");
                    xw.WriteEndElement();
                    xw.WriteElementString("title", "Your Portfolio News");
                    
                    if (!String.IsNullOrEmpty(firmPodcastSettings.PodcastFirmSiteURL))
                        xw.WriteElementString("link", firmPodcastSettings.PodcastFirmSiteURL);

                    xw.WriteElementString("language", "en");
                    xw.WriteElementString("copyright", firm.Name);
                    xw.WriteElementString("subtitle", iTunesNamespace, "News provided by " + firm.Name + " about your accounts.");
                    xw.WriteElementString("author", iTunesNamespace, firm.Name);
                    xw.WriteElementString("summary", iTunesNamespace, "SUMMARY");
                    xw.WriteStartElement("description");
                    xw.WriteCData(firmPodcastSettings.PodcastDescription);
                    xw.WriteEndElement();
                    xw.WriteStartElement("owner", iTunesNamespace);
                    xw.WriteElementString("name", iTunesNamespace, firmPodcastSettings.PodcastContactName);
                    xw.WriteElementString("email", iTunesNamespace, firmPodcastSettings.PodcastContactEmail);
                    xw.WriteEndElement(); // owner

                    if (!String.IsNullOrEmpty(firmPodcastLogoURL))
                    {
                        xw.WriteStartElement("image", iTunesNamespace);
                        xw.WriteAttributeString("href", firmPodcastLogoURL);
                        xw.WriteEndElement();
                    }

                    xw.WriteStartElement("category", iTunesNamespace);
                    xw.WriteAttributeString("text", "retirement");
                    xw.WriteEndElement();
                    xw.WriteElementString("keywords", iTunesNamespace, "finance, retirement");
                    xw.WriteElementString("explicit", iTunesNamespace, "no");

                    // Iterate items here
                    // 1: Welcome message (ONLY DELIVER IF CLIENT HAS NEVER RECEIVED PODCAST)
                    if (!String.IsNullOrEmpty(firmPodcastSettings.PodcastWelcomeMessage) && string.IsNullOrEmpty(client.Podcast))
                    {
                        PodcastItemDescription welcomePodcastItem = new PodcastItemDescription { Title = "Welcome", Summary = "Welcome to your account summary", Description = firmPodcastSettings.PodcastWelcomeMessage };
                        WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/wm?id=" + firmPodcastSettings.PodcastId, string.Empty, welcomePodcastItem);
                    }

                    // Now we need to go through our content
                    // 2: Firm Messages
                    if (firmPodcastSegmentsForClient.Count() > 0)
                        foreach (FirmPodcastSegment firmPodcastSegment in firmPodcastSegmentsForClient)
                        {
                            PodcastItemDescription firmSegmentPodcastItem = new PodcastItemDescription { Title = firmPodcastSegment.Title, Summary = firmPodcastSegment.Description, Description = firmPodcastSegment.Description };
                            WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/fm?id=" + firmPodcastSegment.PodcastId + "&cid=" + client.PodcastId + "&bld=" + cacheId, string.Empty, firmSegmentPodcastItem);

                            // If we write it out to the client then we should mark it as published so it doens't get published again
                            _firmPodcastSegmentService.MarkAsPublished(client.Id, firmPodcastSegment.Id);
                        }

                    // 3: Client Group Messages
                    if (clientGroupPodcastSegmentsForClient.Count() > 0)
                        foreach (ClientGroupPodcastSegment clientGroupPodcastSegment in clientGroupPodcastSegmentsForClient)
                        {
                            PodcastItemDescription clientGroupSegmentPodcastItem = new PodcastItemDescription { Title = clientGroupPodcastSegment.Title, Summary = clientGroupPodcastSegment.Description, Description = clientGroupPodcastSegment.Description };
                            WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/cgm?id=" + clientGroupPodcastSegment.PodcastId + "&cid=" + client.PodcastId + "&bld=" + cacheId, string.Empty, clientGroupSegmentPodcastItem);

                            // If we write it out to the client then we should mark it as published so it doens't get published again
                            _clientGroupPodcastSegmentService.MarkAsPublished(client.Id, clientGroupPodcastSegment.Id);
                        }

                    // 4: Client Messages (non-system generated. These are imported from the firm 
                    if (clientMessagesDirect.Count() > 0)
                        foreach (ClientMessage cm in clientMessagesDirect)
                        {
                            PodcastItemDescription clientMessagePodcastItem = new PodcastItemDescription { Title = "A message for you", Summary = cm.Message, Description = cm.Message };
                            WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/cm?id=" + cm.PodcastId + "&bld=" + cacheId, string.Empty, clientMessagePodcastItem);

                            // If we write it out to the client then we should mark it as published so it doens't get published again
                            _clientMessageService.MarkAsPublished(cm.Id);
                        }


                    // 5: Client financial information 
                    if (client.AccountActivityNew || client.PeriodicDataNew)
                    {
                        // What accounts does this client have. We will use this list for the next few queries
                        var clientAccounts = _clientAccountService.GetAllInClient(client.Id);

                        if (clientAccounts.Count() > 0)
                        {
                            // 5a: Client Periodic Data (right now that is only their ending balance across all accounts)
                            if (client.PeriodicDataNew)
                            {
                                PodcastItemDescription periodicDataDescription = CreatePeriodicDataDescription(client, clientAccounts);
                                if (periodicDataDescription != null)
                                {
                                    // Save and include into podcast menu
                                    ClientMessage clientPeriodicDataSummaryMessage = new ClientMessage(client.Id, Constants.ClientMessageTypes.DATA, periodicDataDescription.Description, null, true);
                                    clientPeriodicDataSummaryMessage = _clientMessageService.Create(clientPeriodicDataSummaryMessage);
                                    WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/cm?id=" + clientPeriodicDataSummaryMessage.PodcastId, string.Empty, periodicDataDescription);
                                }
                            }

                            // 5b: Client Activity (transactions)
                            if (client.AccountActivityNew)
                            {
                                PodcastItemDescription activityDescription = CreateActivityDescription(client, clientAccounts);
                                if (activityDescription != null)
                                {
                                    // Save and include into podcast menu
                                    ClientMessage clientActivitySummaryMessage = new ClientMessage(client.Id, Constants.ClientMessageTypes.ACTIVITY, activityDescription.Description, null, true);
                                    clientActivitySummaryMessage = _clientMessageService.Create(clientActivitySummaryMessage);
                                    WritePodcastAudioItem(xw, iTunesNamespace, formattedBuildDateTime, firm.Name, _appSettings.PodcastServerEndPoint + "/media/cm?id=" + clientActivitySummaryMessage.PodcastId, string.Empty, activityDescription);
                                }
                            }
                        }
                    }

                    xw.WriteEndElement();   //channel
                    xw.WriteEndElement();   //rss
                }

                ms.Flush();
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);

                // Save podcast 
                string podcast = sr.ReadToEnd();

                return podcast;
            }
        }


        private void WritePodcastAudioItem(XmlWriter xw, string iTunesNamespace, string pubDate,  string author, string audioLink, string imageLink, PodcastItemDescription description)
        {
            xw.WriteStartElement("item");
            xw.WriteElementString("title", description.Title);
            xw.WriteElementString("author", iTunesNamespace, author);
            xw.WriteElementString("link", audioLink);
            xw.WriteStartElement("description");
            xw.WriteCData(description.Description);
            xw.WriteEndElement(); // description
            xw.WriteElementString("subtitle", iTunesNamespace, description.SubTitle);
            xw.WriteElementString("summary", iTunesNamespace, description.Summary);
            xw.WriteElementString("pubDate", pubDate);
            xw.WriteElementString("category", "Podcast");
            xw.WriteElementString("explicit", iTunesNamespace, "no");
            xw.WriteElementString("keywords", iTunesNamespace, "finance, retirement");

            if (!string.IsNullOrEmpty(imageLink))
            {
                xw.WriteStartElement("image", iTunesNamespace);
                xw.WriteAttributeString("href", imageLink);
                xw.WriteEndElement();
            }

            xw.WriteElementString("guid", audioLink);

            xw.WriteStartElement("enclosure");
            xw.WriteAttributeString("url", audioLink);
            xw.WriteAttributeString("length", "100");
            xw.WriteAttributeString("type", "audio/mpeg");
            xw.WriteEndElement();   // enclosure

            xw.WriteEndElement();   // item
        }

        private string TranslateCurrencyIntoText(decimal amount)
        {
            // Separate dollars and cents
            int dollars = (int)amount;
            int cents = (int)((amount - dollars) * 100);

            string result = string.Empty;
            string dollarAsString = Wordify(dollars).Trim();
            if (cents == 0)
                result = String.Format("{0:#} dollars", dollarAsString);
            else
                result = String.Format("{0:#} dollars and {1:#} cents", dollarAsString, Math.Abs(cents));

            return result;
        }

        /// <summary>
        /// From https://stackoverflow.com/questions/309884/code-golf-number-to-words/408776#408776
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static string Wordify(decimal v)
        {
            if (v == 0) return "zero";
            var units = " one two three four five six seven eight nine".Split();
            var teens = " eleven twelve thir# four# fif# six# seven# eigh# nine#".Replace("#", "teen").Split();
            var tens = " ten twenty thirty forty fifty sixty seventy eighty ninety".Split();
            var thou = " thousand m# b# tr# quadr# quint# sext# sept# oct#".Replace("#", "illion").Split();
            var g = (v < 0) ? "minus " : "";
            var w = "";
            var p = 0;
            v = Math.Abs(v);
            while (v > 0)
            {
                int b = (int)(v % 1000);
                if (b > 0)
                {
                    var h = (b / 100);
                    var t = (b - h * 100) / 10;
                    var u = (b - h * 100 - t * 10);
                    var s = ((h > 0) ? units[h] + " hundred" + ((t > 0 | u > 0) ? " and " : "") : "")
                          + ((t > 0) ? (t == 1 && u > 0) ? teens[u] : tens[t] + ((u > 0) ? "-" : "") : "")
                          + ((t != 1) ? units[u] : "");
                    s = (((v > 1000) && (h == 0) && (p == 0)) ? " and " : (v > 1000) ? ", " : "") + s;
                    w = s + " " + thou[p] + w;
                }
                v = v / 1000;
                p++;
            }
            return g + w;
        }

        private class PodcastItemDescription
        {
            public string Title { get; set; }
            public string SubTitle { get; set; }
            public string Summary { get; set; }
            public string Description { get; set; }
        }


    }
}
