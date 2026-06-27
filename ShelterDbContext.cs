using AnimalShelterAI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AnimalShelterAI.Infrastructure.Data
{
    public class ShelterDbContext : DbContext
    {
        public DbSet<user> users { get; set; }
        public DbSet<role> roles { get; set; }
        public DbSet<userrole> userroles { get; set; }
        public DbSet<animal> animals { get; set; }
        public DbSet<animaltype> animaltypes { get; set; }
        public DbSet<breed> breeds { get; set; }
        public DbSet<medicalrecord> medicalrecords { get; set; }
        public DbSet<vaccination> vaccinations { get; set; }
        public DbSet<adoptionapplication> adoptionapplications { get; set; }
        public DbSet<volunteertask> volunteertasks { get; set; }
        public DbSet<donation> donations { get; set; }
        public DbSet<eventlog> events { get; set; }


        public ShelterDbContext()
        {
            // Автоматически конвертировать все DateTime в UTC перед сохранением
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        }

        public ShelterDbContext(DbContextOptions<ShelterDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                })
                .LogTo(Console.WriteLine, LogLevel.Information)
                .EnableSensitiveDataLogging();
            }
        }

     
protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Пользователи
            modelBuilder.Entity<user>(entity =>
            {
                entity.HasKey(e => e.userid);
                entity.HasIndex(e => e.username).IsUnique();
                entity.HasIndex(e => e.email).IsUnique();
                entity.Property(e => e.registrationdate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.isactive).HasDefaultValue(true);
            });

            // Роли
            modelBuilder.Entity<role>(entity =>
            {
                entity.HasKey(e => e.roleid);
                entity.HasIndex(e => e.rolename).IsUnique();
            });

            // Связь пользователей и ролей
            modelBuilder.Entity<userrole>(entity =>
            {
                entity.HasKey(e => e.userroleid);
                entity.HasOne(e => e.user)
                    .WithMany(u => u.userroles)
                    .HasForeignKey(e => e.userid)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.role)
                    .WithMany(r => r.userroles)
                    .HasForeignKey(e => e.roleid)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Животные
            modelBuilder.Entity<animal>(entity =>
            {
                entity.HasKey(e => e.animalid);
                entity.Property(e => e.status).HasDefaultValue("Quarantine");
                entity.Property(e => e.healthstatus).HasDefaultValue("Healthy");
                entity.Property(e => e.admissiondate).HasColumnType("date");

                entity.HasOne(e => e.type)
                    .WithMany(t => t.animals)
                    .HasForeignKey(e => e.typeid)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.breed)
                    .WithMany(b => b.animals)
                    .HasForeignKey(e => e.breedid)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Заявки на усыновление
            modelBuilder.Entity<adoptionapplication>(entity =>
            {
                entity.HasKey(e => e.applicationId);
                entity.Property(e => e.applicationdate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.status).HasDefaultValue("Pending");

                entity.HasOne(e => e.animal)
                    .WithMany(a => a.adoptionapplications)
                    .HasForeignKey(e => e.animalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.user)
                    .WithMany(u => u.applications)
                    .HasForeignKey(e => e.userId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.manager)
                    .WithMany(u => u.managedapplications)
                    .HasForeignKey(e => e.managerid)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<adoptionapplication>(entity =>
            {
                entity.ToTable("adoptionapplications");
                entity.HasKey(e => e.applicationId);
                entity.Property(e => e.applicationId).HasColumnName("applicationid");
                entity.Property(e => e.animalId).HasColumnName("animalid");
                entity.Property(e => e.userId).HasColumnName("userid");
                entity.Property(e => e.applicationdate).HasColumnName("applicationdate");
                entity.Property(e => e.status).HasColumnName("status");
                entity.Property(e => e.notes).HasColumnName("notes");
                entity.Property(e => e.managerid).HasColumnName("managerid");
                entity.Property(e => e.decisionDate).HasColumnName("decisiondate");
            });

            // Задачи волонтёров — КЛЮЧЕВАЯ ОШИБКА ЗДЕСЬ
            modelBuilder.Entity<volunteertask>(entity =>
            {
                entity.ToTable("volunteertasks"); // Имя таблицы точно как в БД

                entity.HasKey(e => e.taskid);

                entity.Property(e => e.taskid).HasColumnName("taskid");
                entity.Property(e => e.title).HasColumnName("title");
                entity.Property(e => e.description).HasColumnName("description");
                entity.Property(e => e.animalid).HasColumnName("animalid");
                entity.Property(e => e.volunteerid).HasColumnName("volunteerid");
                entity.Property(e => e.assigneddate).HasColumnName("assigneddate");
                entity.Property(e => e.duedate).HasColumnName("duedate");
                entity.Property(e => e.status).HasColumnName("status");
                entity.Property(e => e.completeddate).HasColumnName("completeddate"); // ← важно! в БД completeddate
                entity.Property(e => e.notes).HasColumnName("notes");

                // Связи (опционально, но полезно)
                entity.HasOne(e => e.animal)
                      .WithMany(a => a.volunteertasks)
                      .HasForeignKey(e => e.animalid)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.volunteer)
                      .WithMany(u => u.assignedtasks)
                      .HasForeignKey(e => e.volunteerid)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            

            // Пожертвования — КЛЮЧЕВАЯ ОШИБКА ЗДЕСЬ
            modelBuilder.Entity<donation>(entity =>
            {
                entity.ToTable("donations");
                entity.HasKey(e => e.donationid);
                entity.Property(e => e.donationid).HasColumnName("donationid");
                entity.Property(e => e.donorname).HasColumnName("donorname");
                entity.Property(e => e.amount).HasColumnName("amount");
                entity.Property(e => e.donationDate).HasColumnName("donationdate"); // ← Вот где ошибка!
                entity.Property(e => e.donationtype).HasColumnName("donationtype");
                entity.Property(e => e.notes).HasColumnName("notes");
                entity.Property(e => e.isanonymous).HasColumnName("isanonymous");
            });
        }
    }
}