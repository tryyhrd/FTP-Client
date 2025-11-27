using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace Common
{
    public class DataBase: DbContext
    {
        public DbSet<UserAction> Actions { get; set; }
        public DbSet<User> Users { get; set; }

        public DataBase(): base("Server=DESKTOP-E07VVT6\\SQLEXPRESS;Database=FTPServerDB;Trusted_Connection=true;")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataBase, Configuration>());
        }

        internal sealed class Configuration : DbMigrationsConfiguration<DataBase>
        {
            public Configuration()
            {
                AutomaticMigrationsEnabled = true;
                AutomaticMigrationDataLossAllowed = true;
            }
        }

    }
}
