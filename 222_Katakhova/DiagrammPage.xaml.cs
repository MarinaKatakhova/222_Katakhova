using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;

namespace _222_Katakhova
{
    public partial class DiagrammPage : Page
    {
        private KatakhovaEntities context = new KatakhovaEntities();
        private List<SolidColorBrush> _modernColors;

        /// <summary>
        /// Инициализирует новый экземпляр страницы диаграмм и отчетов
        /// </summary>
        public DiagrammPage()
        {
            InitializeComponent();
            InitializeModernColors();
            LoadData();
        }

        /// <summary>
        /// Инициализирует современную цветовую палитру для диаграмм
        /// </summary>
        /// <remarks>
        /// Создает список из 10 предопределенных цветов в современном стиле,
        /// которые используются для визуализации данных на различных типах диаграмм.
        /// Каждый цвет замораживается для оптимизации производительности.
        /// </remarks>
        private void InitializeModernColors()
        {
            _modernColors = new List<SolidColorBrush>
            {
                new SolidColorBrush(Color.FromRgb(74, 124, 255)),
                new SolidColorBrush(Color.FromRgb(255, 87, 87)),
                new SolidColorBrush(Color.FromRgb(50, 215, 75)),
                new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                new SolidColorBrush(Color.FromRgb(171, 71, 255)),
                new SolidColorBrush(Color.FromRgb(255, 119, 0)),
                new SolidColorBrush(Color.FromRgb(0, 200, 200)),
                new SolidColorBrush(Color.FromRgb(255, 61, 158)),
                new SolidColorBrush(Color.FromRgb(130, 200, 255)),
                new SolidColorBrush(Color.FromRgb(160, 90, 44))
            };

            foreach (var brush in _modernColors)
            {
                brush.Freeze();
            }
        }

        /// <summary>
        /// Загружает данные пользователей и типов диаграмм для инициализации интерфейса
        /// </summary>
        /// <remarks>
        /// Получает список пользователей из базы данных и создает список доступных типов диаграмм.
        /// Устанавливает значения по умолчанию для выпадающих списков.
        /// </remarks>
        private void LoadData()
        {
            try
            {
                CmbUser.ItemsSource = context.User.ToList();

                var chartTypes = new List<string>
                {
                    "Горизонтальная столбчатая диаграмма",
                    "Круговая диаграмма",
                    "Линейная диаграмма",
                    "Точечная диаграмма"
                };
                CmbDiagram.ItemsSource = chartTypes;

                if (CmbUser.Items.Count > 0)
                    CmbUser.SelectedIndex = 0;

                if (CmbDiagram.Items.Count > 0)
                    CmbDiagram.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновляет отображение диаграммы при изменении выбора пользователя или типа диаграммы
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие</param>
        /// <param name="e">Данные события изменения выбора</param>
        /// <remarks>
        /// Вычисляет общую сумму платежей по категориям для выбранного пользователя
        /// и отображает данные в выбранном формате диаграммы
        /// </remarks>
        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUser.SelectedItem is User currentUser &&
                CmbDiagram.SelectedItem is string currentType)
            {
                try
                {
                    ChartPanel.Children.Clear();

                    var categoriesList = context.Category.ToList();
                    var paymentsData = new List<PaymentData>();
                    decimal totalSum = 0;

                    foreach (var category in categoriesList)
                    {
                        var userPayments = context.Payment
                            .Where(p => p.UserID == currentUser.ID && p.CategoryID == category.ID)
                            .ToList();

                        decimal totalAmount = 0;
                        foreach (var payment in userPayments)
                        {
                            decimal price = GetDecimalValue(payment.Price);
                            int num = GetIntValue(payment.Num);
                            totalAmount += price * num;
                        }

                        if (totalAmount > 0)
                        {
                            paymentsData.Add(new PaymentData
                            {
                                CategoryName = category.Name,
                                Amount = totalAmount
                            });
                            totalSum += totalAmount;
                        }
                    }

                    if (paymentsData.Count > 0)
                    {
                        var titleBorder = new System.Windows.Controls.Border
                        {
                            Background = new LinearGradientBrush(
                                Color.FromRgb(74, 124, 255),
                                Color.FromRgb(130, 200, 255),
                                90),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(15, 10, 15, 10),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 15)
                        };

                        var titleText = new TextBlock
                        {
                            Text = $"📊 Платежи пользователя: {currentUser.FIO}",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        titleBorder.Child = titleText;
                        ChartPanel.Children.Add(titleBorder);

                        var typeText = new TextBlock
                        {
                            Text = $"Тип отображения: {currentType}",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20),
                            FontStyle = FontStyles.Italic
                        };
                        ChartPanel.Children.Add(typeText);

                        switch (currentType)
                        {
                            case "Горизонтальная столбчатая диаграмма":
                                ShowHorizontalBarChart(paymentsData, totalSum);
                                break;
                            case "Круговая диаграмма":
                                ShowModernPieChart(paymentsData, totalSum);
                                break;
                            case "Линейная диаграмма":
                                ShowModernLineChart(paymentsData);
                                break;
                            case "Точечная диаграмма":
                                ShowModernScatterChart(paymentsData);
                                break;
                        }

                        var totalBorder = new System.Windows.Controls.Border
                        {
                            Background = new LinearGradientBrush(
                                Color.FromRgb(50, 215, 75),
                                Color.FromRgb(130, 255, 150),
                                90),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(20, 12, 20, 12),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 25, 0, 10)
                        };

                        var totalText = new TextBlock
                        {
                            Text = $"💰 Общая сумма: {totalSum:N0} руб.",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        totalBorder.Child = totalText;
                        ChartPanel.Children.Add(totalBorder);
                    }
                    else
                    {
                        ShowNoDataMessage(currentUser.FIO);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления диаграммы: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Отображает данные в виде горизонтальной столбчатой диаграммы
        /// </summary>
        /// <param name="data">Список данных о платежах по категориям</param>
        /// <param name="totalSum">Общая сумма всех платежей</param>
        /// <remarks>
        /// Создает горизонтальные столбцы, где длина каждого столбца пропорциональна
        /// сумме платежей в категории. Отображает названия категорий, суммы и проценты.
        /// </remarks>
        private void ShowHorizontalBarChart(List<PaymentData> data, decimal totalSum)
        {
            var mainContainer = new System.Windows.Controls.Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(20, 10, 20, 20),
                Padding = new Thickness(20)
            };

            var mainGrid = new Grid();
            mainContainer.Child = mainGrid;

            if (data.Count == 0) return;

            double marginLeft = 150;
            double marginRight = 50;
            double marginTop = 50;
            double chartWidth = 700;
            double chartHeight = data.Count * 60 + 100;

            var canvas = new Canvas
            {
                Width = chartWidth,
                Height = chartHeight
            };

            var backgroundRect = new System.Windows.Shapes.Rectangle
            {
                Width = chartWidth,
                Height = chartHeight,
                Fill = new LinearGradientBrush(
                    Color.FromArgb(10, 74, 124, 255),
                    Color.FromArgb(5, 130, 200, 255),
                    0)
            };
            canvas.Children.Add(backgroundRect);

            decimal maxAmount = data.Max(d => d.Amount);
            double barHeight = 40;
            double maxBarWidth = chartWidth - marginLeft - marginRight - 100;
            double verticalSpacing = 60;

            var sortedData = data.OrderByDescending(d => d.Amount).ToList();

            for (int i = 0; i < sortedData.Count; i++)
            {
                var item = sortedData[i];

                double barWidth = 0;
                if (maxAmount > 0)
                {
                    barWidth = (double)(item.Amount / maxAmount) * maxBarWidth;
                }

                double y = marginTop + (i * verticalSpacing);
                double x = marginLeft;

                var color = _modernColors[i % _modernColors.Count].Color;
                var gradient = new LinearGradientBrush(
                    Color.FromArgb(220, color.R, color.G, color.B),
                    Color.FromArgb(180, color.R, color.G, color.B),
                    0);

                var bar = new System.Windows.Controls.Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = gradient,
                    CornerRadius = new CornerRadius(0, 4, 4, 0),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    BorderThickness = new Thickness(1)
                };

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                canvas.Children.Add(bar);

                var categoryText = new TextBlock
                {
                    Text = item.CategoryName,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Width = marginLeft - 20
                };

                Canvas.SetLeft(categoryText, 10);
                Canvas.SetTop(categoryText, y + 12);
                canvas.Children.Add(categoryText);

                var rightValueText = new TextBlock
                {
                    Text = $"{item.Amount:N0} руб.",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                Canvas.SetLeft(rightValueText, x + barWidth + 10);
                Canvas.SetTop(rightValueText, y + 12);
                canvas.Children.Add(rightValueText);

                double percent = totalSum > 0 ? (double)(item.Amount / totalSum * 100) : 0;
                var percentText = new TextBlock
                {
                    Text = $"{percent:F1}%",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    FontStyle = FontStyles.Italic
                };

                Canvas.SetLeft(percentText, x + barWidth + 10);
                Canvas.SetTop(percentText, y + 28);
                canvas.Children.Add(percentText);
            }

            var xAxisTitle = new TextBlock
            {
                Text = "Сумма платежей (руб.)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Canvas.SetLeft(xAxisTitle, marginLeft + maxBarWidth / 2 - 60);
            Canvas.SetTop(xAxisTitle, chartHeight - 25);
            canvas.Children.Add(xAxisTitle);

            mainGrid.Children.Add(canvas);
            ChartPanel.Children.Add(mainContainer);
        }

        /// <summary>
        /// Отображает данные в виде круговой диаграммы с легендой
        /// </summary>
        /// <param name="data">Список данных о платежах по категориям</param>
        /// <param name="totalSum">Общая сумма всех платежей</param>
        /// <remarks>
        /// Создает круговую диаграмму, где каждый сегмент представляет категорию платежей.
        /// Размер сегмента пропорционален доле категории в общей сумме. Включает легенду с детальной информацией.
        /// </remarks>
        private void ShowModernPieChart(List<PaymentData> data, decimal totalSum)
        {
            var mainContainer = new System.Windows.Controls.Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(20, 10, 20, 20),
                Padding = new Thickness(20)
            };

            var mainStack = new StackPanel();
            mainContainer.Child = mainStack;

            if (data.Count == 0) return;

            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var pieCanvas = new Canvas
            {
                Width = 300,
                Height = 300,
                Margin = new Thickness(0, 0, 40, 0)
            };

            double currentAngle = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                double sweepAngle = 360 * (double)(item.Amount / totalSum);

                var segment = CreatePieSegment(150, 150, 120, currentAngle, sweepAngle, _modernColors[i % _modernColors.Count]);
                pieCanvas.Children.Add(segment);

                currentAngle += sweepAngle;
            }

            var centerCircle = new Ellipse
            {
                Width = 80,
                Height = 80,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(Color.FromArgb(100, 200, 200, 200)),
                StrokeThickness = 1
            };

            Canvas.SetLeft(centerCircle, 110);
            Canvas.SetTop(centerCircle, 110);
            pieCanvas.Children.Add(centerCircle);

            var centerText = new TextBlock
            {
                Text = "Итого",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };

            Canvas.SetLeft(centerText, 140);
            Canvas.SetTop(centerText, 144);
            pieCanvas.Children.Add(centerText);

            var legendStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                double percent = (double)(item.Amount / totalSum * 100);

                var legendItem = new System.Windows.Controls.Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(50, 200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 15, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var legendGrid = new Grid();
                legendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                legendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var colorIndicator = new System.Windows.Controls.Border
                {
                    Width = 16,
                    Height = 16,
                    Background = _modernColors[i % _modernColors.Count],
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 10, 0),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(colorIndicator, 0);
                legendGrid.Children.Add(colorIndicator);

                var legendTextStack = new StackPanel();

                var categoryText = new TextBlock
                {
                    Text = item.CategoryName,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                };

                var amountText = new TextBlock
                {
                    Text = $"{item.Amount:N0} руб. ({percent:F1}%)",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    Margin = new Thickness(0, 2, 0, 0)
                };

                legendTextStack.Children.Add(categoryText);
                legendTextStack.Children.Add(amountText);

                Grid.SetColumn(legendTextStack, 1);
                legendGrid.Children.Add(legendTextStack);

                legendItem.Child = legendGrid;
                legendStack.Children.Add(legendItem);
            }

            container.Children.Add(pieCanvas);
            container.Children.Add(legendStack);
            mainStack.Children.Add(container);
            ChartPanel.Children.Add(mainContainer);
        }

        private Path CreatePieSegment(double centerX, double centerY, double radius, double startAngle, double sweepAngle, Brush fill)
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();

            double startAngleRad = (startAngle - 90) * Math.PI / 180;
            double endAngleRad = (startAngle + sweepAngle - 90) * Math.PI / 180;

            System.Windows.Point startPoint = new System.Windows.Point(
                centerX + radius * Math.Cos(startAngleRad),
                centerY + radius * Math.Sin(startAngleRad));

            pathFigure.StartPoint = startPoint;

            var arcSegment = new ArcSegment
            {
                Point = new System.Windows.Point(
                    centerX + radius * Math.Cos(endAngleRad),
                    centerY + radius * Math.Sin(endAngleRad)),
                Size = new System.Windows.Size(radius, radius),
                IsLargeArc = sweepAngle > 180,
                SweepDirection = SweepDirection.Clockwise,
                RotationAngle = 0
            };

            pathFigure.Segments.Add(arcSegment);
            pathFigure.Segments.Add(new LineSegment(new System.Windows.Point(centerX, centerY), true));
            pathFigure.IsClosed = true;

            pathGeometry.Figures.Add(pathFigure);

            return new Path
            {
                Fill = fill,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Data = pathGeometry
            };
        }

        private void ShowModernLineChart(List<PaymentData> data)
        {
            var mainContainer = new System.Windows.Controls.Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(20, 10, 20, 20),
                Padding = new Thickness(20)
            };

            var canvas = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = new LinearGradientBrush(
                    Color.FromArgb(10, 74, 124, 255),
                    Color.FromArgb(5, 130, 200, 255),
                    45)
            };

            if (data.Count > 0)
            {
                var xAxis = new System.Windows.Shapes.Line
                {
                    X1 = 50,
                    Y1 = 350,
                    X2 = 550,
                    Y2 = 350,
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    StrokeThickness = 2
                };

                var yAxis = new System.Windows.Shapes.Line
                {
                    X1 = 50,
                    Y1 = 350,
                    X2 = 50,
                    Y2 = 50,
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    StrokeThickness = 2
                };

                canvas.Children.Add(xAxis);
                canvas.Children.Add(yAxis);

                decimal maxValue = data.Max(d => d.Amount);
                double xStep = 500.0 / Math.Max(1, data.Count - 1);
                double yScale = 300.0 / (double)Math.Max(1, maxValue);

                var polyline = new Polyline
                {
                    Stroke = new LinearGradientBrush(
                        Color.FromRgb(74, 124, 255),
                        Color.FromRgb(130, 200, 255),
                        0),
                    StrokeThickness = 3,
                    StrokeLineJoin = PenLineJoin.Round
                };

                var points = new PointCollection();

                for (int i = 0; i < data.Count; i++)
                {
                    double x = 50 + i * xStep;
                    double y = 350 - (double)data[i].Amount * yScale;
                    points.Add(new System.Windows.Point(x, y));

                    var point = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = Brushes.White,
                        Stroke = new SolidColorBrush(Color.FromRgb(74, 124, 255)),
                        StrokeThickness = 2,
                        ToolTip = $"{data[i].CategoryName}: {data[i].Amount:N0} руб."
                    };

                    Canvas.SetLeft(point, x - 5);
                    Canvas.SetTop(point, y - 5);
                    canvas.Children.Add(point);

                    var categoryText = new TextBlock
                    {
                        Text = ShortenCategoryName(data[i].CategoryName, 8),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        TextAlignment = TextAlignment.Center
                    };

                    Canvas.SetLeft(categoryText, x - 25);
                    Canvas.SetTop(categoryText, 360);
                    canvas.Children.Add(categoryText);

                    if (i == 0 || i == data.Count - 1 || data[i].Amount == maxValue)
                    {
                        var valueText = new TextBlock
                        {
                            Text = data[i].Amount.ToString("N0"),
                            FontSize = 9,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            Background = Brushes.White
                        };

                        Canvas.SetLeft(valueText, x - 15);
                        Canvas.SetTop(valueText, y - 20);
                        canvas.Children.Add(valueText);
                    }
                }

                polyline.Points = points;
                canvas.Children.Add(polyline);

                var yAxisTitle = new TextBlock
                {
                    Text = "Сумма (руб.)",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    RenderTransform = new RotateTransform(-90)
                };
                Canvas.SetLeft(yAxisTitle, 10);
                Canvas.SetTop(yAxisTitle, 200);
                canvas.Children.Add(yAxisTitle);

                var xAxisTitle = new TextBlock
                {
                    Text = "Категории",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80))
                };
                Canvas.SetLeft(xAxisTitle, 300);
                Canvas.SetTop(xAxisTitle, 380);
                canvas.Children.Add(xAxisTitle);
            }

            mainContainer.Child = canvas;
            ChartPanel.Children.Add(mainContainer);
        }

        private void ShowModernScatterChart(List<PaymentData> data)
        {
            var mainContainer = new System.Windows.Controls.Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(20, 10, 20, 20),
                Padding = new Thickness(20)
            };

            var canvas = new Canvas
            {
                Width = 600,
                Height = 400,
                Background = new LinearGradientBrush(
                    Color.FromArgb(10, 255, 87, 87),
                    Color.FromArgb(5, 255, 193, 7),
                    45)
            };

            if (data.Count > 0)
            {
                var xAxis = new System.Windows.Shapes.Line
                {
                    X1 = 50,
                    Y1 = 350,
                    X2 = 550,
                    Y2 = 350,
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    StrokeThickness = 2
                };

                var yAxis = new System.Windows.Shapes.Line
                {
                    X1 = 50,
                    Y1 = 350,
                    X2 = 50,
                    Y2 = 50,
                    Stroke = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    StrokeThickness = 2
                };

                canvas.Children.Add(xAxis);
                canvas.Children.Add(yAxis);

                decimal maxValue = data.Max(d => d.Amount);
                double xStep = 500.0 / Math.Max(1, data.Count - 1);
                double yScale = 300.0 / (double)Math.Max(1, maxValue);

                for (int i = 0; i < data.Count; i++)
                {
                    double x = 50 + i * xStep;
                    double y = 350 - (double)data[i].Amount * yScale;

                    double pointSize = 8 + (double)(data[i].Amount / maxValue) * 12;

                    var point = new Ellipse
                    {
                        Width = pointSize,
                        Height = pointSize,
                        Fill = _modernColors[i % _modernColors.Count],
                        Stroke = Brushes.White,
                        StrokeThickness = 2,
                        ToolTip = CreateToolTip(data[i].CategoryName, data[i].Amount)
                    };

                    Canvas.SetLeft(point, x - pointSize / 2);
                    Canvas.SetTop(point, y - pointSize / 2);
                    canvas.Children.Add(point);

                    var categoryText = new TextBlock
                    {
                        Text = ShortenCategoryName(data[i].CategoryName, 8),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        TextAlignment = TextAlignment.Center
                    };

                    Canvas.SetLeft(categoryText, x - 25);
                    Canvas.SetTop(categoryText, 360);
                    canvas.Children.Add(categoryText);

                    if (i == 0 || i == data.Count - 1 || data[i].Amount == maxValue || data[i].Amount == data.Min(d => d.Amount))
                    {
                        var valueText = new TextBlock
                        {
                            Text = data[i].Amount.ToString("N0"),
                            FontSize = 9,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            Background = Brushes.White
                        };

                        Canvas.SetLeft(valueText, x - 15);
                        Canvas.SetTop(valueText, y - 25);
                        canvas.Children.Add(valueText);
                    }
                }

                var yAxisTitle = new TextBlock
                {
                    Text = "Сумма (руб.)",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    RenderTransform = new RotateTransform(-90)
                };
                Canvas.SetLeft(yAxisTitle, 10);
                Canvas.SetTop(yAxisTitle, 200);
                canvas.Children.Add(yAxisTitle);

                var xAxisTitle = new TextBlock
                {
                    Text = "Категории",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80))
                };
                Canvas.SetLeft(xAxisTitle, 300);
                Canvas.SetTop(xAxisTitle, 380);
                canvas.Children.Add(xAxisTitle);
            }

            mainContainer.Child = canvas;
            ChartPanel.Children.Add(mainContainer);
        }

        private void ShowNoDataMessage(string userName)
        {
            var noDataContainer = new System.Windows.Controls.Border
            {
                Background = new LinearGradientBrush(
                    Color.FromArgb(20, 100, 100, 100),
                    Color.FromArgb(10, 200, 200, 200),
                    45),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromArgb(50, 150, 150, 150)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(40, 60, 40, 60),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 40, 20, 20)
            };

            var noDataStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = "📊",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var messageText = new TextBlock
            {
                Text = $"У пользователя {userName} нет платежей",
                FontSize = 16,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            noDataStack.Children.Add(iconText);
            noDataStack.Children.Add(messageText);
            noDataContainer.Child = noDataStack;

            ChartPanel.Children.Add(noDataContainer);
        }

        private string ShortenCategoryName(string name, int maxLength = 12)
        {
            if (name.Length > maxLength)
            {
                return name.Substring(0, maxLength - 3) + "...";
            }
            return name;
        }

        private ToolTip CreateToolTip(string category, decimal amount)
        {
            var toolTip = new ToolTip();
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = category,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"{amount:N0} руб.",
                FontSize = 11
            });

            toolTip.Content = stack;
            return toolTip;
        }

        private decimal GetDecimalValue(decimal? value)
        {
            return value.HasValue ? value.Value : 0m;
        }

        private int GetIntValue(int? value)
        {
            return value.HasValue ? value.Value : 1;
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportToExcel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Excel: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportToWord();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта в Word: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel()
        {
            var allUsers = context.User.ToList();
            var allCategories = context.Category.ToList();

            var excelApp = new Excel.Application();
            Excel.Workbook workbook = excelApp.Workbooks.Add();
            Excel.Worksheet worksheet = workbook.ActiveSheet;

            try
            {
                int currentRow = 1;

                worksheet.Cells[currentRow, 1] = "Отчет по платежам пользователей";
                Excel.Range titleRange = worksheet.Range[worksheet.Cells[currentRow, 1], worksheet.Cells[currentRow, 3]];
                titleRange.Merge();
                titleRange.Font.Bold = true;
                titleRange.Font.Size = 16;
                titleRange.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                currentRow += 2;

                worksheet.Cells[currentRow, 1] = $"Дата создания: {DateTime.Now:dd.MM.yyyy HH:mm}";
                currentRow += 2;

                foreach (var user in allUsers)
                {
                    worksheet.Cells[currentRow, 1] = user.FIO;
                    Excel.Range userRange = worksheet.Range[worksheet.Cells[currentRow, 1], worksheet.Cells[currentRow, 3]];
                    userRange.Merge();
                    userRange.Font.Bold = true;
                    userRange.Font.Size = 14;
                    userRange.Interior.Color = Excel.XlRgbColor.rgbLightGray;
                    currentRow++;

                    worksheet.Cells[currentRow, 1] = "Категория";
                    worksheet.Cells[currentRow, 2] = "Сумма расходов (руб.)";
                    worksheet.Cells[currentRow, 3] = "Процент от общей суммы";

                    Excel.Range headerRange = worksheet.Range[worksheet.Cells[currentRow, 1], worksheet.Cells[currentRow, 3]];
                    headerRange.Font.Bold = true;
                    headerRange.Interior.Color = Excel.XlRgbColor.rgbLightBlue;
                    currentRow++;

                    decimal userTotal = 0;

                    foreach (var category in allCategories)
                    {
                        var userPayments = context.Payment
                            .Where(p => p.UserID == user.ID && p.CategoryID == category.ID)
                            .ToList();

                        decimal totalAmount = 0;
                        foreach (var payment in userPayments)
                        {
                            decimal price = GetDecimalValue(payment.Price);
                            int num = GetIntValue(payment.Num);
                            totalAmount += price * num;
                        }
                        userTotal += totalAmount;
                    }

                    foreach (var category in allCategories)
                    {
                        var userPayments = context.Payment
                            .Where(p => p.UserID == user.ID && p.CategoryID == category.ID)
                            .ToList();

                        decimal totalAmount = 0;
                        foreach (var payment in userPayments)
                        {
                            decimal price = GetDecimalValue(payment.Price);
                            int num = GetIntValue(payment.Num);
                            totalAmount += price * num;
                        }

                        if (totalAmount > 0)
                        {
                            worksheet.Cells[currentRow, 1] = category.Name;
                            worksheet.Cells[currentRow, 2] = totalAmount;

                            if (userTotal > 0)
                            {
                                double percent = (double)(totalAmount / userTotal * 100);
                                worksheet.Cells[currentRow, 3] = $"{percent:F1}%";
                            }
                            else
                            {
                                worksheet.Cells[currentRow, 3] = "0%";
                            }

                            currentRow++;
                        }
                    }

                    worksheet.Cells[currentRow, 1] = "ВСЕГО:";
                    worksheet.Cells[currentRow, 2] = userTotal;
                    Excel.Range totalRange = worksheet.Range[worksheet.Cells[currentRow, 1], worksheet.Cells[currentRow, 3]];
                    totalRange.Font.Bold = true;
                    totalRange.Interior.Color = Excel.XlRgbColor.rgbLightYellow;
                    currentRow += 2;

                    var allUserPayments = context.Payment.Where(p => p.UserID == user.ID).ToList();

                    if (allUserPayments.Count > 0)
                    {
                        var maxPayment = allUserPayments
                            .OrderByDescending(p => GetDecimalValue(p.Price) * GetIntValue(p.Num))
                            .FirstOrDefault();

                        if (maxPayment != null)
                        {
                            worksheet.Cells[currentRow, 1] = "Самый дорогостоящий платеж:";
                            worksheet.Cells[currentRow, 2] = $"{maxPayment.Name} - {GetDecimalValue(maxPayment.Price) * GetIntValue(maxPayment.Num):N2} руб. (дата: {maxPayment.Date:dd.MM.yyyy})";
                            currentRow++;
                        }

                        var minPayment = allUserPayments
                            .Where(p => GetDecimalValue(p.Price) * GetIntValue(p.Num) > 0)
                            .OrderBy(p => GetDecimalValue(p.Price) * GetIntValue(p.Num))
                            .FirstOrDefault();

                        if (minPayment != null)
                        {
                            worksheet.Cells[currentRow, 1] = "Самый дешевый платеж:";
                            worksheet.Cells[currentRow, 2] = $"{minPayment.Name} - {GetDecimalValue(minPayment.Price) * GetIntValue(minPayment.Num):N2} руб. (дата: {minPayment.Date:dd.MM.yyyy})";
                            currentRow++;
                        }
                    }

                    currentRow += 2;
                }

                worksheet.Columns.AutoFit();
                excelApp.Visible = true;

                MessageBox.Show("Экспорт в Excel завершен успешно!", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                workbook.Close(false);
                excelApp.Quit();
            }
        }

        private void ExportToWord()
        {
            var allUsers = context.User.ToList();
            var allCategories = context.Category.ToList();

            var wordApp = new Word.Application();
            Word.Document document = wordApp.Documents.Add();

            try
            {
                foreach (var user in allUsers)
                {
                    Word.Paragraph userParagraph = document.Paragraphs.Add();
                    Word.Range userRange = userParagraph.Range;
                    userRange.Text = user.FIO;

                    try
                    {
                        userParagraph.set_Style("Заголовок 1");
                    }
                    catch
                    {
                        try
                        {
                            userParagraph.set_Style("Heading 1");
                        }
                        catch
                        {
                            userRange.Font.Size = 16;
                            userRange.Font.Bold = 1;
                        }
                    }

                    userRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    userRange.InsertParagraphAfter();
                    document.Paragraphs.Add();

                    Word.Paragraph tableParagraph = document.Paragraphs.Add();
                    Word.Range tableRange = tableParagraph.Range;
                    Word.Table paymentsTable = document.Tables.Add(tableRange, allCategories.Count + 1, 2);

                    paymentsTable.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    paymentsTable.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    paymentsTable.Range.Cells.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                    Word.Range cellRange;
                    cellRange = paymentsTable.Cell(1, 1).Range;
                    cellRange.Text = "Категория";
                    cellRange = paymentsTable.Cell(1, 2).Range;
                    cellRange.Text = "Сумма расходов (руб.)";

                    paymentsTable.Rows[1].Range.Font.Name = "Times New Roman";
                    paymentsTable.Rows[1].Range.Font.Size = 14;
                    paymentsTable.Rows[1].Range.Bold = 1;
                    paymentsTable.Rows[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;

                    for (int i = 0; i < allCategories.Count; i++)
                    {
                        var currentCategory = allCategories[i];
                        cellRange = paymentsTable.Cell(i + 2, 1).Range;
                        cellRange.Text = currentCategory.Name;
                        cellRange.Font.Name = "Times New Roman";
                        cellRange.Font.Size = 12;

                        cellRange = paymentsTable.Cell(i + 2, 2).Range;

                        var userPayments = context.Payment
                            .Where(p => p.UserID == user.ID && p.CategoryID == currentCategory.ID)
                            .ToList();

                        decimal totalAmount = 0;
                        foreach (var payment in userPayments)
                        {
                            decimal price = GetDecimalValue(payment.Price);
                            int num = GetIntValue(payment.Num);
                            totalAmount += price * num;
                        }

                        cellRange.Text = totalAmount.ToString("N2") + " руб.";
                        cellRange.Font.Name = "Times New Roman";
                        cellRange.Font.Size = 12;
                    }

                    document.Paragraphs.Add();

                    var allUserPayments = context.Payment.Where(p => p.UserID == user.ID).ToList();

                    if (allUserPayments.Count > 0)
                    {
                        var maxPayment = allUserPayments
                            .OrderByDescending(p => GetDecimalValue(p.Price) * GetIntValue(p.Num))
                            .FirstOrDefault();

                        if (maxPayment != null)
                        {
                            Word.Paragraph maxParagraph = document.Paragraphs.Add();
                            Word.Range maxRange = maxParagraph.Range;
                            decimal maxAmount = GetDecimalValue(maxPayment.Price) * GetIntValue(maxPayment.Num);
                            maxRange.Text = $"Самый дорогостоящий платеж: {maxPayment.Name} - {maxAmount:N2} руб. (дата: {maxPayment.Date:dd.MM.yyyy})";
                            maxRange.Font.Color = Word.WdColor.wdColorDarkRed;
                            maxRange.Font.Bold = 1;
                            maxRange.InsertParagraphAfter();
                        }

                        var minPayment = allUserPayments
                            .Where(p => GetDecimalValue(p.Price) * GetIntValue(p.Num) > 0)
                            .OrderBy(p => GetDecimalValue(p.Price) * GetIntValue(p.Num))
                            .FirstOrDefault();

                        if (minPayment != null)
                        {
                            Word.Paragraph minParagraph = document.Paragraphs.Add();
                            Word.Range minRange = minParagraph.Range;
                            decimal minAmount = GetDecimalValue(minPayment.Price) * GetIntValue(minPayment.Num);
                            minRange.Text = $"Самый дешевый платеж: {minPayment.Name} - {minAmount:N2} руб. (дата: {minPayment.Date:dd.MM.yyyy})";
                            minRange.Font.Color = Word.WdColor.wdColorDarkGreen;
                            minRange.Font.Bold = 1;
                            minRange.InsertParagraphAfter();
                        }
                    }

                    if (user != allUsers.Last())
                    {
                        document.Paragraphs.Add();
                        document.Words.Last.InsertBreak(Word.WdBreakType.wdPageBreak);
                    }
                }

                wordApp.Visible = true;

                MessageBox.Show("Экспорт в Word завершен успешно!", "Экспорт",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                wordApp.Quit();
            }
        }
    }

    public class PaymentData
    {
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
    }
}