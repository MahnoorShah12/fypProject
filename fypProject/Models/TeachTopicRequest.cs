using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class TeachTopicRequest
    {
        public int? section_id { get; set; }   // nullable
        public int? course_id { get; set; }    // nullable
        public int? user_id { get; set; }      // nullable
        public int? topic_id { get; set; }     // nullable
        public int? session_id { get; set; }   // nullable
    }


}