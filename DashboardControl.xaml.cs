using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using AnimalShelterAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnimalShelterAI
{
    public partial class DashboardControl : UserControl
    {
        public DashboardControl()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                using var context = new ShelterDbContext();
                
                // Загружаем статистику
                var totalAnimals = await context.animals.CountAsync();
                var availableAnimals = await context.animals
                    .Where(a => a.status == "Available" || a.status == "Ищет дом")
                    .CountAsync();
                var totalApplications = await context.adoptionapplications.CountAsync();
                var totalTasks = await context.volunteertasks
                    .Where(t => t.status == "Pending" || t.status == "В процессе")
                    .CountAsync();

                TotalAnimals.Text = totalAnimals.ToString();
                AvailableAnimals.Text = availableAnimals.ToString();
                TotalApplications.Text = totalApplications.ToString();
                TotalTasks.Text = totalTasks.ToString();

                // Загружаем последних животных
                var recentAnimals = await context.animals
                    .Include(a => a.type)
                    .Include(a => a.breed)
                    .OrderByDescending(a => a.admissiondate)
                    .Take(10)
                    .ToListAsync();

                var animalItems = recentAnimals.Select(a => new Animalitem
                {
                    Name = a.name ?? "Без имени",
                    Type = a.type?.typename ?? "Не указан",
                    Breed = a.breed?.breedname ?? "Не указана",
                    Gender = a.gender ?? "?",
                    Age = a.age ?? 0,
                    Status = a.status ?? "Не указан"
                }).ToList();

                AnimalsGrid.ItemsSource = animalItems;
            }
            catch (System.Data.Common.DbException)
            {
                // Если база данных недоступна, используем тестовые данные
                LoadTestData();
            }
            catch (Exception)
            {
                // Если произошла другая ошибка, используем тестовые данные
                LoadTestData();
            }
        }

        private void LoadTestData()
        {
            var animals = new List<Animalitem>
            {
                new Animalitem { Name = "Барсик", Type = "Кошка", Breed = "Дворовая", Gender = "М", Age = 2, Status = "Ищет дом" },
                new Animalitem { Name = "Шарик", Type = "Собака", Breed = "Дворняга", Gender = "М", Age = 3, Status = "Ищет дом" },
                new Animalitem { Name = "Мурка", Type = "Кошка", Breed = "Сиамская", Gender = "Ж", Age = 1, Status = "Карантин" },
                new Animalitem { Name = "Рекс", Type = "Собака", Breed = "Овчарка", Gender = "М", Age = 5, Status = "Забронирован" },
                new Animalitem { Name = "Пушистик", Type = "Кролик", Breed = "Декоративный", Gender = "Ж", Age = 1, Status = "Ищет дом" }
            };

            AnimalsGrid.ItemsSource = animals;
            TotalAnimals.Text = "5";
            AvailableAnimals.Text = "3";
            TotalApplications.Text = "2";
            TotalTasks.Text = "1";
        }

        public class Animalitem
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public string Breed { get; set; } = "";
            public string Gender { get; set; } = "";
            public int Age { get; set; }
            public string Status { get; set; } = "";
        }
    }
}