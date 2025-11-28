using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Common;
using Client;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Interface
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IPAddress IpAddress;
        private int Port;
        private int Id = -1;
        private Socket _socket;
        
        private bool isConnected = false;

        User thisUser = null;

        public ObservableCollection<FileSystemItem> FileItems { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            FileItems = new ObservableCollection<FileSystemItem>();
            listViewFiles.ItemsSource = FileItems;
        }

        public class FileSystemItem
        {
            public string Icon { get; set; }
            public string Name { get; set; }
            public string Size { get; set; }
            public string Type { get; set; }
            public string ModifiedDate { get; set; }
            public bool IsDirectory { get; set; }
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbPort.Text, out Port) && IPAddress.TryParse(tbServer.Text, out IpAddress))
            {
                if (ConnectToServer(IpAddress, Port))
                {
                    placeholder.Visibility = Visibility.Collapsed;
                    tbStatus.Text = "Подключено";

                    isConnected = true;
                }
                else
                {
                    MessageBox.Show("Неверный IP адрес или порт", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public bool ConnectToServer(IPAddress IPAddress, int Port)
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

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Вы не подключены к серверу");
                return;
            }

            using (var db = new DataBase())
            {
                thisUser = db.Users.FirstOrDefault(x => x.login == tbLogin.Text && x.password == tbPassword.Password);

                if (thisUser == null)
                {
                    MessageBox.Show("Пользователь не найден");
                    return;
                }
            }

            SendCommand($"connect {tbLogin.Text} {tbPassword.Password}");
            autorizationPanel.Visibility = Visibility.Collapsed;

            SendCommand($"cd");
            tbCurrentPath.Text = thisUser.src;
        }

        public void SendCommand(string message)
        {
            try
            {
                ViewModelSend viewModelSend = new ViewModelSend(message, Id);

                //if (message.Split(new string[1] { " " }, StringSplitOptions.None)[0] == "set")
                //{
                //    string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

                //    string NameFile = "";
                //    for (int i = 1; i < DataMessage.Length; i++)
                //    {
                //        if (NameFile == "")
                //            NameFile += DataMessage[i];
                //        else
                //            NameFile += " " + DataMessage[i];
                //    }

                //    if (File.Exists(NameFile))
                //    {
                //        FileInfo FileInfo = new FileInfo(NameFile);
                //        FileInfoFTP NewFileInfo = new FileInfoFTP(File.ReadAllBytes(NameFile), FileInfo.Name);
                //        viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(NewFileInfo), Id);
                //    }
                //    else
                //    {
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        Console.WriteLine("Указанный файл не существует");
                //        return;
                //    }
                //}

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
                MessageBox.Show("Ошибка при отправке команды: " + exp.Message);

                _socket = null;
                throw;
            }
        }

        private void ProcessServerResponse(string messageServer, ViewModelSend originalCommand)
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

                    DisplayDirectoryContents(FoldresFiles);

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

        private void DisplayDirectoryContents(List<string> contents)
        {
            FileItems.Clear();

            foreach (string item in contents)
            {
                var fileItem = new FileSystemItem();

                if (item.EndsWith("/"))
                {
                    // Это папка
                    fileItem.Icon = "📁";
                    fileItem.Name = item.TrimEnd('/');
                    fileItem.Size = "";
                    fileItem.Type = "Папка";
                    fileItem.ModifiedDate = "";
                    fileItem.IsDirectory = true;
                }
                else
                {
                    // Это файл
                    fileItem.Icon = "📄";
                    fileItem.Name = item;
                    fileItem.Size = "-";
                    fileItem.Type = "Файл";
                    fileItem.ModifiedDate = "";
                    fileItem.IsDirectory = false;
                }

                FileItems.Add(fileItem);
            }

            tbItemCount.Text = $"Элементов: {contents.Count}";

            placeholder.Visibility = Visibility.Collapsed;
            listViewFiles.Visibility = Visibility.Visible;
        }

        private void ListViewFiles_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        private void ListViewFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void BtnCreateFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuDownload_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
