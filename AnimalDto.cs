using System;
using System.ComponentModel.DataAnnotations;

namespace AnimalShelterAI.Core.DTOs
{
    public class AnimalDto
    {
        [Key]
        public int Animalid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Typename { get; set; }
        public string? Breedname { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }
        public string? Photourl { get; set; }
        public DateTime Admissiondate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Healthstatus { get; set; } = string.Empty;
        public bool IsAvailable => Status == "Available";

        // Свойство для отображения фото в карточке
        public bool HasPhoto => !string.IsNullOrEmpty(Photourl) && System.IO.File.Exists(Photourl);
    }

    public class AnimalCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int Typeid { get; set; }
        public int? Breedid { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public decimal? Weight { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }
        public string? Photourl { get; set; }
        public DateTime Admissiondate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Healthstatus { get; set; } = string.Empty;
    }

    public class UserDto
    {
        [Key]
        public int Userid { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime Registrationdate { get; set; }
        public bool IsActive { get; set; }
    }
}