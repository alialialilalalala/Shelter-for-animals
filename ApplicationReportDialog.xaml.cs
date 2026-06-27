using AnimalShelterAI.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class ApplicationReportDialog : Window
    {
        public class AnimalItem
        {
            public int AnimalId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public ApplicationReportDialog()
        {
            InitializeComponent();
            Loaded += ApplicationReportDialog_Loaded;
        }

        private async void ApplicationReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAnimalsAsync();
        }

        private async Task LoadAnimalsAsync()
        {
            try
            {
                using var context = new ShelterDbContext();
                var animals = await context.animals
                    .OrderBy(a => a.name)
                    .Select(a => new AnimalItem { AnimalId = a.animalid, DisplayName = a.name })
                    .ToListAsync();

                animals.Insert(0, new AnimalItem { AnimalId = 0, DisplayName = "Все животные" });

                cmbAnimal.ItemsSource = animals;
                cmbAnimal.SelectedValuePath = "AnimalId";
                cmbAnimal.DisplayMemberPath = "DisplayName";
                cmbAnimal.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string status = null;
                if (cmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все")
                {
                    status = statusItem.Content.ToString() switch
                    {
                        "На рассмотрении" => "Pending",
                        "Одобрена" => "Approved",
                        "Отклонена" => "Rejected",
                        "Завершена" => "Completed",
                        _ => null
                    };
                }

                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                int? animalId = null;
                if (cmbAnimal.SelectedItem is AnimalItem selectedAnimal && selectedAnimal.AnimalId > 0)
                    animalId = selectedAnimal.AnimalId;

                using var context = new ShelterDbContext();
                var query = context.adoptionapplications
                    .Include(a => a.animal)
                    .Include(a => a.user)
                    .Include(a => a.manager)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(a => a.status == status);
                if (startDate.HasValue)
                    query = query.Where(a => a.applicationdate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(a => a.applicationdate <= endDate.Value);
                if (animalId.HasValue)
                    query = query.Where(a => a.animalId == animalId.Value);

                var apps = await query.OrderByDescending(a => a.applicationdate).ToListAsync();

                if (!apps.Any())
                {
                    MessageBox.Show("Нет данных для выбранных параметров.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчет_по_заявкам_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Заявки");

                    worksheet.Cell(1, 1).Value = "ID заявки";
                    worksheet.Cell(1, 2).Value = "Животное";
                    worksheet.Cell(1, 3).Value = "Заявитель";
                    worksheet.Cell(1, 4).Value = "Email";
                    worksheet.Cell(1, 5).Value = "Телефон";
                    worksheet.Cell(1, 6).Value = "Дата заявки";
                    worksheet.Cell(1, 7).Value = "Статус";
                    worksheet.Cell(1, 8).Value = "Менеджер";
                    worksheet.Cell(1, 9).Value = "Дата решения";
                    worksheet.Cell(1, 10).Value = "Примечания";

                    var header = worksheet.Range(1, 1, 1, 10);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var app in apps)
                    {
                        worksheet.Cell(row, 1).Value = app.applicationId;
                        worksheet.Cell(row, 2).Value = app.animal?.name ?? "Неизвестно";
                        worksheet.Cell(row, 3).Value = app.user?.fullname ?? "Неизвестно";
                        worksheet.Cell(row, 4).Value = app.user?.email ?? "";
                        worksheet.Cell(row, 5).Value = app.user?.phone ?? "";
                        worksheet.Cell(row, 6).Value = app.applicationdate.ToString("dd.MM.yyyy HH:mm");
                        worksheet.Cell(row, 7).Value = TranslateStatus(app.status);
                        worksheet.Cell(row, 8).Value = app.manager?.fullname ?? "Не назначен";
                        worksheet.Cell(row, 9).Value = app.decisionDate?.ToString("dd.MM.yyyy HH:mm") ?? "-";
                        worksheet.Cell(row, 10).Value = app.notes ?? "";
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                    MessageBox.Show($"Отчёт сохранён:\n{saveDialog.FileName}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string TranslateStatus(string status)
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