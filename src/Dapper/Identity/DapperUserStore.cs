using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Fulgoribus.Luxae.Dapper.Identity
{
    /// <remarks>
    /// This implementation cannot inherit from <see cref="UserStoreBase{TUser, TKey, TUserClaim, TUserLogin, TUserToken}"/> because we cannot implement
    /// <see cref="IQueryableUserStore{TUser}"/> using Dapper.
    /// </remarks>
    public sealed class DapperUserStore : IUserAuthenticatorKeyStore<IdentityUser>, IUserAuthenticationTokenStore<IdentityUser>, IUserClaimStore<IdentityUser>,
        IUserEmailStore<IdentityUser>, IUserLoginStore<IdentityUser>, IUserPasswordStore<IdentityUser>, IUserPhoneNumberStore<IdentityUser>, IUserTwoFactorStore<IdentityUser>,
        IUserTwoFactorRecoveryCodeStore<IdentityUser>
    {
        private const string InternalLoginProvider = "[AspNetUserStore]";
        private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
        private const string RecoveryCodeTokenName = "RecoveryCodes";

        private readonly SqlConnection db;

        // Mimic the behavior of the private DbSet instances in UserStore. These should probably be persisted as part of the IdentityUser class,
        // but 
        private readonly List<IdentityUserClaim<string>> insertIdentityUserClaims = new List<IdentityUserClaim<string>>();
        private readonly List<IdentityUserClaim<string>> deleteIdentityUserClaims = new List<IdentityUserClaim<string>>();
        private readonly List<IdentityUserClaim<string>> updateIdentityUserClaims = new List<IdentityUserClaim<string>>();
        private readonly List<IdentityUserLogin<string>> insertIdentityUserLogins = new List<IdentityUserLogin<string>>();
        private readonly List<IdentityUserLogin<string>> deleteIdentityUserLogins = new List<IdentityUserLogin<string>>();
        private readonly List<IdentityUserToken<string>> insertIdentityUserTokens = new List<IdentityUserToken<string>>();
        private readonly List<IdentityUserToken<string>> deleteIdentityUserTokens = new List<IdentityUserToken<string>>();
        private readonly List<IdentityUserToken<string>> updateIdentityUserTokens = new List<IdentityUserToken<string>>();

        public DapperUserStore(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                var userClaim = new IdentityUserClaim<string>
                {
                    UserId = user.Id
                };
                userClaim.InitializeFromClaim(claim);
                insertIdentityUserClaims.Add(userClaim);
            }
            return Task.CompletedTask;
        }

        public Task AddLoginAsync(IdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            var userLogin = new IdentityUserLogin<string>
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey,
                ProviderDisplayName = login.ProviderDisplayName,
                UserId = user.Id
            };
            insertIdentityUserLogins.Add(userLogin);
            return Task.CompletedTask;
        }

        public async Task<int> CountCodesAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? string.Empty;
            return string.IsNullOrWhiteSpace(mergedCodes)
                ? 0
                : mergedCodes.Split(';').Length;
        }

        public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            var sql = "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp,"
                + " ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount) VALUES"
                + $" (@{nameof(user.Id)}, @{nameof(user.UserName)}, @{nameof(user.NormalizedUserName)}, @{nameof(user.Email)},"
                + $" @{nameof(user.NormalizedEmail)}, @{nameof(user.EmailConfirmed)}, @{nameof(user.PasswordHash)}, @{nameof(user.SecurityStamp)},"
                + $" @{nameof(user.ConcurrencyStamp)}, @{nameof(user.PhoneNumber)}, @{nameof(user.PhoneNumberConfirmed)}, @{nameof(user.TwoFactorEnabled)},"
                + $" @{nameof(user.LockoutEnd)}, @{nameof(user.LockoutEnabled)}, @{nameof(user.AccessFailedCount)})";
            var cmd = new CommandDefinition(sql, user, cancellationToken: cancellationToken);
            await db.QuerySingleAsync<string>(cmd);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            // The foreign keys are all set to cascade deletes, so we do not need to delete from other tables individually.
            var sql = $"DELETE FROM AspNetUsers WHERE Id = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            await db.ExecuteAsync(cmd);

            // The EF UserStore returns success even if the user didn't exist.
            return IdentityResult.Success;
        }

        public void Dispose() => db.Dispose();

        public async Task<IdentityUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE NormalizedEmail = @{nameof(normalizedEmail)}";
            var cmd = new CommandDefinition(sql, new { normalizedEmail }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUser>(cmd);
        }

        public async Task<IdentityUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE Id = @{nameof(userId)}";
            var cmd = new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUser>(cmd);
        }

        public async Task<IdentityUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var sql = "SELECT u.* FROM AspNetUserTokens ut JOIN AspNetUsers u ON u.Id = ut.UserId"
                + $" WHERE ut.LoginProvider = @{nameof(loginProvider)} AND ut.ProviderKey = @{nameof(providerKey)}";
            var cmd = new CommandDefinition(sql, new { loginProvider, providerKey }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUser>(cmd);
        }

        public async Task<IdentityUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE NormalizedUserName = @{nameof(normalizedUserName)}";
            var cmd = new CommandDefinition(sql, new { normalizedUserName }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUser>(cmd);
        }

        public Task<string?> GetAuthenticatorKeyAsync(IdentityUser user, CancellationToken cancellationToken)
            => GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);

        public async Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserClaims WHERE UserId = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserClaim<string>>(cmd);
            return result.Select(c => c.ToClaim()).ToList();
        }

        public Task<string> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserLogins WHERE UserId = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserLogin<string>>(cmd);
            return result.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList();
        }

        public Task<string> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

        public Task<string> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

        public Task<string> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

        public Task<string> GetPhoneNumberAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumber);

        public Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumberConfirmed);

        public async Task<string?> GetTokenAsync(IdentityUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var token = await FindTokenAsync(user, loginProvider, name, cancellationToken);
            return token?.Value;
        }

        public Task<bool> GetTwoFactorEnabledAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.TwoFactorEnabled);

        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

        public Task<string> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public async Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            var sql = "SELECT u.* FROM AspNetUserClaims uc JOIN AspNetUsers u ON u.Id = uc.UserId"
                + $" WHERE uc.ClaimType = @{nameof(claim.Type)} AND uc.ClaimValue= @{nameof(claim.Value)}";
            var cmd = new CommandDefinition(sql, claim, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUser>(cmd);
            return result.ToList();
        }

        public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash != null);

        public async Task<bool> RedeemCodeAsync(IdentityUser user, string code, CancellationToken cancellationToken)
        {
            var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? string.Empty;
            var splitCodes = mergedCodes.Split(';');
            if (splitCodes.Contains(code))
            {
                var updatedCodes = new List<string>(splitCodes.Where(s => s != code));
                await ReplaceCodesAsync(user, updatedCodes, cancellationToken);
                return true;
            }
            return false;
        }

        public Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                var userClaim = new IdentityUserClaim<string>
                {
                    UserId = user.Id
                };
                userClaim.InitializeFromClaim(claim);
                deleteIdentityUserClaims.Add(userClaim);
            }
            return Task.CompletedTask;
        }

        public Task RemoveLoginAsync(IdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var userLogin = new IdentityUserLogin<string>
            {
                LoginProvider = loginProvider,
                ProviderKey = providerKey,
                UserId = user.Id
            };
            deleteIdentityUserLogins.Add(userLogin);
            return Task.CompletedTask;
        }

        public Task RemoveTokenAsync(IdentityUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var userToken = new IdentityUserToken<string>
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name
            };
            deleteIdentityUserTokens.Add(userToken);
            return Task.CompletedTask;
        }

        public async Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserClaims WHERE UserId = @{nameof(user.Id)} AND ClaimType = @{nameof(claim.Type)} AND ClaimValue = {nameof(claim.Value)}";
            var cmd = new CommandDefinition(sql, new { user.Id, claim.Type, claim.Value }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserClaim<string>>(cmd);

            foreach (var dbClaim in result)
            {
                dbClaim.InitializeFromClaim(newClaim);
                updateIdentityUserClaims.Add(dbClaim);
            }
        }

        public Task ReplaceCodesAsync(IdentityUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            var mergedCodes = string.Join(";", recoveryCodes);
            return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
        }

        public Task SetAuthenticatorKeyAsync(IdentityUser user, string key, CancellationToken cancellationToken)
            => SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);

        public Task SetEmailAsync(IdentityUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(IdentityUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(IdentityUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(IdentityUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberAsync(IdentityUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task SetTokenAsync(IdentityUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            var token = await FindTokenAsync(user, loginProvider, name, cancellationToken);
            if (token == null)
            {
                token = new IdentityUserToken<string>
                {
                    UserId = user.Id,
                    LoginProvider = loginProvider,
                    Name = name,
                    Value = value
                };
                insertIdentityUserTokens.Add(token);
            }
            else
            {
                token.Value = value;
                updateIdentityUserTokens.Add(token);
            }
        }

        public Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(IdentityUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            // Note that the cancellation token does *not* need to be passed in when defining our CommandDefinitions. ExecuteTransactionAsync will
            // use the token it receives and ignore any on the incoming commands.

            var userSql = $"UPDATE AspNetUsers SET UserName = @{nameof(user.UserName)}, NormalizedUserName = @{nameof(user.NormalizedUserName)},"
                + $" Email = @{nameof(user.Email)}, NormalizedEmail = @{nameof(user.NormalizedEmail)},  EmailConfirmed = @{nameof(user.EmailConfirmed)},"
                + $" PasswordHash = @{nameof(user.PasswordHash)}, SecurityStamp = @{nameof(user.SecurityStamp)}, ConcurrencyStamp = @{nameof(user.ConcurrencyStamp)},"
                + $" PhoneNumber = @{nameof(user.PhoneNumber)}, PhoneNumberConfirmed = @{nameof(user.PhoneNumberConfirmed)},"
                + $" TwoFactorEnabled = @{nameof(user.TwoFactorEnabled)}, LockoutEnd = @{nameof(user.LockoutEnd)},"
                + $" LockoutEnabled = @{nameof(user.LockoutEnabled)}, AccessFailedCount = @{nameof(user.AccessFailedCount)} WHERE Id = @{nameof(user.Id)}";
            var userCmd = new CommandDefinition(userSql, user);

            var claimsInsertSql = "INSERT INTO AspNetUserClaims (UserId, ClaimType, ClaimValue)"
                + $" VALUES (@{nameof(IdentityUserClaim<string>.UserId)}, @{nameof(IdentityUserClaim<string>.ClaimType)}, @{nameof(IdentityUserClaim<string>.ClaimValue)})";
            var claimsInsertCmds = insertIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsInsertSql, c));

            var claimsDeleteSql = "DELETE FROM AspNetUserClaims"
                + $" WHERE UserId = @{nameof(IdentityUserClaim<string>.UserId)}"
                + $" AND ClaimType = @{nameof(IdentityUserClaim<string>.ClaimType)}"
                + $" AND ClaimValue = @{nameof(IdentityUserClaim<string>.ClaimValue)}";
            var claimsDeleteCmds = deleteIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsDeleteSql, c));

            var claimsUpdateSql = "UPDATE AspNetUserClaims SET"
                + $" ClaimType = @{nameof(IdentityUserClaim<string>.ClaimType)},"
                + $" ClaimValue = @{nameof(IdentityUserClaim<string>.ClaimValue)}"
                + $" WHERE Id = @{nameof(IdentityUserClaim<string>.Id)}";
            var claimsUpdateCmds = updateIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsUpdateSql, c));

            var loginsInsertSql = "INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)"
                + $" VALUES (@{nameof(IdentityUserLogin<string>.LoginProvider)}, @{nameof(IdentityUserLogin<string>.ProviderKey)},"
                + $" @{nameof(IdentityUserLogin<string>.ProviderDisplayName)}, @{nameof(IdentityUserLogin<string>.UserId)})";
            var loginsInsertCmds = insertIdentityUserLogins
                .Where(l => l.UserId == user.Id)
                .Select(l => new CommandDefinition(loginsInsertSql, l));

            var loginsDeleteSql = "DELETE FROM AspNetUserLogins"
                + $" WHERE LoginProvider = @{nameof(IdentityUserLogin<string>.LoginProvider)}"
                + $" AND ProviderKey = @{nameof(IdentityUserLogin<string>.ProviderKey)}"
                + $" AND UserId = @{nameof(IdentityUserLogin<string>.UserId)}";
            var loginsDeleteCmds = deleteIdentityUserLogins
                .Where(l => l.UserId == user.Id)
                .Select(l => new CommandDefinition(loginsDeleteSql, l));

            var tokensInsertSql = "INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value)"
                + $" VALUES (@{nameof(IdentityUserToken<string>.UserId)}, @{nameof(IdentityUserToken<string>.LoginProvider)},"
                + $" @{nameof(IdentityUserToken<string>.Name)}, @{nameof(IdentityUserToken<string>.Value)})";
            var tokensInsertCmds = insertIdentityUserTokens
                .Where(t => t.UserId == user.Id)
                .Select(t => new CommandDefinition(tokensInsertSql, t));

            var tokensDeleteSql = "DELETE FROM AspNetUserTokens"
                + $" WHERE UserId = @{nameof(IdentityUserToken<string>.UserId)}"
                + $" AND LoginProvider = @{nameof(IdentityUserToken<string>.LoginProvider)}"
                + $" AND Name = @{nameof(IdentityUserToken<string>.Name)}";
            var tokensDeleteCmds = deleteIdentityUserTokens
                .Where(t => t.UserId == user.Id)
                .Select(t => new CommandDefinition(tokensDeleteSql, t));

            var tokensUpdateSql = "UPDATE AspNetUserTokens"
                + $" SET Value = @{nameof(IdentityUserToken<string>.Value)}"
                + $" WHERE UserId = @{nameof(IdentityUserToken<string>.UserId)}"
                + $" AND LoginProvider = @{nameof(IdentityUserToken<string>.LoginProvider)}"
                + $" AND Name = @{nameof(IdentityUserToken<string>.Name)}";
            var tokensUpdateCmds = updateIdentityUserTokens
                .Where(t => t.UserId == user.Id)
                .Select(t => new CommandDefinition(tokensUpdateSql, t));

            var cmds = new List<CommandDefinition>
            {
                userCmd
            };
            cmds.AddRange(claimsInsertCmds);
            cmds.AddRange(claimsDeleteCmds);
            cmds.AddRange(claimsUpdateCmds);
            cmds.AddRange(loginsInsertCmds);
            cmds.AddRange(loginsDeleteCmds);
            cmds.AddRange(tokensInsertCmds);
            cmds.AddRange(tokensDeleteCmds);
            cmds.AddRange(tokensUpdateCmds);

            await db.ExecuteTransactionAsync(cmds, cancellationToken);

            insertIdentityUserClaims.RemoveAll(c => c.UserId == user.Id);
            deleteIdentityUserClaims.RemoveAll(c => c.UserId == user.Id);
            updateIdentityUserClaims.RemoveAll(c => c.UserId == user.Id);
            insertIdentityUserLogins.RemoveAll(l => l.UserId == user.Id);
            deleteIdentityUserLogins.RemoveAll(l => l.UserId == user.Id);
            insertIdentityUserTokens.RemoveAll(t => t.UserId == user.Id);
            deleteIdentityUserTokens.RemoveAll(t => t.UserId == user.Id);
            updateIdentityUserTokens.RemoveAll(t => t.UserId == user.Id);

            return IdentityResult.Success;
        }

        private async Task<IdentityUserToken<string>?> FindTokenAsync(IdentityUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserTokens WHERE UserId = @{nameof(user.Id)} AND LoginProvider = @{nameof(loginProvider)} AND Name = @{nameof(name)}";
            var cmd = new CommandDefinition(sql, new { user.Id, loginProvider, name }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUserToken<string>>(cmd);
        }
    }
}
