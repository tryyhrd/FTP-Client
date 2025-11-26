using System.ComponentModel.DataAnnotations;

namespace Common
{
    public class UserAction
    {
        [Key]
        public int Id { get; set; }
        public string Action {  get; set; }
        public string Command { get; set; }
        public virtual User User { get; set; }
        public UserAction() { }
        public UserAction(User user, string action, string command)
        {
            User = user;
            Action = action;
            Command = command;
        }
    }
}
