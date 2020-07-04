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
    public sealed class DapperUserStore : DapperUserStore<IdentityUser, string>
    {
        public DapperUserStore(IConfiguration configuration) : base(configuration)
        {
        }
    }

    /// <remarks>
    /// This implementation cannot inherit from <see cref="UserStoreBase{TUser, TKey, TUserClaim, TUserLogin, TUserToken}"/> because we cannot implement
    /// <see cref="IQueryableUserStore{TUser}"/> using Dapper.
    /// </remarks>
    public class DapperUserStore<TUser, TKey> : IUserAuthenticatorKeyStore<TUser>, IUserAuthenticationTokenStore<TUser>, IUserClaimStore<TUser>,
        IUserEmailStore<TUser>, IUserLockoutStore<TUser>, IUserLoginStore<TUser>, IUserPasswordStore<TUser>, IUserPhoneNumberStore<TUser>,
        IUserTwoFactorStore<TUser>, IUserTwoFactorRecoveryCodeStore<TUser> where TUser : IdentityUser<TKey> where TKey : class, IEquatable<TKey>
    {
        private const string InternalLoginProvider = "[AspNetUserStore]";
        private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
        private const string RecoveryCodeTokenName = "RecoveryCodes";

        private readonly SqlConnection db;

        // Mimic the behavior of the private DbSet instances in UserStore. These should probably be persisted as part of the IdentityUser class,
        // but haven't investigated the framework code to make sure it gets persisted.
        private readonly List<IdentityUserClaim<TKey>> insertIdentityUserClaims = new List<IdentityUserClaim<TKey>>();
        private readonly List<IdentityUserClaim<TKey>> deleteIdentityUserClaims = new List<IdentityUserClaim<TKey>>();
        private readonly List<IdentityUserClaim<TKey>> updateIdentityUserClaims = new List<IdentityUserClaim<TKey>>();
        private readonly List<IdentityUserLogin<TKey>> insertIdentityUserLogins = new List<IdentityUserLogin<TKey>>();
        private readonly List<IdentityUserLogin<TKey>> deleteIdentityUserLogins = new List<IdentityUserLogin<TKey>>();
        private readonly List<IdentityUserToken<TKey>> insertIdentityUserTokens = new List<IdentityUserToken<TKey>>();
        private readonly List<IdentityUserToken<TKey>> deleteIdentityUserTokens = new List<IdentityUserToken<TKey>>();
        private readonly List<IdentityUserToken<TKey>> updateIdentityUserTokens = new List<IdentityUserToken<TKey>>();
        private bool disposedValue;

        public DapperUserStore(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                var userClaim = new IdentityUserClaim<TKey>
                {
                    UserId = user.Id
                };
                userClaim.InitializeFromClaim(claim);
                insertIdentityUserClaims.Add(userClaim);
            }
            return Task.CompletedTask;
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            var userLogin = new IdentityUserLogin<TKey>
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey,
                ProviderDisplayName = login.ProviderDisplayName,
                UserId = user.Id
            };
            insertIdentityUserLogins.Add(userLogin);
            return Task.CompletedTask;
        }

        public async Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
        {
            var mergedCodes = await GetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? string.Empty;
            return string.IsNullOrWhiteSpace(mergedCodes)
                ? 0
                : mergedCodes.Split(';').Length;
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            var sql = "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp,"
                + " ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount) VALUES"
                + $" (@{nameof(user.Id)}, @{nameof(user.UserName)}, @{nameof(user.NormalizedUserName)}, @{nameof(user.Email)},"
                + $" @{nameof(user.NormalizedEmail)}, @{nameof(user.EmailConfirmed)}, @{nameof(user.PasswordHash)}, @{nameof(user.SecurityStamp)},"
                + $" @{nameof(user.ConcurrencyStamp)}, @{nameof(user.PhoneNumber)}, @{nameof(user.PhoneNumberConfirmed)}, @{nameof(user.TwoFactorEnabled)},"
                + $" @{nameof(user.LockoutEnd)}, @{nameof(user.LockoutEnabled)}, @{nameof(user.AccessFailedCount)})";
            var cmd = new CommandDefinition(sql, user, cancellationToken: cancellationToken);
            await db.ExecuteAsync(cmd);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            // The foreign keys are all set to cascade deletes, so we do not need to delete from other tables individually.
            var sql = $"DELETE FROM AspNetUsers WHERE Id = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            await db.ExecuteAsync(cmd);

            // The EF UserStore returns success even if the user didn't exist.
            return IdentityResult.Success;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE NormalizedEmail = @{nameof(normalizedEmail)}";
            var cmd = new CommandDefinition(sql, new { normalizedEmail }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<TUser>(cmd);
        }

        public async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE Id = @{nameof(userId)}";
            var cmd = new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<TUser>(cmd);
        }

        public async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var sql = "SELECT u.* FROM AspNetUserLogins ul JOIN AspNetUsers u ON u.Id = ul.UserId"
                + $" WHERE ul.LoginProvider = @{nameof(loginProvider)} AND ul.ProviderKey = @{nameof(providerKey)}";
            var cmd = new CommandDefinition(sql, new { loginProvider, providerKey }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<TUser>(cmd);
        }

        public async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUsers WHERE NormalizedUserName = @{nameof(normalizedUserName)}";
            var cmd = new CommandDefinition(sql, new { normalizedUserName }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<TUser>(cmd);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.AccessFailedCount);

        public Task<string?> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
            => GetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, cancellationToken);

        public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserClaims WHERE UserId = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserClaim<TKey>>(cmd);
            return result.Select(c => c.ToClaim()).ToList();
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

        public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnabled);

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEnd);

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserLogins WHERE UserId = @{nameof(user.Id)}";
            var cmd = new CommandDefinition(sql, new { user.Id }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserLogin<TKey>>(cmd);
            return result.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToList();
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

        public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumber);

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumberConfirmed);

        public async Task<string?> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var token = await FindTokenAsync(user, loginProvider, name, cancellationToken);
            return token?.Value;
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.TwoFactorEnabled);

        public Task<string?> GetUserIdAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id?.ToString());

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            var sql = "SELECT u.* FROM AspNetUserClaims uc JOIN AspNetUsers u ON u.Id = uc.UserId"
                + $" WHERE uc.ClaimType = @{nameof(claim.Type)} AND uc.ClaimValue= @{nameof(claim.Value)}";
            var cmd = new CommandDefinition(sql, claim, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<TUser>(cmd);
            return result.ToList();
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash != null);

        public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        public async Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
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

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                var userClaim = new IdentityUserClaim<TKey>
                {
                    UserId = user.Id
                };
                userClaim.InitializeFromClaim(claim);
                deleteIdentityUserClaims.Add(userClaim);
            }
            return Task.CompletedTask;
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            var userLogin = new IdentityUserLogin<TKey>
            {
                LoginProvider = loginProvider,
                ProviderKey = providerKey,
                UserId = user.Id
            };
            deleteIdentityUserLogins.Add(userLogin);
            return Task.CompletedTask;
        }

        public Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var userToken = new IdentityUserToken<TKey>
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name
            };
            deleteIdentityUserTokens.Add(userToken);
            return Task.CompletedTask;
        }

        public async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserClaims WHERE UserId = @{nameof(user.Id)} AND ClaimType = @{nameof(claim.Type)} AND ClaimValue = {nameof(claim.Value)}";
            var cmd = new CommandDefinition(sql, new { user.Id, claim.Type, claim.Value }, cancellationToken: cancellationToken);
            var result = await db.QueryAsync<IdentityUserClaim<TKey>>(cmd);

            foreach (var dbClaim in result)
            {
                dbClaim.InitializeFromClaim(newClaim);
                updateIdentityUserClaims.Add(dbClaim);
            }
        }

        public Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            var mergedCodes = string.Join(";", recoveryCodes);
            return SetTokenAsync(user, InternalLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
        }

        public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
            => SetTokenAsync(user, InternalLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            var token = await FindTokenAsync(user, loginProvider, name, cancellationToken);
            if (token == null)
            {
                token = new IdentityUserToken<TKey>
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

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
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
                + $" VALUES (@{nameof(IdentityUserClaim<string>.UserId)}, @{nameof(IdentityUserClaim<TKey>.ClaimType)}, @{nameof(IdentityUserClaim<TKey>.ClaimValue)})";
            var claimsInsertCmds = insertIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsInsertSql, c));

            var claimsDeleteSql = "DELETE FROM AspNetUserClaims"
                + $" WHERE UserId = @{nameof(IdentityUserClaim<TKey>.UserId)}"
                + $" AND ClaimType = @{nameof(IdentityUserClaim<TKey>.ClaimType)}"
                + $" AND ClaimValue = @{nameof(IdentityUserClaim<TKey>.ClaimValue)}";
            var claimsDeleteCmds = deleteIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsDeleteSql, c));

            var claimsUpdateSql = "UPDATE AspNetUserClaims SET"
                + $" ClaimType = @{nameof(IdentityUserClaim<TKey>.ClaimType)},"
                + $" ClaimValue = @{nameof(IdentityUserClaim<TKey>.ClaimValue)}"
                + $" WHERE Id = @{nameof(IdentityUserClaim<TKey>.Id)}";
            var claimsUpdateCmds = updateIdentityUserClaims
                .Where(c => c.UserId == user.Id)
                .Select(c => new CommandDefinition(claimsUpdateSql, c));

            var loginsInsertSql = "INSERT INTO AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)"
                + $" VALUES (@{nameof(IdentityUserLogin<TKey>.LoginProvider)}, @{nameof(IdentityUserLogin<TKey>.ProviderKey)},"
                + $" @{nameof(IdentityUserLogin<TKey>.ProviderDisplayName)}, @{nameof(IdentityUserLogin<TKey>.UserId)})";
            var loginsInsertCmds = insertIdentityUserLogins
                .Where(l => l.UserId == user.Id)
                .Select(l => new CommandDefinition(loginsInsertSql, l));

            var loginsDeleteSql = "DELETE FROM AspNetUserLogins"
                + $" WHERE LoginProvider = @{nameof(IdentityUserLogin<TKey>.LoginProvider)}"
                + $" AND ProviderKey = @{nameof(IdentityUserLogin<TKey>.ProviderKey)}"
                + $" AND UserId = @{nameof(IdentityUserLogin<TKey>.UserId)}";
            var loginsDeleteCmds = deleteIdentityUserLogins
                .Where(l => l.UserId == user.Id)
                .Select(l => new CommandDefinition(loginsDeleteSql, l));

            var tokensInsertSql = "INSERT INTO AspNetUserTokens (UserId, LoginProvider, Name, Value)"
                + $" VALUES (@{nameof(IdentityUserToken<TKey>.UserId)}, @{nameof(IdentityUserToken<TKey>.LoginProvider)},"
                + $" @{nameof(IdentityUserToken<TKey>.Name)}, @{nameof(IdentityUserToken<TKey>.Value)})";
            var tokensInsertCmds = insertIdentityUserTokens
                .Where(t => t.UserId == user.Id)
                .Select(t => new CommandDefinition(tokensInsertSql, t));

            var tokensDeleteSql = "DELETE FROM AspNetUserTokens"
                + $" WHERE UserId = @{nameof(IdentityUserToken<TKey>.UserId)}"
                + $" AND LoginProvider = @{nameof(IdentityUserToken<TKey>.LoginProvider)}"
                + $" AND Name = @{nameof(IdentityUserToken<TKey>.Name)}";
            var tokensDeleteCmds = deleteIdentityUserTokens
                .Where(t => t.UserId == user.Id)
                .Select(t => new CommandDefinition(tokensDeleteSql, t));

            var tokensUpdateSql = "UPDATE AspNetUserTokens"
                + $" SET Value = @{nameof(IdentityUserToken<TKey>.Value)}"
                + $" WHERE UserId = @{nameof(IdentityUserToken<TKey>.UserId)}"
                + $" AND LoginProvider = @{nameof(IdentityUserToken<TKey>.LoginProvider)}"
                + $" AND Name = @{nameof(IdentityUserToken<TKey>.Name)}";
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

        private async Task<IdentityUserToken<TKey>?> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM AspNetUserTokens WHERE UserId = @{nameof(user.Id)} AND LoginProvider = @{nameof(loginProvider)} AND Name = @{nameof(name)}";
            var cmd = new CommandDefinition(sql, new { user.Id, loginProvider, name }, cancellationToken: cancellationToken);
            return await db.QuerySingleOrDefaultAsync<IdentityUserToken<TKey>>(cmd);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                }

                disposedValue = true;
            }
        }
    }
}
