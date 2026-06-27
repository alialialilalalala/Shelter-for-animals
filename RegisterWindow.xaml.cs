using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        // ДОБАВЬ ЭТОТ МЕТОД
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var firstName = txtFirstName.Text.Trim();
            var lastName = txtLastName.Text.Trim();
            var email = txtEmail.Text.Trim();
            var phone = txtPhone.Text.Trim();
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Password;
            var confirmPassword = txtConfirmPassword.Password;
            var roleItem = cmbRole.SelectedItem as ComboBoxItem;
            var roleName = roleItem?.Tag?.ToString() ?? "User";

            // Валидация
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                MessageBox.Show("Заполните имя и фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                MessageBox.Show("Введите корректный email адрес", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Введите имя пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnRegister.IsEnabled = false;
                btnRegister.Content = "Регистрация...";

                using var context = new ShelterDbContext();
                var authService = new AuthService(context);

                var existingUser = context.users.FirstOrDefault(u => u.username == username || u.email == email);
                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким именем или email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnRegister.IsEnabled = true;
                    btnRegister.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
                    return;
                }

                var role = context.roles.FirstOrDefault(r => r.rolename == roleName);
                if (role == null)
                {
                    MessageBox.Show($"Роль '{roleName}' не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnRegister.IsEnabled = true;
                    btnRegister.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
                    return;
                }

                var passwordHash = authService.ComputeSha256Hash(password);
                var user = new Core.Entities.user
                {
                    firstname = firstName,
                    lastname = lastName,
                    email = email,
                    phone = phone,
                    username = username,
                    passwordhash = passwordHash,
                    registrationdate = DateTime.Now,
                    isactive = true
                };

                context.users.Add(user);
                await context.SaveChangesAsync();

                var userRole = new Core.Entities.userrole
                {
                    userid = user.userid,
                    roleid = role.roleid
                };

                context.userroles.Add(userRole);
                await context.SaveChangesAsync();

                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nВнутренняя ошибка: {ex.InnerException.Message}";
                }
                MessageBox.Show($"Ошибка при регистрации:\n{errorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnRegister.IsEnabled = true;
                btnRegister.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
            }
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}