using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Fulgoribus.Luxae.Dapper
{
    public static class DapperExtensions
    {
        public static async Task ExecuteTransactionAsync(this IDbConnection db, IEnumerable<CommandDefinition> commands, CancellationToken cancellationToken = default)
        {
            if (commands.Any())
            {
                var wasClosed = db.State == ConnectionState.Closed;
                if (wasClosed)
                {
                    db.Open();
                }
                try
                {
                    using (var tx = db.BeginTransaction())
                    {
                        try
                        {
                            // Copy the commands to new ones with our transaction.
                            var cmds = commands.Select(c => new CommandDefinition(c.CommandText, c.Parameters, tx, c.CommandTimeout, c.CommandType,
                                c.Flags, cancellationToken));
                            await Task.WhenAll(cmds.Select(c => db.ExecuteAsync(c)));
                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            // An error occurred. Try to roll back the transaction.
                            try
                            {
                                tx.Rollback();
                            }
                            catch (Exception rex)
                            {
                                // We failed to roll back. Return both of these.
                                throw new AggregateException("An error occurred during rollback.", ex, rex);
                            }

                            // We rolled back successfully. Let the original exception bubble up.
                            throw;
                        }
                    }
                }
                finally
                {
                    if (wasClosed)
                    {
                        db.Close();
                    }
                }
            }
        }

        public static async Task ExecuteTransactionAsync(this IDbConnection db, string sql, IEnumerable<object> parameters, CancellationToken cancellationToken = default)
        {
            if (!parameters.Any())
            {
                // Just run the SQL as-is using an implicit transaction.
                await db.ExecuteAsync(sql);
            }
            else
            {
                var wasClosed = db.State == ConnectionState.Closed;
                if (wasClosed)
                {
                    db.Open();
                }
                try
                {
                    using (var tx = db.BeginTransaction())
                    {
                        try
                        {
                            var cmds = parameters.Select(p => new CommandDefinition(sql, p, tx, cancellationToken: cancellationToken));
                            await Task.WhenAll(cmds.Select(c => db.ExecuteAsync(c)));
                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            // An error occurred. Try to roll back the transaction.
                            try
                            {
                                tx.Rollback();
                            }
                            catch (Exception rex)
                            {
                                // We failed to roll back. Return both of these.
                                throw new AggregateException("An error occurred during rollback.", ex, rex);
                            }

                            // We rolled back successfully. Let the original exception bubble up.
                            throw;
                        }
                    }
                }
                finally
                {
                    if (wasClosed)
                    {
                        db.Close();
                    }
                }
            }
        }
    }
}
