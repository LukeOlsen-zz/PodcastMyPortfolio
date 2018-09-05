using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IInvalidAccessAttemptService
    {
        InvalidAccessAttempt Get(string IPAddress);
        void Clear(string IPAddress);
        InvalidAccessAttempt Create(InvalidAccessAttempt invalidAccessAttempt);
        void Update(InvalidAccessAttempt invalidAccessAttempt);
    }

    public class InvalidAccessAttemptService : IInvalidAccessAttemptService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public InvalidAccessAttemptService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public InvalidAccessAttempt Create(InvalidAccessAttempt invalidAccessAttempt)
        {
            try
            {
                invalidAccessAttempt.HitCount = 1;
                invalidAccessAttempt.LastHitOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                _context.Add(invalidAccessAttempt);
                _context.SaveChanges();
                return invalidAccessAttempt;
            }
            catch (Exception)
            {
                _context.Entry(invalidAccessAttempt).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public InvalidAccessAttempt Get(string IPAddress)
        {
            var invalidAccessAttempt = _context.InvalidAccessAttempts.Find(IPAddress);
            return invalidAccessAttempt;
        }

        public void Update(InvalidAccessAttempt invalidAccessAttempt)
        {
            var existingInvalidAccessAttempt = _context.InvalidAccessAttempts.Find(invalidAccessAttempt.IPAddress);

            if (existingInvalidAccessAttempt == null)
                throw new Exception("Cannot find existing invalid access attempt for " + invalidAccessAttempt.IPAddress.ToString());

            existingInvalidAccessAttempt.HitCount = existingInvalidAccessAttempt.HitCount + 1;
            existingInvalidAccessAttempt.LastHitOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;

            _context.Attach(existingInvalidAccessAttempt);
            _context.SaveChanges();
        }

        public void Clear(string IPAddress)
        {
            var existingInvalidAccessAttempt = _context.InvalidAccessAttempts.Find(IPAddress);

            if (existingInvalidAccessAttempt != null)
            {
                _context.InvalidAccessAttempts.Remove(existingInvalidAccessAttempt);
                _context.SaveChanges();
            }
        }
    }
}
