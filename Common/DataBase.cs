using System.Data.Entity;

namespace Common
{
    public class DataBase: DbContext
    {
        public DbSet<ViewModelSend> Sends { get; set; }
        public DbSet<User> Users { get; set; }

        public DataBase(): base("Server=10.0.201.112;Database=base1_ISP_22_4_12;User=ISP_22_4_12;Password=7m4tIyDMeybp_;")
        {
            Database.CreateIfNotExists();
        }

    }
}
