using System.Collections.Generic;
using System.Threading.Tasks;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<UserDto>> SearchUsersAsync(string term);
        Task<User?> GetUserByUsernameAsync(string username);
    }
} 