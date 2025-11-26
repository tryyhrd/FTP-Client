namespace Common
{
    public class ViewModelMessage
    {
        public int Id { get; set; }
        public string Command {  get; set; }
        public string Data { get; set; }
        public ViewModelMessage(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }
}
