using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using System;

namespace fypProject.Controllers
{
    public class DutySwitchController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        [HttpGet]
        [Route("api/dutySwitch/current_director")]
        public HttpResponseMessage GetCurrentDirector()
        {
            try
            {
                var directorRoleId = db.Roles
                                       .Where(r => r.name == "director")
                                       .Select(r => r.id)
                                       .FirstOrDefault();

                if (directorRoleId == 0)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Director role not found");

                var director = (from ra in db.Role_Assignment
                                join u in db.Users on ra.user_id equals u.id
                                where ra.role_id == directorRoleId && u.status == true
                                select new
                                {
                                    u.id,
                                    u.name,
                                    ra.IsTemporary
                                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, director);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("api/dutySwitch/get_all_teachers")]
        public HttpResponseMessage GetAllTeachers()
        {
            try
            {
                var facultyRoleId = db.Roles
                                      .Where(r => r.name == "faculty")
                                      .Select(r => r.id)
                                      .FirstOrDefault();

                var directorRoleId = db.Roles
                                       .Where(r => r.name == "director")
                                       .Select(r => r.id)
                                       .FirstOrDefault();

                var teachers = db.Users
                    .Where(u => u.status == true &&
                                db.Role_Assignment.Any(ra =>
                                    ra.user_id == u.id &&
                                    ra.role_id == facultyRoleId))
                    .Select(u => new
                    {
                        u.id,
                        u.name,
                        IsDirector = db.Role_Assignment.Any(ra =>
                            ra.user_id == u.id &&
                            ra.role_id == directorRoleId),
                        IsTemporary = db.Role_Assignment.Any(ra =>
                            ra.user_id == u.id &&
                            ra.role_id == directorRoleId &&
                            ra.IsTemporary)
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, teachers);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        [Route("api/dutySwitch/assign_temporary_director/{id}")]
        public HttpResponseMessage AssignTemporaryDirector(int id)
        {
            try
            {
                var directorRoleId = db.Roles
                                       .Where(r => r.name == "director")
                                       .Select(r => r.id)
                                       .FirstOrDefault();

                if (directorRoleId == 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Director role not found");

                var existingTemporary = db.Role_Assignment
                                          .Where(r => r.role_id == directorRoleId && r.IsTemporary)
                                          .ToList();

                if (existingTemporary.Any())
                {
                    db.Role_Assignment.RemoveRange(existingTemporary);
                }

                var target = db.Role_Assignment
                               .FirstOrDefault(r => r.user_id == id && r.role_id == directorRoleId);

                if (target != null)
                {
                    target.IsTemporary = true; 
                }
                else
                {
                    db.Role_Assignment.Add(new Role_Assignment
                    {
                        user_id = id,
                        role_id = directorRoleId,
                        IsTemporary = true
                    });
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Temporary director assigned successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        [Route("api/dutySwitch/ReStore_All_My_Responsiables")]
        public HttpResponseMessage ReStoreAllMyResponsiables()
        {
            try
            {
            
                var directorRoleId = db.Roles
                                       .Where(r => r.name == "director")
                                       .Select(r => r.id)
                                       .FirstOrDefault();

                if (directorRoleId == 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Director role not found");

               
                var tempDirectors = db.Role_Assignment
                                      .Where(r => r.role_id == directorRoleId && r.IsTemporary)
                                      .ToList();

                if (tempDirectors.Any())
                {
                    db.Role_Assignment.RemoveRange(tempDirectors); 
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Temporary director(s) removed successfully");
                }

                return Request.CreateResponse(HttpStatusCode.NotFound, "No temporary director found");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
