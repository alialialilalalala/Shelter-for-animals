using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnimalShelterAI
{
    public partial class MedicalControl : UserControl
    {
        private List<AnimalFilterItem> animals = new List<AnimalFilterItem>();
        private int? _currentAnimalId = null;
        private List<MedicalRecordItem> medicalRecords = new List<MedicalRecordItem>();
        private List<VaccinationItem> vaccinations = new List<VaccinationItem>();

        public MedicalControl()
        {
            InitializeComponent();
            Loaded += MedicalControl_Loaded;
        }

        private async void MedicalControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAnimalsFilter();
        }

        private async Task LoadAnimalsFilter()
        {
            try
            {
                using var context = new ShelterDbContext();

                var dbAnimals = await context.animals
                    .Include(a => a.type)
                    .OrderBy(a => a.name)
                    .ToListAsync();

                animals = dbAnimals.Select(a => new AnimalFilterItem
                {
                    AnimalId = a.animalid,
                    DisplayName = $"{a.name} ({a.type?.typename ?? "Без вида"})"
                }).ToList();

                CmbAnimalFilter.ItemsSource = animals;
                CmbAnimalFilter.DisplayMemberPath = "DisplayName";
                CmbAnimalFilter.SelectedValuePath = "AnimalId";
                CmbAnimalFilter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}", "Ошибка");
            }
        }

        private async void CmbAnimalFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbAnimalFilter.SelectedItem is AnimalFilterItem selectedAnimal && selectedAnimal.AnimalId > 0)
            {
                _currentAnimalId = selectedAnimal.AnimalId;
                await LoadMedicalCard();
                MedicalCardPanel.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                MedicalCardPanel.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadMedicalCard()
        {
            if (!_currentAnimalId.HasValue) return;

            try
            {
                using var context = new ShelterDbContext();

                // Загружаем информацию о животном
                var animal = await context.animals
                    .Include(a => a.type)
                    .Include(a => a.breed)
                    .FirstOrDefaultAsync(a => a.animalid == _currentAnimalId.Value);

                if (animal != null)
                {
                    txtAnimalName.Text = animal.name ?? "Без имени";
                    txtAnimalDetails.Text = $"{animal.type?.typename ?? "Не указан"} • {animal.breed?.breedname ?? "Без породы"} • {animal.age ?? 0} лет • {(animal.gender == "Male" ? "Мужской" : "Женский")}";
                    txtAnimalDescription.Text = animal.description ?? "Нет описания";

                    // Статус с цветом
                    txtAnimalStatus.Text = GetStatusDisplay(animal.status);
                    borderAnimalStatus.Background = GetStatusBrush(animal.status);

                    // Загружаем фото
                    if (!string.IsNullOrEmpty(animal.photourl) && File.Exists(animal.photourl))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(animal.photourl, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            imgAnimalPhoto.Source = bitmap;
                            txtNoPhoto.Visibility = Visibility.Collapsed;
                        }
                        catch
                        {
                            imgAnimalPhoto.Source = null;
                            txtNoPhoto.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        imgAnimalPhoto.Source = null;
                        txtNoPhoto.Visibility = Visibility.Visible;
                    }
                }

                // Загружаем медицинские записи
                var records = await context.medicalrecords
                    .Include(r => r.vet)
                    .Where(r => r.animalid == _currentAnimalId.Value)
                    .OrderByDescending(r => r.recorddate)
                    .ToListAsync();

                medicalRecords = records.Select(r => new MedicalRecordItem
                {
                    RecordId = r.recordid,
                    RecordDate = r.recorddate,
                    Diagnosis = r.diagnosis ?? "",
                    Treatment = r.treatment ?? "",
                    VetName = r.vet?.fullname ?? "Не указан",
                    NextVisitDate = r.nextvisitdate
                }).ToList();

                MedicalRecordsGrid.ItemsSource = medicalRecords;

                // Загружаем вакцинации
                var vaccs = await context.vaccinations
                    .Include(v => v.vet)
                    .Where(v => v.animalid == _currentAnimalId.Value)
                    .OrderByDescending(v => v.vaccinationdate)
                    .ToListAsync();

                vaccinations = vaccs.Select(v => new VaccinationItem
                {
                    VaccinationId = v.vaccinationid,
                    VaccineName = v.vaccinename,
                    VaccinationDate = v.vaccinationdate,
                    NextVaccinationDate = v.nextvaccinationdate,
                    VetName = v.vet?.fullname ?? "Не указан"
                }).ToList();

                VaccinationsGrid.ItemsSource = vaccinations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки медицинской карты: {ex.Message}", "Ошибка");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAnimalId.HasValue)
                LoadMedicalCard();
        }

        private async void BtnAddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentAnimalId.HasValue) return;

            var dialog = new MedicalEditDialog(_currentAnimalId.Value);
            if (dialog.ShowDialog() == true)
            {
                await LoadMedicalCard();
            }
        }

        private async void BtnAddVaccination_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentAnimalId.HasValue) return;

            var dialog = new VaccinationEditDialog(_currentAnimalId.Value);
            if (dialog.ShowDialog() == true)
            {
                await LoadMedicalCard();
            }
        }

        private async void BtnEditRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int recordId)
            {
                using var context = new ShelterDbContext();
                var record = await context.medicalrecords.FindAsync(recordId);
                if (record != null)
                {
                    var dialog = new MedicalEditDialog(record);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadMedicalCard();
                    }
                }
            }
        }

        private async void BtnDeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int recordId)
            {
                var result = MessageBox.Show("Удалить медицинскую запись?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using var context = new ShelterDbContext();
                    var record = await context.medicalrecords.FindAsync(recordId);
                    if (record != null)
                    {
                        context.medicalrecords.Remove(record);
                        await context.SaveChangesAsync();
                        await LoadMedicalCard();
                    }
                }
            }
        }

        private async void BtnEditVaccination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int vaccId)
            {
                using var context = new ShelterDbContext();
                var vacc = await context.vaccinations.FindAsync(vaccId);
                if (vacc != null)
                {
                    var dialog = new VaccinationEditDialog(vacc);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadMedicalCard();
                    }
                }
            }

        }
        private async void BtnViewRecord_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int recordId)
            {
                using var context = new ShelterDbContext();
                var record = await context.medicalrecords
                    .Include(r => r.animal)
                    .Include(r => r.vet)
                    .FirstOrDefaultAsync(r => r.recordid == recordId);
                if (record != null)
                {
                    var dialog = new MedicalDetailsDialog(record);
                    dialog.ShowDialog();
                }
            }
        }

        private async void BtnViewVaccination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int vaccId)
            {
                var dialog = new VaccinationDetailsDialog(vaccId);
                dialog.ShowDialog();
            }
        }

        private async void BtnDeleteVaccination_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int vaccId)
            {
                var result = MessageBox.Show("Удалить запись о вакцинации?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using var context = new ShelterDbContext();
                    var vacc = await context.vaccinations.FindAsync(vaccId);
                    if (vacc != null)
                    {
                        context.vaccinations.Remove(vacc);
                        await context.SaveChangesAsync();
                        await LoadMedicalCard();
                    }
                }
            }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Quarantine" => "Карантин",
                "Available" => "Ищет дом",
                "Reserved" => "Забронирован",
                "Adopted" => "Усыновлен",
                "Treatment" => "На лечении",
                _ => status ?? "Неизвестно"
            };
        }

        private Brush GetStatusBrush(string status)
        {
            return status switch
            {
                "Quarantine" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "Available" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Reserved" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                "Adopted" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                "Treatment" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => Brushes.Gray
            };
        }

        public class AnimalFilterItem
        {
            public int AnimalId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public class MedicalRecordItem
        {
            public int RecordId { get; set; }
            public DateTime RecordDate { get; set; }
            public string Diagnosis { get; set; } = "";
            public string Treatment { get; set; } = "";
            public string VetName { get; set; } = "";
            public DateTime? NextVisitDate { get; set; }
        }

        public class VaccinationItem
        {
            public int VaccinationId { get; set; }
            public string VaccineName { get; set; } = "";
            public DateTime VaccinationDate { get; set; }
            public DateTime? NextVaccinationDate { get; set; }
            public string VetName { get; set; } = "";
        }
    }
}