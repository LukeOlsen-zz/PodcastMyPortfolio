using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PodcastServer.Security;
using PodcastServer.Utilities;
using System.Text;
using Serilog;


namespace PodcastServer.Services
{
    public interface ICognativeServices
    {
        Task<byte[]> TextToSpeech(string voice, string text);
        Task<byte[]> TextToSpeechOrig(string voice, string text);
    }

    public class CognativeServices : ICognativeServices
    {
        private HttpClient _httpClient;
        private readonly AppSettings _appSettings;
        private const int MAX_RETRY_ON_TTS = 10;
        SpeechServicesAuthentication _auth;

        public CognativeServices(IOptions<AppSettings> appSettings, HttpClient httpClient)
        {
            _appSettings = appSettings.Value;
            _httpClient = httpClient;

            _auth = new SpeechServicesAuthentication(_appSettings.AzureSpeechServicesKey);
        }

        public async Task<byte[]> TextToSpeechOrig(string voice, string text)
        {
            string token = _auth.GetAccessToken();

            _httpClient.Timeout = new TimeSpan(1, 0, 0);
            _httpClient.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "pTest");
            _httpClient.DefaultRequestHeaders.Add("Authorization", token);

            voice = "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)";
            string smil = string.Empty;
            smil = @"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='" + voice + @"'>" + text + @"</voice></speak>";
            var content = new StringContent(smil, System.Text.Encoding.UTF8, "application/ssml+xml");
            var r1 = _httpClient.PostAsync(_appSettings.AzureSpeechAPIEndPoint, content);
            await Task.WhenAll(r1);
            var s1 = r1.Result.Content.ReadAsByteArrayAsync();
            await Task.WhenAll(s1);

            return s1.Result;
        }


        public async Task<byte[]> TextToSpeech(string voice, string text)
        {
            byte[] audio = null;

            try
            {
                // Service only allows 1000 characters max.
                if (text.Length > 1000)
                    text = text.Substring(0, 1000);

                string token = _auth.GetAccessToken();

                int retryCount = 0;
                bool success = false;
                HttpResponseMessage lastResponse = null;

                while (retryCount < MAX_RETRY_ON_TTS && !success)
                {
                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri(_appSettings.AzureSpeechAPIEndPoint);
                        string smil = @"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='" + voice + @"'>" + text + @"</voice></speak>";
                        request.Content = new StringContent(smil, Encoding.UTF8, "application/ssml+xml");
                        request.Headers.Add("Authorization", "Bearer " + token);
                        request.Headers.Add("User-Agent", "PodcastMyPortfolioTTS");


                        request.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");

                        using (var response = await _httpClient.SendAsync(request).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                audio = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                                success = true;
                            }
                            else
                            {
                                lastResponse = response;
                                if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                                    Log.Error("Problem getting TTS " + response.ReasonPhrase);

                                // Pause and then try again
                                System.Threading.Thread.Sleep(5000);
                            }
                        }

                    }

                    retryCount++;
                }

                // If retry fails we need to log
                if (!success)
                {
                    if (lastResponse != null)
                        Log.Error("Problem getting TTS after retries " + lastResponse.ReasonPhrase);
                    else
                        Log.Error("Problem getting TTS after retries with unknonw response");
                }

            }
            catch (Exception ex)
            {
                Log.Error("TextToSpeech: " +ex.Message);
            }
            return audio;
        }

    }
}
