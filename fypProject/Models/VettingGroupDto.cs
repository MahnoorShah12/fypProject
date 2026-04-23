using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class VettingGroupDto
    {
        public int? SessionId { get; set; }   // optional
        public string NameStart { get; set; }
        public string NameEnd { get; set; }

        // Make nullable to avoid default date
        public DateTime? VettingDate { get; set; }

        // Optional time
        public string VettingTime { get; set; }

        public int SenderId { get; set; }
        public List<int> TeacherIds { get; set; }
    }
}
