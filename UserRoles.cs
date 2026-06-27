using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimalShelterAI.Core.Enums
{
    public enum UserRole
    {
        Administrator = 1,
        Manager = 2,
        Veterinarian = 3,
        Volunteer = 4,
        User = 5,
        Employee = 6
    }

    public enum AnimalStatus
    {
        Quarantine = 1,
        Available = 2,
        Reserved = 3,
        Adopted = 4,
        Treatment = 5
    }

    public enum HealthStatus
    {
        Healthy = 1,
        Sick = 2,
        Recovering = 3,
        Chronic = 4
    }

    public enum TaskStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum ApplicationStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Completed = 4
    }

    public enum DonationType
    {
        Money = 1,
        Food = 2,
        Medicine = 3,
        Other = 4
    }
}