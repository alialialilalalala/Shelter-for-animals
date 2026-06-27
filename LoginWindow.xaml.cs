using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Windows;

namespace AnimalShelterAI.Views
{
    public partial class LoginWindow : Window
    {
        // Ключи реестра для хранения настроек
        private const string REGISTRY_PATH = @"Software\AnimalShelterAI";
        private const string REGISTRY_USERNAME = "Username";
        private const string REGISTRY_REMEMBER = "RememberMe";

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем сохранённые настройки из реестра
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH))
                {
                    if (key != null)
                    {
                        bool remember = Convert.ToBoolean(key.GetValue(REGISTRY_REMEMBER, false));
                        if (remember)
                        {
                            string savedUsername = key.GetValue(REGISTRY_USERNAME, "") as string;
                            txtUsername.Text = savedUsername;
                            chkRememberMe.IsChecked = true;
                        }
                        else
                        {
                            chkRememberMe.IsChecked = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnLogin.IsEnabled = false;
                btnLogin.Content = "Вход...";

                using var scope = App.ServiceProvider?.CreateScope() ?? throw new InvalidOperationException("DI не инициализирован");

                var context = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
                var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

                var user = authService.Authenticate(username, password);

                if (user != null)
                {
                    // Сохраняем настройки в реестр
                    try
                    {
                        using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH))
                        {
                            if (chkRememberMe.IsChecked == true)
                            {
                                key.SetValue(REGISTRY_USERNAME, username);
                                key.SetValue(REGISTRY_REMEMBER, true);
                            }
                            else
                            {
                                key.SetValue(REGISTRY_USERNAME, "");
                                key.SetValue(REGISTRY_REMEMBER, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
                    }

                    var mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}\nДетали: {ex.InnerException?.Message ?? "Нет"}",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ВОЙТИ";
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}