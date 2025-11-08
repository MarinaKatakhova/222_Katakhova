using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _222_Katakhova
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Инициализирует новый экземпляр главного окна приложения
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обрабатывает событие загрузки главного окна
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Данные события загрузки окна</param>
        /// <remarks>
        /// Инициализирует таймер для отображения текущего времени,
        /// устанавливает тему по умолчанию и выполняет навигацию на страницу авторизации
        /// </remarks>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.IsEnabled = true;
            timer.Tick += (o, t) => {
                DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            };
            timer.Start();

            ThemeComboBox.SelectedIndex = 0;

            MainFrame.Navigate(new AuthPage());
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите закрыть приложение?", "Подтверждение выхода",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                string theme = item.Tag.ToString();
                ApplyTheme(theme);
            }
        }

        private void ApplyTheme(string themeName)
        {
            string dictionaryName = themeName == "DictionaryDarkTheme" ? "DictionaryDarkTheme.xaml" : "Dictionary.xaml";

            var uri = new Uri(dictionaryName, UriKind.Relative);

            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;

            Application.Current.Resources.Clear();

            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }
    }
}