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
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");
                }


                if (string.IsNullOrWhiteSpace(request.course_code) ||
                    string.IsNullOrWhiteSpace(request.title) ||
                    string.IsNullOrWhiteSpace(request.credit_hours))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "course_code,title and credit_hours are required");
                }


                if (db.courses.Any(u => u.course_code == request.course_code))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Course Code already exists");
                }




                var CourseCodePattern = @"^[A-Z]{2,4}-[0-9]{3}$";
                if (!Regex.IsMatch(request.course_code, CourseCodePattern))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Course Code must be in the format: CS-101");
                }

                //var CreditCodePattern = @"^[0-9]\(\d-\d\)$";
                //if (!Regex.IsMatch(request.credit_hours, CreditCodePattern))
                //{
                //    return Request.CreateResponse(HttpStatusCode.BadRequest, "Credit hours must be in the format: 4(2-2)");
                //}


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
        //[HttpPost]
        //[Route("api/course/add-Course")]
        //public HttpResponseMessage AddCourse([FromBody] course request)
        //{
        //    try
        //    {
        //        if (request == null)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

        //        if (string.IsNullOrWhiteSpace(request.course_code) ||
        //            string.IsNullOrWhiteSpace(request.title) ||
        //            string.IsNullOrWhiteSpace(request.credit_hours))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "course_code, title and credit_hours are required");
        //        }

        //        if (db.courses.Any(u => u.course_code == request.course_code))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.Conflict, "Course Code already exists");
        //        }

        //        // --- Remove credit hours regex validation temporarily ---
        //        // string CreditCodePattern = @"^[0-9]\(\d-\d\)$";
        //        // if (!Regex.IsMatch(request.credit_hours, CreditCodePattern))
        //        // {
        //        //     return Request.CreateResponse(HttpStatusCode.BadRequest, "Credit hours must be in the format: 4(2-2)");
        //        // }

        //        db.courses.Add(request);
        //        db.SaveChanges();

        //        return Request.CreateResponse(HttpStatusCode.OK, new
        //        {
        //            message = "Course added successfully",
        //            CourseId = request.id
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}





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
                    string.IsNullOrWhiteSpace(request.credit_hours))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "course_code,title and credit_hours are required");

                }


                if (db.courses.Any(u => u.course_code == request.course_code && u.id!=request.id))
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Course Code already exists");
                }




                var CourseCodePattern = @"^[A-Z]{2,4}-[0-9]{3}$";
                if (!Regex.IsMatch(request.course_code, CourseCodePattern))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Course Code must be in the format: CS-101");
                }

                //var CreditCodePattern = @"^[0-9]\(\d,\d\)$";
                //if (!Regex.IsMatch(request.credit_hours, CreditCodePattern))
                //{
                //    return Request.CreateResponse(HttpStatusCode.BadRequest, "Credit hours must be in the format: 4(2,2)");
                //}


          
                course.credit_hours = request.credit_hours;
                course.course_code = request.course_code;
                course.title= request.title.Trim();
                

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
        public HttpResponseMessage DeleteTeacher(int id)
        {
            try
            {
                if (id == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");
                }


                var course = db.courses.FirstOrDefault(u => u.id == id);
                if (course== null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "course not found");
                }



               

                db.courses.Remove(course);


                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Course delete successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }























        [HttpGet]
        [Route("api/course/search")]
        public HttpResponseMessage SearchCourses([FromUri] string search)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Search value is required");
                }

                var courses = db.courses
                                .Where(c =>
                                    c.course_code.Contains(search) ||
                                    c.title.Contains(search) ||
                                    c.credit_hours.ToString().Contains(search)
                                )
                                .Select(c => new
                                {
                                    c.id,
                                    c.course_code,
                                    c.title,
                                    c.credit_hours
                                })
                                .Take(9)
                                .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, courses);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



    }






}
