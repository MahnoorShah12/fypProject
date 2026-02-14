using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using ClosedXML.Excel;
using fypProject.Models;

namespace fypProject.Controllers
{
    public class CourseAssignmentController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        [HttpPost]
        [Route("api/excel/upload")]
        public IHttpActionResult UploadExcel(int sessionId)
        {
            try
            {

                var session = db.sessions.FirstOrDefault(s => s.id == sessionId);
                if (session == null)
                    return BadRequest("Invalid sessionId");

                var httpRequest = System.Web.HttpContext.Current.Request;
                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded");

                var file = httpRequest.Files[0];

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    var newAssignments = new List<Course_Assignment>();

                    foreach (var row in rows)
                    {
                        string sectionName = row.Cell(1).GetString().Trim();
                        string courseTitle = row.Cell(2).GetString().Trim();
                        string courseCode = row.Cell(3).GetString().Trim();
                        string teacherName = row.Cell(4).GetString().Trim();

                        if (string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(courseTitle) || string.IsNullOrEmpty(courseCode) || string.IsNullOrEmpty(teacherName))
                            continue;

                        var section = db.sections.FirstOrDefault(s => s.name == sectionName);
                        var course = db.courses.FirstOrDefault(c => c.title == courseTitle && c.course_code == courseCode);
                        var user = db.Users.FirstOrDefault(u => u.name == teacherName);

                        if (section == null || course == null || user == null)
                            continue;

                        bool exists = db.Course_Assignment.Any(x =>
                            x.section_id == section.id &&
                            x.course_id == course.id &&
                            x.user_id == user.id &&
                            x.session_id == sessionId
                        ) || newAssignments.Any(x =>
                            x.section_id == section.id &&
                            x.course_id == course.id &&
                            x.user_id == user.id &&
                            x.session_id == sessionId
                        );

                        if (!exists)
                        {
                            newAssignments.Add(new Course_Assignment
                            {
                                section_id = section.id,
                                session_id = sessionId,
                                course_id = course.id,
                                user_id = user.id
                            });
                        }
                    }


                    if (newAssignments.Count > 0)
                        db.Course_Assignment.AddRange(newAssignments);

                    db.SaveChanges();
                }

                return Ok("Excel uploaded successfully (duplicates skipped)");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }














        [HttpGet]
        [Route("api/courseAssignment/get_teacher/{courseId}")]
        public HttpResponseMessage GetTeachersByCourse(int courseId)
        {
            try
            {

                var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");


                var teachers = (from ca in db.Course_Assignment
                                join u in db.Users on ca.user_id equals u.id
                                where ca.course_id == courseId
                                      && ca.session_id == activeSession.id
                                select new
                                {
                                    TeacherId = u.id,
                                    TeacherName = u.name
                                }).Distinct().ToList();

                if (teachers.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No teachers found for this course in the active session");

                return Request.CreateResponse(HttpStatusCode.OK, teachers);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


















    }
}
