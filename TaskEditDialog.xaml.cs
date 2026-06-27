using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class TaskEditDialog : Window, INotifyPropertyChanged
    {
        private readonly volunteertask? _task;
        private readonly bool _isEditMode;

        private ObservableCollection<VolunteerItem> _volunteers = new ObservableCollection<VolunteerItem>();
        private ObservableCollection<AnimalItem> _animals = new ObservableCollection<AnimalItem>();

        public ObservableCollection<VolunteerItem> Volunteers
        {
            get => _volunteers;
            set { _volunteers = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AnimalItem> Animals
        {
            get => _animals;
            set { _animals = value; OnPropertyChanged(); }
        }

        public TaskEditDialog(volunteertask? task = null)
        {
            InitializeComponent();
            DataContext = this;

            _task = task;
            _isEditMode = task != null;

            if (_isEditMode)
            {
                txtWindowTitle.Text = "Редактирование задачи";
                btnSave.Content = "Сохранить изменения";
                Title = "Редактирование задачи - Приют";
            }
            else
            {
                txtWindowTitle.Text = "Новая задача";
                btnSave.Content = "Создать задачу";
                Title = "Новая задача - Приют";
            }

            Loaded += TaskEditDialog_Loaded;
        }

        private async void TaskEditDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadVolunteersAsync();
            await LoadAnimalsAsync();

            if (_isEditMode && _task != null)
            {
                LoadTaskData();
            }
            else
            {
                dpDueDate.SelectedDate = DateTime.Today.AddDays(7);
                cmbStatus.SelectedIndex = 0;
            }
        }

        private async Task LoadVolunteersAsync()
        {
            try
            {
                using var context = new ShelterDbContext();

                // Ищем пользователей у которых есть роль "Волонтер" (roleid = 4)
                var volunteers = await context.userroles
                    .Where(ur => ur.roleid == 4)  // roleid = 4 для Волонтер
                    .Select(ur => ur.user)
                    .Where(u => u != null && u.isactive == true)
                    .OrderBy(u => u.lastname)
                    .ThenBy(u => u.firstname)
                    .ToListAsync();


                var volunteerList = volunteers.Select(v => new VolunteerItem
                {
                    UserId = v.userid,
                    DisplayName = $"{v.lastname} {v.firstname}",
                    Username = v.username
                }).ToList();

                cmbVolunteer.ItemsSource = volunteerList;
                cmbVolunteer.DisplayMemberPath = "DisplayName";
                cmbVolunteer.SelectedValuePath = "UserId";

                if (cmbVolunteer.Items.Count > 0)
                    cmbVolunteer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки волонтёров: {ex.Message}", "Ошибка");
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

                Animals.Clear();

                // Добавляем пустой вариант
                Animals.Add(new AnimalItem { AnimalId = 0, DisplayName = "— Не выбрано —" });

                foreach (var a in animals)
                {
                    Animals.Add(new AnimalItem
                    {
                        AnimalId = a.animalid,
                        DisplayName = $"{a.name} ({a.type?.typename ?? "Без вида"})",
                        Name = a.name,
                        Type = a.type?.typename ?? ""
                    });
                }

                cmbAnimal.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки животных: {ex.Message}", "Ошибка");
            }
        }

        private void LoadTaskData()
        {
            txtTaskTitle.Text = _task!.title ?? "";
            txtDescription.Text = _task.description ?? "";
            dpDueDate.SelectedDate = _task.duedate;
            txtNotes.Text = _task.notes ?? "";

            var statusItem = cmbStatus.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag.ToString() == _task.status);
            if (statusItem != null) cmbStatus.SelectedItem = statusItem;

            // Выбираем волонтёра
            var volunteer = Volunteers.FirstOrDefault(v => v.UserId == _task.volunteerid);
            if (volunteer != null)
            {
                cmbVolunteer.SelectedItem = volunteer;
            }

            // Выбираем животное
            if (_task.animalid.HasValue && _task.animalid.Value > 0)
            {
                var animal = Animals.FirstOrDefault(a => a.AnimalId == _task.animalid.Value);
                if (animal != null)
                {
                    cmbAnimal.SelectedItem = animal;
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTaskTitle.Text))
                {
                    MessageBox.Show("Введите название задачи", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbVolunteer.SelectedItem == null)
                {
                    MessageBox.Show("Выберите волонтёра", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpDueDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите срок выполнения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new ShelterDbContext();

                volunteertask task;
                if (_isEditMode && _task != null)
                {
                    task = await context.volunteertasks.FindAsync(_task.taskid);
                    if (task == null)
                    {
                        MessageBox.Show("Задача не найдена", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    task = new volunteertask { assigneddate = DateTime.UtcNow };
                    context.volunteertasks.Add(task);
                }

                task.title = txtTaskTitle.Text.Trim();
                task.description = txtDescription.Text.Trim();
                task.duedate = dpDueDate.SelectedDate;
                task.notes = txtNotes.Text.Trim();

                var selectedVolunteer = cmbVolunteer.SelectedItem as VolunteerItem;
                task.volunteerid = selectedVolunteer?.UserId ?? 0;

                var selectedAnimal = cmbAnimal.SelectedItem as AnimalItem;
                task.animalid = (selectedAnimal != null && selectedAnimal.AnimalId > 0) ? selectedAnimal.AnimalId : null;

                var selectedItem = cmbStatus.SelectedItem as ComboBoxItem;
                string selectedStatus = selectedItem?.Tag?.ToString() ?? "Pending";
                task.status = selectedStatus;

                if (selectedStatus == "Completed" && task.completeddate == null)
                    task.completeddate = DateTime.UtcNow;
                else if (selectedStatus != "Completed")
                    task.completeddate = null;

                await context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении задачи:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class VolunteerItem
        {
            public int UserId { get; set; }
            public string DisplayName { get; set; } = "";
            public string Username { get; set; } = "";
        }

        public class AnimalItem
        {
            public int AnimalId { get; set; }
            public string DisplayName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
        }
    }
}