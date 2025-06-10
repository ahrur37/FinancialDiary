using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FinancialDiary
{
    public partial class MainWindow : Window
    {
        private List<(string Description, string Type, string Category, DateTime Date, decimal Amount)> entries = new List<(string, string, string, DateTime, decimal)>();

        private Dictionary<string, decimal> categoryLimits = new Dictionary<string, decimal>
        {
            ["Еда"] = 1000,
            ["Транспорт"] = 500,
            ["Прочее"] = 300
        };

        private decimal financialGoal = 100000;
        private decimal currentSavings = 0;

        public MainWindow()
        {
            InitializeComponent();
            dpDate.SelectedDate = DateTime.Today;
            cmbCategory.SelectedIndex = 0;
            cmbType.SelectedIndex = 1;
            cmbSortOrder.SelectedIndex = 0;

            // Заполняем фильтр по категориям
            cmbFilterCategory.Items.Add("Все категории");
            foreach (var cat in categoryLimits.Keys)
                cmbFilterCategory.Items.Add(cat);
            cmbFilterCategory.SelectedIndex = 0;
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedType = (cmbType.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedType == "Доход")
            {
                cmbCategory.Visibility = Visibility.Collapsed;
            }
            else
            {
                cmbCategory.Visibility = Visibility.Visible;
            }
        }

        private void AddEntry_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Введите корректную сумму!");
                return;
            }

            var description = txtDescription.Text;
            var type = (cmbType.SelectedItem as ComboBoxItem)?.Content.ToString();
            var date = dpDate.SelectedDate ?? DateTime.Today;

            string category = null;
            if (type == "Расход")
            {
                var catItem = cmbCategory.SelectedItem as ComboBoxItem;
                category = catItem?.Content.ToString();
                if (string.IsNullOrWhiteSpace(category))
                {
                    MessageBox.Show("Выберите категорию для расхода.");
                    return;
                }
            }

            entries.Add((description, type, category, date, amount));

            string displayLine = $"{date.ToShortDateString()} | {type}";
            if (type == "Расход")
                displayLine += $" ({category})";
            displayLine += $" — {description} — {amount:C}";

            lstEntries.Items.Add(displayLine);

            txtDescription.Clear();
            txtAmount.Clear();


            if (type == "Расход" && category != null)
            {
                CheckLimit(category, amount);
            }
            else if (type == "Доход")
            {
                currentSavings += amount;
            }

            UpdateBalance();
            UpdateGoalProgress();
            ApplyFilters(null, null);
        }

        private void UpdateBalance()
        {
            decimal balance = 0;
            foreach (var entry in entries)
            {
                balance += entry.Type == "Доход" ? entry.Amount : -entry.Amount;
            }
            txtBalance.Text = $"Баланс: {balance:C}";
        }

        private void UpdateGoalProgress()
        {
            decimal progressPercent = Math.Min(100, currentSavings / financialGoal * 100);
            txtGoalProgress.Text = $"Цель: {currentSavings:C} / {financialGoal:C} ({progressPercent:F1}%)";
        }

        private void CheckLimit(string category, decimal amount)
        {
            if (categoryLimits.TryGetValue(category, out decimal limit))
            {
                decimal totalSpent = 0;
                foreach (var entry in entries)
                {
                    if (entry.Category == category && entry.Type == "Расход")
                        totalSpent += entry.Amount;
                }

                if (totalSpent > limit)
                {
                    txtLimitWarning.Text = $"❌ Вы превысили лимит по '{category}'!";
                }
                else if (totalSpent >= limit * 0.9m)
                {
                    txtLimitWarning.Text = $"⚠️ Вы приближаетесь к лимиту по '{category}'!";
                }
                else
                {
                    txtLimitWarning.Text = "";
                }
            }
        }

        private void ApplyFilters(object sender, EventArgs e)
        {
            lstEntries.Items.Clear();

            string selectedCategory = cmbFilterCategory.SelectedItem?.ToString() ?? "Все категории";
            bool sortByAscending = cmbSortOrder.SelectedIndex == 0;

            var filtered = new List<(string Description, string Type, string Category, DateTime Date, decimal Amount)>();

            foreach (var entry in entries)
            {
                bool matchesCategory = selectedCategory == "Все категории" || (entry.Type == "Расход" && entry.Category == selectedCategory);

                if (matchesCategory)
                    filtered.Add(entry);
            }

            if (sortByAscending)
            {
                filtered.Sort((a, b) => a.Date.CompareTo(b.Date));
            }
            else
            {
                filtered.Sort((a, b) => b.Date.CompareTo(a.Date));
            }

            foreach (var entry in filtered)
            {
                string displayLine = $"{entry.Date.ToShortDateString()} | {entry.Type}";
                if (entry.Type == "Расход")
                    displayLine += $" ({entry.Category})";
                displayLine += $" — {entry.Description} — {entry.Amount:C}";

                lstEntries.Items.Add(displayLine);
            }
        }

        private void EditLimits_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new LimitEditWindow(categoryLimits);
            if (editWindow.ShowDialog() == true)
            {       
                categoryLimits = editWindow.UpdatedLimits;
                MessageBox.Show("Лимиты успешно обновлены.");
            }
        }
    }

    public partial class LimitEditWindow : Window
    {
        public Dictionary<string, decimal> UpdatedLimits { get; private set; }

        private Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();

        public LimitEditWindow(Dictionary<string, decimal> limits)
        {
            UpdatedLimits = new Dictionary<string, decimal>(limits);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Title = "Редактировать лимиты";
            this.Width = 300;
            this.Height = 200;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var panel = new StackPanel { Margin = new Thickness(10) };

            foreach (var key in UpdatedLimits.Keys.ToList())
            {
                var stack = new StackPanel { Orientation = Orientation.Horizontal };
                var label = new TextBlock { Text = key + ":", Width = 80 };
                var txtBox = new TextBox
                {
                    Text = UpdatedLimits[key].ToString(),
                    Tag = key,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                textBoxes[key] = txtBox;

                stack.Children.Add(label);
                stack.Children.Add(txtBox);
                panel.Children.Add(stack);
            }

            var btnSave = new Button { Content = "Сохранить", Width = 100, Margin = new Thickness(0, 10, 0, 0) };
            btnSave.Click += (s, e) =>
            {
                foreach (var pair in textBoxes)
                {
                    if (decimal.TryParse(pair.Value.Text, out decimal value))
                    {
                        UpdatedLimits[pair.Key] = value;
                    }
                }
                DialogResult = true;
                Close();
            };

            panel.Children.Add(btnSave);
            this.Content = panel;
        }
    }
}