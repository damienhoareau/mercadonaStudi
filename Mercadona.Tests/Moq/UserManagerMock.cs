using Mercadona.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercadona.Tests.Moq
{
    public class UserManagerMock : UserManager<IdentityUser>
    {
        private static IOptions<IdentityOptions> PrivateIdentityOptions
        {
            get
            {
                Mock<IOptions<IdentityOptions>> options = new();
                IdentityOptions idOptions = new();
                idOptions.Lockout.AllowedForNewUsers = false;
                options.Setup(o => o.Value).Returns(idOptions);
                return options.Object;
            }
        }
        private static readonly Mock<IUserValidator<IdentityUser>> PrivateUserValidatorMock = new();
        private static List<IUserValidator<IdentityUser>> PrivateUserValidators
        {
            get
            {
                List<IUserValidator<IdentityUser>> userValidators =
                    new() { PrivateUserValidatorMock.Object };
                return userValidators;
            }
        }

        public UserManagerMock(
            bool creatingUserShouldFail = false,
            params UserModel[] existingUsers
        )
            : base(
                new UserStoreMock(creatingUserShouldFail),
                PrivateIdentityOptions,
                new PasswordHasher<IdentityUser>(),
                PrivateUserValidators,
                new List<PasswordValidator<IdentityUser>>()
                {
                    new PasswordValidator<IdentityUser>()
                },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                new Mock<ILogger<UserManager<IdentityUser>>>().Object
            )
        {
            PrivateUserValidatorMock
                .Setup(v => v.ValidateAsync(this, It.IsAny<IdentityUser>()))
                .Returns(Task.FromResult(IdentityResult.Success))
                .Verifiable();

            // Add existing users
            foreach (UserModel userModel in existingUsers)
            {
                IdentityUser user =
                    new()
                    {
                        Email = userModel.Username,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        UserName = userModel.Username
                    };
                user.PasswordHash = PasswordHasher.HashPassword(user, userModel.Password);
                Store.CreateAsync(user, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public override Task<bool> CheckPasswordAsync(IdentityUser user, string password)
        {
            PasswordVerificationResult result = PasswordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password
            );
            return Task.FromResult(
                result == PasswordVerificationResult.Success
                    || result == PasswordVerificationResult.SuccessRehashNeeded
            );
        }
    }
}
