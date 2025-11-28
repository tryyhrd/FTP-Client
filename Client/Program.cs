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
    public class Program
    {
        //public static IPAddress IpAddress;
        //public static int Port;
        public static int Id = -1;
        private static Socket _socket;

        static void Main(string[] args)
        {
            //if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAddress))
            //{
            //    Console.ForegroundColor = ConsoleColor.Green;
            //    Console.WriteLine("Данные успешно введены. Запускаю сервер");

            //    if (ConnectToServer())
            //    {
            //        while (true)
            //        {
            //            try
            //            {
            //                Console.ForegroundColor = ConsoleColor.White;
            //                string message = Console.ReadLine();

            //                if (CheckCommand(message))
            //                {
            //                    SendCommand(message);
            //                }
            //            }
            //            catch
            //            {
            //                if (!ConnectToServer())
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
        }

        public static bool ConnectToServer(IPAddress IPAddress, int Port)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress, Port);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(endPoint);

                if (_socket.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Подключение к серверу установлено");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
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

        public static void SendCommand(string message)
        {
            try
            {
                if (_socket == null || !_socket.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Нет подключения к серверу");
                    return;
                }

                ViewModelSend viewModelSend = new ViewModelSend(message, Id);

                if (message.Split(new string[1] { " " }, StringSplitOptions.None)[0] == "set")
                {
                    string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

                    string NameFile = "";
                    for (int i = 1; i < DataMessage.Length; i++)
                    {
                        if (NameFile == "")
                            NameFile += DataMessage[i];
                        else
                            NameFile += " " + DataMessage[i];
                    }

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
                        return;
                    }
                }

                byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                int BytesSend = _socket.Send(messageByte);

                byte[] bytes = new byte[10485760];
                int BytesRes = _socket.Receive(bytes);
                string messageServer = Encoding.UTF8.GetString(bytes, 0, BytesRes);

                ProcessServerResponse(messageServer, viewModelSend);
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка при отправке команды: " + exp.Message);

                _socket = null;
                throw;
            }
        }

        private static void ProcessServerResponse(string messageServer, ViewModelSend originalCommand)
        {
            try
            {
                ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(messageServer);

                if (viewModelMessage.Command == "autorization")
                {
                    Id = int.Parse(viewModelMessage.Data);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Авторизация успешна. ID: {Id}");
                }
                else if (viewModelMessage.Command == "message")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(viewModelMessage.Data);
                }
                else if (viewModelMessage.Command == "cd")
                {
                    List<string> FoldresFiles = new List<string>();
                    FoldresFiles = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Содержимое директории:");
                    foreach (string Name in FoldresFiles)
                        Console.WriteLine($"  {Name}");
                }
                else if (viewModelMessage.Command == "file")
                {
                    string[] DataMessage = originalCommand.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                    string getFile = "";
                    for (int i = 1; i < DataMessage.Length; i++)
                    {
                        if (getFile == "")
                            getFile = DataMessage[i];
                        else
                            getFile += " " + DataMessage[i];
                    }

                    byte[] byteFile = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                    File.WriteAllBytes(getFile, byteFile);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Файл {getFile} успешно скачан");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка обработки ответа сервера: {ex.Message}");
            }
        }
    }
}
