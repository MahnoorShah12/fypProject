using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

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
                                .ToList(),

                    AssignTeacher = db.paper_Assignment
                                .Where(ca => ca.course_id == c.id && ca.session_id == session.id)
                                .Join(db.Users, ca => ca.user_id, u => u.id, (ca, u) => new {
                                    TeacherId = u.id,
                                    TeacherName = u.name
                                })
                                .GroupBy(t => t.TeacherId)
                                .Select(g => g.FirstOrDefault()) // ✅ use FirstOrDefault
                                .ToList(),


                })
                .ToList();

            return Ok(courses);
        }

        [HttpGet]
        [Route("api/AssignPaper/search_by_hod")]
        public HttpResponseMessage SearchByHod(int userId, int? sessionId = null, int page = 1, int pageSize = 10)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                // 1️⃣ Get Session (Provided OR Active)
                var session = sessionId.HasValue
                    ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
                    : db.sessions.FirstOrDefault(s => s.Active == true);

                if (session == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "No valid session found.");
                }

                // 2️⃣ Check if user is HOD and get department
                var hodDepartmentId = db.Role_Assignment
                                        .Where(r => r.user_id == userId && r.role_id == 9)
                                        .Select(r => r.department_id)
                                        .FirstOrDefault();

                if (hodDepartmentId == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden,
                        "User is not assigned as HOD.");
                }

                // 3️⃣ Filter courses of HOD department AND session assigned
                var query = db.courses
                    .Where(c => c.department_id == hodDepartmentId &&
                                db.Course_Assignment.Any(ca =>
                                    ca.course_id == c.id &&
                                    ca.session_id == session.id))
                    .Select(c => new
                    {
                        CourseId = c.id,
                        CourseName = c.title,
                        c.course_code,

                        // 🔹 Assigned Teachers (Course Assignment)
                        Teachers = db.Course_Assignment
                            .Where(ca => ca.course_id == c.id && ca.session_id == session.id)
                            .Join(db.Users,
                                  ca => ca.user_id,
                                  u => u.id,
                                  (ca, u) => new
                                  {
                                      TeacherId = u.id,
                                      TeacherName = u.name
                                  })
                            .GroupBy(t => t.TeacherId)
                            .Select(g => g.FirstOrDefault())
                            .ToList(),

                        // 🔹 Paper Assigned Teachers
                        AssignTeacher = db.paper_Assignment
                            .Where(pa => pa.course_id == c.id && pa.session_id == session.id)
                            .Join(db.Users,
                                  pa => pa.user_id,
                                  u => u.id,
                                  (pa, u) => new
                                  {
                                      TeacherId = u.id,
                                      TeacherName = u.name
                                  })
                            .GroupBy(t => t.TeacherId)
                            .Select(g => g.FirstOrDefault())
                            .ToList(),
                    });

                var totalCount = query.Count();

                var result = query
                                .OrderBy(c => c.CourseName)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = result,
                    total = totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
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



            // 3️⃣ Check if the teacher is assigned to this course in this session
            bool teacherAssigned = db.Course_Assignment.Any(ca =>
                ca.course_id == dto.CourseId &&
                ca.user_id == dto.TeacherId &&
                ca.session_id == session.id);

            if (!teacherAssigned)
                return BadRequest("This teacher is not assigned to the course in this session");



            // 2️⃣ Check if this course is already assigned in this session
            bool exists = db.paper_Assignment.Any(x =>
                x.course_id == dto.CourseId &&
                x.session_id == session.id);

            if (exists)
            {
                var exitdata = db.paper_Assignment.FirstOrDefault(x =>
                  x.course_id == dto.CourseId &&
                  x.session_id == session.id);



                db.paper_Assignment.Remove(exitdata);
            }
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
