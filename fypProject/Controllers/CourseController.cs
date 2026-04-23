using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;
using System.Web.Helpers;


using System.Text.RegularExpressions;
using System.Data.Entity.Migrations;
namespace fypProject.Controllers
{
    public class CourseController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();




        [HttpPost]
        [Route("api/course/add-Course")]
        public HttpResponseMessage AddCourse([FromBody] course request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                if (string.IsNullOrWhiteSpace(request.course_code) ||
                    string.IsNullOrWhiteSpace(request.title) ||
                    string.IsNullOrWhiteSpace(request.credit_hours) ||
                    request.department_id == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "course_code, title, credit_hours, and department_id are required");
                }

                if (db.courses.Any(u => u.course_code == request.course_code))
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Course Code already exists");

                var CourseCodePattern = @"^[A-Z]{2,4}-[0-9]{3}$";
                if (!Regex.IsMatch(request.course_code, CourseCodePattern))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Course Code must be in the format: CS-101");

                var CreditCodePattern = @"^[0-9]\(\d-\d\)$";
                if (!Regex.IsMatch(request.credit_hours, CreditCodePattern))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Credit hours must be in the format: 4(2-2)");

                db.courses.Add(request);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Course added successfully",
                    CourseId = request.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("api/course/get_all_courses")]
        public HttpResponseMessage GetCourses(int page = 1, int pageSize = 10)
        {
            try
            {
                // Validate page and pageSize
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                // Select required fields
                var query = db.courses
                              .Select(c => new
                              {
                                  c.id,
                                  c.course_code,
                                  c.title,
                                  c.credit_hours
                              });

                // Total count
                var totalCount = query.Count();

                // Pagination
                var courses = query
                                .OrderBy(c => c.title) // Corrected variable: t -> c
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                // Return response
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = courses,
                    total = totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                // Return error (you can log ex.Message if needed)
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }







        [HttpGet]
        [Route("api/course/get_all_courses_of_Hod")]
        public HttpResponseMessage GetCoursesOfHod(int userId, int page = 1, int pageSize = 10)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                // 1️⃣ Check if user is HOD
                var hodDepartmentId = db.Role_Assignment
                                        .Where(r => r.user_id == userId && r.role_id == 9)
                                        .Select(r => r.department_id)
                                        .FirstOrDefault();

                if (hodDepartmentId == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden,
                        "User is not assigned as HOD.");
                }

                // 2️⃣ Filter courses
                //var query = db.courses
                //              .Where(c => c.department_id == hodDepartmentId)
                //              .Select(c => new
                //              {
                //                  c.id,
                //                  c.course_code,
                //                  c.title,
                //                  c.credit_hours
                //              });
                // 2️⃣ Filter courses including common courses
                var query = db.courses
                              .Where(c => c.department_id == hodDepartmentId || c.department_id == null)
                              .Select(c => new
                              {
                                  c.id,
                                  c.course_code,
                                  c.title,
                                  c.credit_hours
                              });

                var totalCount = query.Count();

                var courses = query
                                .OrderBy(c => c.title)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = courses,
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
        [Route("api/course/edit_Course_data")]
        public HttpResponseMessage EditTeacherData([FromBody] course request)
        {
            try
            {
                if (request == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");
                }

                var course = db.courses.FirstOrDefault(u => u.id == request.id);
                if (course == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Course not found");
                }

                if (string.IsNullOrWhiteSpace(request.course_code) ||
                    string.IsNullOrWhiteSpace(request.title) ||
                    string.IsNullOrWhiteSpace(request.credit_hours) ||
                    request.department_id == 0) // check department
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "course_code, title, credit_hours, and department_id are required");
                }

                // Check duplicate course code for other courses
                if (db.courses.Any(u => u.course_code == request.course_code && u.id != request.id))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Course Code already exists");
                }

                var CourseCodePattern = @"^[A-Z]{2,4}-[0-9]{3}$";
                if (!Regex.IsMatch(request.course_code, CourseCodePattern))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Course Code must be in the format: CS-101");
                }

                var CreditCodePattern = @"^[0-9]\(\d-\d\)$";
                if (!Regex.IsMatch(request.credit_hours, CreditCodePattern))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Credit hours must be in the format: 4(2-2)");
                }

                // UPDATE COURSE DATA
                course.course_code = request.course_code.Trim();
                course.title = request.title.Trim();
                course.credit_hours = request.credit_hours.Trim();
                course.department_id = request.department_id; // NEW: update department

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Course data updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }







        [HttpPost]
        [Route("api/course/delete_course/{id}")]
        public HttpResponseMessage DeleteCourse(int id)
        {
            try
            {
                var course = db.courses.FirstOrDefault(c => c.id == id);
                if (course == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Course not found");
                }

                db.courses.Remove(course);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Course deleted successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }





        [HttpGet]
        [Route("api/course/isTeacherAssigned")]
        public HttpResponseMessage IsTeacherAssigned(int course_id, int user_id, int? session_id = null)
        {
            try
            {
                if (user_id <= 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, false);

                // Determine the session
                int activeSessionId;
                if (session_id == null)
                {
                    var session = db.sessions.FirstOrDefault(s => s.Active == true);
                    if (session == null)
                        return Request.CreateResponse(HttpStatusCode.OK, false); // no active session
                    activeSessionId = session.id;
                }
                else
                {
                    activeSessionId = session_id.Value;
                }

                // Check if teacher is assigned to this course in this session
                bool isAssigned = db.Course_Assignment
                    .Any(a => a.course_id == course_id && a.user_id == user_id && a.session_id == activeSessionId);

                return Request.CreateResponse(HttpStatusCode.OK, isAssigned);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, false);
            }
        }





































        [HttpGet]
        [Route("api/course/get_course_details/{courseId}")]
        public IHttpActionResult GetCourseDetails(int courseId)
        {
            var course = db.courses
                .Where(c => c.id == courseId)
                .Select(c => new
                {
                    c.title,
                    c.course_code
                })
                .FirstOrDefault();

            if (course == null)
                return NotFound();

            return Ok(course);
        }








        // GET: api/course/get_teacher_sections?course_id=1&user_id=5&session_id=2
        [HttpGet]
        [Route("api/course/get_teacher_sections")]
        public IHttpActionResult GetTeacherSections(int course_id, int user_id, int? session_id = null)
        {
            try
            {
                // Use provided session_id or default to active session
                var sessionIdToUse = session_id ?? db.sessions
                                                .Where(s => s.Active == true)
                                                .Select(s => s.id)
                                                .FirstOrDefault();



                // Get sections assigned to teacher for this course and session
                var sectionIds = db.Course_Assignment
                    .Where(ca => ca.course_id == course_id && ca.user_id == user_id && ca.session_id == sessionIdToUse)
                    .Select(ca => ca.section_id)
                    .ToList();

                var sections = db.sections
                    .Where(s => sectionIds.Contains(s.id))
                    .Select(s => new { s.id, s.name })
                    .ToList();

                return Ok(sections);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
































        [HttpGet]
        [Route("api/course/search")]
        public IHttpActionResult SearchCourses(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return Ok(new List<course>()); // return empty list if no search term
            }

            // Case-insensitive search on course name or code
            var courses = db.courses
                            .Where(c => c.title.Contains(search) || c.course_code.Contains(search))
                            .Select(c => new
                            {
                                c.id,
                                c.title,
                                c.course_code,
                                c.credit_hours
                            })
                            .ToList();

            return Ok(courses);
        }



















        [HttpGet]
        [Route("api/course/searchForHod")]
        public HttpResponseMessage SearchCoursesForHod(string search, int userId)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                // return empty list correctly
                return Request.CreateResponse(HttpStatusCode.OK, new List<course>());
            }

            // Get HOD department
            var hodDepartmentId = db.Role_Assignment
                                    .Where(r => r.user_id == userId && r.role_id == 9)
                                    .Select(r => r.department_id)
                                    .FirstOrDefault();

            if (hodDepartmentId == 0)
                return Request.CreateResponse(HttpStatusCode.Forbidden,
                       "User is not assigned as HOD.");

            // Filter courses by department and search
            var courses = db.courses
                            .Where(c => c.department_id == hodDepartmentId &&
                                        (c.title.Contains(search) || c.course_code.Contains(search)))
                            .Select(c => new
                            {
                                c.id,
                                c.title,
                                c.course_code,
                                c.credit_hours
                            })
                            .ToList();

            return Request.CreateResponse(HttpStatusCode.OK, courses);
        }





        // GET: api/department/get_all_departments
        [HttpGet]
        [Route("api/course/get_all_departments")]
        public HttpResponseMessage GetAllDepartments()
        {
            try
            {
                var departments = db.departments
                                    .Select(d => new
                                    {
                                        d.id,
                                        d.name
                                    })
                                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, departments);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




    }






}
