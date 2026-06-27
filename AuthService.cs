using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnimalShelterAI.Services
{
    public class AuthService
    {
        private readonly ShelterDbContext _context;
        private readonly string _salt;

        public AuthService(ShelterDbContext context)
        {
            _context = context;
            _salt = "animal_shelter_salt_2024";
        }

        // Метод аутентификации (ИСПРАВЛЕН)
        public user? Authenticate(string username, string password)
        {
            // Хешируем введённый пароль
            string hashedPassword = ComputeSha256Hash(password);
            
            // Ищем пользователя с таким логином и хешем пароля
            var user = _context.users
                .Include(u => u.userroles)
                    .ThenInclude(ur => ur.role)
                .FirstOrDefault(u => u.username == username && u.passwordhash == hashedPassword && u.isactive);
            
            if (user != null)
            {
                user.lastlogindate = System.DateTime.UtcNow;
                _context.SaveChanges();
            }
            
            return user;
        }

        // Метод хеширования пароля
        public string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData + _salt));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        // Метод для создания нового пользователя (для регистрации)
        public bool CreateUser(string username, string password, string email, string firstName, string lastName, string phone, string roleName)
        {
            if (_context.users.Any(u => u.username == username || u.email == email))
                return false;
                
            string hashedPassword = ComputeSha256Hash(password);
            
            var user = new user
            {
                username = username,
                passwordhash = hashedPassword,
                email = email,
                firstname = firstName,
                lastname = lastName,
                phone = phone,
                registrationdate = System.DateTime.UtcNow,
                isactive = true
            };
            
            _context.users.Add(user);
            _context.SaveChanges();
            
            // Добавление роли (аналогично существующему коду)
            var role = _context.roles.FirstOrDefault(r => r.rolename == roleName);
            if (role == null)
            {
                role = new role { rolename = roleName };
                _context.roles.Add(role);
                _context.SaveChanges();
            }
            
            _context.userroles.Add(new userrole { userid = user.userid, roleid = role.roleid });
            _context.SaveChanges();
            
            return true;
        }
    }
}