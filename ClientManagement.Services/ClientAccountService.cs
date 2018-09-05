using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientAccountService
    {
        ClientAccount Get(int id);
        ClientAccount Get(int firmId, string firmClientAccountId);
        void Update(ClientAccount clientAccount);
        IEnumerable<ClientAccount> GetAllInClient(int clientId);
        ClientAccount Create(ClientAccount clientAccount);
        void Delete(int id);
    }

    public class ClientAccountService : IClientAccountService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientAccountService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientAccount Create(ClientAccount clientAccount)
        {
            try
            {
                clientAccount.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                _context.ClientAccounts.Add(clientAccount);
                _context.SaveChanges();

                return clientAccount;
            }
            catch (Exception)
            {
                _context.Entry(clientAccount).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Delete(int id)
        {
            var clientAccount = _context.ClientAccounts.Find(id);
            if (clientAccount != null)
            {
                _context.ClientAccounts.Remove(clientAccount);
                _context.SaveChanges();
            }
        }

        public ClientAccount Get(int id)
        {
            var clientAccount = _context.ClientAccounts.Find(id);
            return clientAccount;
        }

        public ClientAccount Get(int firmId, string firmClientAccountId)
        {
            return _context.ClientAccounts.SingleOrDefault(c => c.FirmId == firmId && c.FirmClientAccountId == firmClientAccountId);
        }

        public IEnumerable<ClientAccount> GetAllInClient(int clientId)
        {
            return _context.ClientAccounts.Where(c => c.ClientId == clientId).OrderBy(o => o.CommonName).ToList();
        }

        public void Update(ClientAccount clientAccount)
        {
            clientAccount.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;

            _context.Attach(clientAccount);
            _context.SaveChanges();
        }
    }
}
