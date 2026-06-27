using System;
using System.ComponentModel.DataAnnotations; // Добавьте эту строку в начало файла


namespace AnimalShelterAI.Core.Entities
{
    public class adoptionapplication
    {
        [Key]

        public int applicationId { get; set; }
        public int animalId { get; set; }
        public int userId { get; set; }
        public DateTime applicationdate { get; set; }
        public string status { get; set; } = "Pending";
        public string? notes { get; set; }
        public int? managerid { get; set; }
        public DateTime? decisionDate { get; set; }

        public virtual animal? animal { get; set; }
        public virtual user? user { get; set; }
        public virtual user? manager { get; set; }
    }

    public class volunteertask
    {
        [Key]

        public int taskid { get; set; }
        public string title { get; set; } = string.Empty;
        public string? description { get; set; }
        public int? animalid { get; set; }
        public int volunteerid { get; set; }
        public DateTime assigneddate { get; set; }
        public DateTime? duedate { get; set; }
        public string status { get; set; } = "Pending";
        public DateTime? completeddate { get; set; }
        public string? notes { get; set; }

        public virtual animal? animal { get; set; }
        public virtual user? volunteer { get; set; }
    }

    public class donation
    {
        [Key]

        public int donationid { get; set; }
        public string? donorname { get; set; }
        public decimal amount { get; set; }
        public DateTime donationDate { get; set; }
        public string? donationtype { get; set; }
        public string? notes { get; set; }
        public bool isanonymous { get; set; }
    }

    public class eventlog
    {
        [Key]

        public int eventid { get; set; }
        public int? userid { get; set; }
        public string eventtype { get; set; } = string.Empty;
        public string? description { get; set; }
        public DateTime eventdate { get; set; }
        public string? ipaddress { get; set; }

        public virtual user? user { get; set; }
    }
}