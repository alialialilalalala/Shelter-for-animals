using System;

namespace AnimalShelterAI.Core.DTOs
{
    public class Volunteertaskdto
    {
        public int Taskid { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Animalid { get; set; }
        public string? Animalname { get; set; }
        public int Volunteerid { get; set; }
        public string? Volunteername { get; set; }
        public DateTime Assigneddate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? Completeddate { get; set; }
        public string? Notes { get; set; }
        public bool IsOverdue => Status == "Pending" && DueDate < DateTime.Now;
    }

    public class Volunteertaskcreatedto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Animalid { get; set; }
        public int Volunteerid { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
    }
}