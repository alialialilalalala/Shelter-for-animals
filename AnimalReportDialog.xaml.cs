using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AnimalShelterAI
{
    public partial class AnimalReportDialog : Window
    {
        public AnimalReportDialog()
        {
            InitializeComponent();
            Loaded += AnimalReportDialog_Loaded;
        }

        private async void AnimalReportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTypesAsync();
        }

        private async Task LoadTypesAsync()
        {
            try
            {
                using var context = new ShelterDbContext();
                var types = await context.animaltypes.OrderBy(t => t.typename).ToListAsync();
                cmbType.Items.Clear();
                cmbType.Items.Add(new ComboBoxItem { Content = "Все виды", Tag = null });
                foreach (var t in types)
                {
                    cmbType.Items.Add(new ComboBoxItem { Content = t.typename, Tag = t.typeid });
                }
                cmbType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов: {ex.Message}");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем выбранные параметры
                int? typeId = null;
                if (cmbType.SelectedItem is ComboBoxItem typeItem && typeItem.Tag != null)
                    typeId = (int)typeItem.Tag;

                string status = null;
                if (cmbStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все статусы")
                {
                    status = statusItem.Content.ToString() switch
                    {
                        "Карантин" => "Quarantine",
                        "Ищет дом" => "Available",
                        "Забронирован" => "Reserved",
                        "Усыновлен" => "Adopted",
                        "На лечении" => "Treatment",
                        _ => null
                    };
                }

                string gender = null;
                if (cmbGender.SelectedItem is ComboBoxItem genderItem && genderItem.Content.ToString() != "Все")
                {
                    gender = genderItem.Content.ToString() == "Мужской" ? "Male" : "Female";
                }

                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                bool includeDescription = chkIncludeDescription.IsChecked == true;

                using var context = new ShelterDbContext();
                var query = context.animals
                    .Include(a => a.type)
                    .Include(a => a.breed)
                    .AsQueryable();

                if (typeId.HasValue)
                    query = query.Where(a => a.typeid == typeId.Value);
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(a => a.status == status);
                if (!string.IsNullOrEmpty(gender))
                    query = query.Where(a => a.gender == gender);
                if (startDate.HasValue)
                    query = query.Where(a => a.admissiondate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(a => a.admissiondate <= endDate.Value);

                var animals = await query.OrderBy(a => a.name).ToListAsync();

                if (!animals.Any())
                {
                    MessageBox.Show("Нет данных для выбранных параметров.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Экспорт в Excel
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel файлы (*.xlsx)|*.xlsx",
                    FileName = $"Отчет_по_животным_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Животные");

                    // Заголовки
                    int col = 1;
                    worksheet.Cell(1, col++).Value = "ID";
                    worksheet.Cell(1, col++).Value = "Кличка";
                    worksheet.Cell(1, col++).Value = "Вид";
                    worksheet.Cell(1, col++).Value = "Порода";
                    worksheet.Cell(1, col++).Value = "Пол";
                    worksheet.Cell(1, col++).Value = "Возраст";
                    worksheet.Cell(1, col++).Value = "Вес (кг)";
                    worksheet.Cell(1, col++).Value = "Окрас";
                    worksheet.Cell(1, col++).Value = "Статус";
                    worksheet.Cell(1, col++).Value = "Здоровье";
                    worksheet.Cell(1, col++).Value = "Дата поступления";
                    if (includeDescription)
                        worksheet.Cell(1, col++).Value = "Описание";

                    var headerRange = worksheet.Range(1, 1, 1, col - 1);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var a in animals)
                    {
                        col = 1;
                        worksheet.Cell(row, col++).Value = a.animalid;
                        worksheet.Cell(row, col++).Value = a.name;
                        worksheet.Cell(row, col++).Value = a.type?.typename ?? "";
                        worksheet.Cell(row, col++).Value = a.breed?.breedname ?? "";
                        worksheet.Cell(row, col++).Value = a.gender == "Male" ? "М" : a.gender == "Female" ? "Ж" : "";
                        worksheet.Cell(row, col++).Value = a.age ?? 0;
                        worksheet.Cell(row, col++).Value = a.weight ?? 0;
                        worksheet.Cell(row, col++).Value = a.color ?? "";
                        worksheet.Cell(row, col++).Value = TranslateAnimalStatus(a.status);
                        worksheet.Cell(row, col++).Value = TranslateHealthStatus(a.healthstatus);
                        worksheet.Cell(row, col++).Value = a.admissiondate.ToString("dd.MM.yyyy");
                        if (includeDescription)
                            worksheet.Cell(row, col++).Value = a.description ?? "";
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);

                    MessageBox.Show($"Отчёт успешно сохранён:\n{saveDialog.FileName}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчёта:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

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
    }
}