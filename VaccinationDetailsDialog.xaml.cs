using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class VaccinationDetailsDialog : Window
    {
        private readonly int _vaccinationId;

        public VaccinationDetailsDialog(int vaccinationId)
        {
            InitializeComponent();
            _vaccinationId = vaccinationId;
            Loaded += VaccinationDetailsDialog_Loaded;
        }

        private async void VaccinationDetailsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                using var context = new ShelterDbContext();
                var vacc = await context.vaccinations
                    .Include(v => v.animal)
                    .Include(v => v.vet)
                    .FirstOrDefaultAsync(v => v.vaccinationid == _vaccinationId);

                if (vacc == null)
                {
                    MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                txtSubtitle.Text = $"№{vacc.vaccinationid} от {vacc.vaccinationdate:dd.MM.yyyy}";
                txtVaccineName.Text = vacc.vaccinename ?? "Не указано";
                txtAnimalName.Text = vacc.animal?.name ?? "Не указано";
                txtVetName.Text = vacc.vet?.fullname ?? "Не указан";
                txtVaccinationDate.Text = vacc.vaccinationdate.ToString("dd.MM.yyyy");
                txtNextVaccinationDate.Text = vacc.nextvaccinationdate?.ToString("dd.MM.yyyy") ?? "Не назначена";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}