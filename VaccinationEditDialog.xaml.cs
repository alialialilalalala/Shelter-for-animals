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
    public partial class VaccinationEditDialog : Window
    {
        private readonly vaccination? _vaccination;
        private readonly bool _isEditMode;
        private readonly int? _preselectedAnimalId;

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

        // Конструктор без параметров (новая запись)
        public VaccinationEditDialog() : this(null, null) { }

        // Конструктор для редактирования существующей записи
        public VaccinationEditDialog(vaccination? vaccination) : this(vaccination, null) { }

        // Конструктор для новой записи с предустановленным ID животного
        public VaccinationEditDialog(int animalId) : this(null, animalId) { }

        // Приватный конструктор, делающий всю работу
        private VaccinationEditDialog(vaccination? vaccination, int? preselectedAnimalId)
        {
            InitializeComponent();

            _vaccination = vaccination;
            _isEditMode = vaccination != null;
            _preselectedAnimalId = preselectedAnimalId;

            if (_isEditMode)
            {
                txtWindowTitle.Text = "Редактирование вакцинации";
                btnSave.Content = "Сохранить изменения";
                Title = "Редактирование вакцинации - Приют";
            }
            else
            {
                txtWindowTitle.Text = "Новая вакцинация";
                btnSave.Content = "Добавить вакцинацию";
                Title = "Новая вакцинация - Приют";
            }

            Loaded += VaccinationEditDialog_Loaded;
        }

        private async void VaccinationEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAnimalsAsync();
            await LoadVetsAsync();

            if (_isEditMode && _vaccination != null)
            {
                LoadVaccinationData();
            }
            else
            {
                dpVaccinationDate.SelectedDate = DateTime.Today;
                if (_preselectedAnimalId.HasValue && _preselectedAnimalId.Value > 0)
                    SelectAnimalById(_preselectedAnimalId.Value);
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

        private void LoadVaccinationData()
        {
            // Животное
            if (_vaccination!.animalid > 0)
                SelectAnimalById(_vaccination.animalid);

            // Ветеринар
            if (_vaccination.vetid > 0 && cmbVet.Items.Count > 0)
            {
                for (int i = 0; i < cmbVet.Items.Count; i++)
                {
                    var item = cmbVet.Items[i] as VetItem;
                    if (item != null && item.UserId == _vaccination.vetid)
                    {
                        cmbVet.SelectedIndex = i;
                        break;
                    }
                }
            }

            txtVaccineName.Text = _vaccination.vaccinename ?? "";
            dpVaccinationDate.SelectedDate = _vaccination.vaccinationdate;
            dpNextVaccinationDate.SelectedDate = _vaccination.nextvaccinationdate;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                if (string.IsNullOrWhiteSpace(txtVaccineName.Text))
                {
                    MessageBox.Show("Введите название вакцины", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpVaccinationDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату вакцинации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new ShelterDbContext();

                vaccination vacc;
                if (_isEditMode && _vaccination != null)
                {
                    vacc = await context.vaccinations.FindAsync(_vaccination.vaccinationid);
                    if (vacc == null)
                    {
                        MessageBox.Show("Запись не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    vacc = new vaccination();
                    context.vaccinations.Add(vacc);
                }

                var selectedAnimal = cmbAnimal.SelectedItem as AnimalItem;
                vacc.animalid = selectedAnimal?.AnimalId ?? 0;

                var selectedVet = cmbVet.SelectedItem as VetItem;
                vacc.vetid = selectedVet?.UserId ?? 0;

                vacc.vaccinename = txtVaccineName.Text.Trim();
                vacc.vaccinationdate = dpVaccinationDate.SelectedDate.Value;
                vacc.nextvaccinationdate = dpNextVaccinationDate.SelectedDate;

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