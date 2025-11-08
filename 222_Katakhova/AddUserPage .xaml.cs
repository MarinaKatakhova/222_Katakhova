using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public partial class AddUserPage : Page
    {
        private User _currentUser = new User();
        private bool _isEditing = false;

        /// <summary>
        /// Инициализирует новый экземпляр страницы добавления/редактирования пользователя
        /// </summary>
        /// <param name="selectedUser">Пользователь для редактирования. Если null - создается новый пользователь</param>
        public AddUserPage(User selectedUser)
        {
            InitializeComponent();

            if (selectedUser != null)
            {
                _currentUser = selectedUser;
                _isEditing = true;
                ShowPhotoPreview();
            }

            DataContext = _currentUser;
        }

        private void ButtonLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = "Выберите фото пользователя";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Размер файла не должен превышать 5MB", "Ошибка");
                        return;
                    }

                    byte[] imageData = File.ReadAllBytes(openFileDialog.FileName);
                    _currentUser.Photo = imageData;

                    ShowPhotoPreview();

                    TBPhotoStatus.Text = "Фото загружено";
                    TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Green;

                    MessageBox.Show("Фото загружено успешно!", "Успех");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Нет доступа к файлу", "Ошибка доступа");
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Ошибка чтения файла: {ioEx.Message}", "Ошибка ввода-вывода");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке фото: {ex.Message}", "Ошибка");
                }
            }
        }

        private void ShowPhotoPreview()
        {
            if (_currentUser.Photo != null && _currentUser.Photo.Length > 0)
            {
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream(_currentUser.Photo))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.EndInit();
                        ImagePreview.Source = bitmapImage;
                    }
                    TBPhotoStatus.Text = "Фото загружено";
                    TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отображении фото: {ex.Message}", "Ошибка");
                    ImagePreview.Source = null;
                    TBPhotoStatus.Text = "Ошибка загрузки фото";
                    TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                ImagePreview.Source = null;
                TBPhotoStatus.Text = "Фото не загружено";
                TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_currentUser.Login))
                errors.AppendLine("Укажите логин!");
            if (string.IsNullOrWhiteSpace(_currentUser.Password))
                errors.AppendLine("Укажите пароль!");
            if (string.IsNullOrWhiteSpace(cmbRole.Text))
                errors.AppendLine("Выберите роль!");
            if (string.IsNullOrWhiteSpace(_currentUser.FIO))
                errors.AppendLine("Укажите ФИО!");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации");
                return;
            }

            string passwordValidationError = ValidatePassword(_currentUser.Password);
            if (!string.IsNullOrEmpty(passwordValidationError))
            {
                MessageBox.Show(passwordValidationError, "Ошибка валидации пароля");
                return;
            }

            try
            {
                using (var db = new KatakhovaEntities())
                {
                    if (!_isEditing)
                    {
                        var existingUser = db.User.FirstOrDefault(u => u.Login == _currentUser.Login.Trim());
                        if (existingUser != null)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка");
                            return;
                        }
                    }
                    else
                    {
                        var existingUserWithSameLogin = db.User.FirstOrDefault(u =>
                            u.Login == _currentUser.Login.Trim() && u.ID != _currentUser.ID);
                        if (existingUserWithSameLogin != null)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка");
                            return;
                        }
                    }

                    if (_currentUser.ID == 0)
                    {
                        string hashedPassword = AuthPage.GetHash(_currentUser.Password.Trim());

                        var newUser = new User
                        {
                            Login = _currentUser.Login.Trim(),
                            Password = hashedPassword,
                            Role = cmbRole.Text.Trim(),
                            FIO = _currentUser.FIO.Trim(),
                            Photo = _currentUser.Photo
                        };
                        db.User.Add(newUser);
                    }
                    else
                    {
                        var existingUser = db.User.FirstOrDefault(u => u.ID == _currentUser.ID);
                        if (existingUser != null)
                        {
                            string newPassword = _currentUser.Password.Trim();
                            string hashedNewPassword = AuthPage.GetHash(newPassword);

                            if (existingUser.Password == hashedNewPassword)
                            {
                            }
                            else
                            {
                                existingUser.Password = hashedNewPassword;
                            }

                            existingUser.Login = _currentUser.Login.Trim();
                            existingUser.Role = cmbRole.Text.Trim();
                            existingUser.FIO = _currentUser.FIO.Trim();
                            existingUser.Photo = _currentUser.Photo;
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден в базе данных!", "Ошибка");
                            return;
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Данные успешно сохранены!", "Успех");
                    NavigationService?.GoBack();
                }
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var innerException = GetInnerException(ex);
                if (innerException.Contains("UNIQUE KEY") || innerException.Contains("UQ_User"))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует в базе данных!", "Ошибка уникальности");
                }
                else
                {
                    MessageBox.Show($"Ошибка обновления базы данных: {innerException}", "Ошибка базы данных");
                }
            }
            catch (Exception ex)
            {
                var innerException = GetInnerException(ex);
                MessageBox.Show($"Ошибка при сохранении: {innerException}", "Ошибка");
            }
        }

        private string ValidatePassword(string password)
        {
            if (password.Length < 6)
                return "Пароль должен содержать 6 или более символов!";

            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]*$"))
                return "Допускается только английская раскладка!";

            if (!password.Any(char.IsDigit))
                return "Пароль должен содержать хотя бы одну цифру!";

            return null;
        }

        private string GetInnerException(Exception ex)
        {
            var message = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                message += $"\nВнутренняя ошибка: {inner.Message}";
                inner = inner.InnerException;
            }
            return message;
        }

        private void ButtonClean_Click(object sender, RoutedEventArgs e)
        {
            TBLogin.Text = "";
            TBPass.Text = "";
            cmbRole.SelectedItem = null;
            TBFio.Text = "";

            _currentUser.Photo = null;
            ImagePreview.Source = null;
            TBPhotoStatus.Text = "Фото не загружено";
            TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void ButtonRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _currentUser.Photo = null;
            ImagePreview.Source = null;
            TBPhotoStatus.Text = "Фото не загружено";
            TBPhotoStatus.Foreground = System.Windows.Media.Brushes.Gray;
            MessageBox.Show("Фото удалено", "Информация");
        }

        private void TBPass_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TBPass.Text))
            {
                string validationError = ValidatePassword(TBPass.Text);
                if (!string.IsNullOrEmpty(validationError))
                {
                    TBPass.Background = new SolidColorBrush(Color.FromArgb(30, 255, 0, 0));
                }
                else
                {
                    TBPass.Background = new SolidColorBrush(Color.FromArgb(30, 0, 255, 0));
                }
            }
            else
            {
                TBPass.Background = System.Windows.Media.Brushes.White;
            }
        }
    }
}