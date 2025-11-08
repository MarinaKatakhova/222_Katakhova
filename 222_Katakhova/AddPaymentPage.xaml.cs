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
    public partial class AddPaymentPage : Page
    {
        private Payment _currentPayment = new Payment();

        /// <summary>
        /// Инициализирует новый экземпляр страницы добавления/редактирования платежа
        /// </summary>
        /// <param name="selectedPayment">Платеж для редактирования. Если null - создается новый платеж</param>
        public AddPaymentPage(Payment selectedPayment)
        {
            InitializeComponent();

            LoadComboBoxData();

            if (selectedPayment != null)
            {
                _currentPayment = selectedPayment;
                CBUser.SelectedValue = _currentPayment.UserID;
                CBCategory.SelectedValue = _currentPayment.CategoryID;
            }
            else
            {
                _currentPayment.Date = DateTime.Now;
                TBDate.Text = _currentPayment.Date.ToString("dd.MM.yyyy");
            }

            DataContext = _currentPayment;
        }

        private void LoadComboBoxData()
        {
            try
            {
                using (var db = new KatakhovaEntities())
                {
                    CBCategory.ItemsSource = db.Category.ToList();
                    CBCategory.DisplayMemberPath = "Name";
                    CBCategory.SelectedValuePath = "ID";

                    CBUser.ItemsSource = db.User.ToList();
                    CBUser.DisplayMemberPath = "FIO";
                    CBUser.SelectedValuePath = "ID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка");
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(_currentPayment.Name))
                errors.AppendLine("Укажите название платежа!");

            if (string.IsNullOrWhiteSpace(TBAmount.Text) || !decimal.TryParse(TBAmount.Text, out decimal price) || price <= 0)
                errors.AppendLine("Укажите корректную сумму!");
            else
                _currentPayment.Price = price;

            if (string.IsNullOrWhiteSpace(TBCount.Text) || !int.TryParse(TBCount.Text, out int num) || num <= 0)
                errors.AppendLine("Укажите корректное количество!");
            else
                _currentPayment.Num = num;

            if (string.IsNullOrWhiteSpace(TBDate.Text) || !DateTime.TryParse(TBDate.Text, out DateTime date))
                errors.AppendLine("Укажите корректную дату в формате ДД.ММ.ГГГГ!");
            else
                _currentPayment.Date = date;

            if (CBUser.SelectedValue == null)
                errors.AppendLine("Выберите пользователя!");
            else
                _currentPayment.UserID = (int)CBUser.SelectedValue;

            if (CBCategory.SelectedValue == null)
                errors.AppendLine("Выберите категорию!");
            else
                _currentPayment.CategoryID = (int)CBCategory.SelectedValue;

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации");
                return;
            }

            try
            {
                using (var db = new KatakhovaEntities())
                {
                    if (_currentPayment.ID == 0)
                    {
                        var newPayment = new Payment
                        {
                            Name = _currentPayment.Name.Trim(),
                            Price = _currentPayment.Price,
                            Num = _currentPayment.Num,
                            Date = _currentPayment.Date,
                            UserID = _currentPayment.UserID,
                            CategoryID = _currentPayment.CategoryID
                        };
                        db.Payment.Add(newPayment);
                    }
                    else
                    {
                        var existingPayment = db.Payment.FirstOrDefault(p => p.ID == _currentPayment.ID);
                        if (existingPayment != null)
                        {
                            existingPayment.Name = _currentPayment.Name.Trim();
                            existingPayment.Price = _currentPayment.Price;
                            existingPayment.Num = _currentPayment.Num;
                            existingPayment.Date = _currentPayment.Date;
                            existingPayment.UserID = _currentPayment.UserID;
                            existingPayment.CategoryID = _currentPayment.CategoryID;
                        }
                        else
                        {
                            MessageBox.Show("Платеж не найден в базе данных!", "Ошибка");
                            return;
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Данные успешно сохранены!", "Успех");
                    NavigationService?.GoBack();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                MessageBox.Show($"Ошибки валидации: {fullErrorMessage}", "Ошибка валидации");
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                var innerException = GetInnerException(ex);
                MessageBox.Show($"Ошибка обновления базы данных: {innerException}", "Ошибка базы данных");
            }
            catch (Exception ex)
            {
                var innerException = GetInnerException(ex);
                MessageBox.Show($"Ошибка при сохранении: {innerException}", "Ошибка");
            }
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
            TBPaymentName.Text = "";
            TBAmount.Text = "";
            TBCount.Text = "";
            TBDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            CBUser.SelectedItem = null;
            CBCategory.SelectedItem = null;
        }
    }
}