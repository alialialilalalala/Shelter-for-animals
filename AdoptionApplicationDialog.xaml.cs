using AnimalShelterAI.Core.DTOs;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimalShelterAI
{
    public partial class AdoptionApplicationDialog : Window
    {
        private readonly AnimalDto _animal;
        private readonly user _currentUser;

        public AdoptionApplicationDialog(AnimalDto animal, user currentUser)
        {
            InitializeComponent();
            _animal = animal;
            _currentUser = currentUser;

            LoadAnimalData();
            LoadUserData();
        }

        private void LoadAnimalData()
        {
            txtAnimalName.Text = _animal.Name;
            txtAnimalDetails.Text = $"{_animal.Typename ?? ""} • {_animal.Breedname ?? "Без породы"} • {(_animal.Age ?? 0)} лет";
            txtStatus.Text = GetStatusDisplay(_animal.Status);
            borderStatus.Background = GetStatusBrush(_animal.Status);

            // Загружаем фото
            if (!string.IsNullOrEmpty(_animal.Photourl) && File.Exists(_animal.Photourl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_animal.Photourl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgAnimalPhoto.Source = bitmap;
                    txtNoPhoto.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    txtNoPhoto.Visibility = Visibility.Visible;
                }
            }
            else
            {
                txtNoPhoto.Visibility = Visibility.Visible;
            }
        }

        private void LoadUserData()
        {
            txtFullName.Text = _currentUser.fullname;
            txtEmail.Text = _currentUser.email;
            txtPhone.Text = _currentUser.phone ?? "";
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

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Проверка телефона
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Пожалуйста, укажите номер телефона для связи.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите подать заявку на усыновление {_animal.Name}?\n\n" +
                "После отправки заявки с вами свяжется менеджер приюта для уточнения деталей.",
                "Подтверждение заявки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new ShelterDbContext();

                    // Формируем примечания с дополнительной информацией
                    string notes = $"Причина: {txtReason.Text}\n" +
                                   $"Условия проживания: {((ComboBoxItem)cmbLivingCondition.SelectedItem)?.Content}\n" +
                                   $"Другие животные: {((ComboBoxItem)cmbOtherPets.SelectedItem)?.Content}\n" +
                                   $"Дети: {((ComboBoxItem)cmbChildren.SelectedItem)?.Content}\n" +
                                   $"Опыт: {((ComboBoxItem)cmbExperience.SelectedItem)?.Content}\n" +
                                   $"Адрес: {txtAddress.Text}\n" +
                                   $"Доп. комментарии: {txtNotes.Text}";

                    var application = new adoptionapplication
                    {
                        animalId = _animal.Animalid,
                        userId = _currentUser.userid,
                        applicationdate = DateTime.UtcNow,
                        status = "Pending",
                        notes = notes
                    };

                    context.adoptionapplications.Add(application);
                    await context.SaveChangesAsync();

                    MessageBox.Show(
                        $"✓ Заявка на усыновление {_animal.Name} успешно создана!\n\n" +
                        "Номер заявки: " + application.applicationId + "\n" +
                        "Статус: На рассмотрении\n\n" +
                        "Менеджер свяжется с вами в ближайшее время.",
                        "Заявка отправлена",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Отменить заполнение заявки?", "Отмена",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}