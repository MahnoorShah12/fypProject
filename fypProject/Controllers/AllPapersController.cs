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
        public HttpResponseMessage GetAllPapers()
        {
            try
            {
                var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No active session found");

                var papers = db.papers
                    .Where(p => p.session_id == activeSession.id)
                    .Select(p => new
                    {
                        PaperId = p.id,
                        CourseId = p.course.id,
                        CourseTitle = p.course.title,
                        CourseCode = p.course.course_code,
                        Term = p.term,
                        DbStatus = p.status,
                        SessionId = p.session_id
                    })
                    .ToList();

                var mappedPapers = papers.Select(p => new
                {
                    p.PaperId,
                    p.CourseTitle,
                    p.CourseId,
                    p.Term,
                    p.CourseCode,
                    Status = MapStatus(p.DbStatus),

                    // 🔥 NEW: Get assigned teacher names if Pending
                    AssignedTeachers = MapStatus(p.DbStatus) == "Pending"
                        ? db.paper_Assignment
                            .Where(pa =>
                                pa.course_id == p.CourseId &&
                                pa.session_id == p.SessionId)
                            .Join(db.Users,
                                pa => pa.user_id,
                                u => u.id,
                                (pa, u) => u.name)
                            .Distinct()
                            .ToList()
                        : null
                })
                .ToList();

                var counts = new
                {
                    All = mappedPapers.Count,
                    Uploaded = mappedPapers.Count(x => x.Status == "Uploaded"),
                    Pending = mappedPapers.Count(x => x.Status == "Pending"),
                    Approved = mappedPapers.Count(x => x.Status == "Approved"),
                    Printed = mappedPapers.Count(x => x.Status == "Printed")
                };

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Papers = mappedPapers,
                    Counts = counts
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 🔥 Status Mapping Function
        private string MapStatus(string dbStatus)
        {
            if (dbStatus == "Submitted")
                return "Uploaded";

            if (dbStatus == "Approved")
                return "Approved";

            if (dbStatus == "Printed")
                return "Printed";

            if (dbStatus == "Creation" ||
                dbStatus == "ReadyForFacultyApprover" ||
                dbStatus == "WaitingForFacultyApprover")
                return "Pending";

            return "Pending";
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