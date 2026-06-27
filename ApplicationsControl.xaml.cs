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
    public partial class ApplicationsControl : UserControl
    {
        private List<Applicationitem> applications = new List<Applicationitem>();
        private user? _currentUser;
        private bool _canManage;

        public ApplicationsControl(user? currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _canManage = currentUser != null && IsUserCanManage(currentUser);
            Loaded += ApplicationsControl_Loaded;
        }

        private bool IsUserCanManage(user user)
        {
            if (user.userroles == null) return false;

            var roleNames = user.userroles
                .Where(ur => ur.role != null)
                .Select(ur => ur.role!.rolename)
                .ToList();

            // Только Администратор и Менеджер могут управлять заявками
            return roleNames.Contains("Администратор") || roleNames.Contains("Менеджер");
        }

        private void ApplicationsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadApplications();
        }

        private async void LoadApplications()
        {
            try
            {
                using var context = new ShelterDbContext();

                var apps = await context.adoptionapplications
                    .Include(a => a.animal)
                        .ThenInclude(a => a.type)
                    .Include(a => a.user)
                    .OrderByDescending(a => a.applicationdate)
                    .ToListAsync();

                applications = apps.Select(a => new Applicationitem
                {
                    ApplicationId = a.applicationId,
                    AnimalName = a.animal?.name ?? "Не указано",
                    AnimalType = a.animal?.type?.typename ?? "",
                    UserFullName = a.user?.fullname ?? "Аноним",
                    ApplicationDate = a.applicationdate,
                    Status = TranslateStatus(a.status),
                    StatusColor = GetStatusColor(a.status),
                    Notes = a.notes ?? "",
                    DecisionDate = a.decisionDate,
                    NotesVisibility = string.IsNullOrEmpty(a.notes) ? Visibility.Collapsed : Visibility.Visible,
                    DecisionDateVisibility = a.decisionDate != null ? Visibility.Visible : Visibility.Collapsed,
                    // Кнопки управления видны только если пользователь может управлять И заявка в статусе Pending
                    ApproveButtonVisibility = (_canManage && a.status == "Pending") ? Visibility.Visible : Visibility.Collapsed,
                    RejectButtonVisibility = (_canManage && a.status == "Pending") ? Visibility.Visible : Visibility.Collapsed
                }).ToList();

                ApplicationsList.ItemsSource = applications;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обновить список
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadApplications();
        }

        // Фильтр по статусу
        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterApplications();
        }

        private void FilterApplications()
        {
            if (applications == null || ApplicationsList == null) return;

            var filtered = applications.AsEnumerable();

            if (CmbStatusFilter != null && CmbStatusFilter.SelectedItem is ComboBoxItem statusItem &&
                statusItem.Content.ToString() != "Все заявки")
            {
                filtered = filtered.Where(a => a.Status == statusItem.Content.ToString());
            }

            Dispatcher.Invoke(() =>
            {
                if (ApplicationsList != null)
                {
                    ApplicationsList.ItemsSource = filtered.ToList();
                }
            });
        }

        // Просмотр заявки
        private async void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                try
                {
                    using var context = new ShelterDbContext();

                    var application = await context.adoptionapplications
                        .Include(a => a.animal)
                        .Include(a => a.user)
                        .Include(a => a.manager)
                        .FirstOrDefaultAsync(a => a.applicationId == id);

                    if (application != null)
                    {
                        int managerId = _currentUser?.userid ?? 0;

                        var dialog = new ApplicationReviewDialog(application, _canManage, managerId);
                        dialog.Owner = Window.GetWindow(this);

                        if (dialog.ShowDialog() == true)
                        {
                            LoadApplications();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия деталей заявки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Одобрить заявку
        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Одобрить заявку?\n\nПосле одобрения животное будет забронировано.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var app = await context.adoptionapplications.FindAsync(id);
                        if (app != null)
                        {
                            app.status = "Approved";
                            app.managerid = _currentUser?.userid;
                            app.decisionDate = DateTime.UtcNow;

                            // Меняем статус животного на "Забронирован"
                            if (app.animalId > 0)
                            {
                                var animal = await context.animals.FindAsync(app.animalId);
                                if (animal != null && animal.status == "Available")
                                {
                                    animal.status = "Reserved";
                                }
                            }

                            await context.SaveChangesAsync();
                            LoadApplications();
                            MessageBox.Show("Заявка одобрена! Животное забронировано.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Отклонить заявку
        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Отклонить заявку?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var app = await context.adoptionapplications.FindAsync(id);
                        if (app != null)
                        {
                            app.status = "Rejected";
                            app.managerid = _currentUser?.userid;
                            app.decisionDate = DateTime.UtcNow;
                            await context.SaveChangesAsync();
                            LoadApplications();
                            MessageBox.Show("Заявка отклонена.", "Информация",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
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
                "Approved" => "Одобрена",
                "Rejected" => "Отклонена",
                "Completed" => "Завершена",
                _ => status
            };
        }

        private Brush GetStatusColor(string status)
        {
            return status switch
            {
                "Pending" => Brushes.Orange,
                "Approved" => Brushes.Green,
                "Rejected" => Brushes.Red,
                "Completed" => Brushes.Blue,
                _ => Brushes.Gray
            };
        }

        public class Applicationitem
        {
            public int ApplicationId { get; set; }
            public string AnimalName { get; set; } = "";
            public string AnimalType { get; set; } = "";
            public string UserFullName { get; set; } = "";
            public DateTime ApplicationDate { get; set; }
            public string Status { get; set; } = "";
            public Brush StatusColor { get; set; } = Brushes.Gray;
            public string Notes { get; set; } = "";
            public DateTime? DecisionDate { get; set; }
            public Visibility NotesVisibility { get; set; } = Visibility.Collapsed;
            public Visibility DecisionDateVisibility { get; set; } = Visibility.Collapsed;
            public Visibility ApproveButtonVisibility { get; set; } = Visibility.Collapsed;
            public Visibility RejectButtonVisibility { get; set; } = Visibility.Collapsed;
        }
    }
}