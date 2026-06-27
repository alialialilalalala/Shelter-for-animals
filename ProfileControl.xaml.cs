using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AnimalShelterAI
{
    public partial class ProfileControl : UserControl
    {
        private user _currentUser;

        public ProfileControl(user currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += ProfileControl_Loaded;
        }

        private async void ProfileControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProfileData();
                await LoadApplications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}\n{ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfileData()
        {
            try
            {
                txtFullName.Text = _currentUser.fullname;
                txtEmail.Text = _currentUser.email;
                txtPhone.Text = _currentUser.phone ?? "Не указан";
                txtRole.Text = GetUserRole();
                txtRegDate.Text = _currentUser.registrationdate.ToString("dd.MM.yyyy");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных профиля: {ex.Message}");
            }
        }

        private string GetUserRole()
        {
            var role = _currentUser.userroles?.FirstOrDefault()?.role?.rolename;
            return role switch
            {
                "Administrator" => "Администратор",
                "Manager" => "Менеджер",
                "Veterinarian" => "Ветеринар",
                "Volunteer" => "Волонтёр",
                "User" => "Пользователь",
                "Employee" => "Сотрудник",
                _ => "Пользователь"
            };
        }

        private async Task LoadApplications()
        {
            using var context = new ShelterDbContext();
            var apps = await context.adoptionapplications
                .Include(a => a.animal)
                    .ThenInclude(a => a.type)
                .Where(a => a.userId == _currentUser.userid)
                .OrderByDescending(a => a.applicationdate)
                .ToListAsync();

            if (apps.Any())
            {
                var appItems = apps.Select(a => new
                {
                    a.applicationId,
                    AnimalName = a.animal?.name ?? "Неизвестно",
                    AnimalType = a.animal?.type?.typename ?? "Не указан",
                    PhotoUrl = a.animal?.photourl,
                    HasPhoto = !string.IsNullOrEmpty(a.animal?.photourl) && File.Exists(a.animal?.photourl),
                    ApplicationDate = a.applicationdate,
                    Status = GetStatusDisplay(a.status),
                    StatusColor = GetStatusBrush(a.status),
                    DecisionDate = a.decisionDate,
                    DecisionDateVisibility = a.decisionDate.HasValue ? Visibility.Visible : Visibility.Collapsed,
                    CanCancel = a.status == "Pending"
                }).ToList();

                ApplicationsList.ItemsSource = appItems;
                txtNoApplications.Visibility = Visibility.Collapsed;
            }
            else
            {
                ApplicationsList.ItemsSource = null;
                txtNoApplications.Visibility = Visibility.Visible;
            }
        }

        private Brush GetStatusBrush(string status)
        {
            return status switch
            {
                "Pending" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Оранжевый
                "Approved" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),  // Зелёный
                "Rejected" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Красный
                "Completed" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Синий
                _ => Brushes.Gray
            };
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "На рассмотрении",
                "Approved" => "Одобрена",
                "Rejected" => "Отклонена",
                "Completed" => "Завершена",
                _ => status
            };
        }

        private void BtnEditProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog(_currentUser);
            if (dialog.ShowDialog() == true)
            {
                using var context = new ShelterDbContext();
                var updatedUser = context.users
                    .Include(u => u.userroles)
                        .ThenInclude(ur => ur.role)
                    .FirstOrDefault(u => u.userid == _currentUser.userid);
                if (updatedUser != null)
                {
                    _currentUser = updatedUser;
                    LoadProfileData();
                    _ = LoadApplications();
                    MessageBox.Show("Профиль обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        
    }
}