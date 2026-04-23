using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;

namespace fypProject.Controllers
{
    public class CommentController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();



        [HttpPost]
        [Route("api/comment/add")]
        public HttpResponseMessage AddComment([FromBody] AddCommentRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Description))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                // 1️⃣ Get sender from database
                var sender = db.Users.FirstOrDefault(u => u.id == request.SenderId);
                if (sender == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Sender not found");

                // 2️⃣ Validate paper
                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

                // 3️⃣ Validate question if provided
                if (request.QuestionId.HasValue)
                {
                    var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId.Value);
                    if (question == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Question not found");

                    if (question.paper_id != request.PaperId)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Question does not belong to this paper");
                }

                // 4️⃣ Create comment
                var newComment = new Comment
                {
                    description = request.Description,
                    comment_date = DateTime.Now.Date,
                    comment_time = DateTime.Now.TimeOfDay,
                    mark_read = false
                };
                db.Comments.Add(newComment);
                db.SaveChanges();

                // 5️⃣ Get receiver from paper assignment
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                var paperCreator = db.paper_Assignment
                                     .FirstOrDefault(pa => pa.course_id == paper.course_id
                                                        && pa.session_id == activeSession.id);
                if (paperCreator == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No user found who created this paper in active session");

                int receiverId = paperCreator.user_id;

                // 6️⃣ Link comment
                var link = new Comment_Paper_Question
                {
                    comment_id = newComment.id,
                    paper_id = paper.id,
                    sender_id = sender.id,        // Use sender from DB
                    receiver_id = receiverId,     // Receiver from paper assignment
                    question_id = request.QuestionId
                };
                db.Comment_Paper_Question.Add(link);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Comment added successfully",
                    CommentId = newComment.id,
                    ReceiverId = receiverId
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [Route("api/comment/get_by_paper/{paperId}")]
        public IHttpActionResult GetCommentsByPaper(int paperId)
        {
            try
            {
                var paperExists = db.papers.Any(p => p.id == paperId);
                if (!paperExists)
                    return NotFound();

                var comments = (from cpq in db.Comment_Paper_Question
                                join c in db.Comments on cpq.comment_id equals c.id
                                join sender in db.Users on cpq.sender_id equals sender.id
                                join receiver in db.Users
                                    on cpq.receiver_id equals receiver.id into receiverGroup
                                from receiver in receiverGroup.DefaultIfEmpty()
                                where cpq.paper_id == paperId
                                orderby c.comment_date ascending, c.comment_time ascending
                                select new
                                {
                                    Id = cpq.id,
                                    CommentId = c.id,
                                    Text = c.description,
                                    CommentDate = c.comment_date,
                                    CommentTime = c.comment_time,
                                    IsRead = c.mark_read,

                                    SenderId = sender.id,
                                    SenderName = sender.name,

                                    ReceiverId = receiver != null ? (int?)receiver.id : null,
                                    ReceiverName = receiver != null ? receiver.name : null,

                                    QuestionId = cpq.question_id,
                                    PaperId = cpq.paper_id
                                })
                    .AsNoTracking()
                    .ToList();


                // ✅ Combine Date + TimeSpan in memory
                var result = comments.Select(c => new
                {
                    c.Id,
                    c.CommentId,
                    c.Text,
                    CreatedAt = c.CommentDate.HasValue && c.CommentTime.HasValue
                                ? c.CommentDate.Value.Add(c.CommentTime.Value).ToString("s")
                                : null,
                    c.IsRead,
                    c.SenderId,
                    c.SenderName,
                    c.ReceiverId,
                    c.ReceiverName,
                    c.QuestionId,
                    c.PaperId
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }





        [HttpGet]
        [Route("api/comment/get_by_question/{questionId}")]
        public IHttpActionResult GetCommentsByQuestion(int questionId)
        {
            try
            {
                var questionExists = db.Questions.Any(q => q.id == questionId);
                if (!questionExists)
                    return BadRequest("Question not found");

                var commentsQuery = from cpq in db.Comment_Paper_Question
                                    join c in db.Comments on cpq.comment_id equals c.id
                                    join sender in db.Users on cpq.sender_id equals sender.id
                                    join receiver in db.Users on cpq.receiver_id equals receiver.id into recv
                                    from receiver in recv.DefaultIfEmpty()
                                    join ra in db.Role_Assignment on sender.id equals ra.user_id into roleAssign
                                    from ra in roleAssign.DefaultIfEmpty()
                                    join role in db.Roles on ra.role_id equals role.id into roles
                                    from role in roles.DefaultIfEmpty()
                                    where cpq.question_id == questionId
                                    orderby c.comment_date descending, c.comment_time descending
                                    select new
                                    {
                                        cpq.id,
                                        CommentId = c.id,
                                        CommentText = c.description,
                                        CommentDate = c.comment_date,
                                        CommentTime = c.comment_time,
                                        MarkRead = c.mark_read,
                                        SenderId = sender.id,
                                        SenderName = sender.name,
                                        SenderRole = role != null ? role.name : null,
                                        ReceiverId = receiver != null ? (int?)receiver.id : null,
                                        ReceiverName = receiver != null ? receiver.name : null,
                                        cpq.paper_id,
                                        cpq.question_id
                                    };

                // Remove duplicates by CommentId
                var comments = commentsQuery
                               .GroupBy(c => c.CommentId)
                               .Select(g => g.FirstOrDefault())
                               .ToList();

                return Json(comments); // return JSON for React frontend
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}