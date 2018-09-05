using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IClientGroupService
    {
        ClientGroup Get(int id);
        ClientGroup GetViaFirmId(string id);
        void Update(ClientGroup clientGroup);
        IEnumerable<ClientGroup> GetAllInFirm(int firmId);
        int GetFilteredCountViaNameInFirm(int firmId, string name);
        IEnumerable<ClientGroup> GetFilteredViaNameInFirm(int firmId, string name, int pageNumber, int perPageQuantity);
        ClientGroup Create(ClientGroup clientGroup);
        void Delete(int id);
        ClientGroup GetDefault(int firmId);
    }

    public class ClientGroupService : IClientGroupService
    {
        public const string DEFAULT_FIRMGROUPID = "DEFAULT";

        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();


        public ClientGroupService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public ClientGroup Create(ClientGroup clientGroup)
        {
            try
            {
                _context.ClientGroups.Add(clientGroup);
                _context.SaveChanges();
                return clientGroup;
            }
            catch (Exception)
            {
                _context.Entry(clientGroup).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                throw;
            }
        }

        public void Delete(int id)
        {
            var group = _context.ClientGroups.Find(id);
            if (group != null)
            {
                _context.ClientGroups.Remove(group);
                _context.SaveChanges();
            }
        }

        public ClientGroup Get(int id)
        {
            return _context.ClientGroups.Find(id);
        }

        public ClientGroup GetViaFirmId(string id)
        {
            return _context.ClientGroups.FirstOrDefault(cg => cg.FirmGroupId == id);
        }

        public ClientGroup GetDefault(int firmId)
        {
            // Return default client group. If one does not exist add one
            ClientGroup cg = _context.ClientGroups.FirstOrDefault(g => g.FirmId == firmId && g.FirmGroupId == DEFAULT_FIRMGROUPID);

            if (cg == null)
            {
                cg = new ClientGroup { Name = "Default Client Group", FirmGroupId = DEFAULT_FIRMGROUPID, FirmId = firmId };
                _context.Add(cg);
                _context.SaveChanges();
            }

            return cg; 
        }

        public IEnumerable<ClientGroup> GetAllInFirm(int firmId)
        {
            return _context.ClientGroups.Where(f => f.FirmId == firmId).OrderBy(o => o.Name).ToList();
        }

        public int GetFilteredCountViaNameInFirm(int firmId, string name)
        {
            var q = _context.ClientGroups
                .Where(f => f.FirmId == firmId && (f.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)));

            var total = q.Count();

            return total;
        }

        public IEnumerable<ClientGroup> GetFilteredViaNameInFirm(int firmId, string name, int pageNumber, int perPageQuantity)
        {
            var q = _context.ClientGroups
                .Where(f => f.FirmId == firmId && (f.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)))
                .OrderBy(o => o.Name)
                .Skip(pageNumber * perPageQuantity)
                .Take(perPageQuantity)
                .ToList();

            return q;
        }

        public void Update(ClientGroup clientGroup)
        {
            clientGroup.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(clientGroup);
            _context.SaveChanges();
        }
    }
}
