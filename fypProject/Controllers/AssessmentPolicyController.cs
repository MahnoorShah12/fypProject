


using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http; 
using WebGrease.Css.Ast.Selectors;
using System;
using System.Web.SessionState;

namespace fypProject.Controllers
{
    public class AssessmentPolicyController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();










        [HttpGet]
        [Route("api/policy/get-difficulty-policy/{courseId}")]
        public IHttpActionResult GetDifficultyPolicy(int courseId)
        {
            try
            {
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                    return BadRequest("No active session found.");

                // Project to anonymous object to avoid circular reference
                var policy = db.Difficulty_level_policy
                               .Where(d => d.course_id == courseId && d.session_id == activeSession.id)
                               .Select(d => new
                               {
                                   d.id,
                                   d.course_id,
                                   d.session_id,
                                   d.medium_Q,
                                   d.difficult_Q,
                                   d.eassy_Q,
                                   d.term,
                                   // Add other fields you want
                               })
                               .ToList();

                if (!policy.Any())
                    return NotFound();

                return Ok(new
                {
                    Data = policy,
                    Message = "difficulty level  retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }






























        [HttpPost]
        [Route("api/policy/save-difficulty-policy/{courseId}")]
        public IHttpActionResult SaveDifficultyPolicy(int courseId, List<DifficultyPolicyDTO> difficultyPolicy)
        {
            if (difficultyPolicy == null || difficultyPolicy.Count == 0)
                return BadRequest("Difficulty policy data is required");

            var activeSession = db.sessions.FirstOrDefault(s => s.Active);

            if (activeSession == null)
                return BadRequest("No active session found");

            try
            {
                foreach (var item in difficultyPolicy)
                {
                    // Check if a record already exists for this course, term, and session
                    var existing = db.Difficulty_level_policy
                                     .FirstOrDefault(d => d.course_id == courseId
                                                       && d.term == item.Term
                                                       && d.session_id == activeSession.id);

                    if (existing != null)
                    {
                        // Update existing record
                        existing.eassy_Q = item.Easy;
                        existing.medium_Q = item.Medium;
                        existing.difficult_Q = item.Tough;
                    }
                    else
                    {//////////
                        // Add new record
                        db.Difficulty_level_policy.Add(new Difficulty_level_policy
                        {
                            course_id = courseId,
                            term = item.Term,
                            eassy_Q = item.Easy,
                            medium_Q = item.Medium,
                            difficult_Q = item.Tough,
                            session_id = activeSession.id
                        });
                    }
                }

                db.SaveChanges();
                return Ok("Difficulty policy saved successfully");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



    }
}
