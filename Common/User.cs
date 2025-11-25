using System.ComponentModel.DataAnnotations;

namespace Common
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string src { get; set; }
        public string temp_src { get; set; }
        public User() { }
        public User(string login, string password, string src)
        {
            this.login = login;
            this.password = password;
            this.src = src;

            temp_src = src;
        }
    }
}
