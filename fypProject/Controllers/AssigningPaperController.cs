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
        public IHttpActionResult Search(int? sessionId = null)
        {
            var session = sessionId.HasValue
                ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
                : db.sessions.FirstOrDefault(s => s.Active == true);

            if (session == null)
                return BadRequest("No valid session found");

            var courses = db.courses
                .Where(c => db.Course_Assignment.Any(ca => ca.course_id == c.id && ca.session_id == session.id))
                .Select(c => new
                {
                    CourseId = c.id,
                    CourseName = c.title,
                    Teachers = db.Course_Assignment
                                .Where(ca => ca.course_id == c.id && ca.session_id == session.id)
                                .Join(db.Users, ca => ca.user_id, u => u.id, (ca, u) => new {
                                    TeacherId = u.id,
                                    TeacherName = u.name
                                })
                                .GroupBy(t => t.TeacherId)
                                .Select(g => g.FirstOrDefault()) // ✅ use FirstOrDefault
                                .ToList()
                })
                .ToList();

            return Ok(courses);
        }




        [HttpPost]
        [Route("api/AssignPaper/assign")]
        public IHttpActionResult AssignPaper(AssignPaperDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            // 1️⃣ Get session from dto or fallback to active session
            var session = db.sessions.FirstOrDefault(s => s.id == dto.SessionId)
                          ?? db.sessions.FirstOrDefault(s => s.Active == true);

            if (session == null)
                return BadRequest("No valid session found");

            // 2️⃣ Check if this course is already assigned in this session
            bool exists = db.paper_Assignment.Any(x =>
                x.course_id == dto.CourseId &&
                x.session_id == session.id);

            if (exists)
                return BadRequest("This course is already assigned in the selected session");

            // 3️⃣ Check if the teacher is assigned to this course in this session
            bool teacherAssigned = db.Course_Assignment.Any(ca =>
                ca.course_id == dto.CourseId &&
                ca.user_id == dto.TeacherId &&
                ca.session_id == session.id);

            if (!teacherAssigned)
                return BadRequest("This teacher is not assigned to the course in this session");

            // 4️⃣ Assign the paper
            var assignment = new paper_Assignment
            {
                course_id = dto.CourseId,
                session_id = session.id,
                user_id = dto.TeacherId
            };

            db.paper_Assignment.Add(assignment);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Paper assigned successfully",
                SessionId = session.id,
                SessionName = session.name
            });
        }

    }
}
