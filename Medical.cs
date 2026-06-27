using System;
using System.ComponentModel.DataAnnotations; // Добавьте эту строку в начало файла

namespace AnimalShelterAI.Core.Entities
{
    public class medicalrecord
    {
        [Key]
        public int recordid { get; set; }
        public int animalid { get; set; }
        public int vetid { get; set; }
        public DateTime recorddate { get; set; }
        public string? diagnosis { get; set; }
        public string? treatment { get; set; }
        public string? notes { get; set; }
        public DateTime? nextvisitdate { get; set; }

        public virtual animal? animal { get; set; }
        public virtual user? vet { get; set; }
    }

    public class vaccination
    {
        [Key]

        public int vaccinationid { get; set; }
        public int animalid { get; set; }
        public string vaccinename { get; set; } = string.Empty;
        public DateTime vaccinationdate { get; set; }
        public DateTime? nextvaccinationdate { get; set; }
        public int vetid { get; set; }

        public virtual animal? animal { get; set; }
        public virtual user? vet { get; set; }
    }
}