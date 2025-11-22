using System;
using System.Net;
using System.Net.Sockets;
using Common;

namespace Client
{
    internal class Program
    {
        public static IPAddress IpAddress;
        public static int Port;
        public static int Id = -1;

        public static bool CheckCommand(string message)
        {
            bool BCommand = false;
            string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

            if (DataMessage.Length > 0)
            {
                string Command = DataMessage[0];
                if (Command == "connect")
                {
                    if (DataMessage.Length != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password] \nПример: connect User1 P@ssw0rd");
                        BCommand = true;
                    }
                    else
                        BCommand = true;
                }
                else if (Command == "cd")
                    BCommand = true;
                else if (Command == "get")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        BCommand = true;
                    }
                    else
                        BCommand = true;
                }
                else if (Command == "set")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                        BCommand = true;
                    }
                    else
                        BCommand = true;
                }
            }
            return BCommand;
        }
        public static void ConnectServer()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);

                Socket socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                socket.Connect(endPoint);
                if (socket.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string message = Console.ReadLine();
                    if (CheckCommand(message))
                    {
                        ViewModelSend viewModeSend = new ViewModelSend(message, Id);
                        if (CheckCommand(message))
                        {
                            ViewModelSend viewModelSend = new ViewModelSend(message, Id);
                        }
                    }
                }
            }
            catch
            {

            }
        }
    }
}
