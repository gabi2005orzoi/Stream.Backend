using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
        Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task UpdateAsync(User user, CancellationToken cancellationToken);
    }
}
