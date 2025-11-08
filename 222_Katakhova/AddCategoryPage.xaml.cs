using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class AddCategoryPage : Page, INotifyPropertyChanged
    {
        private Category _currentCategory = new Category();
        private bool _isEditing = false;
        private string _categoryName;

        public string TitleText => _isEditing ? "Редактирование категории" : "Добавление категории";

        public string CategoryName
        {
            get { return _categoryName; }
            set
            {
                _categoryName = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Инициализирует новый экземпляр страницы добавления/редактирования категории
        /// </summary>
        /// <param name="selectedCategory">Категория для редактирования. Если null - создается новая категория</param>
        public AddCategoryPage(Category selectedCategory)
        {
            InitializeComponent();
            DataContext = this;

            if (selectedCategory != null)
            {
                _currentCategory = selectedCategory;
                _categoryName = selectedCategory.Name;
                _isEditing = true;
            }
            else
            {
                _currentCategory = new Category();
                _categoryName = "";
            }
        }

        private void ButtonSaveCategory_Click(object sender, RoutedEventArgs e)
        {
            SaveCategory();
        }

        private void SaveCategory()
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                ShowError("Укажите название категории!");
                return;
            }

            if (CategoryName.Trim().Length < 2)
            {
                ShowError("Название категории должно содержать минимум 2 символа!");
                return;
            }

            HideError();

            try
            {
                using (var db = new KatakhovaEntities())
                {
                    if (!_isEditing)
                    {
                        var existingCategory = db.Category.FirstOrDefault(c =>
                            c.Name.Trim().ToLower() == CategoryName.Trim().ToLower());
                        if (existingCategory != null)
                        {
                            ShowError("Категория с таким названием уже существует!");
                            return;
                        }
                    }
                    else
                    {
                        var existingCategory = db.Category.FirstOrDefault(c =>
                            c.Name.Trim().ToLower() == CategoryName.Trim().ToLower() &&
                            c.ID != _currentCategory.ID);
                        if (existingCategory != null)
                        {
                            ShowError("Категория с таким названием уже существует!");
                            return;
                        }
                    }

                    if (_currentCategory.ID == 0)
                    {
                        var newCategory = new Category
                        {
                            Name = CategoryName.Trim()
                        };
                        db.Category.Add(newCategory);
                    }
                    else
                    {
                        var existingCategory = db.Category.FirstOrDefault(c => c.ID == _currentCategory.ID);
                        if (existingCategory != null)
                        {
                            existingCategory.Name = CategoryName.Trim();
                        }
                        else
                        {
                            MessageBox.Show("Категория не найдена в базе данных!", "Ошибка");
                            return;
                        }
                    }

                    db.SaveChanges();
                    MessageBox.Show("Данные успешно сохранены!", "Успех");
                    NavigationService?.GoBack();
                }
            }
            catch (Exception ex)
            {
                var innerException = GetInnerException(ex);
                MessageBox.Show($"Ошибка при сохранении: {innerException}", "Ошибка");
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Visibility = Visibility.Collapsed;
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
            CategoryName = "";
            HideError();
            TBCategoryName.Focus();
        }

        private void TBCategoryName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveCategory();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}