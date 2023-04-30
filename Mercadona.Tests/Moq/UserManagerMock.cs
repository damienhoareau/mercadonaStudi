using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercadona.Tests.Moq
{
    public static class UserManagerMock
    {
        public static UserManager<TUser> GetUserManager<TUser>(IUserStore<TUser>? store = null)
            where TUser : class
        {
            store ??= new Mock<IUserStore<TUser>>().Object;
            Mock<IOptions<IdentityOptions>> options = new();
            IdentityOptions idOptions = new();
            idOptions.Lockout.AllowedForNewUsers = false;
            options.Setup(o => o.Value).Returns(idOptions);
            List<IUserValidator<TUser>> userValidators = new();
            Mock<IUserValidator<TUser>> validator = new();
            userValidators.Add(validator.Object);
            List<PasswordValidator<TUser>> pwdValidators = new() { new PasswordValidator<TUser>() };
            UserManager<TUser> userManager = new UserManager<TUser>(
                store,
                options.Object,
                new PasswordHasher<TUser>(),
                userValidators,
                pwdValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                new Mock<ILogger<UserManager<TUser>>>().Object
            );
            validator
                .Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>()))
                .Returns(Task.FromResult(IdentityResult.Success))
                .Verifiable();
            return userManager;
        }
    }
}
