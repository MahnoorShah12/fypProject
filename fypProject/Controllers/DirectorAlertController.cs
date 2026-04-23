using DocumentFormat.OpenXml.EMMA;
using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class DirectorAlertController : ApiController
    {
        private readonly DirectorDashboardEntities db = new DirectorDashboardEntities();


        private int ResolveSessionId(int? sessionIdFromFrontend)
        {
            // If frontend sends sessionId → validate it
            if (sessionIdFromFrontend.HasValue)
            {
                var session = db.sessions
                                .FirstOrDefault(s => s.id == sessionIdFromFrontend.Value);

                if (session == null)
                    throw new Exception("Session not found");

                if (!session.Active)
                    throw new Exception("Selected session is not active");

                return session.id;
            }

            // Otherwise → fallback to active session
            var activeSession = db.sessions.FirstOrDefault(s => s.Active);
            //      
            if (activeSession == null)
                throw new Exception("No active session found");

            return activeSession.id;
        
            }


        //[HttpPost]
        //[Route("api/DirectorAlert/submission-period/send")]
        //public IHttpActionResult SendSubmissionPeriod([FromBody] SubmissionPeriodDto model)
        //{
        //    if (model == null)
        //        return BadRequest("Invalid data");

        //    int sessionId;
        //    try
        //    {
        //        sessionId = ResolveSessionId(model.SessionId);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }

        //    var period = new submission_period
        //    {
        //        session_id = sessionId,
        //        start_date = model.StartDate,
        //        end_date = model.EndDate
        //    };

        //    db.submission_period.Add(period);

        //    var role = db.Roles.First(r => r.name.ToLower() == "faculty");

        //    var teacherIds = db.Role_Assignment
        //                       .Where(r => r.role_id == role.id)
        //                       .Select(r => r.user_id)
        //                       .ToList();

        //    foreach (var tid in teacherIds)
        //    {
        //        db.Alerts.Add(new Alert
        //        {
        //            sender_id = model.SenderId,
        //            reciever_id = tid,
        //            description = $"Submission Period: {model.StartDate:dd-MM-yyyy} → {model.EndDate:dd-MM-yyyy}"
        //        });
        //    }

        //    db.SaveChanges();
        //    return Ok("Submission period alert sent to All Teahers");
        //}
        [HttpPost]
        [Route("api/DirectorAlert/submission-period/send")]
        public IHttpActionResult SendSubmissionPeriod([FromBody] SubmissionPeriodDto model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            int sessionId;

            try
            {
                sessionId = ResolveSessionId(model.SessionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var session = db.sessions.FirstOrDefault(s => s.id == sessionId);

            if (session == null)
                return BadRequest("Session not found");

            // ✅ DATE RANGE CHECK
            if (model.StartDate < session.start_date || model.EndDate > session.end_date)
            {
                return BadRequest(
                    $"Submission period must be within active session dates ({session.start_date:dd-MM-yyyy} to {session.end_date:dd-MM-yyyy})"
                );
            }

            var period = new submission_period
            {
                session_id = sessionId,
                start_date = model.StartDate,
                end_date = model.EndDate

            };

            db.submission_period.Add(period);

            var role = db.Roles.First(r => r.name.ToLower() == "faculty");

            var teacherIds = db.Role_Assignment
                               .Where(r => r.role_id == role.id)
                               .Select(r => r.user_id)
                               .ToList();

            //foreach (var tid in teacherIds)
            //{
            //    db.Alerts.Add(new Alert
            //    {
            //      sender_id = model.SenderId,
            //       reciever_id = tid,


            //        description = $"Submission Period: {model.StartDate:dd-MM-yyyy} → {model.EndDate:dd-MM-yyyy}"
            //    });
            var teacherPapers = db.paper_Assignment
                      .Where(p => p.session_id == sessionId)
                      .ToList();

            foreach (var paper in teacherPapers)
            {
                db.Alerts.Add(new Alert
                {
                    sender_id = model.SenderId,
                    reciever_id = paper.user_id,   // 👈 yahi teacherId hai
                    paper_id = paper.id,
                    description = $"Submission Period: {model.StartDate:dd-MM-yyyy} → {model.EndDate:dd-MM-yyyy}"
                });
            




        }

            db.SaveChanges();

            return Ok("Submission period alert sent to All Teachers");
        }

        [HttpGet]
        [Route("api/DirectorAlert/teachers/by-alphabet")]
        public IHttpActionResult GetTeachersByAlphabet(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
                return BadRequest("Start and End alphabet required");

            var role = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
            if (role == null)
                return BadRequest("Faculty role not found");

            var teachers = db.Users
                             .Join(db.Role_Assignment,
                                   u => u.id,
                                   r => r.user_id,
                                   (u, r) => new { u, r })
                             .Where(x => x.r.role_id == role.id &&
                                         !string.IsNullOrEmpty(x.u.name) &&
                                         string.Compare(x.u.name.Substring(0, 1), start, true) >= 0 &&
                                         string.Compare(x.u.name.Substring(0, 1), end, true) <= 0)
                             .OrderBy(x => x.u.name)
                             .Select(x => new
                             {
                                 x.u.id,
                                 x.u.name,
                                 x.u.email
                             })
                             .ToList();

            return Ok(teachers);
        }








        [HttpPost]
        [Route("api/DirectorAlert/vetting/assign-group")]
        public IHttpActionResult AssignVettingToGroup([FromBody] VettingGroupDto model)
        {
            if (model == null || model.TeacherIds == null || !model.TeacherIds.Any())
                return BadRequest("No teachers selected");

            int sessionId;
            try
            {
                sessionId = ResolveSessionId(model.SessionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // Parse vetting time if provided
            TimeSpan? vettingTime = null;
            if (!string.IsNullOrEmpty(model.VettingTime))
            {
                if (TimeSpan.TryParse(model.VettingTime, out TimeSpan parsedTime))
                    vettingTime = parsedTime;
                else
                    return BadRequest("Invalid vetting time format");
            }

            var teachers = db.Users
                .Where(u => model.TeacherIds.Contains(u.id))
                .ToList();

            var results = new List<object>();

            foreach (var teacher in teachers)
            {
                try
                {
                    var existingAssignment = db.Vetting_Timming_Assignment
                        .FirstOrDefault(v => v.user_id == teacher.id && v.session_id == sessionId);

                    if (existingAssignment != null)
                    {
                        bool updated = false;

                        // Update only if a date is provided
                        if (model.VettingDate.HasValue)
                        {
                            existingAssignment.Vetting_Date = model.VettingDate.Value;
                            updated = true;
                        }

                        // Update only if time is provided
                        if (vettingTime.HasValue)
                        {
                            existingAssignment.Vetting_Time = vettingTime;
                            updated = true;
                        }

                        if (updated)
                        {
                            db.Alerts.Add(new Alert
                            {
                                sender_id = model.SenderId,
                                reciever_id = teacher.id,
                                description = $"Vetting updated: " +
                                              $"{(existingAssignment.Vetting_Date.HasValue ? existingAssignment.Vetting_Date.Value.ToString("dd-MM-yyyy") : "")}" +
                                              $"{(existingAssignment.Vetting_Time.HasValue ? $" | Time: {existingAssignment.Vetting_Time}" : "")}"
                            });

                            results.Add(new { Teacher = teacher.name, Status = "Updated" });
                        }
                        else
                        {
                            results.Add(new { Teacher = teacher.name, Status = "No changes" });
                        }

                        continue; // skip creation
                    }

                    // No existing assignment: date is required
                    if (!model.VettingDate.HasValue)
                    {
                        results.Add(new { Teacher = teacher.name, Status = "Failed", Reason = "Vetting date is required" });
                        continue; // skip invalid entry
                    }

                    // Create new assignment
                    db.Vetting_Timming_Assignment.Add(new Vetting_Timming_Assignment
                    {
                        user_id = teacher.id,
                        session_id = sessionId,
                        Vetting_Date = model.VettingDate.Value,
                        Vetting_Time = vettingTime
                    });

                    db.Alerts.Add(new Alert
                    {
                        sender_id = model.SenderId,
                        reciever_id = teacher.id,
                        description = $"Vetting assigned: {model.VettingDate.Value:dd-MM-yyyy}" +
                                      $"{(vettingTime.HasValue ? $" | Time: {vettingTime}" : "")}"
                    });

                    results.Add(new { Teacher = teacher.name, Status = "Assigned" });
                }
                catch (Exception ex)
                {
                    // Catch unexpected errors per teacher to avoid breaking the loop
                    results.Add(new { Teacher = teacher.name, Status = "Failed", Reason = ex.Message });
                }
            }

            db.SaveChanges();

            return Ok(new
            {
                Message = "Vetting assignments processed",
                Details = results
            });
        }






        [HttpGet]
        [Route("api/DirectorAlert/teachers-with-courses")]
        public IHttpActionResult GetTeachersWithAssignedPapers(int? sessionId = null)
        {
            int resolvedSessionId;
            try
            {
                resolvedSessionId = ResolveSessionId(sessionId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var facultyRole = db.Roles.FirstOrDefault(r => r.name.ToLower() == "faculty");
            if (facultyRole == null)
                return BadRequest("Faculty role not found");

            // Only teachers who have paper assignments in this session
            var query =
                from u in db.Users
                join ra in db.Role_Assignment on u.id equals ra.user_id
                join pa in db.paper_Assignment on u.id equals pa.user_id
                join c in db.courses on pa.course_id equals c.id
                where ra.role_id == facultyRole.id
                      && pa.session_id == resolvedSessionId
                select new
                {
                    //TeacherId = u.id,
                    //TeacherName = u.name,
                    //CourseName = c.title
                    TeacherId = u.id,
                    TeacherName = u.name,
                    CourseId = c.id,
                    CourseName = c.title,
                    PaperId = pa.id
                };

            //var result = query
            //    .AsEnumerable()
            //    .GroupBy(x => new { x.TeacherId, x.TeacherName })
            //    .Select(g => new
            //    {
            //        id = g.Key.TeacherId,
            //        name = g.Key.TeacherName,
            //        courses = g
            //            .Select(x => x.CourseName
            //            )
            //            .Distinct()
            //            .OrderBy(c => c)
            //            .ToList()
            //    })
            //    .OrderBy(x => x.name)
            //    .ToList();

            var result = query
    .AsEnumerable()
    .GroupBy(x => new { x.TeacherId, x.TeacherName })
    .Select(g => new
    {
        id = g.Key.TeacherId,
        name = g.Key.TeacherName,
        courses = g
            .Select(x => new
            {
                courseId = x.CourseId,
                courseName = x.CourseName,
                paperId = x.PaperId
            })
            .OrderBy(x => x.courseName)
            .ToList()
    })
    .OrderBy(x => x.name)
    .ToList();
            return Ok(result);
        }


    }
}
