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
    public partial class UsersTabPage : Page
    {
        private List<User> _allUsers = new List<User>();
        private bool _isSortedAscending = true;

        /// <summary>
        /// Инициализирует новый экземпляр страницы управления пользователями
        /// </summary>
        public UsersTabPage()
        {
            InitializeComponent();
            LoadUsers();
            this.IsVisibleChanged += Page_IsVisibleChanged;
        }

        /// <summary>
        /// Загружает список пользователей из базы данных
        /// </summary>
        /// <remarks>
        /// Выполняет подключение к базе данных и получает всех пользователей,
        /// после чего применяет текущие фильтры и поиск к загруженным данным
        /// </remarks>
        private void LoadUsers()
        {
            try
            {
                using (var db = new KatakhovaEntities())
                {
                    _allUsers = db.User.ToList();
                    ApplyFiltersAndSearch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}");
            }
        }

        private void ApplyFiltersAndSearch()
        {
            if (DataGridUser == null)
                return;

            try
            {
                var filteredUsers = _allUsers.AsQueryable();

                if (!string.IsNullOrEmpty(SearchTextBox?.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filteredUsers = filteredUsers.Where(u =>
                        (u.Login != null && u.Login.ToLower().Contains(searchText)) ||
                        (u.FIO != null && u.FIO.ToLower().Contains(searchText)) ||
                        (u.Role != null && u.Role.ToLower().Contains(searchText)));
                }

                if (RoleFilterComboBox?.SelectedItem is ComboBoxItem selectedRoleItem && selectedRoleItem.Content?.ToString() != "Все роли")
                {
                    string selectedRole = selectedRoleItem.Content.ToString();
                    filteredUsers = filteredUsers.Where(u => u.Role == selectedRole);
                }

                if (_isSortedAscending)
                {
                    filteredUsers = filteredUsers.OrderBy(u => u.FIO);
                }
                else
                {
                    filteredUsers = filteredUsers.OrderByDescending(u => u.FIO);
                }

                DataGridUser.ItemsSource = filteredUsers.ToList();

                UpdateResultsInfo(filteredUsers.Count());
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
                searchInfo = $"по запросу '{SearchTextBox.Text}'";
            }

            if (RoleFilterComboBox?.SelectedItem is ComboBoxItem selectedRoleItem && selectedRoleItem.Content?.ToString() != "Все роли")
            {
                filterInfo = $"с ролью '{selectedRoleItem.Content}'";
            }

            string conjunction = !string.IsNullOrEmpty(searchInfo) && !string.IsNullOrEmpty(filterInfo) ? " и " : "";

            ResultsInfoText.Text = $"Найдено пользователей: {count} {searchInfo}{conjunction}{filterInfo}";

            if (count == 0 && (_allUsers.Count > 0))
            {
                ResultsInfoText.Text += " - ничего не найдено";
                ResultsInfoText.Foreground = Brushes.Red;
            }
            else
            {
                ResultsInfoText.Foreground = Brushes.Gray;
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                LoadUsers();
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddUserPage(null));
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridUser?.SelectedItems == null) return;

            var selectedUsers = DataGridUser.SelectedItems.Cast<User>().ToList();

            if (selectedUsers.Count == 0)
            {
                MessageBox.Show("Выберите пользователей для удаления", "Внимание");
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {selectedUsers.Count} пользователей?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KatakhovaEntities())
                    {
                        var userIds = selectedUsers.Select(u => u.ID).ToList();
                        var usersToDelete = db.User.Where(u => userIds.Contains(u.ID)).ToList();

                        if (usersToDelete.Count > 0)
                        {
                            db.User.RemoveRange(usersToDelete);
                            db.SaveChanges();
                            MessageBox.Show("Данные успешно удалены!");
                            LoadUsers();
                        }
                        else
                        {
                            MessageBox.Show("Пользователи не найдены в базе данных");
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
            var user = (sender as Button)?.DataContext as User;
            if (user != null)
            {
                NavigationService.Navigate(new AddUserPage(user));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = "";
                ApplyFiltersAndSearch();
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null) SearchTextBox.Text = "";
            if (RoleFilterComboBox != null) RoleFilterComboBox.SelectedIndex = 0;

            _isSortedAscending = true;

            if (SortButton != null)
                SortButton.Content = "Сортировка А-Я";

            ApplyFiltersAndSearch();
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _isSortedAscending = !_isSortedAscending;

            if (SortButton != null)
            {
                SortButton.Content = _isSortedAscending ? "Сортировка А-Я" : "Сортировка Я-А";
            }

            ApplyFiltersAndSearch();
        }
    }
}