using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;
using System.Collections.Specialized;

namespace ClientManagement.Services
{

    public interface IClientService
    {
        Client Get(int id);
        Client Get(int firmId, string firmClientId);
        //Client Get(string podcastId);
        Client Get(string userName);
        Client GetViaPodcastId(string podcastId);
        void Update(Client client);
        IEnumerable<Client> GetAllInClientGroup(int clientGroupId);
        IEnumerable<Client> GetAllInFirm(int firmId);
        int GetFilteredCountViaNameInClientFirm(int firmId, string name);
        int GetFilteredCountViaNameInClientGroup(int clientGroupId, string name);
        IEnumerable<ClientWithGroup> GetFilteredViaNameInFirm(int firmId, string name, int pageNumber, int perPageQuantity);
        IEnumerable<ClientWithGroup> GetFilteredViaNameInClientGroup(int clientGroupId, string name, int pageNumber, int perPageQuantity);
        Client Create(Client client);
        void Delete(int id);
        void SetNewAccountActivityAvailable(int clientId);
        void SetNewPeriodicDataAvailable(int clientId);
    }

    public class ClientService : IClientService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public ClientService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public Client Create(Client client)
        {
            try
            {
                client.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
                client.PodcastId = System.Guid.NewGuid();

                _context.Clients.Add(client);
                _context.SaveChanges();

                return client;
            }
            catch (Exception)
            {
                _context.Entry(client).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Delete(int id)
        {
            var client = _context.Clients.Find(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                _context.SaveChanges();
            }
        }

        public Client Get(int id)
        {
            return _context.Clients.Find(id);
        }

        public Client Get(int firmId, string firmClientId)
        {
            return _context.Clients.SingleOrDefault(c => c.FirmId == firmId && c.FirmClientId == firmClientId);
        }

        public Client Get(string userName)
        {
            return _context.Clients.SingleOrDefault(c => c.UserName == userName);
        }

        public Client GetViaPodcastId(string podcastId)
        {
            return _context.Clients.SingleOrDefault(c => c.PodcastId == new Guid(podcastId));
        }


        public IEnumerable<Client> GetAllInFirm(int firmId)
        {
            return _context.Clients.Where(c => c.FirmId == firmId).OrderBy(o => o.Name).ToList();
        }

        public IEnumerable<Client> GetAllInClientGroup(int clientGroupId)
        {
            return _context.Clients.Where(c => c.ClientGroupId == clientGroupId).OrderBy(o => o.Name).ToList();
        }

        public int GetFilteredCountViaNameInClientFirm(int firmId, string name)
        {
            var q = _context.Clients
                .Where(f => f.FirmId == firmId && (f.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)));

            var total = q.Count();
            return total;
        }

        public int GetFilteredCountViaNameInClientGroup(int clientGroupId, string name)
        {
            var q = _context.Clients
                .Where(f => f.ClientGroupId == clientGroupId && (f.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)));

            var total = q.Count();
            return total;
        }

        public IEnumerable<ClientWithGroup> GetFilteredViaNameInClientGroup(int clientGroupId, string name, int pageNumber, int perPageQuantity)
        {
            var q = (from c in _context.Clients
                     join cg in _context.ClientGroups on c.ClientGroupId equals cg.Id
                     where c.ClientGroupId == clientGroupId && c.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)
                     orderby c.Name
                     select new ClientWithGroup()
                     {
                         Client = c,
                         GroupName = cg.Name
                     }).Skip(pageNumber * perPageQuantity)
                     .Take(perPageQuantity)
                     .ToList();

            return q;
        }

        public IEnumerable<ClientWithGroup> GetFilteredViaNameInFirm(int firmId, string name, int pageNumber, int perPageQuantity)
        {
            var q = (from c in _context.Clients
                     join cg in _context.ClientGroups on c.ClientGroupId equals cg.Id
                     where c.FirmId == firmId && c.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)
                     orderby c.Name
                     select new ClientWithGroup()
                     {
                         Client = c,
                         GroupName = cg.Name
                     }).Skip(pageNumber * perPageQuantity)
                     .Take(perPageQuantity)
                     .ToList();

            return q;
        }

        public void Update(Client client)
        {
            client.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(client);
            _context.SaveChanges();
        }

        public void SetNewAccountActivityAvailable(int clientId)
        {
            var c = _context.Clients.Find(clientId);

            if (c == null)
                throw new Exception("Client " + clientId + " not found.");

            c.AccountActivityImportedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            c.AccountActivityNew = true;

            _context.Attach(c);
            _context.SaveChanges();
        }

        public void SetNewPeriodicDataAvailable(int clientId)
        {
            var c = _context.Clients.Find(clientId);

            if (c == null)
                throw new Exception("Client " + clientId + " not found.");

            c.PeriodicDataImportedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            c.PeriodicDataNew = true;

            _context.Attach(c);
            _context.SaveChanges();
        }
    }
}

