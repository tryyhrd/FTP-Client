using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Common;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Data;

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
            public string Type { get; set; }
            public bool IsDirectory { get; set; }
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbPort.Text, out Port) && IPAddress.TryParse(tbServer.Text, out IpAddress))
            {
                if (ConnectToServer(IpAddress, Port))
                {
                    placeholder.Visibility = Visibility.Collapsed;
                    serverPanel.Visibility = Visibility.Collapsed;

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
                    SendCommand("cd");
                }
                else if (viewModelMessage.Command == "cd")
                {
                    var FoldersFile = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);

                    DisplayDirectoryContents(FoldersFile);

                    if (thisUser != null)
                    {
                        using (var db = new DataBase())
                        {
                            var updatedUser = db.Users.FirstOrDefault(x => x.Id == thisUser.Id);
                            if (updatedUser != null)
                            {
                                thisUser.temp_src = updatedUser.temp_src;

                                Dispatcher.Invoke(() =>
                                {
                                    tbCurrentPath.Text = thisUser.temp_src ?? thisUser.src;
                                });
                            }
                        }
                    }
                }
                else if (viewModelMessage.Command == "file")
                {
                    try
                    {
                        var fileTransfer = JsonConvert.DeserializeObject<FileTransfer>(viewModelMessage.Data);

                        if (fileTransfer != null && !string.IsNullOrEmpty(fileTransfer.Data))
                        {
                            var saveDialog = new Microsoft.Win32.SaveFileDialog
                            {
                                FileName = fileTransfer.FileName,
                                Filter = "All files (*.*)|*.*"
                            };

                            if (saveDialog.ShowDialog() == true)
                            {
                                byte[] fileBytes = Convert.FromBase64String(fileTransfer.Data);
                                File.WriteAllBytes(saveDialog.FileName, fileBytes);

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Файл {fileTransfer.FileName} успешно скачан как {saveDialog.FileName}");
                                MessageBox.Show($"Файл успешно скачан: {saveDialog.FileName}", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при скачивании файла: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                    fileItem.Icon = "📁";
                    fileItem.Name = item.TrimEnd('/');
                    fileItem.Type = "Папка";
                    fileItem.IsDirectory = true;
                }
                else
                {
                    fileItem.Icon = "📄";
                    fileItem.Name = item;
                    fileItem.Type = "Файл";
                    fileItem.IsDirectory = false;
                }

                FileItems.Add(fileItem);
            }

            tbItemCount.Text = $"Элементов: {contents.Count}";

            placeholder.Visibility = Visibility.Collapsed;
            listViewFiles.Visibility = Visibility.Visible;
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (thisUser == null)
                return;

            using (var db = new DataBase())
            {
                thisUser = db.Users.FirstOrDefault(x => x.Id == thisUser.Id);
            }
            
            SendCommand($"cd ..");

            tbCurrentPath.Text = thisUser.temp_src;
        }

        private void SelectFile(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var file = listViewFiles.SelectedItem as FileSystemItem;

            if (file.IsDirectory)
            {
                SendCommand($"cd {file.Name}");

                using (var db = new DataBase())
                {
                    thisUser = db.Users.FirstOrDefault(x => x.Id == thisUser.Id);
                    tbCurrentPath.Text = thisUser.temp_src ?? thisUser.src;
                }
            }
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
            var selectedItem = listViewFiles.SelectedItem as FileSystemItem;

            if (selectedItem == null)
            {
                MessageBox.Show("Выберите файл для скачивания", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedItem.IsDirectory)
            {
                MessageBox.Show("Нельзя скачать директорию. Выберите файл.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                SendCommand($"get {selectedItem.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при скачивании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "All files (*.*)|*.*",
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    string filePath = openDialog.FileName;
                    string fileName = Path.GetFileName(filePath);

                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string base64Data = Convert.ToBase64String(fileBytes);

                    var fileTransfer = new FileTransfer
                    {
                        FileName = fileName,
                        Data = base64Data
                    };

                    string fileJson = JsonConvert.SerializeObject(fileTransfer);

                    SendCommand($"set {fileJson}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
