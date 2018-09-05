using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientManagement.Models;
using ClientManagement.Data;
using System.IO;
using System.Drawing;
using NodaTime;

namespace ClientManagement.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        User Get(int id);
        void Update(User user, string password = null);
        void UpdateProfile(int id, string userName, string fullName, string email, string password, MemoryStream profileImage);
        IEnumerable<User> GetAll();
        User Create(User user, string password);
        void Delete(int id);
    }


    public class UserService : IUserService
    {
        private DataContext _context;
        private IClock _clock;
        private readonly DateTimeZone _tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();

        public UserService(DataContext context, IClock clock)
        {
            _context = context;
            _clock = clock;
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.Users.SingleOrDefault(x => x.UserName == username && x.Active == true);

            // check if username exists
            if (user == null)
                return null;

            // If user's first time logging in hash and salt password
            if (user != null && password == username && user.LastLoginOn == null)
            {
                // encode force password for initial logins
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                _context.Attach(user);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                _context.Entry(user).Property(p => p.PasswordHash).IsModified = true;
                _context.Entry(user).Property(p => p.PasswordSalt).IsModified = true;
                _context.SaveChanges();
            }
            else
                if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    return null;

            // authentication successful
            var loginOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;

            user.LastLoginOn = loginOn;

            // update last login and only the last login
            _context.Attach(user);
            _context.Entry(user).Property(p => p.LastLoginOn).IsModified = true;

            //Update login log
            var newLogin = new UserLoginLog(user.Id, loginOn);
            _context.Add(newLogin);

            _context.SaveChanges();

            return user;
        }

        public User Get(int id)
        {
            return _context.Users.Find(id);
        }

        public void Update(User user, string password = null)
        {
            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            _context.Attach(user);
            _context.SaveChanges();
        }

        public void UpdateProfile(int id, string userName, string fullName, string email, string password, MemoryStream profileImage)
        {
            var user = _context.Users.Find(id);

            if (user == null)
                throw new Exception("User " + id + " not found.");

            if (userName != user.UserName)
            {
                // username has changed so check if the new username is already taken
                if (_context.Users.Any(x => x.UserName == userName))
                    throw new ServiceFieldException("userName", "Username " + userName + " is already taken");
            }

            // update profile
            user.UserName = userName;
            user.FullName = fullName;
            user.Email = email;
            user.UpdatedOn = _clock.GetCurrentInstant().InZone(_tz).LocalDateTime;


            _context.Attach(user);
            // Handle profile image (do not update if userParam.ProfileImage is null)
            if (profileImage != null)
            {
                // Re-size if needed
                Bitmap original = new Bitmap(profileImage);
                float scaleHeight = (float)200 / (float)original.Height;
                float scaleWidth = (float)200 / (float)original.Width;

                float scale = Math.Min(scaleHeight, scaleWidth);
                Bitmap resizedImage = new Bitmap(original, (int)(original.Width * scale), (int)(original.Height * scale));
                user.ProfileImage = ImageToByte2(resizedImage);
            }
            else
            {
                _context.Entry(user).Property(p => p.ProfileImage).IsModified = false;
            }

            // If no password was specified do NOT change the existing one
            if (!string.IsNullOrEmpty(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }
            else
            {
                _context.Entry(user).Property(p => p.PasswordSalt).IsModified = false;
                _context.Entry(user).Property(p => p.PasswordHash).IsModified = false;
            }

            _context.SaveChanges();
        }

        public User Create(User user, string password)
        {
            // validation
            if (string.IsNullOrWhiteSpace(password))
                throw new ServiceException("Password is required");

            if (_context.Users.Any(x => x.UserName == user.UserName))
                throw new ServiceException("Username \"" + user.UserName + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public void Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
        }

        public IEnumerable<User> GetAll()
        {
            return _context.Users;
        }






        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64)throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

        public static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }


    }
}
