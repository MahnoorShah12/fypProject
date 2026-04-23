using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class NotificationController : ApiController
    {

        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        [HttpGet]
        [Route("api/notification/{UserId}")]
        public IHttpActionResult GetNotification(int UserId)
        {
            try
            {
                // Get comments for this user
                var commentEntities = (from c in db.Comment_Paper_Question
                                       join d in db.Comments on c.comment_id equals d.id
                                       join p in db.papers on c.paper_id equals p.id
                                       join co in db.courses on p.course_id equals co.id
                                       join u in db.Users on c.sender_id equals u.id
                                       where c.receiver_id == UserId
                                       select new
                                       {
                                           c.comment_id,
                                           c.paper_id,
                                           p.term,
                                           CourseName = co.title,
                                           c.sender_id,
                                           SenderName = u.name,
                                           c.receiver_id,
                                           c.question_id,
                                           d.comment_date,
                                           d.comment_time,
                                           d.description,
                                           d.mark_read
                                       })
                                       .ToList(); // fetch to memory

                // Compute QuestionNo in memory
                var comments = commentEntities.Select(c =>
                {
                    // Get all questions for this paper ordered by ID
                    var questions = db.Questions
                                      .Where(q => q.paper_id == c.paper_id)
                                      .OrderBy(q => q.id)
                                      .ToList();

                    // Find the index of the current question
                    var questionNo = questions.FindIndex(q => q.id == c.question_id) + 1;

                    return new
                    {
                        type = "comment",
                        c.comment_id,
                        c.paper_id,
                        c.term,
                        c.CourseName,
                        c.sender_id,
                        c.SenderName,
                        c.receiver_id,
                        c.question_id,
                        QuestionNo = questionNo > 0 ? questionNo : 0,
                        c.comment_date,
                        c.comment_time,
                        c.description,
                        c.mark_read
                    };
                }).ToList();

                // Alerts
                var alerts = db.Alerts
                               .Where(a => a.reciever_id == UserId)
                               .Select(a => new
                               {
                                   type = "alert",
                                   a.id,
                                   a.sender_id,
                                   a.reciever_id,
                                   a.paper_id,
                                   a.description
                               }).ToList();

                // Submission Alerts
                var submissionAlerts = (from s in db.submission_period
                                        join a in db.Alerts on 1 equals 1
                                        where a.reciever_id == UserId
                                              && a.description.Contains(s.start_date.ToString())
                                        select new
                                        {
                                            type = "submission",
                                            s.session_id,
                                            s.start_date,
                                            s.end_date,
                                            a.description
                                        }).ToList();

                // Combine all notifications
                var allNotifications = comments
                    .Cast<object>()
                    .Concat(alerts)
                    .Concat(submissionAlerts)
                    .OrderByDescending(n =>
                    {
                        dynamic d = n;
                        if (d.GetType().GetProperty("comment_date") != null)
                            return d.comment_date;
                        return DateTime.MinValue;
                    })
                    .ToList();

                return Ok(new { success = true, notifications = allNotifications });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}

