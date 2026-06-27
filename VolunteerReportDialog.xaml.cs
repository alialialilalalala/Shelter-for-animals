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
    public partial class VolunteerReportDialog : Window
    {
        public class VolunteerItem
        {
            public int UserId { get; set; }
            public string DisplayName { get; set; } = "";
        }

        public VolunteerReportDialog()
        {
            InitializeComponent();
            Loaded += VolunteerReportDialog_Loaded;
        }

        private async void VolunteerReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadVolunteersAsync();
        }

        private async Task LoadVolunteersAsync()
        {
            try
            {
                using var context = new ShelterDbContext();
                var volunteers = await context.userroles
                    .Where(ur => ur.roleid == 4) // roleid 4 = Волонтер
                    .Select(ur => ur.user)
                    .Where(u => u != null && u.isactive == true)
                    .OrderBy(u => u.lastname)
                    .Select(u => new VolunteerItem { UserId = u.userid, DisplayName = u.fullname })
                    .ToListAsync();

                volunteers.Insert(0, new VolunteerItem { UserId = 0, DisplayName = "Все волонтёры" });

                cmbVolunteer.ItemsSource = volunteers;
                cmbVolunteer.SelectedValuePath = "UserId";
                cmbVolunteer.DisplayMemberPath = "DisplayName";
                cmbVolunteer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки волонтёров: {ex.Message}");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                string status = null;
                if (cmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все статусы")
                {
                    status = statusItem.Content.ToString() switch
                    {
                        "Ожидает" => "Pending",
                        "В работе" => "InProgress",
                        "Выполнена" => "Completed",
                        "Отклонена" => "Rejected",
                        _ => null
                    };
                }
                int? volunteerId = null;
                if (cmbVolunteer.SelectedItem is VolunteerItem selectedVolunteer && selectedVolunteer.UserId > 0)
                    volunteerId = selectedVolunteer.UserId;

                using var context = new ShelterDbContext();
                var query = context.volunteertasks
                    .Include(t => t.volunteer)
                    .Include(t => t.animal)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(t => t.assigneddate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(t => t.assigneddate <= endDate.Value);
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(t => t.status == status);
                if (volunteerId.HasValue)
                    query = query.Where(t => t.volunteerid == volunteerId.Value);

                var tasks = await query.OrderByDescending(t => t.assigneddate).ToListAsync();

                if (!tasks.Any())
                {
                    MessageBox.Show("Нет данных для выбранных параметров.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчет_по_волонтерам_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Волонтёры");

                    worksheet.Cell(1, 1).Value = "ID задачи";
                    worksheet.Cell(1, 2).Value = "Название";
                    worksheet.Cell(1, 3).Value = "Описание";
                    worksheet.Cell(1, 4).Value = "Волонтёр";
                    worksheet.Cell(1, 5).Value = "Животное";
                    worksheet.Cell(1, 6).Value = "Дата назначения";
                    worksheet.Cell(1, 7).Value = "Срок выполнения";
                    worksheet.Cell(1, 8).Value = "Статус";
                    worksheet.Cell(1, 9).Value = "Дата выполнения";
                    worksheet.Cell(1, 10).Value = "Примечания";

                    var header = worksheet.Range(1, 1, 1, 10);
                    header.Style.Font.Bold = true;
                    header.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var t in tasks)
                    {
                        worksheet.Cell(row, 1).Value = t.taskid;
                        worksheet.Cell(row, 2).Value = t.title;
                        worksheet.Cell(row, 3).Value = t.description ?? "";
                        worksheet.Cell(row, 4).Value = t.volunteer?.fullname ?? "Не назначен";
                        worksheet.Cell(row, 5).Value = t.animal?.name ?? "-";
                        worksheet.Cell(row, 6).Value = t.assigneddate.ToString("dd.MM.yyyy");
                        worksheet.Cell(row, 7).Value = t.duedate?.ToString("dd.MM.yyyy") ?? "-";
                        worksheet.Cell(row, 8).Value = TranslateTaskStatus(t.status);
                        worksheet.Cell(row, 9).Value = t.completeddate?.ToString("dd.MM.yyyy") ?? "-";
                        worksheet.Cell(row, 10).Value = t.notes ?? "";
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

        private string TranslateTaskStatus(string status)
        {
            return status switch
            {
                "Pending" => "Ожидает",
                "InProgress" => "В работе",
                "Completed" => "Выполнена",
                "Rejected" => "Отклонена",
                _ => status
            };
        }
    }
}