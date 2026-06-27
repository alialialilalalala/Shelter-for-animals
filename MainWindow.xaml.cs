using AnimalShelterAI.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AnimalShelterAI
{
    public partial class MainWindow : Window
    {
        private readonly Core.Entities.user _currentUser;
        private string _userRole = "Пользователь";

        public MainWindow(Core.Entities.user user)
        {
            InitializeComponent();
            _currentUser = user;

            // Определяем роль пользователя
            if (user.userroles != null && user.userroles.Any())
            {
                var role = user.userroles.FirstOrDefault()?.role;
                if (role != null)
                {
                    _userRole = TranslateRole(role.rolename);
                    txtUserRoleBadge.Text = role.rolename;
                }
            }

            // Приветствие
            var greeting = GetGreeting();
            var userName = user.firstname ?? user.username ?? "Пользователь";
            txtGreeting.Text = $"{greeting}, {userName}!";
            txtUserName.Text = user.fullname ?? userName;
            txtUserRole.Text = $"Роль: {_userRole}";

            // Настраиваем меню в зависимости от роли
            ConfigureMenuByRole();

            // Загружаем Dashboard по умолчанию
            NavigateToControl(new DashboardControl());
            UpdateMenuSelection(BtnDashboard);
        }

        private string GetGreeting()
        {
            var hour = DateTime.Now.Hour;
            if (hour >= 5 && hour < 12)
                return "Доброе утро";
            else if (hour >= 12 && hour < 17)
                return "Добрый день";
            else if (hour >= 17 && hour < 22)
                return "Добрый вечер";
            else
                return "Доброй ночи";
        }

        private string TranslateRole(string roleName)
        {
            return roleName switch
            {
                "Administrator" => "Администратор",
                "Manager" => "Менеджер",
                "Veterinarian" => "Ветеринар",
                "Volunteer" => "Волонтер",
                "User" => "Пользователь",
                "Employee" => "Сотрудник",
                _ => roleName
            };
        }
        

        private void ConfigureMenuByRole()
        {
            // Скрываем все кнопки по умолчанию
            BtnDashboard.Visibility = Visibility.Visible;
            BtnAnimals.Visibility = Visibility.Visible;

            // В зависимости от роли показываем/скрываем элементы меню
            if (_userRole == "Администратор")
            {
                BtnApplications.Visibility = Visibility.Visible;
                BtnTasks.Visibility = Visibility.Visible;
                BtnMedical.Visibility = Visibility.Visible;
                BtnUsers.Visibility = Visibility.Visible;
                BtnDonations.Visibility = Visibility.Visible;
                BtnReports.Visibility = Visibility.Visible;
            }
            else if (_userRole == "Менеджер")
            {
                BtnApplications.Visibility = Visibility.Visible;
                BtnTasks.Visibility = Visibility.Visible;
                BtnMedical.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnDonations.Visibility = Visibility.Collapsed;
                BtnReports.Visibility = Visibility.Visible;
            }
            else if (_userRole == "Ветеринар")
            {
                BtnApplications.Visibility = Visibility.Collapsed;
                BtnTasks.Visibility = Visibility.Collapsed;
                BtnMedical.Visibility = Visibility.Visible;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnDonations.Visibility = Visibility.Collapsed;
                BtnReports.Visibility = Visibility.Collapsed;
            }
            else if (_userRole == "Волонтер")
            {
                BtnApplications.Visibility = Visibility.Collapsed;
                BtnTasks.Visibility = Visibility.Visible;
                BtnMedical.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnDonations.Visibility = Visibility.Collapsed;
                BtnReports.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnApplications.Visibility = Visibility.Collapsed;
                BtnTasks.Visibility = Visibility.Collapsed;
                BtnMedical.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnDonations.Visibility = Visibility.Collapsed;
                BtnReports.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new DashboardControl());
            UpdateMenuSelection(BtnDashboard);
        }

        private void BtnAnimals_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new AnimalsControl(_currentUser));
            UpdateMenuSelection(BtnAnimals);
        }

        private void BtnApplications_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new ApplicationsControl(_currentUser));
            UpdateMenuSelection(BtnApplications);
        }

        private void BtnTasks_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new TasksControl(_currentUser));
            UpdateMenuSelection(BtnTasks);
        }

        private void BtnMedical_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new MedicalControl());
            UpdateMenuSelection(BtnMedical);
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new UsersControl(_currentUser));
            UpdateMenuSelection(BtnUsers);
        }

        private void BtnDonations_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new DonationsControl());
            UpdateMenuSelection(BtnDonations);
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new ReportsControl());
            UpdateMenuSelection(BtnReports);
        }

        private void NavigateToControl(UserControl control)
        {
            try
            {
                if (control == null)
                {
                    MessageBox.Show("Не удалось загрузить страницу.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MainFrame.Navigate(control);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMenuSelection(Button selectedButton)
        {
            var buttons = new[] { BtnDashboard, BtnAnimals, BtnApplications, BtnTasks,
                          BtnMedical, BtnUsers, BtnDonations, BtnReports, BtnProfile };
            foreach (var btn in buttons)
            {
                btn.Background = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                btn.FontWeight = FontWeights.Normal;
            }
            selectedButton.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            selectedButton.Foreground = Brushes.White;
            selectedButton.FontWeight = FontWeights.SemiBold;
        }
        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            NavigateToControl(new ProfileControl(_currentUser));
            UpdateMenuSelection(BtnProfile);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}