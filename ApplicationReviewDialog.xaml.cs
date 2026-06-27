using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AnimalShelterAI
{
    public partial class ApplicationReviewDialog : Window
    {
        private readonly adoptionapplication _application;
        private readonly bool _canManage;
        private readonly int _managerId;
        private bool _isEditable;

        public ApplicationReviewDialog(adoptionapplication application, bool canManage = false, int managerId = 0)
        {
            InitializeComponent();
            _application = application;
            _canManage = canManage;
            _managerId = managerId;

            Loaded += ApplicationReviewDialog_Loaded;
        }

        private async void ApplicationReviewDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadApplicationData();
            ConfigureButtons();
        }

        private async Task LoadApplicationData()
        {
            using var context = new ShelterDbContext();

            var application = await context.adoptionapplications
                .Include(a => a.animal)
                    .ThenInclude(animal => animal.type)
                .Include(a => a.animal)
                    .ThenInclude(animal => animal.breed)
                .Include(a => a.user)
                .Include(a => a.manager)
                .FirstOrDefaultAsync(a => a.applicationId == _application.applicationId);

            if (application == null) return;

            // Заголовок
            txtApplicationId.Text = $"№ {application.applicationId} от {application.applicationdate:dd.MM.yyyy HH:mm}";

            // Статус
            txtStatus.Text = GetStatusDisplay(application.status);
            txtStatusBadge.Text = GetStatusDisplay(application.status);
            borderStatus.Background = GetStatusBrush(application.status);

            // Информация о животном
            if (application.animal != null)
            {
                txtAnimalName.Text = application.animal.name ?? "Неизвестно";
                txtAnimalDetails.Text = $"{application.animal.type?.typename ?? "Не указан"} • " +
                                        $"{application.animal.breed?.breedname ?? "Без породы"} • " +
                                        $"{application.animal.age ?? 0} лет";
                txtAnimalStatus.Text = $"Статус животного: {GetAnimalStatusDisplay(application.animal.status)}";

                if (application.animal.status == "Available")
                    txtAnimalStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                else
                    txtAnimalStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));

                if (!string.IsNullOrEmpty(application.animal.photourl) && File.Exists(application.animal.photourl))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(application.animal.photourl, UriKind.Absolute);
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
            }

            // Информация о заявителе
            if (application.user != null)
            {
                txtUserFullName.Text = application.user.fullname;
                txtUserEmail.Text = application.user.email;
                txtUserPhone.Text = application.user.phone ?? "Не указан";
                txtUserRegDate.Text = application.user.registrationdate.ToString("dd.MM.yyyy");

                ParseNotes(application.notes);
            }

            // Информация о менеджере
            if (application.manager != null)
            {
                borderManagerInfo.Visibility = Visibility.Visible;
                txtManagerName.Text = application.manager.fullname;
                txtDecisionDate.Text = application.decisionDate?.ToString("dd.MM.yyyy HH:mm") ?? "Не указано";

                // Показываем комментарий менеджера
                var comment = ExtractManagerComment(application.notes);
                txtManagerCommentDisplay.Text = comment ?? "Нет комментариев";
                txtManagerComment.Text = comment ?? "";
            }
        }

        private void ParseNotes(string notes)
        {
            if (string.IsNullOrEmpty(notes))
            {
                txtUserAddress.Text = "Не указан";
                txtLivingCondition.Text = "Не указано";
                txtOtherPets.Text = "Не указано";
                txtChildren.Text = "Не указано";
                txtExperience.Text = "Не указано";
                txtReason.Text = "Не указано";
                txtNotes.Text = "";
                return;
            }

            var lines = notes.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Причина:"))
                    txtReason.Text = line.Replace("Причина:", "").Trim();
                else if (line.StartsWith("Условия проживания:"))
                    txtLivingCondition.Text = line.Replace("Условия проживания:", "").Trim();
                else if (line.StartsWith("Другие животные:"))
                    txtOtherPets.Text = line.Replace("Другие животные:", "").Trim();
                else if (line.StartsWith("Дети:"))
                    txtChildren.Text = line.Replace("Дети:", "").Trim();
                else if (line.StartsWith("Опыт:"))
                    txtExperience.Text = line.Replace("Опыт:", "").Trim();
                else if (line.StartsWith("Адрес:"))
                    txtUserAddress.Text = line.Replace("Адрес:", "").Trim();
                else if (line.StartsWith("Доп. комментарии:"))
                    txtNotes.Text = line.Replace("Доп. комментарии:", "").Trim();
            }

            if (string.IsNullOrEmpty(txtUserAddress.Text))
                txtUserAddress.Text = "Не указан";
        }

        private string ExtractManagerComment(string notes)
        {
            if (string.IsNullOrEmpty(notes)) return null;

            var lines = notes.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("Комментарий менеджера:"))
                    return line.Replace("Комментарий менеджера:", "").Trim();
            }
            return null;
        }

        private void ConfigureButtons()
        {
            if (!_canManage) return;

            // Включаем редактирование комментария
            txtManagerComment.IsEnabled = true;
            btnSaveComment.Visibility = Visibility.Visible;

            // Показываем соответствующие кнопки в зависимости от статуса
            switch (_application.status)
            {
                case "Pending":
                    btnApprove.Visibility = Visibility.Visible;
                    btnReject.Visibility = Visibility.Visible;
                    break;
                case "Approved":
                    btnChangeToPending.Visibility = Visibility.Visible;
                    btnComplete.Visibility = Visibility.Visible;
                    btnReject.Visibility = Visibility.Visible;
                    break;
                case "Rejected":
                    btnChangeToPending.Visibility = Visibility.Visible;
                    break;
                case "Completed":
                    btnChangeToPending.Visibility = Visibility.Visible;
                    break;
            }
        }

        private async void BtnSaveComment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = new ShelterDbContext();

                var app = await context.adoptionapplications.FindAsync(_application.applicationId);
                if (app != null)
                {
                    // Сохраняем комментарий в notes
                    string comment = txtManagerComment.Text.Trim();
                    string currentNotes = app.notes ?? "";

                    if (currentNotes.Contains("Комментарий менеджера:"))
                    {
                        var lines = currentNotes.Split('\n').ToList();
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (lines[i].StartsWith("Комментарий менеджера:"))
                                lines[i] = $"Комментарий менеджера: {comment}";
                        }
                        app.notes = string.Join("\n", lines);
                    }
                    else
                    {
                        app.notes = currentNotes + $"\nКомментарий менеджера: {comment}";
                    }

                    await context.SaveChangesAsync();

                    MessageBox.Show("Комментарий сохранён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadApplicationData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStatus("Approved", "одобрить", "одобрена");
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStatus("Rejected", "отклонить", "отклонена");
        }

        private async void BtnChangeToApproved_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStatus("Approved", "вернуть в статус 'Одобрена'", "возвращена в статус одобренной");
        }

        private async void BtnChangeToPending_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStatus("Pending", "вернуть на рассмотрение", "возвращена на рассмотрение");
        }

        private async void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Завершить усыновление по заявке #{_application.applicationId}?\n\n" +
                $"Животное: {_application.animal?.name}\n" +
                $"Заявитель: {_application.user?.fullname}\n\n" +
                "После завершения статус животного изменится на 'Усыновлен'.",
                "Завершение усыновления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await UpdateStatus("Completed", "завершить", "завершена", true);
            }
        }

        private async Task UpdateStatus(string newStatus, string actionName, string statusName, bool updateAnimal = false)
        {
            var confirmResult = MessageBox.Show(
                $"Вы уверены, что хотите {actionName} заявку #{_application.applicationId}?",
                $"Подтверждение {actionName}",
                MessageBoxButton.YesNo,
                newStatus == "Rejected" ? MessageBoxImage.Warning : MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                using var context = new ShelterDbContext();

                var app = await context.adoptionapplications.FindAsync(_application.applicationId);
                if (app != null)
                {
                    var oldStatus = app.status;
                    app.status = newStatus;
                    app.managerid = _managerId;
                    app.decisionDate = DateTime.UtcNow;

                    // Добавляем комментарий менеджера если есть
                    if (!string.IsNullOrEmpty(txtManagerComment.Text))
                    {
                        string comment = txtManagerComment.Text.Trim();
                        string currentNotes = app.notes ?? "";

                        if (!currentNotes.Contains("Комментарий менеджера:"))
                        {
                            app.notes = currentNotes + $"\nКомментарий менеджера: {comment}";
                        }
                    }

                    // Обновляем статус животного при завершении усыновления
                    if (updateAnimal && newStatus == "Completed" && app.animalId > 0)
                    {
                        var animal = await context.animals.FindAsync(app.animalId);
                        if (animal != null && animal.status == "Reserved")
                        {
                            animal.status = "Adopted";
                        }
                    }

                    // Если возвращаем на рассмотрение, сбрасываем дату решения
                    if (newStatus == "Pending")
                    {
                        app.decisionDate = null;
                        app.managerid = null;
                    }

                    await context.SaveChangesAsync();

                    MessageBox.Show($"✓ Заявка #{_application.applicationId} {statusName}!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "Pending" => "На рассмотрении",
                "Approved" => "Одобрена",
                "Rejected" => "Отклонена",
                "Completed" => "Завершена",
                _ => status ?? "Неизвестно"
            };
        }

        private string GetAnimalStatusDisplay(string status)
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
                "Pending" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                "Approved" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                "Rejected" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                "Completed" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                _ => Brushes.Gray
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}