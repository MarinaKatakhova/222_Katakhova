using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class RegPage : Page
    {
        /// <summary>
        /// Инициализирует новый экземпляр страницы регистрации
        /// </summary>
        public RegPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обрабатывает регистрацию нового пользователя
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Данные события нажатия кнопки</param>
        /// <remarks>
        /// Выполняет проверку введенных данных, валидацию пароля,
        /// проверку уникальности логина и сохранение нового пользователя в базе данных
        /// </remarks>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LoginBox.Text) ||
                string.IsNullOrEmpty(FIOBox.Text) ||
                string.IsNullOrEmpty(PasswordBox1.Password) ||
                string.IsNullOrEmpty(PasswordBox2.Password))
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            using (var db = new KatakhovaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.Login == LoginBox.Text);
                if (user != null)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!");
                    return;
                }

                if (PasswordBox1.Password.Length < 6)
                {
                    MessageBox.Show("Пароль слишком короткий!");
                    return;
                }

                bool en = true;
                bool number = false;

                foreach (char c in PasswordBox1.Password)
                {
                    if (c >= '0' && c <= '9') number = true;
                    else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) en = false;
                }

                if (!en)
                {
                    MessageBox.Show("Используйте только английскую раскладку!");
                    return;
                }
                else if (!number)
                {
                    MessageBox.Show("Добавьте хотябы одну цифру!");
                    return;
                }

                if (PasswordBox1.Password != PasswordBox2.Password)
                {
                    MessageBox.Show("Пароли не совпадают!");
                    return;
                }

                string hashedPassword = GetHash(PasswordBox1.Password);
                string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "User";

                User newUser = new User
                {
                    FIO = FIOBox.Text,
                    Login = LoginBox.Text,
                    Password = hashedPassword,
                    Role = role
                };

                db.User.Add(newUser);
                db.SaveChanges();

                MessageBox.Show("Пользователь успешно зарегистрирован!");
                NavigationService?.Navigate(new AuthPage());
            }
        }

        public static string GetHash(string password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password)).Select(x => x.ToString("X2")));
            }
        }
    }
}