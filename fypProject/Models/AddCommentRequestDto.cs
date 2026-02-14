using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class AddCommentRequestDto
    {

        public int PaperId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int? QuestionId { get; set; } // optional
        public string Description { get; set; } // actual comment text
    }


}