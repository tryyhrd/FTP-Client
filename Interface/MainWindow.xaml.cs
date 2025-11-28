using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Common;
using Client;

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
                if (Program.ConnectToServer(IpAddress, Port))
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

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("Вы не подключены к серверу");
                return;
            }

            Program.SendCommand($"connect {tbLogin.Text} {tbPassword.Password}");
            autorizationPanel.Visibility = Visibility.Collapsed;
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
