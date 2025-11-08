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
    public partial class ChangePassPage : Page
    {
        /// <summary>
        /// Инициализирует новый экземпляр страницы изменения пароля
        /// </summary>
        public ChangePassPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обрабатывает изменение пароля пользователя
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Данные события нажатия кнопки</param>
        /// <remarks>
        /// Выполняет проверку введенных данных, валидацию нового пароля
        /// и обновление пароля в базе данных при успешной проверке
        /// </remarks>
        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
                string.IsNullOrEmpty(NewPasswordBox.Password) ||
                string.IsNullOrEmpty(ConfirmPasswordBox.Password) ||
                string.IsNullOrEmpty(TbLogin.Text))
            {
                MessageBox.Show("Все поля обязательны к заполнению!");
                return;
            }

            string hashedPass = AuthPage.GetHash(CurrentPasswordBox.Password);

            using (var db = new KatakhovaEntities())
            {
                var user = db.User.FirstOrDefault(u => u.Login == TbLogin.Text && u.Password == hashedPass);

                if (user == null)
                {
                    MessageBox.Show("Текущий пароль/Логин неверный!");
                    return;
                }

                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Пароль слишком короткий, должно быть минимум 6 символов!");
                    return;
                }

                bool en = true;
                bool number = false;

                foreach (char c in NewPasswordBox.Password)
                {
                    if (c >= '0' && c <= '9')
                        number = true;
                    else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
                        en = false;
                }

                if (!en)
                {
                    MessageBox.Show("Используйте только английскую раскладку!");
                    return;
                }
                else if (!number)
                {
                    MessageBox.Show("Добавьте хотя бы одну цифру!");
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Пароли не совпадают!");
                    return;
                }

                user.Password = AuthPage.GetHash(NewPasswordBox.Password);
                db.SaveChanges();

                MessageBox.Show("Пароль успешно изменен!");
                NavigationService?.Navigate(new AuthPage());
            }
        }
    }
}