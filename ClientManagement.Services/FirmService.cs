using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IFirmService
    {
        Firm Get(int id);
        void Update(Firm firm);
        IEnumerable<Firm> GetAll();
        User Create(Firm firm);
        void Delete(int id);
    }

    public class FirmService : IFirmService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();


        public FirmService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public User Create(Firm firm)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Firm Get(int id)
        {
            return _context.Firms.Find(id);
        }

        public IEnumerable<Firm> GetAll()
        {
            return _context.Firms;
        }

        public void Update(Firm firm)
        {
            firm.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;
            _context.Attach(firm);
            _context.SaveChanges();
        }
    }
}
