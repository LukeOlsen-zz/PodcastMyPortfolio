using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClientManagement.Models;
using NodaTime;

namespace ClientManagement.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<UserLoginLog> UserLoginLog { get; set; }
        public DbSet<Firm> Firms { get; set; }
        public DbSet<FirmPodcastSettings> FirmPodcastSettings { get; set; }
        public DbSet<Voice> Voices { get; set; }
        public DbSet<FirmPodcastSegment> FirmPodcastSegments { get; set; }
        public DbSet<ClientGroupPodcastSegment> ClientGroupPodcastSegments { get; set; }
        public DbSet<ClientGroup> ClientGroups { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ClientMessage> ClientMessages { get; set; }
        public DbSet<ClientMessageType> ClientMessageTypes { get; set; }
        public DbSet<ClientAccount> ClientAccounts { get; set; }
        public DbSet<ClientAccountPeriodicData> ClientAccountPeriodicData { get; set; }
        public DbSet<ClientAccountActivity> ClientAccountActivities { get; set; }
        public DbSet<ClientAccountActivityType> ClientAccountActivityTypes { get; set; }
        public DbSet<InvalidAccessAttempt> InvalidAccessAttempts { get; set; }
        public DbSet<ClientReceivedFirmPodcastSegment> ClientReceivedFirmPodcastSegments { get; set; }
        public DbSet<ClientReceivedClientGroupPodcastSegment> ClientReceivedClientGroupPodcastSegments { get; set; }
        public DbSet<ClientPublishedFirmPodcastSegment> ClientPublishedFirmPodcastSegments { get; set; }
        public DbSet<ClientPublishedClientGroupPodcastSegment> ClientPublishedClientGroupPodcastSegments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(c => new { c.Id });
            modelBuilder.Entity<Firm>()
                .HasKey(c => new { c.Id });
            modelBuilder.Entity<UserLoginLog>()
                .HasKey(c => new { c.UserId, c.LoginOn });
            modelBuilder.Entity<FirmPodcastSettings>()
                .HasKey(c => new { c.FirmId });
            modelBuilder.Entity<InvalidAccessAttempt>()
                .HasKey(c => new { c.IPAddress });
            modelBuilder.Entity<ClientReceivedFirmPodcastSegment>()
                .HasKey(c => new { c.ClientId, c.FirmPodcastSegmentId });
            modelBuilder.Entity<ClientReceivedClientGroupPodcastSegment>()
                .HasKey(c => new { c.ClientId, c.ClientGroupPodcastSegmentId });
            modelBuilder.Entity<ClientPublishedFirmPodcastSegment>()
                .HasKey(c => new { c.ClientId, c.FirmPodcastSegmentId });
            modelBuilder.Entity<ClientPublishedClientGroupPodcastSegment>()
                .HasKey(c => new { c.ClientId, c.ClientGroupPodcastSegmentId });
        }
    }
}
