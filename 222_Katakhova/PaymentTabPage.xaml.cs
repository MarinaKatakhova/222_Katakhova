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
    public partial class PaymentTabPage : Page
    {
        private List<Payment> _allPayments = new List<Payment>();
        private List<User> _allUsers = new List<User>();
        private List<Category> _allCategories = new List<Category>();
        private bool _isDateSortedAscending = false;

        /// <summary>
        /// Инициализирует новый экземпляр страницы управления платежами
        /// </summary>
        public PaymentTabPage()
        {
            InitializeComponent();
            LoadData();
            this.IsVisibleChanged += Page_IsVisibleChanged;
        }

        /// <summary>
        /// Загружает данные о платежах, пользователях и категориях из базы данных
        /// </summary>
        /// <remarks>
        /// Выполняет загрузку всех платежей с включенными связанными данными пользователей и категорий,
        /// а также загружает отдельные списки пользователей и категорий для фильтрации
        /// </remarks>
        private void LoadData()
        {
            try
            {
                using (var db = new KatakhovaEntities())
                {
                    _allPayments = db.Payment.Include("User").Include("Category").ToList();
                    _allUsers = db.User.ToList();
                    _allCategories = db.Category.ToList();

                    LoadFilterComboBoxes();
                    ApplyFiltersAndSearch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка");
            }
        }

        private void LoadFilterComboBoxes()
        {
            UserFilterComboBox.Items.Clear();
            UserFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все пользователи" });
            foreach (var user in _allUsers)
            {
                UserFilterComboBox.Items.Add(user);
            }
            UserFilterComboBox.SelectedIndex = 0;

            CategoryFilterComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все категории" });
            foreach (var category in _allCategories)
            {
                CategoryFilterComboBox.Items.Add(category);
            }
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void ApplyFiltersAndSearch()
        {
            if (DataGridPayment == null) return;

            try
            {
                var filteredPayments = _allPayments.AsQueryable();

                if (!string.IsNullOrEmpty(SearchTextBox?.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filteredPayments = filteredPayments.Where(p =>
                        (p.Name != null && p.Name.ToLower().Contains(searchText)) ||
                        (p.User.FIO != null && p.User.FIO.ToLower().Contains(searchText)) ||
                        (p.Category.Name != null && p.Category.Name.ToLower().Contains(searchText)));
                }

                if (UserFilterComboBox.SelectedItem is User selectedUser)
                {
                    filteredPayments = filteredPayments.Where(p => p.User.ID == selectedUser.ID);
                }

                if (CategoryFilterComboBox.SelectedItem is Category selectedCategory)
                {
                    filteredPayments = filteredPayments.Where(p => p.Category.ID == selectedCategory.ID);
                }

                if (decimal.TryParse(MinAmountTextBox.Text, out decimal minAmount))
                {
                    filteredPayments = filteredPayments.Where(p => p.Price >= minAmount);
                }

                if (decimal.TryParse(MaxAmountTextBox.Text, out decimal maxAmount) && maxAmount > 0)
                {
                    filteredPayments = filteredPayments.Where(p => p.Price <= maxAmount);
                }

                filteredPayments = _isDateSortedAscending ?
                    filteredPayments.OrderBy(p => p.Date) :
                    filteredPayments.OrderByDescending(p => p.Date);

                DataGridPayment.ItemsSource = filteredPayments.ToList();

                UpdateResultsInfo(filteredPayments.Count());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтров: {ex.Message}");
            }
        }

        private void UpdateResultsInfo(int count)
        {
            if (ResultsInfoText == null) return;

            string searchInfo = "";
            string filterInfo = "";

            if (!string.IsNullOrEmpty(SearchTextBox?.Text))
            {
                searchInfo = $" по запросу '{SearchTextBox.Text}'";
            }

            List<string> filters = new List<string>();

            if (UserFilterComboBox.SelectedItem is User)
                filters.Add("пользователю");

            if (CategoryFilterComboBox.SelectedItem is Category)
                filters.Add("категории");

            if (decimal.TryParse(MinAmountTextBox.Text, out _) || decimal.TryParse(MaxAmountTextBox.Text, out _))
                filters.Add("сумме");

            if (filters.Count > 0)
                filterInfo = $" с фильтром по {string.Join(", ", filters)}";

            string conjunction = !string.IsNullOrEmpty(searchInfo) && !string.IsNullOrEmpty(filterInfo) ? " и " : "";

            ResultsInfoText.Text = $"Найдено платежей: {count}{searchInfo}{conjunction}{filterInfo}";

            if (count == 0 && _allPayments.Count > 0)
            {
                ResultsInfoText.Text += " - ничего не найдено";
                ResultsInfoText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                ResultsInfoText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void Page_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == System.Windows.Visibility.Visible)
            {
                LoadData();
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddPaymentPage(null));
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            var selectedPayments = DataGridPayment.SelectedItems.Cast<Payment>().ToList();

            if (selectedPayments.Count == 0)
            {
                MessageBox.Show("Выберите платежи для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {selectedPayments.Count} платежей?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KatakhovaEntities())
                    {
                        var paymentIds = selectedPayments.Select(p => p.ID).ToList();
                        var paymentsToDelete = db.Payment.Where(p => paymentIds.Contains(p.ID)).ToList();

                        if (paymentsToDelete.Count > 0)
                        {
                            db.Payment.RemoveRange(paymentsToDelete);
                            db.SaveChanges();
                            MessageBox.Show("Платежи успешно удалены!");
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}\n\nДетали: {ex.InnerException?.Message}");
                }
            }
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            var payment = (sender as Button)?.DataContext as Payment;
            if (payment != null)
            {
                NavigationService.Navigate(new AddPaymentPage(payment));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void UserFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void AmountFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            ApplyFiltersAndSearch();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            UserFilterComboBox.SelectedIndex = 0;
            CategoryFilterComboBox.SelectedIndex = 0;
            MinAmountTextBox.Text = "";
            MaxAmountTextBox.Text = "";
            _isDateSortedAscending = false;
            SortButton.Content = "Сортировка по дате ▼";
            ApplyFiltersAndSearch();
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _isDateSortedAscending = !_isDateSortedAscending;
            SortButton.Content = _isDateSortedAscending ? "Сортировка по дате ▲" : "Сортировка по дате ▼";
            ApplyFiltersAndSearch();
        }
    }
}