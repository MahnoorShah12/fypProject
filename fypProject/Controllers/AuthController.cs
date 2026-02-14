using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;
using System.Web.Http.Cors;

namespace fypProject.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();
        [HttpPost]
        [Route("login")]

        public HttpResponseMessage Login([FromBody] LoginRequest request)
        {

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { success = false, message = "Email and password are required" });
                }

                // Find user
                var user = db.Users.FirstOrDefault(u => u.email == request.Email && u.status == true);
                if (user == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { success = false, message = "Invalid email or inactive account" });
                }

                // Verify password
                if (!PasswordHelper.VerifyPassword(request.Password, user.password))
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { success = false, message = "Invalid password" });
                }

                // Get roles
                var roleIds = db.Role_Assignment.Where(ra => ra.user_id == user.id).Select(ra => ra.role_id).ToList();
                if (!roleIds.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { success = false, message = "No roles assigned to user" });
                }

                var roles = db.Roles.Where(r => roleIds.Contains(r.id)).Select(r => r.name).ToList();

                // Success response
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    user = new
                    {
                        id = user.id,
                        name = user.name,
                        email = user.email
                    },
                    roles = roles,
                    token = "OptionalJWTTokenHere" // generate if you have JWT
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }












    }

}
