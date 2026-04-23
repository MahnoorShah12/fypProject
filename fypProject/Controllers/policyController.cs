using fypProject.Models;
using System;

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;



namespace fypProject.Controllers
{
    public class policyController : ApiController
    {

        private DirectorDashboardEntities db =new DirectorDashboardEntities();




        [HttpGet]
        [Route("api/policy/getPolicy/{courseId}/{term?}")]
        public IHttpActionResult GetPolicy(int courseId, string term = null)
        {
            try
            {
                // 🔹 If term not provided → get active session
                if (string.IsNullOrWhiteSpace(term))
                {
                    var activeSession = db.sessions
                                          .FirstOrDefault(s => s.Active == true);

                    if (activeSession == null)
                        return BadRequest("No active session found.");

                    term = activeSession.name; // or activeSession.term (use your column name)
                }

                var policy = db.Difficulty_level_policy
                               .FirstOrDefault(p => p.course_id == courseId
                                                 && p.term.ToLower() == term.ToLower());

                if (policy == null)
                    return NotFound();

                return Ok(new
                {
                    easy = policy.eassy_Q,
                    medium = policy.medium_Q,
                    tough = policy.difficult_Q,
                    term_used = term   // optional (for debugging)
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }























        [HttpGet]
        [Route("api/policy/getCloPolicy/{courseId}/{term}")]
        public IHttpActionResult GetCloPolicy(int courseId, string term, int? sessionId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest("Term is required.");

                // 🔹 Determine Session
                if (sessionId == null)
                {
                    var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                    if (activeSession == null)
                        return BadRequest("No active session found.");
                    sessionId = activeSession.id;
                }
                else
                {
                    var sessionExists = db.sessions.Any(s => s.id == sessionId);
                    if (!sessionExists)
                        return BadRequest("Invalid session id.");
                }

                // 🔹 Base Query + Filter by Term
                var cloPolicy = (from cw in db.Clo_Weightage
                                 join clo in db.cloes on cw.clo_id equals clo.id
                                 where clo.course_id == courseId
                                       && clo.session_id == sessionId
                                       && cw.term.Equals(term, StringComparison.OrdinalIgnoreCase)
                                 select new
                                 {
                                     cloId = clo.id,
                                     term = cw.term,
                                     weightage = cw.weightage
                                 }).ToList();

                if (!cloPolicy.Any())
                    return NotFound();

                return Ok(cloPolicy);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }





















    }



















}



