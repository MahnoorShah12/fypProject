using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class SwapQuestionsRequest
    {


        public int PaperId { get; set; }
        public int QuestionId1 { get; set; }
        public int QuestionId2 { get; set; }


    }

}