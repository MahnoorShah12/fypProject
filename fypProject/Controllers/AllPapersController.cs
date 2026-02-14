using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;

namespace fypProject.Controllers
{
    public class AllPapersController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();






        [HttpGet]
        [Route("api/allPapers/get")]
        public HttpResponseMessage GetAllPapers([FromUri] string status = null)
        {
            try
            {
                var a = "My name mahnoor shah huzaifaaa";
              
                var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No active session found");

                var query = db.papers.Where(p => p.session_id == activeSession.id);

                
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.status.ToLower() == status.ToLower());
                }

                
                var papers = query
                    .Select(p => new
                    {
                        PaperId = p.id,
                        CourseTitle = p.course.title,
                        Status = p.status,
                        Term = p.term
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, papers);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
























        [HttpGet]
        [Route("api/allPapers/past_papers")]
        public HttpResponseMessage GetPastPapers([FromUri] int? sessionId = null, [FromUri] string term = null)
        {
            try
            {
                
                var query = db.papers.Where(p => p.status == "printed");

                if (sessionId.HasValue)
                {
                    query = query.Where(p => p.session_id == sessionId.Value);
                }

               
                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(p => p.term.ToLower() == term.ToLower());
                }

         
                var papers = query
                    .Select(p => new
                    {
                        PaperId = p.id,
                        SessionName = p.session.name,
                        CourseTitle = p.course.title,
                        CreditHours = p.course.credit_hours,
                        Term = p.term,
                        Status = p.status
                    })
                    .ToList();

               
                var summary = new
                {
                    MidCount = papers.Count(p => p.Term.ToLower() == "mid"),
                    FinalCount = papers.Count(p => p.Term.ToLower() == "final"),
                    Total = papers.Count
                };

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Papers = papers,
                    Summary = summary
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



    }






}
