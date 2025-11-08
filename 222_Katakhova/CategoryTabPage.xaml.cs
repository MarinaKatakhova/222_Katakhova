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
    public partial class CategoryTabPage : Page
    {
        private List<Category> _allCategories = new List<Category>();
        private bool _isSortedAToZ = false;

        /// <summary>
        /// Инициализирует новый экземпляр страницы управления категориями
        /// </summary>
        public CategoryTabPage()
        {
            InitializeComponent();
            LoadCategories();
            this.IsVisibleChanged += Page_IsVisibleChanged;
        }

        /// <summary>
        /// Загружает список категорий из базы данных
        /// </summary>
        /// <remarks>
        /// Выполняет подключение к базе данных и получает все категории,
        /// после чего применяет поиск и сортировку к загруженным данным
        /// </remarks>
        private void LoadCategories()
        {
            try
            {
                using (var db = new KatakhovaEntities())
                {
                    _allCategories = db.Category.ToList();
                    ApplySearchAndSort();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}");
            }
        }

        private void ApplySearchAndSort()
        {
            if (DataGridCategory == null)
                return;

            try
            {
                var filteredCategories = _allCategories.AsQueryable();

                if (!string.IsNullOrEmpty(SearchTextBox?.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filteredCategories = filteredCategories.Where(c =>
                        c.Name != null && c.Name.ToLower().Contains(searchText));
                }

                if (_isSortedAToZ)
                {
                    filteredCategories = filteredCategories.OrderBy(c => c.Name);
                }
                else
                {
                    filteredCategories = filteredCategories.OrderBy(c => c.Name);
                }

                DataGridCategory.ItemsSource = filteredCategories.ToList();

                UpdateResultsInfo(filteredCategories.Count());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении поиска: {ex.Message}");
            }
        }

        private void UpdateResultsInfo(int count)
        {
            if (ResultsInfoText == null) return;

            string searchInfo = "";
            string sortInfo = _isSortedAToZ ? " (отсортировано А-Я)" : "";

            if (!string.IsNullOrEmpty(SearchTextBox?.Text))
            {
                searchInfo = $" по запросу '{SearchTextBox.Text}'";
            }

            ResultsInfoText.Text = $"Найдено категорий: {count}{searchInfo}{sortInfo}";

            if (count == 0 && (_allCategories.Count > 0))
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
                LoadCategories();
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddCategoryPage(null));
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridCategory?.SelectedItems == null) return;

            var selectedCategories = DataGridCategory.SelectedItems.Cast<Category>().ToList();

            if (selectedCategories.Count == 0)
            {
                MessageBox.Show("Выберите категории для удаления", "Внимание");
                return;
            }

            if (CheckCategoriesInUse(selectedCategories))
            {
                MessageBox.Show("Некоторые выбранные категории используются в платежах и не могут быть удалены.", "Ошибка");
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить {selectedCategories.Count} категорий?", "Внимание",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KatakhovaEntities())
                    {
                        var categoryIds = selectedCategories.Select(c => c.ID).ToList();
                        var categoriesToDelete = db.Category.Where(c => categoryIds.Contains(c.ID)).ToList();

                        if (categoriesToDelete.Count > 0)
                        {
                            db.Category.RemoveRange(categoriesToDelete);
                            db.SaveChanges();
                            MessageBox.Show("Категории успешно удалены!");
                            LoadCategories();
                        }
                        else
                        {
                            MessageBox.Show("Категории не найдены в базе данных");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}\n\nДетали: {ex.InnerException?.Message}");
                }
            }
        }

        private bool CheckCategoriesInUse(List<Category> categories)
        {
            try
            {
                using (var db = new KatakhovaEntities())
                {
                    var categoryIds = categories.Select(c => c.ID).ToList();
                    var categoriesInUse = db.Payment.Any(p => categoryIds.Contains(p.CategoryID));
                    return categoriesInUse;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            var category = (sender as Button)?.DataContext as Category;
            if (category != null)
            {
                NavigationService.Navigate(new AddCategoryPage(category));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchAndSort();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = "";
                ApplySearchAndSort();
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _isSortedAToZ = !_isSortedAToZ;

            SortButton.Content = _isSortedAToZ ? "А-Я ✓" : "А-Я";

            SortButton.ToolTip = _isSortedAToZ ? "Сортировка от А до Я активна" : "Сортировать от А до Я";

            ApplySearchAndSort();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchTextBox.Text = "";
                ApplySearchAndSort();
            }
            else if (e.Key == Key.Enter)
            {
                ApplySearchAndSort();
            }
        }
    }
}