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
    public class SwapQuestionController : ApiController
    {





        private DirectorDashboardEntities db = new DirectorDashboardEntities();




        [HttpGet]
        [Route("api/SwapQuestion/get_by_paper/{paperId}")]
        public HttpResponseMessage GetQuestionsByPaper(int paperId)
        {
            try
            {
                var paper = db.papers.FirstOrDefault(p => p.id == paperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

                var questions = db.Questions
                                  .Where(q => q.paper_id == paperId)
                                  .Select(q => new
                                  {
                                      q.id,
                                      q.text,
                                      q.description,
                                      q.image,
                                      q.difficulty_level,
                                      q.clo_id,
                                      q.marks,
                                      q.isextra,
                                      q.editor_id
                                  }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, questions);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("api/SwapQuestion/swap")]
        public HttpResponseMessage SwapQuestions([FromBody] SwapQuestionsRequest request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

                var question1 = db.Questions.FirstOrDefault(q => q.id == request.QuestionId1 && q.paper_id == request.PaperId);
                var question2 = db.Questions.FirstOrDefault(q => q.id == request.QuestionId2 && q.paper_id == request.PaperId);

                if (question1 == null || question2 == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "One or both questions not found in this paper");

                var temp = new
                {
                    question1.text,
                    question1.description,
                    question1.image,
                    question1.difficulty_level,
                    question1.clo_id,
                    question1.marks,
                    question1.isextra,
                    question1.editor_id
                };

                question1.text = question2.text;
                question1.description = question2.description;
                question1.image = question2.image;
                question1.difficulty_level = question2.difficulty_level;
                question1.clo_id = question2.clo_id;
                question1.marks = question2.marks;
                question1.isextra = question2.isextra;
                question1.editor_id = question2.editor_id;

                question2.text = temp.text;
                question2.description = temp.description;
                question2.image = temp.image;
                question2.difficulty_level = temp.difficulty_level;
                question2.clo_id = temp.clo_id;
                question2.marks = temp.marks;
                question2.isextra = temp.isextra;
                question2.editor_id = temp.editor_id;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Questions swapped successfully",
                    QuestionId1 = question1.id,
                    QuestionId2 = question2.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }























































    }






}
