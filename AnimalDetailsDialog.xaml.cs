using AnimalShelterAI.Core.DTOs;
using AnimalShelterAI.Services;
using AnimalShelterAI.Infrastructure.Data;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimalShelterAI
{
    public partial class AnimalDetailsDialog : Window
    {
        private readonly AnimalDto _animal;
        private readonly bool _canEdit;
        private readonly int _currentUserId;

        // Добавьте это свойство в класс AnimalDetailsDialog
        public bool HasPhoto => !string.IsNullOrEmpty(_animal.Photourl) && File.Exists(_animal.Photourl);

        // И добавьте DataContext для привязки
        public AnimalDetailsDialog(AnimalDto animal, bool canEdit = false, int currentUserId = 0)
        {
            InitializeComponent();
            _animal = animal;
            _canEdit = canEdit;
            _currentUserId = currentUserId;

            DataContext = this; // Добавьте эту строку

            LoadAnimalData();
            ConfigureButtons();
        }

        private void LoadAnimalData()
        {
            // Заголовок
            txtAnimalName.Text = _animal.Name;
            txtAnimalStatus.Text = GetStatusDisplay(_animal.Status);

            // Краткая информация
            txtName.Text = _animal.Name;
            txtType.Text = _animal.Typename ?? "Не указан";
            txtBreed.Text = _animal.Breedname ?? "Не указана";
            txtGender.Text = _animal.Gender == "Male" ? "Мужской" : _animal.Gender == "Female" ? "Женский" : "Не указан";
            txtAge.Text = _animal.Age.HasValue ? $"{_animal.Age} лет" : "Не указан";

            // Детальная информация
            txtWeight.Text = _animal.Weight.HasValue ? $"{_animal.Weight:0.##} кг" : "Не указан";
            txtColor.Text = _animal.Color ?? "Не указан";
            txtAdmissionDate.Text = _animal.Admissiondate.ToString("dd.MM.yyyy");
            txtDescription.Text = _animal.Description ?? "Нет описания";

            // Статус с цветом
            txtStatus.Text = GetStatusDisplay(_animal.Status);
            borderStatus.Background = GetStatusBrush(_animal.Status);

            // Здоровье с цветом
            txtHealth.Text = GetHealthDisplay(_animal.Healthstatus);
            borderHealth.Background = GetHealthBrush(_animal.Healthstatus);

            // Загружаем фото
            LoadPhoto();
        }

        private void LoadPhoto()
        {
            try
            {
                if (!string.IsNullOrEmpty(_animal.Photourl) && File.Exists(_animal.Photourl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_animal.Photourl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgPhoto.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки фото: {ex.Message}");
            }
        }

        private void ConfigureButtons()
        {
            // Кнопка редактирования только для админов, менеджеров и ветврачей
            btnEdit.Visibility = _canEdit ? Visibility.Visible : Visibility.Collapsed;

            // Кнопка подачи заявки видна всегда, но только если животное доступно
            btnAdopt.Visibility = _animal.Status == "Available" ? Visibility.Visible : Visibility.Collapsed;
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

        private string GetHealthDisplay(string health)
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

        private Brush GetHealthBrush(string health)
        {
            return health switch
            {
                "Healthy" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Sick" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                "Recovering" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                "Chronic" => new SolidColorBrush(Color.FromRgb(121, 85, 72)),
                _ => Brushes.Gray
            };
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AnimalEditDialog(_animal);
            if (dialog.ShowDialog() == true)
            {
                DialogResult = true;
                Close();
            }
        }

        private async void BtnAdopt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserId == 0)
            {
                MessageBox.Show("Пожалуйста, войдите в систему для подачи заявки.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы действительно хотите подать заявку на усыновление {_animal.Name}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new ShelterDbContext();
                    var application = new Core.Entities.adoptionapplication
                    {
                        animalId = _animal.Animalid,
                        userId = _currentUserId,
                        applicationdate = DateTime.UtcNow,
                        status = "Pending",
                        notes = $"Заявка на усыновление {_animal.Name}"
                    };

                    context.adoptionapplications.Add(application);
                    await context.SaveChangesAsync();

                    MessageBox.Show($"Заявка на усыновление {_animal.Name} успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при создании заявки: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}