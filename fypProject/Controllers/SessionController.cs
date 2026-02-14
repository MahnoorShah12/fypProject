using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;
using Microsoft.Ajax.Utilities;

namespace fypProject.Controllers
{
    public class SessionController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();



        [HttpGet]
        [Route("api/session/get_all_sessions")]
        public HttpResponseMessage GetAllSessions()
        {
            try
            {
                var sessions = db.sessions.Select(s => new { s.id, s.name, s.start_date, s.end_date }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, sessions);

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }





        [HttpPost]
        [Route("api/session/add_session")]
        public HttpResponseMessage AddSession([FromBody] session request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                if (string.IsNullOrWhiteSpace(request.name))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Name is required");


                if (!request.start_date.HasValue || !request.end_date.HasValue)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Start date and end date are required");

                if (request.start_date.Value > request.end_date.Value)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Start date cannot be after end date");

                if (db.sessions.Any(u => u.name == request.name))
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Session name must be unique");

                db.sessions.Add(request);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Session added successfully",
                    SessionId = request.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }












        [HttpPost]
        [Route("api/session/active_session/{id}")]
        public HttpResponseMessage ActiveSession(int id)
        {
            try
            {

                var session = db.sessions.FirstOrDefault(s => s.id == id);
                if (session == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Session not found");
                }

                db.sessions.Where(s => s.Active == true).ToList()
                    .ForEach(s => s.Active = false);


                session.Active = true;

                db.SaveChanges();


                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Session activated successfully",
                    SessionId = session.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }










        [HttpGet]
        [Route("api/session/get_session_by_name/{name}")]
        public HttpResponseMessage GetSessionByName(string name)
        {
            try
            {
                int sessionId = db.sessions.FirstOrDefault(s => s.name == name).id;
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Session = sessionId
                });

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




















    }
}
