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
    public partial class MedicalEditDialog : Window
    {
        private readonly medicalrecord? _record;
        private readonly bool _isEditMode;
        private readonly int? _preselectedAnimalId;

        // Классы для отображения в ComboBox
        public class AnimalItem
        {
            public int AnimalId { get; set; }
            public string DisplayName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
        }

        public class VetItem
        {
            public int UserId { get; set; }
            public string DisplayName { get; set; } = "";
            public string Username { get; set; } = "";
        }

        public MedicalEditDialog() : this(null, null) { }

        public MedicalEditDialog(medicalrecord? record = null) : this(record, null) { }

        public MedicalEditDialog(int animalId) : this(null, animalId) { }

        private MedicalEditDialog(medicalrecord? record, int? preselectedAnimalId)
        {
            InitializeComponent();

            _record = record;
            _isEditMode = record != null;
            _preselectedAnimalId = preselectedAnimalId;

            if (_isEditMode)
            {
                txtWindowTitle.Text = "Редактирование медицинской записи";
                btnSave.Content = "Сохранить изменения";
                Title = "Редактирование медзаписи - Приют";
            }
            else
            {
                txtWindowTitle.Text = "Новая медицинская запись";
                btnSave.Content = "Создать запись";
                Title = "Новая медзапись - Приют";
            }

            Loaded += MedicalEditDialog_Loaded;
        }

        private async void MedicalEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAnimalsAsync();
            await LoadVetsAsync();

            if (_isEditMode && _record != null)
            {
                LoadRecordData();
            }
            else
            {
                dpRecordDate.SelectedDate = DateTime.Today;
                // Если передан ID животного, предустанавливаем его
                if (_preselectedAnimalId.HasValue && _preselectedAnimalId.Value > 0)
                {
                    SelectAnimalById(_preselectedAnimalId.Value);
                }
            }
        }

        private async Task LoadAnimalsAsync()
        {
            try
            {
                using var context = new ShelterDbContext();

                var animals = await context.animals
                    .Include(a => a.type)
                    .OrderBy(a => a.name)
                    .ToListAsync();

                var animalList = animals.Select(a => new AnimalItem
                {
                    AnimalId = a.animalid,
                    DisplayName = $"{a.name} ({a.type?.typename ?? "Без вида"})",
                    Name = a.name,
                    Type = a.type?.typename ?? ""
                }).ToList();

                cmbAnimal.ItemsSource = animalList;
                cmbAnimal.DisplayMemberPath = "DisplayName";
                cmbAnimal.SelectedValuePath = "AnimalId";

                if (cmbAnimal.Items.Count > 0 && !_preselectedAnimalId.HasValue)
                    cmbAnimal.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}", "Ошибка");
            }
        }

        private async Task LoadVetsAsync()
        {
            try
            {
                using var context = new ShelterDbContext();

                // Ищем пользователей с ролью Ветеринар (roleid = 3)
                var vets = await context.userroles
                    .Where(ur => ur.roleid == 3)  // roleid = 3 для Ветеринар
                    .Select(ur => ur.user)
                    .Where(u => u != null && u.isactive == true)
                    .OrderBy(u => u.lastname)
                    .ThenBy(u => u.firstname)
                    .ToListAsync();

                var vetList = vets.Select(v => new VetItem
                {
                    UserId = v.userid,
                    DisplayName = $"{v.lastname} {v.firstname}",
                    Username = v.username
                }).ToList();

                cmbVet.ItemsSource = vetList;
                cmbVet.DisplayMemberPath = "DisplayName";
                cmbVet.SelectedValuePath = "UserId";

                if (cmbVet.Items.Count > 0)
                    cmbVet.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ветеринаров: {ex.Message}", "Ошибка");
            }
        }

        private void SelectAnimalById(int animalId)
        {
            if (cmbAnimal.Items.Count == 0) return;
            for (int i = 0; i < cmbAnimal.Items.Count; i++)
            {
                var item = cmbAnimal.Items[i] as AnimalItem;
                if (item != null && item.AnimalId == animalId)
                {
                    cmbAnimal.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadRecordData()
        {
            // Выбираем животное
            if (_record!.animalid > 0)
                SelectAnimalById(_record.animalid);

            // Выбираем ветеринара
            if (_record.vetid > 0 && cmbVet.Items.Count > 0)
            {
                for (int i = 0; i < cmbVet.Items.Count; i++)
                {
                    var item = cmbVet.Items[i] as VetItem;
                    if (item != null && item.UserId == _record.vetid)
                    {
                        cmbVet.SelectedIndex = i;
                        break;
                    }
                }
            }

            dpRecordDate.SelectedDate = _record.recorddate;
            txtDiagnosis.Text = _record.diagnosis ?? "";
            txtTreatment.Text = _record.treatment ?? "";
            dpNextVisit.SelectedDate = _record.nextvisitdate;
            txtNotes.Text = _record.notes ?? "";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (cmbAnimal.SelectedItem == null)
                {
                    MessageBox.Show("Выберите животное", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbVet.SelectedItem == null)
                {
                    MessageBox.Show("Выберите ветеринара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpRecordDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату осмотра", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDiagnosis.Text) && string.IsNullOrWhiteSpace(txtTreatment.Text))
                {
                    MessageBox.Show("Заполните хотя бы диагноз или лечение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new ShelterDbContext();

                medicalrecord record;
                if (_isEditMode && _record != null)
                {
                    record = await context.medicalrecords.FindAsync(_record.recordid);
                    if (record == null)
                    {
                        MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    record = new medicalrecord();
                    context.medicalrecords.Add(record);
                }

                var selectedAnimal = cmbAnimal.SelectedItem as AnimalItem;
                record.animalid = selectedAnimal?.AnimalId ?? 0;

                var selectedVet = cmbVet.SelectedItem as VetItem;
                record.vetid = selectedVet?.UserId ?? 0;

                record.recorddate = dpRecordDate.SelectedDate.Value;
                record.diagnosis = txtDiagnosis.Text.Trim();
                record.treatment = txtTreatment.Text.Trim();
                record.nextvisitdate = dpNextVisit.SelectedDate;
                record.notes = txtNotes.Text.Trim();

                await context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}