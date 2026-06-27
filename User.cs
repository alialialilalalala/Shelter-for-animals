using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Добавьте эту строку в начало файла


namespace AnimalShelterAI.Core.Entities
{
    public class user
    {
        [Key]

        public int userid { get; set; }
        public string username { get; set; } = string.Empty;
        public string passwordhash { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string lastname { get; set; } = string.Empty;
        public string firstname { get; set; } = string.Empty;
        public string? middlename { get; set; }
        public string? phone { get; set; }
        public DateTime registrationdate { get; set; }
        public DateTime? lastlogindate { get; set; }
        public bool isactive { get; set; }

        public string fullname => $"{lastname} {firstname} {middlename}".Trim();

        public virtual ICollection<userrole> userroles { get; set; } = new HashSet<userrole>();
        public virtual ICollection<medicalrecord> medicalrecords { get; set; } = new HashSet<medicalrecord>();
        public virtual ICollection<vaccination> vaccinations { get; set; } = new HashSet<vaccination>();
        public virtual ICollection<adoptionapplication> applications { get; set; } = new HashSet<adoptionapplication>();
        public virtual ICollection<adoptionapplication> managedapplications { get; set; } = new HashSet<adoptionapplication>();
        public virtual ICollection<volunteertask> assignedtasks { get; set; } = new HashSet<volunteertask>();
    }

    public class role
    {
        [Key]

        public int roleid { get; set; }
        public string rolename { get; set; } = string.Empty;
        public string? description { get; set; }

        public virtual ICollection<userrole> userroles { get; set; } = new HashSet<userrole>();
    }

    public class userrole
    {
        [Key]

        public int userroleid { get; set; }
        public int userid { get; set; }
        public int roleid { get; set; }

        public virtual user? user { get; set; }
        public virtual role? role { get; set; }
    }
}