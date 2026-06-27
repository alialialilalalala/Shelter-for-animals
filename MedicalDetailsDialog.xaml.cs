using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class MedicalDetailsDialog : Window
    {
        private readonly medicalrecord _record;
        private readonly bool _canEdit;

        public MedicalDetailsDialog(medicalrecord record, bool canEdit = false)
        {
            InitializeComponent();
            _record = record;
            _canEdit = canEdit;

            Loaded += MedicalDetailsDialog_Loaded;
        }

        private async void MedicalDetailsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRecordData();
            ConfigureButtons();
        }

        private async System.Threading.Tasks.Task LoadRecordData()
        {
            using var context = new ShelterDbContext();

            var record = await context.medicalrecords
                .Include(m => m.animal)
                    .ThenInclude(a => a.type)
                .Include(m => m.animal)
                    .ThenInclude(a => a.breed)
                .Include(m => m.vet)
                .FirstOrDefaultAsync(m => m.recordid == _record.recordid);

            if (record == null) return;

            txtTitle.Text = $"Медицинская запись #{record.recordid}";
            txtSubtitle.Text = record.recorddate.ToString("dd.MM.yyyy");

            // Информация о визите
            txtRecordDate.Text = record.recorddate.ToString("dd.MM.yyyy HH:mm");
            txtNextVisit.Text = record.nextvisitdate?.ToString("dd.MM.yyyy") ?? "Не назначен";

            // Информация о пациенте
            txtAnimalName.Text = record.animal?.name ?? "Не указано";
            txtAnimalType.Text = $"{record.animal?.type?.typename ?? "Не указан"} / {record.animal?.breed?.breedname ?? "Без породы"}";
            txtVetName.Text = record.vet?.fullname ?? "Не указан";

            // Диагноз и лечение
            txtDiagnosis.Text = record.diagnosis ?? "Не указан";
            txtTreatment.Text = record.treatment ?? "Не указано";
            txtNotes.Text = record.notes ?? "";
        }

        private void ConfigureButtons()
        {
            btnEdit.Visibility = _canEdit ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MedicalEditDialog(_record);
            if (dialog.ShowDialog() == true)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}