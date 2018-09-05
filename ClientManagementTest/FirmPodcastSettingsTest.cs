using Microsoft.VisualStudio.TestTools.UnitTesting;
using NodaTime;

namespace ClientManagementTest
{
    [TestClass]
    public class FirmPodcastSettingsTest
    {
        ClientManagement.Models.FirmPodcastSettings fps;

        [TestInitialize]
        public void SetupTest()
        {
            fps = new ClientManagement.Models.FirmPodcastSettings();

            // Let us setup so that Wednesday and Sundays are podcast publishing dates and 8:00 pm is the podcast publishing time
            fps.PodcastPublishingSunday = true;
            fps.PodcastPublishingWednesday = true;
            fps.PodcastPublishingTime = new LocalTime(20, 0);
        }

        [TestMethod]
        public void PodcastAllowed_ImportBeforePublishDate()
        {
            // New podcast data imported on 1/29/2019 at 10:30 AM (Tuesday)
            LocalDateTime podcastNewDataDateTest = new LocalDateTime(2019, 1, 29, 10, 30);

            // Current time set for 11:30 AM on 1/29/2019 (Tuesday)
            LocalDateTime fakeCurrentDateTime1 = new LocalDateTime(2019, 1, 29, 11, 30);
            // Podcasts get published Sunday and Wednesday and since the data import came AFTER Sunday's publishing date we will use existing data
            Assert.AreEqual(false, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime1));

            // Current time set for 7:30 PM on 1/30/2019 (Wednesday)
            LocalDateTime fakeCurrentDateTime2 = new LocalDateTime(2019, 1, 30, 19, 30);
            // Podcasts get published Sunday and Wednesday and since the data import came AFTER Sunday's publishing date we will use existing data
            // Also even though this is a publishing date it isn't 8:00 PM yet so we will use existing data
            Assert.AreEqual(false, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime2));

            // Current time set for 8:30 PM on 1/30/2019 (Wednesday)
            LocalDateTime fakeCurrentDateTime3 = new LocalDateTime(2019, 1, 30, 20, 30);
            // Podcasts get published Sunday and Wednesday and since the data import came AFTER Sunday's publishing date we will use existing data
            // Since it is Wednesday after 8 PM we should allow for a new Podcast
            Assert.AreEqual(true, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime3));

            // Current time set for 6:30 PM on 2/1/2019 (Friday)
            LocalDateTime fakeCurrentDateTime4 = new LocalDateTime(2019, 2, 1, 18, 30);
            // Podcasts get published Sunday and Wednesday and since the data import came AFTER Sunday's publishing date we will use existing data
            // Since it is Wednesday after 8 PM we should allow for a new Podcast
            Assert.AreEqual(true, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime4));
        }

        [TestMethod]
        public void PodcastAllowed_ImportOnPublishDate()
        {
            // New podcast data imported on 1/30/2019 at 10:30 AM (Wednesday)
            LocalDateTime podcastNewDataDateTest = new LocalDateTime(2019, 1, 30, 10, 30);

            // Current time set for 11:00 AM 1/30/2019  (Wednesday)
            LocalDateTime fakeCurrentDateTime1 = new LocalDateTime(2019, 1, 30, 11, 00);
            // Podcast should not be available until 8pm 
            Assert.AreEqual(false, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime1));

            // However after 8pm we should be good to go 1/30/2019 11:00 PM
            LocalDateTime fakeCurrentDateTime2 = new LocalDateTime(2019, 1, 30, 23, 00);
            Assert.AreEqual(true, fps.NewPodcastAllowed(podcastNewDataDateTest, fakeCurrentDateTime2));
        }


    }
}
