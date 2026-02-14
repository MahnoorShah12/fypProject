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

                var sender = db.Users.FirstOrDefault(u => u.id == request.SenderId);
                if (sender == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Sender not found");

             
                var receiver = db.Users.FirstOrDefault(u => u.id == request.ReceiverId);
                if (receiver == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Receiver not found");

                
                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

               
                if (request.QuestionId.HasValue)
                {
                    var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId.Value);
                    if (question == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Question not found");

                
                    if (question.paper_id != request.PaperId)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Question does not belong to this paper");
                }

                
                var newComment = new Comment
                {
                    description = request.Description,
                    comment_date = DateTime.Now.Date,
                    comment_time = DateTime.Now.TimeOfDay,
                    mark_read = false
                };
                db.Comments.Add(newComment);
                db.SaveChanges(); 
                var link = new Comment_Paper_Question
                {
                    comment_id = newComment.id,
                    paper_id = request.PaperId,
                    sender_id = request.SenderId,
                    receiver_id = request.ReceiverId,
                    question_id = request.QuestionId
                };
                db.Comment_Paper_Question.Add(link);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Comment added successfully",
                    CommentId = newComment.id,
                    CommentPaperQuestionId = link.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }




















        [HttpGet]
        [Route("api/comment/get_by_paper/{paperId}")]
        public HttpResponseMessage GetCommentsByPaper(int paperId)
        {
            try
            {
             
                var paper = db.papers.FirstOrDefault(p => p.id == paperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

               
                var comments = db.Comment_Paper_Question
                                 .Where(c => c.paper_id == paperId)
                                 .Select(c => new
                                 {
                                     c.id,
                                     CommentText = c.Comment.description,
                                     c.comment_id,
                                     SenderId = c.sender_id,
                                     SenderName = c.User.name,
                                     SenderRole = db.Role_Assignment
                                                    .Where(r => r.user_id == c.sender_id)
                                                    .Join(db.Roles,
                                                          ra => ra.role_id,
                                                          role => role.id,
                                                          (ra, role) => role.name)
                                                    .FirstOrDefault(),
                                     ReceiverId = c.receiver_id,
                                     ReceiverName = c.User1.name,
                                     c.question_id,
                                     c.paper_id
                                 })
                                 .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, comments);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("api/comment/get_by_question/{paperId}/{questionId}")]
        public HttpResponseMessage GetCommentsByQuestion(int paperId, int questionId)
        {
            try
            {
                var paper = db.papers.FirstOrDefault(p => p.id == paperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

              
                var question = db.Questions.FirstOrDefault(q => q.id == questionId && q.paper_id == paperId);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question not found or does not belong to this paper");

                
                var comments = db.Comment_Paper_Question
                                 .Where(c => c.paper_id == paperId && c.question_id == questionId)
                                 .Select(c => new
                                 {
                                     c.id,
                                     CommentText = c.Comment.description,
                                     c.comment_id,
                                     SenderId = c.sender_id,
                                     SenderName = c.User.name,
                                     SenderRole = db.Role_Assignment
                                                    .Where(r => r.user_id == c.sender_id)
                                                    .Join(db.Roles,
                                                          ra => ra.role_id,
                                                          role => role.id,
                                                          (ra, role) => role.name)
                                                    .FirstOrDefault(),
                                     ReceiverId = c.receiver_id,
                                     ReceiverName = c.User1.name,
                                     c.paper_id,
                                     c.question_id
                                 })
                                 .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, comments);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }








































    }






}
