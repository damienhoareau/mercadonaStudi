using Microsoft.AspNetCore.Identity;

namespace Mercadona.Tests.Moq
{
    public class UserStoreMock : IUserStore<IdentityUser>, IUserPasswordStore<IdentityUser>
    {
        private readonly bool _creatingUserShouldFail;
        private readonly List<IdentityUser> _users = new();

        public UserStoreMock(bool creatingUserShouldFail = false)
        {
            _creatingUserShouldFail = creatingUserShouldFail;
        }

        public Task<IdentityResult> CreateAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            if (_creatingUserShouldFail)
                return Task.FromResult(IdentityResult.Failed());
            _users.Add(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        public Task<IdentityUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityUser> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(
                _users.FirstOrDefault(_ => _.UserName.ToUpperInvariant() == normalizedUserName)
            );
        }

        public Task<string> GetNormalizedUserNameAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(user.UserName.ToUpperInvariant());
        }

        public Task<string> GetPasswordHashAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(
            IdentityUser user,
            string normalizedName,
            CancellationToken cancellationToken
        )
        {
            return Task.Run(
                () =>
                {
                    IdentityUser? identityUser = _users.FirstOrDefault(
                        _ => _.UserName == user.UserName
                    );
                    if (identityUser != null)
                        identityUser.NormalizedUserName = normalizedName;
                },
                cancellationToken
            );
        }

        public Task SetPasswordHashAsync(
            IdentityUser user,
            string passwordHash,
            CancellationToken cancellationToken
        )
        {
            return Task.Run(
                () =>
                {
                    IdentityUser? identityUser = _users.FirstOrDefault(
                        _ => _.UserName == user.UserName
                    );
                    if (identityUser != null)
                    {
                        identityUser.PasswordHash = passwordHash;
                    }
                },
                cancellationToken
            );
        }

        public Task SetUserNameAsync(
            IdentityUser user,
            string userName,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(
            IdentityUser user,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}
