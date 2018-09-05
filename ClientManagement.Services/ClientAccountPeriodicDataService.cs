using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{

    public interface IClientAccountPeriodicDataService
    {
        ClientAccountPeriodicData Get(int id);
        ClientAccountPeriodicData Get(int accountId, LocalDate asOfDate);
        void Update(ClientAccountPeriodicData clientAccountPeriodicData);
        ClientAccountPeriodicData Create(ClientAccountPeriodicData clientAccountPeriodicData);
        void Delete(int id);
        IEnumerable<ClientAccountPeriodicData> GetAccountPeriodicDataAvailableToClient(int clientId);
        IEnumerable<ClientAccountPeriodicData> Get(int accountId, int pageNumber, int perPageQuantity);
        int GetCountInClientAccount(int accountId);
        LocalDate? GetMostCurrentDateAcrossAllClientAccounts(int clientId);
        LocalDate? GetPreviousDateGivenByTimeSpanAcrossAllClientAccounts(int clientId, LocalDate referenceDate, int maximumTimeSpanDays);
        IEnumerable<ClientAccountPeriodicData> Get(int clientId, IEnumerable<LocalDate> dataDates);
    }

    public class ClientAccountPeriodicDataService : IClientAccountPeriodicDataService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientAccountPeriodicDataService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientAccountPeriodicData Create(ClientAccountPeriodicData clientAccountPeriodicData)
        {
            try
            {
                clientAccountPeriodicData.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                _context.ClientAccountPeriodicData.Add(clientAccountPeriodicData);
                _context.SaveChanges();

                return clientAccountPeriodicData;
            }
            catch (Exception)
            {
                _context.Entry(clientAccountPeriodicData).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Delete(int id)
        {
            var clientAccountPeriodicData = _context.ClientAccountPeriodicData.Find(id);
           
            if (clientAccountPeriodicData != null)
            {
                _context.ClientAccountPeriodicData.Remove(clientAccountPeriodicData);
                _context.SaveChanges();
            }
        }

        public ClientAccountPeriodicData Get(int id)
        {
            var clientAccountPeriodicData = _context.ClientAccountPeriodicData.Find(id);
            return clientAccountPeriodicData;
        }

        public void Update(ClientAccountPeriodicData clientAccountPeriodicData)
        {
            clientAccountPeriodicData.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(clientAccountPeriodicData);
            _context.SaveChanges();
        }

        public IEnumerable<ClientAccountPeriodicData> GetAccountPeriodicDataAvailableToClient(int clientId)
        {
            // The Account periodic data available to a client will cover all their Accounts and include the most recent TWO periods
            var cppds = (from cpd in _context.ClientAccountPeriodicData
                         join cp in _context.ClientAccounts on cpd.AccountId equals cp.Id
                         where cp.ClientId == clientId
                         orderby cpd.PeriodicDataAsOf descending
                         select cpd).Take(2).ToList();

            return cppds;
        }

        public ClientAccountPeriodicData Get(int accountId, LocalDate asOfDate)
        {
            ClientAccountPeriodicData capd = _context.ClientAccountPeriodicData.FirstOrDefault(c => c.AccountId == accountId && c.PeriodicDataAsOf == asOfDate);
            return capd;
        }

        public IEnumerable<ClientAccountPeriodicData> Get(int accountId, int pageNumber, int perPageQuantity)
        {
            var q = (from capd in _context.ClientAccountPeriodicData
                     where capd.AccountId == accountId
                     orderby capd.PeriodicDataAsOf descending, capd.UpdatedOn descending
                     select capd)
                     .Skip(pageNumber * perPageQuantity)
                     .Take(perPageQuantity)
                     .ToList();

            return q; 
        }

        public int GetCountInClientAccount(int accountId)
        {
            var q = _context.ClientAccountPeriodicData
                .Where(c => c.AccountId == accountId);
            var total = q.Count();
            return total;
        }

        public LocalDate? GetMostCurrentDateAcrossAllClientAccounts(int clientId)
        {
            LocalDate? mostCurrentDate = null;
            var mostCurrentDateQuery = (from capd in _context.ClientAccountPeriodicData
                                        join ca in _context.ClientAccounts on capd.AccountId equals ca.Id
                                        orderby capd.PeriodicDataAsOf descending
                                        where ca.ClientId == clientId
                                        select capd)
                                        .Take(1).ToList();

            if (mostCurrentDateQuery.Count > 0)
                mostCurrentDate = mostCurrentDateQuery[0].PeriodicDataAsOf;

            return mostCurrentDate;
        }

        public LocalDate? GetPreviousDateGivenByTimeSpanAcrossAllClientAccounts(int clientId, LocalDate referenceDate, int maximumTimeSpanDays)
        {
            LocalDate? previousDate = null;
            var previousDateQuery = (from capd in _context.ClientAccountPeriodicData
                                     join ca in _context.ClientAccounts on capd.AccountId equals ca.Id
                                     orderby capd.PeriodicDataAsOf ascending
                                     where ca.ClientId == clientId && capd.PeriodicDataAsOf > referenceDate.Minus(Period.FromDays(maximumTimeSpanDays))
                                     select capd)
                                    .Take(1).ToList();

            if (previousDateQuery.Count > 0)
                previousDate = previousDateQuery[0].PeriodicDataAsOf;

            return previousDate;
        }

        public IEnumerable<ClientAccountPeriodicData> Get(int clientId, IEnumerable<LocalDate> dataDates)
        {
            var periodicData = (from capd in _context.ClientAccountPeriodicData
                                join ca in _context.ClientAccounts on capd.AccountId equals ca.Id
                                join dD in dataDates on capd.PeriodicDataAsOf equals dD
                                where ca.ClientId == clientId
                                orderby capd.PeriodicDataAsOf descending
                                select capd
                       ).ToList();

            return periodicData;
        }
    }
}
