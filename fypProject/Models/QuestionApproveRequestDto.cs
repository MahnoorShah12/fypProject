using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class QuestionApproveRequestDto
    {
        public int PaperId { get; set; }
        public int QuestionId { get; set; } // position in paper
        public int UserId { get; set; }
        public string Status { get; set; } // approved or reject

    }
}