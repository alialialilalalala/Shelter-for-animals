using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnimalShelterAI
{
    public partial class TasksControl : UserControl
    {
        private List<Taskitem> tasks = new List<Taskitem>();
        private user? _currentUser;
        private bool _canManage;

        public TasksControl(user? currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _canManage = currentUser != null && IsUserCanManage(currentUser);
            Loaded += TasksControl_Loaded;
        }

        private bool IsUserCanManage(user user)
        {
            if (user.userroles == null) return false;

            var roleNames = user.userroles
                .Where(ur => ur.role != null)
                .Select(ur => ur.role!.rolename)
                .ToList();

            return roleNames.Contains("Администратор") ||
                   roleNames.Contains("Менеджер") ||
                   roleNames.Contains("Ветеринар");
        }

        private void TasksControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTasks();
            if (CmbStatusFilter != null) CmbStatusFilter.SelectedIndex = 0;
            FilterTasks();
        }

        private async void LoadTasks()
        {
            try
            {
                using var context = new ShelterDbContext();

                var dbTasks = await context.volunteertasks
                    .Include(t => t.volunteer)
                    .Include(t => t.animal)
                    .OrderByDescending(t => t.assigneddate)
                    .ToListAsync();

                tasks = dbTasks.Select(t => new Taskitem
                {
                    Taskid = t.taskid,
                    Title = t.title ?? "Без названия",
                    Description = t.description ?? "",
                    Volunteername = t.volunteer?.fullname ?? "Не назначен",
                    Animalname = t.animal?.name ?? "",
                    Duedate = t.duedate ?? DateTime.Now.AddDays(7),
                    Status = TranslateStatus(t.status ?? "Pending"),
                    Statuscolor = GetStatusColor(t.status ?? "Pending"),
                    Descriptionvisibility = string.IsNullOrEmpty(t.description) ? Visibility.Collapsed : Visibility.Visible,
                    CanCompleteVisibility = (t.status == "Pending" || t.status == "InProgress") ? Visibility.Visible : Visibility.Collapsed,
                    CanRejectVisibility = (t.status == "Pending" || t.status == "InProgress") ? Visibility.Visible : Visibility.Collapsed
                }).ToList();

                TasksList.ItemsSource = tasks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            if (tasks == null || TasksList == null) return;

            var filtered = tasks.AsEnumerable();

            if (CmbStatusFilter != null && CmbStatusFilter.SelectedItem is ComboBoxItem statusItem &&
                statusItem.Content.ToString() != "Все задачи")
            {
                var statusText = statusItem.Content.ToString();
                filtered = filtered.Where(t => t.Status == statusText);
            }

            Dispatcher.Invoke(() =>
            {
                if (TasksList != null)
                {
                    TasksList.ItemsSource = filtered.ToList();
                }
            });
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTasks();
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TaskEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadTasks();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTasks();
        }

        private async void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Отметить задачу как выполненную?", "Выполнение задачи",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var task = await context.volunteertasks.FindAsync(id);
                        if (task != null)
                        {
                            task.status = "Completed";
                            task.completeddate = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                            LoadTasks();
                            MessageBox.Show("Задача успешно выполнена!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Отклонить задачу?", "Отклонение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var task = await context.volunteertasks.FindAsync(id);
                        if (task != null)
                        {
                            task.status = "Rejected";
                            await context.SaveChangesAsync();
                            LoadTasks();
                            MessageBox.Show("Задача отклонена.", "Информация");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                try
                {
                    using var context = new ShelterDbContext();
                    var task = await context.volunteertasks.FindAsync(id);
                    if (task != null)
                    {
                        var dialog = new TaskEditDialog(task);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadTasks();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить задачу?", "Удаление задачи",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var task = await context.volunteertasks.FindAsync(id);
                        if (task != null)
                        {
                            context.volunteertasks.Remove(task);
                            await context.SaveChangesAsync();
                            LoadTasks();
                            MessageBox.Show("Задача успешно удалена.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private string TranslateStatus(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает",
                "InProgress" => "В работе",
                "Completed" => "Выполнена",
                "Rejected" => "Отклонена",
                _ => "Неизвестно"
            };
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "Pending" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "InProgress" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                "Completed" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                "Rejected" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                _ => Brushes.Gray
            };
        }

        public class Taskitem
        {
            public int Taskid { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Volunteername { get; set; } = "";
            public string Animalname { get; set; } = "";
            public DateTime Duedate { get; set; }
            public string Status { get; set; } = "";
            public Brush Statuscolor { get; set; } = Brushes.Gray;
            public Visibility Descriptionvisibility { get; set; } = Visibility.Collapsed;
            public Visibility CanCompleteVisibility { get; set; } = Visibility.Visible;
            public Visibility CanRejectVisibility { get; set; } = Visibility.Visible;

            public Visibility AnimalVisibility =>
                !string.IsNullOrEmpty(Animalname) && Animalname != "—" ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}