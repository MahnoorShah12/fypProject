using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class PaperQuestionsResponse
    {



        public int PaperId { get; set; }
        public string Term { get; set; }
        public string TeacherName { get; set; }
        public string PaperStatus { get; set; }
        public int TotalMarks { get; set; }
        public int NoOfQuestions { get; set; }
        public string DegreePrograms { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public string CourseCreditHours { get; set; }
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public object Questions { get; set; }




    }
}