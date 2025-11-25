using System.Data.Entity;

namespace Common
{
    public class DataBase: DbContext
    {
        public DbSet<ViewModelSend> Sends { get; set; }
        public DbSet<User> Users { get; set; }

        public DataBase(): base("Server=localhost;Database=test;User=;Password=;")
        {
            Database.CreateIfNotExists();
        }

    }
}
