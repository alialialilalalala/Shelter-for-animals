using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class MyApplicationsControl : UserControl
    {
        private user _currentUser;

        public MyApplicationsControl(user currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += MyApplicationsControl_Loaded;
        }

        private async void MyApplicationsControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadApplicationsAsync();
        }

        private async System.Threading.Tasks.Task LoadApplicationsAsync()
        {
            try
            {
                using var context = new ShelterDbContext();
                var applications = await context.adoptionapplications
                    .Include(a => a.animal)
                        .ThenInclude(animal => animal != null ? animal.type : null)
                    .Include(a => a.animal)
                        .ThenInclude(animal => animal != null ? animal.breed : null)
                    .Include(a => a.manager)
                    .Where(a => a.userId == _currentUser.userid)
                    .OrderByDescending(a => a.applicationdate)
                    .ToListAsync();

                var applicationDtos = applications.Select(a => new Applicationdto
                {
                    Applicationid = a.applicationId,
                    Animalname = a.animal?.name ?? "Не указано",
                    Animaltype = a.animal?.type?.typename ?? "Не указан",
                    Applicationdate = a.applicationdate,
                    Status = a.status ?? "Не указан",
                    Statusdisplay = GetStatusDisplay(a.status ?? ""),
                    DecisionDate = a.decisionDate,
                    Managername = a.manager != null && a.manager.firstname != null && a.manager.lastname != null ?
                        $"{a.manager.lastname} {a.manager.firstname[0]}." : "Не назначен",
                    Notes = a.notes ?? ""
                }).ToList();

                dgApplications.ItemsSource = applicationDtos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        public class Applicationdto
        {
            public int Applicationid { get; set; }
            public string Animalname { get; set; } = "";
            public string Animaltype { get; set; } = "";
            public DateTime Applicationdate { get; set; }
            public string Status { get; set; } = "";
            public string Statusdisplay { get; set; } = "";
            public DateTime? DecisionDate { get; set; }
            public string Managername { get; set; } = "";
            public string Notes { get; set; } = "";
        }
    }
}