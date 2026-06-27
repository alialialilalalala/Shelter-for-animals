using System;
using System.ComponentModel.DataAnnotations; // Добавьте эту строку в начало файла


namespace AnimalShelterAI.Core.DTOs
{
    public class AdoptionApplicationDto
    {
        [Key]

        public int ApplicationId { get; set; }
        public int AnimalId { get; set; }
        public string? Animalname { get; set; }
        public string? Animaltype { get; set; }
        public int Userid { get; set; }
        public string? Userfullname { get; set; }
        public DateTime Applicationdate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int? Managerid { get; set; }
        public string? Managername { get; set; }
        public DateTime? Decisiondate { get; set; }
    }

    public class AdoptionApplicationCreateDto
    {
        [Key]


        public int Animalid { get; set; }
        public int Userid { get; set; }
        public string? Notes { get; set; }
    }
}