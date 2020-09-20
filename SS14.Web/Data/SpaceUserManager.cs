using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SS14.Web.Data
{
    [UsedImplicitly]
    public sealed class SpaceUserManager : UserManager<SpaceUser>
    {
        public SpaceUserManager(IUserStore<SpaceUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<SpaceUser> passwordHasher,
            IEnumerable<IUserValidator<SpaceUser>> userValidators,
            IEnumerable<IPasswordValidator<SpaceUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<SpaceUser>> logger) : base(
            store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services,
            logger)
        {

        }

        public async Task<SpaceUser> FindByNameOrEmailAsync(string nameOrEmail)
        {
            var user = await FindByNameAsync(nameOrEmail);
            if (user != null)
            {
                return user;
            }

            return await FindByEmailAsync(nameOrEmail);
        }
    }
}