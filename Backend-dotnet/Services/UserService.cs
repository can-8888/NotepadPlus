using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using NotepadPlusApi.Services.Interfaces;

namespace NotepadPlusApi.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<IEnumerable<UserDto>> SearchUsersAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<UserDto>();
            }

            var users = await _context.Users
                .Where(u => u.Username.Contains(term) || u.Email.Contains(term))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt
                })
                .Take(10)
                .ToListAsync();

            return users;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }
    }
} 