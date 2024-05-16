using Frame.Repository.DBContexts;
using System.Collections.Generic;

namespace Frame.Repository.Databases
{
    public class DatabaseContextBuilder
    {
        private readonly List<DatabaseContextConfiguration> DatabaseContexts = new();

        internal IEnumerable<DatabaseContextConfiguration> GetDatabaseContext()
        {
            return DatabaseContexts;
        }

        public void UseDatabaseContext<TDatabaseContext>(DBConnectionStr dbconnectionStr) where TDatabaseContext : DatabaseContext, new()
        {
            DatabaseContexts.Add(new DatabaseContextConfiguration(
                typeof(TDatabaseContext),
                (provider) =>
            {
                var databaseContext = new TDatabaseContext();
                databaseContext.Initialize(provider, dbconnectionStr);
                return databaseContext;
            }));
        }
    }
}
