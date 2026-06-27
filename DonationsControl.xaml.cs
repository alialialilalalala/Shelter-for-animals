using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AnimalShelterAI
{
    public partial class DonationsControl : UserControl
    {
        private List<DonationItem> donations = new List<DonationItem>();

        public DonationsControl()
        {
            InitializeComponent();
            Loaded += DonationsControl_Loaded;
        }

        private async void DonationsControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDonations();
        }

        private async System.Threading.Tasks.Task LoadDonations()
        {
            try
            {
                using var context = new ShelterDbContext();

                var dbDonations = await context.donations
                    .OrderByDescending(d => d.donationDate)
                    .ToListAsync();

                donations = dbDonations.Select(d => new DonationItem
                {
                    DonationId = d.donationid,
                    DonationDate = d.donationDate,
                    Amount = d.amount,
                    DonationType = d.donationtype ?? "Не указан",
                    DonorName = d.isanonymous ? "Аноним" : (d.donorname ?? "Не указан"),
                    IsAnonymous = d.isanonymous,
                    Notes = d.notes ?? ""
                }).ToList();

                DonationsGrid.ItemsSource = donations;

                // Обновляем статистику
                var totalSum = donations.Sum(d => d.Amount);
                var totalCount = donations.Count;

                txtTotalSum.Text = $"{totalSum:N2} ₽";
                txtTotalCount.Text = totalCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пожертвований: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DonationEditDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadDonations();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                using var context = new ShelterDbContext();
                var donation = await context.donations.FindAsync(id);
                if (donation != null)
                {
                    var dialog = new DonationEditDialog(donation);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadDonations();
                    }
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                var result = MessageBox.Show("Удалить запись о пожертвовании?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var donation = await context.donations.FindAsync(id);
                        if (donation != null)
                        {
                            context.donations.Remove(donation);
                            await context.SaveChangesAsync();
                            await LoadDonations();
                            MessageBox.Show("Запись удалена.", "Успех");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Пожертвования_{DateTime.Now:yyyy-MM-dd}.xlsx"
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
                    header.Style.Fill.BackgroundColor = XLColor.LightYellow;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var d in donations)
                    {
                        worksheet.Cell(row, 1).Value = d.DonationId;
                        worksheet.Cell(row, 2).Value = d.DonationDate.ToString("dd.MM.yyyy");
                        worksheet.Cell(row, 3).Value = d.Amount;
                        worksheet.Cell(row, 4).Value = d.DonationType;
                        worksheet.Cell(row, 5).Value = d.DonorName;
                        worksheet.Cell(row, 6).Value = d.IsAnonymous ? "Да" : "Нет";
                        worksheet.Cell(row, 7).Value = d.Notes;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);

                    MessageBox.Show($"Данные успешно экспортированы:\n{saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        public class DonationItem
        {
            public int DonationId { get; set; }
            public DateTime DonationDate { get; set; }
            public decimal Amount { get; set; }
            public string DonationType { get; set; } = "";
            public string DonorName { get; set; } = "";
            public bool IsAnonymous { get; set; }
            public string Notes { get; set; } = "";
        }

        private void DonationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}