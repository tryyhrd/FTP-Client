using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using Newtonsoft.Json;

namespace Client
{
    internal class Program
    {
        public static IPAddress IpAddress;
        public static int Port;
        public static int Id = -1;

        static void Main(string[] args)
        {
            Console.Write("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();

            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAddress))
            {
                Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine("Данные успешно введены. Запускаю сервер");
                while (true)
                {
                    ConnectServer();
                }
            }
        }

        public static bool CheckCommand(string message)
        {
            bool BCommand = false;
            string[] DataCommand = message.Split(' ');

            if (DataCommand.Length > 0)
            {
                if (DataCommand[0] == "connect")
                {
                    if (DataCommand.Length == 3) BCommand = true;
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password] \nПример: connect User1 P@ssw0rd");
                    }
                }
                else if (DataCommand[0] == "cd")
                    BCommand = true;
                else if (DataCommand[0] == "get")
                {
                    if (DataCommand.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        BCommand = true;
                    }
                    else
                        BCommand = true;
                }
                else if (DataCommand[0] == "set")
                {
                    if (DataCommand.Length == 1)
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
                        ViewModelSend viewModelSend = new ViewModelSend(message, Id);
                        if (message.Split(new string[1] { " " }, StringSplitOptions.None)[0] == "set")
                        {
                            string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

                            string NameFile = "";
                            for (int i = 1; i < DataMessage.Length; i++)
                                if (NameFile == "")
                                    NameFile += DataMessage[i];
                                else
                                    NameFile += DataMessage[i];
                            if (File.Exists(NameFile))
                            {
                                FileInfo FileInfo = new FileInfo(NameFile);
                                FileInfoFTP NewFileInfo = new FileInfoFTP(File.ReadAllBytes(NameFile), FileInfo.Name);
                                viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(NewFileInfo), Id);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Указанный файл не существует");
                            }
                        }
                        byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                        int BytesSend = socket.Send(messageByte);
                        byte[] bytes = new byte[10485760];
                        int BytesRes = socket.Receive(bytes);
                        string messageServer = Encoding.UTF8.GetString(bytes, 0, BytesRes);
                        ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(messageServer);
                        if (viewModelMessage.Command == "autorization")
                            Id = int.Parse(viewModelMessage.Data);
                        else if (viewModelMessage.Command == "message")
                            Console.WriteLine(viewModelMessage.Data);
                        else if (viewModelMessage.Command == "cd")
                        {
                            List<string> FoldresFiles = new List<string>();
                            FoldresFiles = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);
                            foreach (string Name in FoldresFiles)
                                Console.WriteLine(Name);
                        }
                        else if (viewModelMessage.Command == "file")
                        {
                            string[] DataMessage = viewModelSend.Message.Split(new string[1] {" "}, StringSplitOptions.None);
                            string getFile = "";
                            for (int i = 1; i < DataMessage.Length; i++)
                                if (getFile == "")
                                    getFile = DataMessage[i];
                                else
                                    getFile += " " + DataMessage[i];
                            byte[] byteFile = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                            File.WriteAllBytes(getFile, byteFile);
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Подключение не удалось.");
                }
                socket.Close();
            }
            catch (Exception exp)
            {
                Console.ForegroundColor= ConsoleColor.Red;
                Console.WriteLine("Что-то случилось: " + exp.Message);
            }
        }

    }
}
