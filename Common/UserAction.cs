using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common
{
    public class UserAction
    {
        [Key]
        public int Id { get; set; }
        public string Action {  get; set; }
        public string Command { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        public UserAction() { }
        public UserAction(User user, string action, string command)
        {
            User = user;
            UserId = user.Id;
            Action = action;
            Command = command;
        }
    }
}
