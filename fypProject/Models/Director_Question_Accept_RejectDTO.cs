using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using fypProject;
using DocumentFormat.OpenXml.Spreadsheet;
namespace fypProject.Models
{
    public class Director_Question_Accept_RejectDTO
    {
        public int PaperId { get; set; }
        public int QuestionId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } // "approved" or "reject"

    }
}