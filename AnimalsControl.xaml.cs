using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnimalShelterAI.Core.DTOs;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Services;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AnimalShelterAI
{
    public partial class AnimalsControl : UserControl
    {
        private List<Animalitem> animals = new List<Animalitem>();
        private ShelterDbContext? _context;
        private AnimalService? _animalService;
        private user? _currentUser;
        private bool _canEdit;
        

        public AnimalsControl(user? currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _canEdit = currentUser != null && IsUserCanEdit(currentUser);
            BtnAdd.Visibility = _canEdit ? Visibility.Visible : Visibility.Collapsed;
            Loaded += AnimalsControl_Loaded;
        }

        private bool IsUserCanEdit(user user)
        {
            if (user.userroles == null) return false;
            var roleNames = user.userroles.Where(ur => ur.role != null).Select(ur => ur.role!.rolename).ToList();
            var allowedRoles = new[] { "Администратор", "Менеджер", "Ветеринар" };
            return roleNames.Any(r => allowedRoles.Contains(r));
        }

        private void AnimalsControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAnimals();
            if (CmbStatus != null) CmbStatus.SelectedIndex = 0;
            if (CmbType != null) CmbType.SelectedIndex = 0;
            FilterAnimals();
        }

        private async void LoadAnimals()
        {
            try
            {
                _context = new ShelterDbContext();
                _animalService = new AnimalService(_context);

                var animalDtos = await _animalService.GetAnimalsAsync();

                // Загружаем избранное текущего пользователя
              

                animals = animalDtos.Select(dto => new Animalitem
                {
                    Id = dto.Animalid,
                    Name = dto.Name,
                    Type = dto.Typename ?? "",
                    Breed = dto.Breedname ?? "",
                    Gender = dto.Gender switch { "Male" => "М", "Female" => "Ж", _ => "" },
                    Age = dto.Age ?? 0,
                    Status = GetStatusDisplay(dto.Status),
                    AdmissionDate = dto.Admissiondate,
                    Description = dto.Description,
                    Color = dto.Color,
                    Weight = dto.Weight,
                    Healthstatus = GetHealthDisplay(dto.Healthstatus),
                    CanEdit = _canEdit,
                    PhotoUrl = dto.Photourl
                }).ToList();

                AnimalsGrid.ItemsSource = animals;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadTestData();
            }
        }

        private void LoadTestData()
        {
            animals = new List<Animalitem>
            {
                new Animalitem { Id = 1, Name = "Мурка", Type = "Кошка", Breed = "Британская", Gender = "Ж", Age = 2, Status = "Ищет дом", AdmissionDate = DateTime.Now.AddDays(-30), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 2, Name = "Бобик", Type = "Собака", Breed = "Лабрадор", Gender = "М", Age = 3, Status = "Карантин", AdmissionDate = DateTime.Now.AddDays(-10), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 3, Name = "Рыжик", Type = "Кошка", Breed = "Персидская", Gender = "М", Age = 1, Status = "На лечении", AdmissionDate = DateTime.Now.AddDays(-5), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 4, Name = "Шарик", Type = "Собака", Breed = "Овчарка", Gender = "М", Age = 4, Status = "Ищет дом", AdmissionDate = DateTime.Now.AddDays(-45), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 5, Name = "Милка", Type = "Кролик", Breed = "", Gender = "Ж", Age = 1, Status = "Ищет дом", AdmissionDate = DateTime.Now.AddDays(-20), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 6, Name = "Граф", Type = "Собака", Breed = "Лабрадор", Gender = "М", Age = 4, Status = "Ищет дом", AdmissionDate = DateTime.Now.AddDays(-15), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 7, Name = "Снежок", Type = "Кошка", Breed = "Персидская", Gender = "М", Age = 2, Status = "На лечении", AdmissionDate = DateTime.Now.AddDays(-5), CanEdit = _canEdit, PhotoUrl = null },
                new Animalitem { Id = 8, Name = "Кеша", Type = "Попугай", Breed = "Волнистый", Gender = "М", Age = 1, Status = "Ищет дом", AdmissionDate = DateTime.Now.AddDays(-25), CanEdit = _canEdit, PhotoUrl = null }
            };
            AnimalsGrid.ItemsSource = animals;
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

        private string GetHealthDisplay(string healthstatus)
        {
            return healthstatus switch
            {
                "Healthy" => "Здоров",
                "Sick" => "Болен",
                "Recovering" => "Восстанавливается",
                "Chronic" => "Хроническое",
                _ => healthstatus ?? "Не указано"
            };
        }

        private void FilterAnimals()
        {
            if (animals == null || AnimalsGrid == null) return;
            var filtered = animals.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
            {
                var searchTerm = TxtSearch.Text.ToLower();
                filtered = filtered.Where(a => (a.Name?.ToLower().Contains(searchTerm) ?? false) ||
                                               (a.Breed?.ToLower().Contains(searchTerm) ?? false) ||
                                               (a.Description?.ToLower().Contains(searchTerm) ?? false) ||
                                               (a.Color?.ToLower().Contains(searchTerm) ?? false));
            }

            if (CmbType != null && CmbType.SelectedItem is ComboBoxItem typeItem && typeItem.Content?.ToString() is string typeContent && typeContent != "Все виды")
                filtered = filtered.Where(a => a.Type == typeContent);
            if (CmbStatus != null && CmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content?.ToString() is string statusContent && statusContent != "Все статусы")
                filtered = filtered.Where(a => a.Status == statusContent);

            AnimalsGrid.ItemsSource = filtered.ToList();
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterAnimals();
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterAnimals();
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterAnimals();

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) { MessageBox.Show("Нет прав", "Доступ запрещен"); return; }
            var dialog = new AnimalEditDialog();
            if (dialog.ShowDialog() == true) LoadAnimals();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Животные_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };
                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Животные");
                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Кличка";
                    worksheet.Cell(1, 3).Value = "Вид";
                    worksheet.Cell(1, 4).Value = "Порода";
                    worksheet.Cell(1, 5).Value = "Пол";
                    worksheet.Cell(1, 6).Value = "Возраст";
                    worksheet.Cell(1, 7).Value = "Статус";
                    worksheet.Cell(1, 8).Value = "Дата поступления";
                    var headerRange = worksheet.Range(1, 1, 1, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    int row = 2;
                    foreach (var animal in animals)
                    {
                        worksheet.Cell(row, 1).Value = animal.Id;
                        worksheet.Cell(row, 2).Value = animal.Name;
                        worksheet.Cell(row, 3).Value = animal.Type;
                        worksheet.Cell(row, 4).Value = animal.Breed;
                        worksheet.Cell(row, 5).Value = animal.Gender;
                        worksheet.Cell(row, 6).Value = animal.Age;
                        worksheet.Cell(row, 7).Value = animal.Status;
                        worksheet.Cell(row, 8).Value = animal.AdmissionDate.ToString("dd.MM.yyyy");
                        row++;
                    }
                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Экспорт выполнен", "Успех");
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
        }

        private async void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var animal = await _animalService!.GetAnimalByIdAsync(id);
                if (animal != null)
                {
                    var dialog = new AnimalDetailsDialog(animal, _canEdit, _currentUser?.userid ?? 0);
                    dialog.ShowDialog();
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) { MessageBox.Show("Нет прав", "Доступ запрещен"); return; }
            if (sender is Button button && button.Tag is int id)
            {
                var animal = await _animalService!.GetAnimalByIdAsync(id);
                if (animal != null && new AnimalEditDialog(animal).ShowDialog() == true) LoadAnimals();
            }
        }

        private async void BtnAdopt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                if (_currentUser == null) { MessageBox.Show("Войдите в систему", "Ошибка"); return; }
                var animal = animals.FirstOrDefault(a => a.Id == id);
                if (animal?.Status != "Ищет дом") { MessageBox.Show("Животное не доступно"); return; }
                var animalDto = await _animalService!.GetAnimalByIdAsync(id);
                if (animalDto != null)
                {
                    var dialog = new AdoptionApplicationDialog(animalDto, _currentUser);
                    if (dialog.ShowDialog() == true) LoadAnimals();
                }
            }
        }

       

        public class Animalitem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string Breed { get; set; } = "";
            public string Gender { get; set; } = "";
            public int Age { get; set; }
            public string Status { get; set; } = "";
            public string? Description { get; set; }
            public string? Color { get; set; }
            public decimal? Weight { get; set; }
            public DateTime AdmissionDate { get; set; }
            public string? Healthstatus { get; set; }
            public bool CanEdit { get; set; } = false;
            public string? PhotoUrl { get; set; }
            public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl) && File.Exists(PhotoUrl);
        }
    }
}