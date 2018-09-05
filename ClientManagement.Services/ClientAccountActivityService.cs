using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientAccountActivityService
    {
        ClientAccountActivity Get(int id);
        ClientAccountActivity Get(int accountId, LocalDate activityDate, int activityTypeId);
        void Update(ClientAccountActivity clientAccountActivity);
        ClientAccountActivity Create(ClientAccountActivity clientAccountActivity);
        void Delete(int id);
        IEnumerable<ClientAccountActivityWithType> GetActivityAvailableToClient(int clientId, int daysOfActivity);
        IEnumerable<ClientAccountActivityWithType> Get(int accountId, int pageNumber, int perPageQuantity);
        int GetCountInClientAccount(int accountId);

    }

    public class ClientAccountActivityService : IClientAccountActivityService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientAccountActivityService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientAccountActivity Create(ClientAccountActivity clientAccountActivity)
        {
            try
            {
                clientAccountActivity.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                _context.ClientAccountActivities.Add(clientAccountActivity);
                _context.SaveChanges();

                return clientAccountActivity;
            }
            catch (Exception)
            {
                _context.Entry(clientAccountActivity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Delete(int id)
        {
            var clientAccountActivity = _context.ClientAccountActivities.Find(id);

            if (clientAccountActivity != null)
            {
                _context.ClientAccountActivities.Remove(clientAccountActivity);
                _context.SaveChanges();
            }
        }

        public ClientAccountActivity Get(int id)
        {
            var clientAccountActivity = _context.ClientAccountActivities.Find(id);
            return clientAccountActivity;
        }

        public ClientAccountActivity Get(int accountId, LocalDate activityDate, int activityTypeId)
        {
            return _context.ClientAccountActivities.FirstOrDefault(c => c.AccountId == accountId && c.ActivityDate == activityDate && c.ActivityTypeId == activityTypeId);
        }


        public IEnumerable<ClientAccountActivityWithType> GetActivityAvailableToClient(int clientId, int daysOfActivity)
        {
            // Activity available to a client are the last week's activity (this will be summarized in the podcast translation portion)
            LocalDate now = _clock.GetCurrentInstant().InZone(_tz).Date;

            var cpas = (from caa in _context.ClientAccountActivities
                        join cp in _context.ClientAccounts on caa.AccountId equals cp.Id
                        join cat in _context.ClientAccountActivityTypes on caa.ActivityTypeId equals cat.Id
                        where cp.ClientId == clientId && Period.Between(caa.ActivityDate, now, PeriodUnits.Days).Days <= daysOfActivity
                        orderby cp.CommonName
                        select new ClientAccountActivityWithType()
                        {
                            AccountId = caa.AccountId,
                            ActivityAmount = caa.ActivityAmount,
                            ActivityDate = caa.ActivityDate,
                            ActivityDescriptionOverride = caa.ActivityDescriptionOverride,
                            ActivityTypeId = caa.ActivityTypeId,
                            ActivityTypeName = cat.Name,
                            ActivityTypeUploadCode = cat.UploadCode,
                            Id = caa.Id
                        }
                        ).ToList();
            return cpas;
        }
        public IEnumerable<ClientAccountActivityWithType> Get(int accountId, int pageNumber, int perPageQuantity)
        {
            var q = (from caa in _context.ClientAccountActivities
                     join cat in _context.ClientAccountActivityTypes on caa.ActivityTypeId equals cat.Id
                     where caa.AccountId == accountId
                     orderby caa.ActivityDate descending, caa.UpdatedOn descending
                     select new ClientAccountActivityWithType()
                     {
                         AccountId = caa.AccountId,
                         ActivityAmount = caa.ActivityAmount,
                         ActivityDate = caa.ActivityDate,
                         ActivityDescriptionOverride = caa.ActivityDescriptionOverride,
                         ActivityTypeId = caa.ActivityTypeId,
                         ActivityTypeName = cat.Name,
                         ActivityTypeUploadCode = cat.UploadCode,
                         Id = caa.Id
                     })
                     .Skip(pageNumber * perPageQuantity)
                     .Take(perPageQuantity)
                     .ToList();

            return q;
        }


        public int GetCountInClientAccount(int accountId)
        {
            var q = _context.ClientAccountActivities
                .Where(c => c.AccountId == accountId);
            var total = q.Count();
            return total;
        }



        public void Update(ClientAccountActivity clientAccountActivity)
        {
            clientAccountActivity.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(clientAccountActivity);
            _context.SaveChanges();
        }

    }
}
