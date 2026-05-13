using System;
using System.Collections.Generic;
using fypProject.Models;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class PaperVerificationController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        [HttpGet]
        [Route("api/paperVerification/get_approved_papers_summary")]
        public HttpResponseMessage GetApprovedPapersSummary(int? sessionId = null)
        {
            try
            {
                // 1️⃣ Get session (requested OR active)
                var session = sessionId.HasValue
                    ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
                    : db.sessions.FirstOrDefault(s => s.Active);

                if (session == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No valid session found");

                // 2️⃣ Fetch approved papers + teacher from paper_Assignment
                var papers = (from p in db.papers
                              join c in db.courses on p.course_id equals c.id
                              join pa in db.paper_Assignment
                                  on new { p.course_id, p.session_id }
                                  equals new { pa.course_id, pa.session_id }
                              join u in db.Users on pa.user_id equals u.id
                              where p.session_id == session.id
                                    && p.status == "Approved" || p.status == "Verified"
                              select new
                              {
                                  Id = p.id,        // ✅ Include paper ID here
                                  SubjectName = c.title,
                                  Term = p.term,
                                  Session = session.name,
                                  Status = p.status,
                                  Teacher = u.name
                              })
                .Distinct()
                .ToList();

                // 3️⃣ Handle empty
                if (!papers.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        message = "No approved papers found",
                        data = papers
                    });
                }

                // 4️⃣ Return response
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Approved papers fetched successfully",
                    total = papers.Count,
                    data = papers
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }















        [HttpPost]
        [Route("api/paperVerification/verifyPaper")]
        public HttpResponseMessage VerifyPaper([FromBody] VerifyPaperRequest request)
        {
            if (request == null || request.PaperId <= 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { success = false, message = "Invalid Paper ID" });

            try
            {
                // Fetch the paper
                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId && p.status == "Approved");

                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { success = false, message = "Paper not found or already verified" });

                // Update status
                paper.status = "Verified";
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { success = true, message = "Paper verified successfully" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        // ✅ Request body model
        public class VerifyPaperRequest
        {
            public int PaperId { get; set; }
        }
    }
}