using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Backend.Persistence;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Infrastructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private AppDbContext db;

        public UserRepository(AppDbContext db)
        {
            this.db = db;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            await db.Users.AddAsync(user, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await db.Users.AnyAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLower();
            return await db.Users.AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            var normalized = username.Trim().ToLower();
            return await db.Users.AnyAsync(u => u.Username.ToLower() == normalized, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLower();
            return await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            var normalized = username.Trim().ToLower();
            return await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalized, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken)
        {
            db.Users.Update(user);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
