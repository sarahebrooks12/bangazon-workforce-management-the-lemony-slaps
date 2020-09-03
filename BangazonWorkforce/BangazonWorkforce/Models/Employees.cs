﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BangazonWorkforce.Models
{
    public class Employees
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int DepartmentId { get; set; }

        public Departments department { get; set; }

        public bool isSupervisor { get; set; }

        public Computers computer { get; set; }

        public List<TrainingPrograms> TrainingProgramList { get; set; } = new List<TrainingPrograms>();
    }
}
