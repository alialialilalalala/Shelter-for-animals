using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnimalShelterAI.Core.Entities;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnimalShelterAI
{
    public partial class UsersControl : UserControl
    {
        private List<UserItem> users = new List<UserItem>();
        private user? _currentUser;
        private bool _canManage;

        public UsersControl(user? currentUser = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _canManage = currentUser != null && IsUserCanManage(currentUser);

            // Скрываем кнопку добавления если нет прав
            BtnAdd.Visibility = _canManage ? Visibility.Visible : Visibility.Collapsed;

            Loaded += UsersControl_Loaded;
        }

        private bool IsUserCanManage(user user)
        {
            if (user.userroles == null) return false;

            var roleNames = user.userroles
                .Where(ur => ur.role != null)
                .Select(ur => ur.role!.rolename)
                .ToList();

            return roleNames.Contains("Администратор");
        }

        private async void UsersControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsers();
        }

        private async System.Threading.Tasks.Task LoadUsers()
        {
            try
            {
                using var context = new ShelterDbContext();

                var dbUsers = await context.users
                    .Include(u => u.userroles)
                        .ThenInclude(ur => ur.role)
                    .OrderBy(u => u.lastname)
                    .ThenBy(u => u.firstname)
                    .ToListAsync();

                users = dbUsers.Select(u => new UserItem
                {
                    UserId = u.userid,
                    Username = u.username ?? "Не указан",
                    FullName = GetFullName(u),
                    Email = u.email ?? "Не указан",
                    Phone = u.phone ?? "Не указан",
                    Role = GetMainRole(u.userroles),
                    Status = u.isactive ? "Активен" : "Заблокирован",
                    StatusColor = u.isactive ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(231, 76, 60))
                }).ToList();

                UsersGrid.ItemsSource = users;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LoadTestData();
            }
        }

        private string GetFullName(user u)
        {
            var parts = new[] { u.lastname, u.firstname, u.middlename }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim());
            var result = string.Join(" ", parts);
            return string.IsNullOrEmpty(result) ? "Не указано" : result;
        }

        private string GetMainRole(ICollection<userrole> userroles)
        {
            if (userroles == null || !userroles.Any())
                return "Пользователь";

            var roleOrder = new Dictionary<string, int>
            {
                { "Администратор", 1 },
                { "Менеджер", 2 },
                { "Ветеринар", 3 },
                { "Волонтер", 4 },
                { "Сотрудник", 5 },
                { "Пользователь", 6 }
            };

            var mainRole = userroles
                .Select(ur => ur.role?.rolename)
                .Where(r => !string.IsNullOrEmpty(r))
                .OrderBy(r => roleOrder.ContainsKey(r) ? roleOrder[r] : 99)
                .FirstOrDefault();

            return mainRole ?? "Пользователь";
        }

        private void LoadTestData()
        {
            users = new List<UserItem>
            {
                new UserItem { UserId = 1, Username = "admin", FullName = "Плюхина Алиса Алексеевна", Email = "admin@shelter.ru", Phone = "+7-999-123-45-67", Role = "Администратор", Status = "Активен", StatusColor = Brushes.Green },
                new UserItem { UserId = 2, Username = "manager", FullName = "Смирнов Алексей", Email = "manager@shelter.ru", Phone = "+7-999-234-56-78", Role = "Менеджер", Status = "Активен", StatusColor = Brushes.Green },
                new UserItem { UserId = 3, Username = "vet", FullName = "Сидоров Андрей", Email = "vet@shelter.ru", Phone = "+7-999-345-67-89", Role = "Ветеринар", Status = "Активен", StatusColor = Brushes.Green },
                new UserItem { UserId = 4, Username = "volunteer", FullName = "Кузнецова Мария", Email = "volunteer@shelter.ru", Phone = "+7-999-456-78-90", Role = "Волонтер", Status = "Активен", StatusColor = Brushes.Green }
            };
            UsersGrid.ItemsSource = users;
        }

        private void ApplyFilter()
        {
            if (users == null || UsersGrid == null) return;

            var filtered = users.AsEnumerable();

            // Поиск по тексту
            if (!string.IsNullOrWhiteSpace(TxtSearch?.Text))
            {
                var searchTerm = TxtSearch.Text.ToLower();
                filtered = filtered.Where(u =>
                    (u.FullName?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.Username?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.Email?.ToLower().Contains(searchTerm) ?? false) ||
                    (u.Phone?.ToLower().Contains(searchTerm) ?? false));
            }

            // Фильтр по роли
            if (CmbRoleFilter?.SelectedItem is ComboBoxItem roleItem && roleItem.Content.ToString() != "Все роли")
            {
                filtered = filtered.Where(u => u.Role == roleItem.Content.ToString());
            }

            UsersGrid.ItemsSource = filtered.ToList();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CmbRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                using var context = new ShelterDbContext();
                var user = await context.users
                    .Include(u => u.userroles)
                        .ThenInclude(ur => ur.role)
                    .FirstOrDefaultAsync(u => u.userid == id);

                if (user != null)
                {
                    var dialog = new UserEditDialog(user);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadUsers();
                    }
                }
            }
        }

        private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                // Не даем заблокировать самого себя
                if (_currentUser != null && id == _currentUser.userid)
                {
                    MessageBox.Show("Вы не можете изменить статус своей учетной записи.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using var context = new ShelterDbContext();
                    var user = await context.users.FindAsync(id);
                    if (user != null)
                    {
                        var newStatus = !user.isactive;
                        var action = newStatus ? "активировать" : "заблокировать";

                        var result = MessageBox.Show($"Вы уверены, что хотите {action} пользователя?",
                            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            user.isactive = newStatus;
                            await context.SaveChangesAsync();
                            await LoadUsers();
                            MessageBox.Show($"Пользователь {action}н.", "Успех");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int id)
            {
                // Не даем удалить самого себя
                if (_currentUser != null && id == _currentUser.userid)
                {
                    MessageBox.Show("Вы не можете удалить свою учетную запись.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Удалить пользователя?\nЭто действие нельзя отменить.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = new ShelterDbContext();
                        var user = await context.users
                            .Include(u => u.userroles)
                            .FirstOrDefaultAsync(u => u.userid == id);

                        if (user != null)
                        {
                            context.userroles.RemoveRange(user.userroles);
                            context.users.Remove(user);
                            await context.SaveChangesAsync();
                            await LoadUsers();
                            MessageBox.Show("Пользователь удалён.", "Успех");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка");
                    }
                }
            }
        }

        private void UsersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserItem selectedUser)
            {
                BtnEdit_Click(sender, new RoutedEventArgs());
            }
        }

        public class UserItem
        {
            public int UserId { get; set; }
            public string Username { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Role { get; set; } = "";
            public string Status { get; set; } = "";
            public Brush StatusColor { get; set; } = Brushes.Gray;
        }
    }
}