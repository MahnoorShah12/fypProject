using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class DifficultyPolicyDTO
    {
        public string Term { get; set; }
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Tough { get; set; }
        public int? session_id { get; set; }

    }
}