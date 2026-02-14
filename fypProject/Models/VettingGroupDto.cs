using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class VettingGroupDto
    {
        public int SenderId { get; set; }
        public DateTime VettingDate { get; set; }
        public string VettingTime { get; set; }


        public string NameStart { get; set; }
        public string NameEnd { get; set; }
    }
}