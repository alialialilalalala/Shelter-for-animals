using AnimalShelterAI.Core.Interfaces;
using AnimalShelterAI.Infrastructure.Data;
using AnimalShelterAI.Infrastructure.Repositories;
using AnimalShelterAI.Services;
using AnimalShelterAI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace AnimalShelterAI
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {

                var services = new ServiceCollection();

                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var config = configBuilder.Build();

                services.AddDbContext<ShelterDbContext>(options =>
                {
                    var connString = config.GetConnectionString("DefaultConnection");
                    options.UseNpgsql(connString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                });

                // Репозитории
                services.AddScoped<IAnimalRepository, AnimalRepository>();
                services.AddScoped<IAdoptionApplicationRepository, AdoptionApplicationRepository>();
                services.AddScoped<IUserRepository, UserRepository>();

                // Сервисы
                services.AddScoped<AnimalService>();
                services.AddScoped<AuthService>();

                ServiceProvider = services.BuildServiceProvider();

                var loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска приложения:\n{ex.Message}\n\nДетали: {ex.InnerException?.Message ?? "Нет"}",
                    "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}