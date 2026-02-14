using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class DirectorAlertController : ApiController
    {
        private readonly DirectorDashboardEntities db = new DirectorDashboardEntities();

      
        [HttpPost]
        [Route("api/DirectorAlert/submission-period/send")]
        public IHttpActionResult SendSubmissionPeriod([FromBody] SubmissionPeriodDto model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            
            var period = new submission_period
            {
                session_id = model.SessionId,
                start_date = model.StartDate,
                end_date = model.EndDate
            };

            db.submission_period.Add(period);

            var role = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
            if (role == null)
                return BadRequest("Faculty role not found");

     
            var teacherIds = db.Role_Assignment
                               .Where(r => r.role_id == role.id)
                               .Select(r => r.user_id)
                               .ToList();

            foreach (var tid in teacherIds)
            {
                var alert = new Alert
                {
                    sender_id = model.SenderId,
                    reciever_id = tid,
                 // set to null if no specific paper
                    description = $"Submission Period: {model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}"
                };
                db.Alerts.Add(alert);
            }

            db.SaveChanges();

            return Ok("Submission period alert sent to all teachers successfully");
        }

     
        [HttpGet]
        [Route("api/DirectorAlert/teachers/by-alphabet")]
        public IHttpActionResult GetTeachersByAlphabet(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
                return BadRequest("Start and End alphabet required");

            var role = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
            if (role == null)
                return BadRequest("Faculty role not found");

            var teachers = db.Users
                             .Join(db.Role_Assignment,
                                   u => u.id,
                                   r => r.user_id,
                                   (u, r) => new { u, r })
                             .Where(x => x.r.role_id == role.id &&
                                         !string.IsNullOrEmpty(x.u.name) &&
                                         string.Compare(x.u.name.Substring(0, 1), start, true) >= 0 &&
                                         string.Compare(x.u.name.Substring(0, 1), end, true) <= 0)
                             .OrderBy(x => x.u.name) 
                             .Select(x => new
                             {
                                 x.u.id,
                                 x.u.name,
                                 x.u.email
                             })
                             .ToList();

            return Ok(teachers);
        }

        
        [HttpPost]
        [Route("api/DirectorAlert/vetting/assign-group")]
        public IHttpActionResult AssignVettingToGroup([FromBody] VettingGroupDto model)
        {
            if (model == null)
                return BadRequest("Invalid request");

            // Get Faculty role
            var role = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
            if (role == null)
                return BadRequest("Faculty role not found");

            // Filter teachers by alphabet range
            var teacherIds = db.Users
                               .Join(db.Role_Assignment, u => u.id, r => r.user_id, (u, r) => new { u, r })
                               .Where(x => x.r.role_id == role.id &&
                                           !string.IsNullOrEmpty(x.u.name) &&
                                           string.Compare(x.u.name.Substring(0, 1), model.NameStart, true) >= 0 &&
                                           string.Compare(x.u.name.Substring(0, 1), model.NameEnd, true) <= 0)
                               .Select(x => x.u.id)
                               .ToList();

            if (teacherIds.Count == 0)
                return BadRequest("No teachers found for the given criteria");

            foreach (var tid in teacherIds)
            {
                // Parse VettingTime safely
                if (!TimeSpan.TryParse(model.VettingTime, out TimeSpan vettingTime))
                    return BadRequest("Invalid vetting time format");

                var vetting = new Vetting_Timming_Assignment
                {
                    user_id = tid,
                    Vetting_Date = model.VettingDate,
                    Vetting_Time = vettingTime
                };
                db.Vetting_Timming_Assignment.Add(vetting);

                var alert = new Alert
                {
                    sender_id = model.SenderId,
                    reciever_id = tid,
                
                    description = $"Vetting Date: {model.VettingDate:yyyy-MM-dd} | Time: {model.VettingTime}"
                };
                db.Alerts.Add(alert);
            }

            db.SaveChanges();

            return Ok("Vetting alerts sent successfully");
        }
    }
}
