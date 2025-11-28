using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        public static IPAddress IpAddress;
        public static int Port;

        static void Main()
        {
            try
            {
                Console.WriteLine("Запуск сервера...");

                try 
                {
                    using (var db = new DataBase())
                    {
                        if (db.Database.Exists())
                        {

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("База данных подключена успешно");

                            if (!db.Users.Any())
                            {
                                var user = new User("vlad", "123", Directory.GetCurrentDirectory());
                                db.Users.Add(user);
                                db.SaveChanges();
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("База данных не подключена");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                string sIpAdress = "127.0.0.1";
                string sPort = "5000";

                if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAddress))
                {
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine("Данные успешно введены. Запускаю сервер");
                    StartServer();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }

        public static bool AutorizationUser(string login, string password)
        {
            using (var db = new DataBase())
            {
                User user = db.Users.FirstOrDefault(x => x.login == login && x.password == password);
                return user != null;
            }
        }

        public static User GetUserById(int id)
        {
            using (var db = new DataBase())
            {
                return db.Users.FirstOrDefault(u => u.Id == id);
            }
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();

            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = Path.GetFileName(dir);

                    if (!string.IsNullOrEmpty(NameDirectory))
                        FoldersFiles.Add(NameDirectory + "/");
                }

                string[] files = Directory.GetFiles(src);

                foreach (string file in files)
                {
                    string NameFile = Path.GetFileName(file);

                    if (!string.IsNullOrEmpty(NameFile))
                        FoldersFiles.Add(NameFile);
                }
            }

            return FoldersFiles;
        }

        public static void StartServer()
        {
            try
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

                        Task.Run(() => HandleClient(Handler));

                        
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Что-то случилось: " + ex.Message);
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка запуска сервера: {ex.Message}");
            }
        }

        private static void HandleClient(Socket Handler)
        {
            try
            {
                while (Handler.Connected)
                {
                    byte[] Bytes = new byte[1024 * 1024];
                    int BytesRec = Handler.Receive(Bytes);

                    if (BytesRec == 0)
                    {
                        break;
                    }

                    string Data = Encoding.UTF8.GetString(Bytes, 0, BytesRec);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Сообщение от пользователя: {Data}");

                    ViewModelSend viewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);

                    if (viewModelSend == null)
                    {
                        SendResponse(Handler, "error", "Неверный формат запроса");
                        continue;
                    }

                    string[] DataCommand = viewModelSend.Message.Split(' ');

                    if (DataCommand.Length == 0)
                    {
                        SendResponse(Handler, "error", "Пустая команда");
                        continue;
                    }

                    string command = DataCommand[0].ToLower();

                    switch (command)
                    {
                        case "connect":
                            HandleConnect(Handler, DataCommand, viewModelSend);
                            break;
                        case "cd":
                            HandleCd(Handler, DataCommand, viewModelSend);
                            break;
                        case "get":
                            HandleGet(Handler, DataCommand, viewModelSend);
                            break;
                        case "set":
                            HandleSet(Handler, viewModelSend);
                            break;
                        case "exit":
                            SendResponse(Handler, "message", "Отключение");
                            return;
                        default:
                            SendResponse(Handler, "error", $"Неизвестная команда: {command}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка обработки клиента: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (Handler.Connected)
                    {
                        Handler.Shutdown(SocketShutdown.Both);
                        Handler.Close();
                    }
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Клиент отключен");
                }
                catch { }
            }
        }

        private static void HandleConnect(Socket Handler, string[] DataCommand, ViewModelSend viewModelSend)
        {
            if (DataCommand.Length < 3)
            {
                SendResponse(Handler, "message", "Использование: connect [login] [password]");
                return;
            }

            string login = DataCommand[1];
            string password = DataCommand[2];

            if (AutorizationUser(login, password))
            {
                using (var db = new DataBase())
                {
                    User authUser = db.Users.FirstOrDefault(x => x.login == login && x.password == password);

                    if (authUser != null)
                    {
                        SendResponse(Handler, "autorization", authUser?.Id.ToString() ?? "-1");

                        var action = new UserAction
                        {
                            User = authUser,
                            Action = viewModelSend.Message,
                            Command = "connect"
                        };
                        db.Actions.Add(action);
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                SendResponse(Handler, "message", "Неверный логин или пароль");
            }
        }

        private static void HandleCd(Socket Handler, string[] DataCommand, ViewModelSend viewModelSend)
        {
            if (viewModelSend.Id == -1)
            {
                SendResponse(Handler, "message", "Необходимо авторизоваться");
                return;
            }

            using (var db = new DataBase())
            {
                User currentUser = db.Users.FirstOrDefault(u => u.Id == viewModelSend.Id);
                if (currentUser == null)
                {
                    SendResponse(Handler, "message", "Пользователь не найден");
                    return;
                }

                List<string> FoldersFiles;
                string userRoot = Path.GetFullPath(currentUser.src).TrimEnd('\\', '/');

                if (DataCommand.Length == 1)
                {
                    currentUser.temp_src = userRoot;
                    FoldersFiles = GetDirectory(currentUser.src);
                }
                else
                {
                    string cdFolder = string.Join(" ", DataCommand.Skip(1));
                    string basePath = currentUser.temp_src ?? userRoot;

                    basePath = Path.GetFullPath(basePath).TrimEnd('\\', '/');

                    string newPath;

                    if (Path.IsPathRooted(cdFolder))
                    {
                        newPath = Path.GetFullPath(cdFolder).TrimEnd('\\', '/');
                    }
                    else
                    {
                        newPath = Path.Combine(basePath, cdFolder);
                        newPath = Path.GetFullPath(newPath).TrimEnd('\\', '/');
                    }

                    if (Directory.Exists(newPath))
                    {
                        currentUser.temp_src = newPath;
                        FoldersFiles = GetDirectory(newPath);
                    }
                    else
                    {
                        SendResponse(Handler, "message", "Директория не существует");
                        return;
                    }
                }

                db.SaveChanges();

                string currentPath = currentUser.temp_src ?? userRoot;

                if (FoldersFiles.Count == 0)
                {
                    SendResponse(Handler, "message", "Директория пуста");
                }
                else
                {
                    SendResponse(Handler, "cd", JsonConvert.SerializeObject(FoldersFiles));

                    var action = new UserAction
                    {
                        User = currentUser,
                        Action = viewModelSend.Message,
                        Command = "cd"
                    };

                    db.Actions.Add(action);
                    db.SaveChanges();
                }
            }
        }

        private static void HandleGet(Socket Handler, string[] DataCommand, ViewModelSend viewModelSend)
        {
            if (viewModelSend.Id == -1)
            {
                SendResponse(Handler, "message", "Необходимо авторизоваться");
                return;
            }

            if (DataCommand.Length < 2)
            {
                SendResponse(Handler, "message", "Использование: get [filename]");
                return;
            }

            using (var db = new DataBase())
            {
                User currentUser = db.Users.FirstOrDefault(u => u.Id == viewModelSend.Id);
                if (currentUser == null)
                {
                    SendResponse(Handler, "message", "Пользователь не найден");
                    return;
                }

                string getFile = string.Join(" ", DataCommand.Skip(1));
                string filePath = Path.Combine(currentUser.temp_src ?? currentUser.src, getFile);

                try
                {
                    if (File.Exists(filePath))
                    {
                        byte[] byteFile = File.ReadAllBytes(filePath);
                        SendResponse(Handler, "file", Convert.ToBase64String(byteFile));

                        var action = new UserAction
                        {
                            User = currentUser,
                            Action = viewModelSend.Message,
                            Command = "get"
                        };
                        db.Actions.Add(action);
                        db.SaveChanges();
                    }
                    else
                    {
                        SendResponse(Handler, "message", "Файл не существует");
                    }
                }
                catch (Exception ex)
                {
                    SendResponse(Handler, "message", $"Ошибка чтения файла: {ex.Message}");
                }
            }
        }

        private static void HandleSet(Socket Handler, ViewModelSend viewModelSend)
        {
            if (viewModelSend.Id == -1)
            {
                SendResponse(Handler, "message", "Необходимо авторизоваться");
                return;
            }

            try
            {
                FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(viewModelSend.Message);
                if (SendFileInfo == null)
                {
                    SendResponse(Handler, "message", "Неверный формат файла");
                    return;
                }

                using (var db = new DataBase())
                {
                    User currentUser = db.Users.FirstOrDefault(u => u.Id == viewModelSend.Id);
                    if (currentUser == null)
                    {
                        SendResponse(Handler, "message", "Пользователь не найден");
                        return;
                    }

                    string filePath = Path.Combine(currentUser.temp_src ?? currentUser.src, SendFileInfo.Name);
                    File.WriteAllBytes(filePath, SendFileInfo.Data);
                    SendResponse(Handler, "message", "Файл загружен");

                    var action = new UserAction
                    {
                        User = currentUser,
                        Action = viewModelSend.Message,
                        Command = "set"
                    };
                    db.Actions.Add(action);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SendResponse(Handler, "message", $"Ошибка загрузки файла: {ex.Message}");
            }
        }

        private static void SendResponse(Socket Handler, string command, string data)
        {
            try
            {
                ViewModelMessage response = new ViewModelMessage(command, data);
                string reply = JsonConvert.SerializeObject(response);
                byte[] messageBytes = Encoding.UTF8.GetBytes(reply);
                Handler.Send(messageBytes);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Отправлен ответ: {command} - {data}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка отправки ответа: {ex.Message}");
            }
        }
    }
}