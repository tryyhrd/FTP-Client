using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using Newtonsoft.Json;

namespace Server
{
    public class Program
    {
        public static IPAddress IpAddress;
        public static int Port;

        public static DataBase context = new DataBase();

        static void Main()
        {
            context = new DataBase();

            var users = context.Users;

            User user = new User("vlad", "123", Directory.GetCurrentDirectory());

            if (!users.Any(x => x.login == user.login && x.password == user.password))
            {
                users.Add(user);
                context.SaveChanges();
            }

            string sIpAdress = "127.0.0.1";
            string sPort = "5000";

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAddress))
            {
                Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine("Данные успешно введены. Запускаю сервер");
                StartServer();
            }

            Console.Read();
        }

        public static bool AutorizationUser(string login, string password)
        {
            User user = context.Users.FirstOrDefault(x => x.login == login && x.password == password);

            return user != null;
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();

            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = dir.Replace(src, "");
                    FoldersFiles.Add(NameDirectory + "/");
                }

                string[] files = Directory.GetFiles(src);

                foreach (string file in files)
                {
                    string NameFile = file.Replace(src, "");
                    FoldersFiles.Add(NameFile);
                }
            }

            return FoldersFiles;
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);

            Socket sListener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            sListener.Bind(endPoint);
            sListener.Listen(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен");

            while (true)
            {
                try
                {
                    Socket Handler = sListener.Accept();

                    byte[] Bytes = new byte[10485760];
                    int BytesRec = Handler.Receive(Bytes);
                    string Data = Encoding.UTF8.GetString(Bytes, 0, BytesRec);

                    Console.Write("Сообщение от пользователя: " + Data + "\n");

                    string Reply = "";

                    ViewModelMessage viewModelMessage;
                    ViewModelSend viewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);

                    if (viewModelSend == null) continue;

                    string[] DataCommand = viewModelSend.Message.Split(' ');

                    if (DataCommand[0] == "connect")
                    {
                        if (AutorizationUser(DataCommand[1], DataCommand[2]))
                        {
                            User authUser = context.Users.FirstOrDefault(x => x.login == DataCommand[1] && x.password == DataCommand[2]);
                            viewModelMessage = new ViewModelMessage("autorization", authUser?.Id.ToString() ?? "-1");

                            var action = new UserAction(authUser, viewModelSend.Message, viewModelMessage.Command);
                            context.Actions.Add(action);
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Неверный логин или пароль");
                        }

                        Reply = JsonConvert.SerializeObject(viewModelMessage);

                        byte[] message = Encoding.UTF8.GetBytes(Reply);
                        Handler.Send(message);
                    }
                    else if (DataCommand[0] == "cd")
                    {
                        if (viewModelSend.Id != -1)
                        {
                            string[] DataMessage = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);

                            List<string> FoldersFiles = new List<string>();

                            if (DataMessage.Length == 1)
                            {
                                User currentUser = context.Users.FirstOrDefault(u => u.Id == viewModelSend.Id);
                                currentUser.temp_src = currentUser.src;
                                FoldersFiles = GetDirectory(currentUser.src);
                            }
                            else
                            {
                                string cdFolder = "";

                                for (int i = 1; i < DataMessage.Length; i++)
                                {
                                    if (cdFolder == "")
                                        cdFolder += DataMessage[i];
                                    else
                                        cdFolder += " " + DataMessage[i];

                                    User currentUser = context.Users.FirstOrDefault(u => u.Id == viewModelSend.Id);
                                    currentUser.temp_src = currentUser.temp_src + cdFolder;
                                    FoldersFiles = GetDirectory(currentUser.temp_src);
                                }
                            }

                            if (FoldersFiles.Count == 0)
                                viewModelMessage = new ViewModelMessage("message", "Директория пуста или не существует");
                            else
                            {
                                viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));

                                var action = new UserAction(
                                    context.Users.FirstOrDefault(u => u.Id == viewModelSend.Id),
                                    viewModelSend.Message,
                                    viewModelMessage.Command);
                                context.Actions.Add(action);
                            }
                        }
                        else
                        {
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                        }

                        Reply = JsonConvert.SerializeObject(viewModelMessage);
                        byte[] message = Encoding.UTF8.GetBytes(Reply);
                        Handler.Send(message);
                    }
                    else if (DataCommand[0] == "get")
                    {
                        if (viewModelSend.Id == -1)
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");

                        else
                        {
                            string getFile = "";

                            for (int i = 1; i < DataCommand.Length; i++)
                            {
                                if (getFile == "")
                                    getFile += DataCommand[i];
                                else
                                    getFile += " " + DataCommand[i];
                            }

                            byte[] byteFile = File.ReadAllBytes(context.Users.FirstOrDefault(u => u.Id == viewModelSend.Id).temp_src + @"\" + getFile);
                            viewModelMessage = new ViewModelMessage("file", Convert.ToBase64String(byteFile));
                        }

                        byte[] Response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelMessage));
                        Handler.Send(Response);
                    }
                    else
                    {
                        if (viewModelSend.Id != -1)
                        {
                            FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(viewModelSend.Message);
                            File.WriteAllBytes(context.Users.FirstOrDefault(u => u.Id == viewModelSend.Id).temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);
                            viewModelMessage = new ViewModelMessage("message", "Файл загружен");
                        }
                        else
                            viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");

                        Reply = JsonConvert.SerializeObject(viewModelMessage);
                        byte[] message = Encoding.UTF8.GetBytes(Reply);
                        Handler.Send(message);
                    }

                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то случилось: " + ex.Message);
                }
            }
        }
    }
}