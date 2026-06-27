using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Добавьте эту строку в начало файла


namespace AnimalShelterAI.Core.Entities
{
    public class animal
    {
        [Key]

        public int animalid { get; set; }
        public string name { get; set; } = string.Empty;
        public int typeid { get; set; }
        public int? breedid { get; set; }
        public string? gender { get; set; }
        public int? age { get; set; }
        public decimal? weight { get; set; }
        public string? color { get; set; }
        public string? description { get; set; }
        public string? photourl { get; set; }
        public DateTime admissiondate { get; set; }
        public string status { get; set; } = "Quarantine";
        public string healthstatus { get; set; } = "Healthy";

        public virtual animaltype? type { get; set; }
        public virtual breed? breed { get; set; }
        public virtual ICollection<medicalrecord> medicalrecords { get; set; } = new HashSet<medicalrecord>();
        public virtual ICollection<vaccination> vaccinations { get; set; } = new HashSet<vaccination>();
        public virtual ICollection<adoptionapplication> adoptionapplications { get; set; } = new HashSet<adoptionapplication>();
        public virtual ICollection<volunteertask> volunteertasks { get; set; } = new HashSet<volunteertask>();
    }

    public class animaltype
    {
        [Key]
        public int typeid { get; set; }
        public string typename { get; set; } = string.Empty;
        public string? description { get; set; }

        public virtual ICollection<animal> animals { get; set; } = new HashSet<animal>();
        public virtual ICollection<breed> breeds { get; set; } = new HashSet<breed>();
    }

    public class breed
    {
        [Key]

        public int breedid { get; set; }
        public string breedname { get; set; } = string.Empty;
        public int typeid { get; set; }
        public string? description { get; set; }

        public virtual animaltype? type { get; set; }
        public virtual ICollection<animal> animals { get; set; } = new HashSet<animal>();
    }
}