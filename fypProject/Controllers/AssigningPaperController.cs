using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System;

namespace fypProject.Controllers
{
    public class AssignningPaperController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        
        [HttpGet]
        [Route("api/AssignPaper/search")]
        public IHttpActionResult Search(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Ok();

            var latestSession = db.sessions.FirstOrDefault(s => s.Active == true);
            if (latestSession == null)
                return BadRequest("No active session found");

            var result = from c in db.courses
                         join ca in db.Course_Assignment on c.id equals ca.course_id
                         join u in db.Users on ca.user_id equals u.id
                         where c.title.Contains(text)
                         select new
                         {
                             CourseId = c.id,
                             CourseName = c.title,
                             TeacherId = u.id,
                             TeacherName = u.name,
                             SessionId = latestSession.id,
                             SessionName = latestSession.name
                         };

            return Ok(result.ToList());
        }

        [HttpPost]
        [Route("api/AssignPaper/assign")]
        public IHttpActionResult AssignPaper(AssignPaperDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var latestSession = db.sessions.OrderByDescending(s => s.id).FirstOrDefault();
            if (latestSession == null)
                return BadRequest("No active session found");

           
            bool exists = db.paper_Assignment.Any(x =>
                x.course_id == dto.CourseId &&
                x.session_id == latestSession.id);

            if (exists)
                return BadRequest("This course is already assigned in the current session");

          
            bool teacherAssigned = db.Course_Assignment.Any(ca =>
                ca.course_id == dto.CourseId &&
                ca.user_id == dto.TeacherId);

            if (!teacherAssigned)
                return BadRequest("This teacher is not assigned to the course");

           
            var assignment = new paper_Assignment
            {
                course_id = dto.CourseId,
                session_id = latestSession.id,
                user_id = dto.TeacherId
            };

            db.paper_Assignment.Add(assignment);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Paper assigned successfully",
                SessionId = latestSession.id,
                SessionName = latestSession.name
            });
        }
    }
}
