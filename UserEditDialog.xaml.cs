using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AnimalShelterAI
{
    public partial class UserEditDialog : Window
    {
        private readonly user? _user;
        private readonly bool _isEditMode;
        private readonly AuthService _authService;

        public UserEditDialog(user? user = null)
        {
            InitializeComponent();

            _user = user;
            _isEditMode = user != null;
            _authService = new AuthService(new ShelterDbContext());

            if (_isEditMode)
            {
                txtWindowTitle.Text = "Редактирование пользователя";
                btnSave.Content = "Сохранить изменения";
                Title = "Редактирование пользователя - Приют";
                LoadUserData();
            }
            else
            {
                txtWindowTitle.Text = "Новый пользователь";
                btnSave.Content = "Создать пользователя";
                Title = "Новый пользователь - Приют";
            }
        }

        private void LoadUserData()
        {
            txtLastName.Text = _user!.lastname;
            txtFirstName.Text = _user.firstname;
            txtMiddleName.Text = _user.middlename ?? "";
            txtEmail.Text = _user.email;
            txtPhone.Text = _user.phone ?? "";
            txtUsername.Text = _user.username;
            txtUsername.IsEnabled = false; // Нельзя менять логин при редактировании

            // Выбираем роль
            var role = _user.userroles?.FirstOrDefault()?.role?.rolename;
            foreach (ComboBoxItem item in cmbRole.Items)
            {
                if (item.Tag.ToString() == role)
                {
                    cmbRole.SelectedItem = item;
                    break;
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!_isEditMode && string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!_isEditMode && txtPassword.Password.Length < 6)
                {
                    MessageBox.Show("Пароль должен быть не менее 6 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = new ShelterDbContext();

                if (_isEditMode && _user != null)
                {
                    // Редактирование существующего пользователя
                    var user = await context.users.FindAsync(_user.userid);
                    if (user == null) return;

                    user.lastname = txtLastName.Text.Trim();
                    user.firstname = txtFirstName.Text.Trim();
                    user.middlename = txtMiddleName.Text.Trim();
                    user.email = txtEmail.Text.Trim();
                    user.phone = txtPhone.Text.Trim();

                    // Смена пароля
                    if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                    {
                        user.passwordhash = _authService.ComputeSha256Hash(txtPassword.Password);
                    }

                    // Смена роли
                    var selectedRole = ((ComboBoxItem)cmbRole.SelectedItem).Tag.ToString();
                    var currentRole = user.userroles?.FirstOrDefault()?.role?.rolename;

                    if (currentRole != selectedRole)
                    {
                        // Удаляем старые роли
                        if (user.userroles != null)
                            context.userroles.RemoveRange(user.userroles);

                        // Добавляем новую роль
                        var role = await context.roles.FirstOrDefaultAsync(r => r.rolename == selectedRole);
                        if (role == null)
                        {
                            role = new role { rolename = selectedRole };
                            context.roles.Add(role);
                            await context.SaveChangesAsync();
                        }

                        context.userroles.Add(new userrole { userid = user.userid, roleid = role.roleid });
                    }

                    await context.SaveChangesAsync();
                    MessageBox.Show("Пользователь обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Создание нового пользователя
                    // Проверка на существование
                    if (await context.users.AnyAsync(u => u.username == txtUsername.Text.Trim()))
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (await context.users.AnyAsync(u => u.email == txtEmail.Text.Trim()))
                    {
                        MessageBox.Show("Пользователь с таким email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newUser = new user
                    {
                        username = txtUsername.Text.Trim(),
                        passwordhash = _authService.ComputeSha256Hash(txtPassword.Password),
                        email = txtEmail.Text.Trim(),
                        lastname = txtLastName.Text.Trim(),
                        firstname = txtFirstName.Text.Trim(),
                        middlename = txtMiddleName.Text.Trim(),
                        phone = txtPhone.Text.Trim(),
                        registrationdate = DateTime.UtcNow,
                        isactive = true
                    };

                    context.users.Add(newUser);
                    await context.SaveChangesAsync();

                    // Добавляем роль
                    var selectedRole = ((ComboBoxItem)cmbRole.SelectedItem).Tag.ToString();
                    var role = await context.roles.FirstOrDefaultAsync(r => r.rolename == selectedRole);
                    if (role == null)
                    {
                        role = new role { rolename = selectedRole };
                        context.roles.Add(role);
                        await context.SaveChangesAsync();
                    }

                    context.userroles.Add(new userrole { userid = newUser.userid, roleid = role.roleid });
                    await context.SaveChangesAsync();

                    MessageBox.Show("Пользователь создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}