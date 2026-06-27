using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;

namespace AnimalShelterAI
{
    public partial class ReportsControl : UserControl
    {
        public ReportsControl()
        {
            InitializeComponent();
            Loaded += ReportsControl_Loaded;
        }

        private async void ReportsControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStatistics();
        }

        private async System.Threading.Tasks.Task LoadStatistics()
        {
            try
            {
                using var context = new ShelterDbContext();

                // Основная статистика из реальной базы
                var totalAnimals = await context.animals.CountAsync();
                var availableAnimals = await context.animals
                    .Where(a => a.status == "Available")
                    .CountAsync();
                var adoptedAnimals = await context.animals
                    .Where(a => a.status == "Adopted")
                    .CountAsync();

                var totalApplications = await context.adoptionapplications.CountAsync();
                var pendingApplications = await context.adoptionapplications
                    .Where(a => a.status == "Pending")
                    .CountAsync();
                var approvedApplications = await context.adoptionapplications
                    .Where(a => a.status == "Approved")
                    .CountAsync();

                // Волонтёры (пользователи с ролью "Волонтер")
                var volunteers = await context.userroles
                    .Where(ur => ur.role != null && ur.role.rolename == "Волонтер")
                    .Select(ur => ur.user)
                    .Where(u => u != null && u.isactive == true)
                    .CountAsync();

                // Активные задачи
                var activeTasks = await context.volunteertasks
                    .Where(t => t.status == "Pending" || t.status == "InProgress")
                    .CountAsync();

                // Всего пользователей
                var totalUsers = await context.users.CountAsync();

                // Пожертвования
                var totalDonations = await context.donations.SumAsync(d => d.amount);
                var donationsCount = await context.donations.CountAsync();

                // Обновляем статистику на форме
                TotalAnimalsStat.Text = totalAnimals.ToString();
                TotalApplicationsStat.Text = totalApplications.ToString();
                TotalVolunteersStat.Text = volunteers.ToString();
                TotalUsersStat.Text = totalUsers.ToString();

                // Дополнительная статистика (если есть элементы на форме)
                // Если этих элементов нет в XAML, нужно добавить или закомментировать
                /*
                AvailableAnimalsStat.Text = availableAnimals.ToString();
                AdoptedAnimalsStat.Text = adoptedAnimals.ToString();
                PendingApplicationsStat.Text = pendingApplications.ToString();
                ApprovedApplicationsStat.Text = approvedApplications.ToString();
                ActiveTasksStat.Text = activeTasks.ToString();
                TotalDonationsStat.Text = totalDonations.ToString("C");
                DonationsCountStat.Text = donationsCount.ToString();
                */
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Тестовые данные при ошибке
                TotalAnimalsStat.Text = "0";
                TotalApplicationsStat.Text = "0";
                TotalVolunteersStat.Text = "0";
                TotalUsersStat.Text = "0";
            }
        }

        // Отчёт по животным
        private void BtnAnimalsReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AnimalReportDialog();
            dialog.ShowDialog();
        }

        private void BtnApplicationsReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ApplicationReportDialog();
            dialog.ShowDialog();
        }

        private void BtnFinancialReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DonationReportDialog();
            dialog.ShowDialog();
        }

        private void BtnVolunteersReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VolunteerReportDialog();
            dialog.ShowDialog();
        }

        // Вспомогательные методы перевода статусов
        private string TranslateAnimalStatus(string status)
        {
            return status switch
            {
                "Quarantine" => "Карантин",
                "Available" => "Ищет дом",
                "Reserved" => "Забронирован",
                "Adopted" => "Усыновлен",
                "Treatment" => "На лечении",
                _ => status
            };
        }

        private string TranslateHealthStatus(string health)
        {
            return health switch
            {
                "Healthy" => "Здоров",
                "Sick" => "Болен",
                "Recovering" => "Восстанавливается",
                "Chronic" => "Хроническое",
                _ => health ?? "Не указано"
            };
        }

        private string TranslateApplicationStatus(string status)
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
    }
}