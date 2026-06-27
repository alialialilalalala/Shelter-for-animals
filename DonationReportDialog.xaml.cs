using AnimalShelterAI.Infrastructure.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class DonationReportDialog : Window
    {
        public DonationReportDialog()
        {
            InitializeComponent();
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                string donationType = null;
                if (cmbType.SelectedItem is ComboBoxItem typeItem && typeItem.Content.ToString() != "Все типы")
                {
                    donationType = typeItem.Content.ToString() switch
                    {
                        "Деньги" => "Money",
                        "Корм" => "Food",
                        "Лекарства" => "Medicine",
                        "Другое" => "Other",
                        _ => null
                    };
                }
                bool onlyAnonymous = chkOnlyAnonymous.IsChecked == true;

                using var context = new ShelterDbContext();
                var query = context.donations.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(d => d.donationDate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(d => d.donationDate <= endDate.Value);
                if (!string.IsNullOrEmpty(donationType))
                    query = query.Where(d => d.donationtype == donationType);
                if (onlyAnonymous)
                    query = query.Where(d => d.isanonymous == true);

                var donations = await query.OrderByDescending(d => d.donationDate).ToListAsync();

                if (!donations.Any())
                {
                    MessageBox.Show("Нет данных для выбранных параметров.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчет_по_пожертвованиям_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Пожертвования");

                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "Дата";
                    worksheet.Cell(1, 3).Value = "Сумма";
                    worksheet.Cell(1, 4).Value = "Тип";
                    worksheet.Cell(1, 5).Value = "Имя донора";
                    worksheet.Cell(1, 6).Value = "Анонимно";
                    worksheet.Cell(1, 7).Value = "Примечания";

                    var header = worksheet.Range(1, 1, 1, 7);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var d in donations)
                    {
                        worksheet.Cell(row, 1).Value = d.donationid;
                        worksheet.Cell(row, 2).Value = d.donationDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(row, 3).Value = d.amount;
                        worksheet.Cell(row, 4).Value = d.donationtype ?? "Не указан";
                        worksheet.Cell(row, 5).Value = d.isanonymous ? "Аноним" : (d.donorname ?? "Не указан");
                        worksheet.Cell(row, 6).Value = d.isanonymous ? "Да" : "Нет";
                        worksheet.Cell(row, 7).Value = d.notes ?? "";
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
    }
}