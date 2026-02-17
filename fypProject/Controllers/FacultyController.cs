using fypProject.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class FacultyController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();



        [HttpPost]
        [Route("api/faculty/add-teacher")]
        public HttpResponseMessage AddTeacher([FromBody] AddTeacherDto request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");



                // BIIT email validation
                var emailPattern = @"^[a-zA-Z0-9._%+-]+@biit\.edu\.pk$";
                if (!Regex.IsMatch(request.email, emailPattern))
                {
                    return Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        "Email must be in the format: example@biit.edu.pk"
                    );
                }

                // Duplicate checks
                if (db.Users.Any(u => u.email == request.email))
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Email already exists");


                // Map DTO → User
                var user = new User
                {
                    name = request.name,
                    email = request.email,
                    phone_no = request.phone,   // nullable ✔
                    designation = request.designation,
                    password = PasswordHelper.HashPassword(request.password),
                    status = true
                };

                db.Users.Add(user);
                db.SaveChanges();

                // Assign faculty role
                var role = db.Roles.FirstOrDefault(r => r.name == "faculty");
                if (role == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Faculty role not found");

                db.Role_Assignment.Add(new Role_Assignment
                {
                    user_id = user.id,
                    role_id = role.id
                });

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Teacher added successfully",
                    teacherId = user.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    ex.Message
                );
            }
        }





        [HttpGet]
        [Route("api/faculty/get_teachers")]
        public HttpResponseMessage GetTeachers(int page = 1, int pageSize = 10)
        {
            try
            {
                var role = db.Roles.FirstOrDefault(r => r.name == "Faculty");
                if (role == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Faculty role not found");

                var query = from u in db.Users
                            join ra in db.Role_Assignment on u.id equals ra.user_id
                            where ra.role_id == role.id && u.status == true
                            select new TeacherDto
                            {
                                Id = u.id,
                                Name = u.name,
                                Email = u.email,
                                Phone = u.phone_no,
                                Designation = u.designation
                            };

                var totalCount = query.Count();

                var teachers = query
                    .OrderBy(t => t.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    data = teachers,
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
        [Route("api/faculty/edit_teacher_data")]
        public HttpResponseMessage EditTeacherData([FromBody] TeacherDto request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                var user = db.Users.FirstOrDefault(u => u.id == request.Id && u.status == true);
                if (user == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Teacher not found");

                if (string.IsNullOrWhiteSpace(request.Name) ||
                    string.IsNullOrWhiteSpace(request.Email))
                {
                    return Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        "Name and Email  are required"
                    );
                }

                var emailPattern = @"^[a-zA-Z0-9._%+-]+@biit\.edu\.pk$";
                if (!Regex.IsMatch(request.Email, emailPattern))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid email format");

                if (db.Users.Any(u => u.email == request.Email && u.id != request.Id))
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Email already exists");



                user.name = request.Name.Trim();
                user.email = request.Email.Trim();
                user.designation = request.Designation;
                user.phone_no = request.Phone;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Teacher updated successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




        [HttpPost]
        [Route("api/faculty/delete_teacher/{id}")]
        public HttpResponseMessage DeleteTeacher(int id)
        {
            try
            {
                var user = db.Users.FirstOrDefault(u => u.id == id && u.status == true);
                if (user == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Teacher not found");


                user.status = false;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Teacher deleted successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        [Route("api/faculty/restore_teacher/{id}")]
        public HttpResponseMessage RestoreTeacher(int id)
        {
            try
            {
                var user = db.Users.FirstOrDefault(u => u.id == id && u.status == false);
                if (user == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Teacher not found");

                user.status = true;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Teacher restored successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }








        [HttpGet]
        [Route("api/faculty/search_teacher")]
        public HttpResponseMessage SearchTeacher([FromUri] string search)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Search term is required.");


                var role = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
                if (role == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Faculty role not found.");


                var users = db.Users
      .Where(u => u.status == true &&
                 (u.name.ToLower().Contains(search.ToLower())
                  || u.email.ToLower().Contains(search.ToLower())
                  || u.phone_no.Contains(search)))
      .Where(u => db.Role_Assignment.Any(ra => ra.user_id == u.id && ra.role_id == role.id))
      .Select(u => new TeacherDto
      {
          Id = u.id,
          Name = u.name,
          Email = u.email,
          Phone = u.phone_no,
          Designation = u.designation
      })
      .Take(9)
      .ToList();


                if (!users.Any())
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Teacher not found.");

                return Request.CreateResponse(HttpStatusCode.OK, users);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


    }
}

