using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;

namespace Jarvis.State
{
    public class ASAChatDataContext : DbContext
    {
        
        public ASAChatDataContext(string connectionString)
            : base(connectionString)
        {
            Database.SetInitializer<ASAChatDataContext>(null);
        }

        /// <summary>
        /// Throw if the database or SqlBotDataEntities table have not been created.
        /// </summary>
        internal static void AssertDatabaseReady()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["ASAChatDataContextConnectionString"]
                .ConnectionString;
            using (var context = new ASAChatDataContext(connectionString))
            {
                if (!context.Database.Exists())
                    throw new ArgumentException("The sql database defined in the connection has not been created.");

                if (context.Database.SqlQuery<int>(@"IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLog') 
                                                                    SELECT 1
                                                                ELSE
                                                                    SELECT 0").SingleOrDefault() != 1)
                    throw new ArgumentException("The UserLog table has not been created in the database.");
            }
        }

        public DbSet<ASAChatDataEntity> BotData { get; set; }

        public DbSet<ASAChatUserLogEntity> LogActivity { get; set; }
    }
}