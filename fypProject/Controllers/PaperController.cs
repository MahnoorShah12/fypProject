using DocumentFormat.OpenXml.Office2016.Excel;
using fypProject.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class PaperController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();







        [HttpGet]
        [Route("api/paper/Get_Course_Info/{id}")]
        public HttpResponseMessage GetCourseInfo(int id)
        {
            try
            {
                
                var course = db.courses.FirstOrDefault(c => c.id == id);
                if (course == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Course not found");

           
                var session = db.sessions.FirstOrDefault(s => s.Active == true);
                string currentSession = session != null ? session.name : "No active session";

                
                var teacherNames = db.Course_Assignment
                                     .Where(ca => ca.course_id == id)
                                     .Join(db.Users,
                                           ca => ca.user_id,
                                           u => u.id,
                                           (ca, u) => u.name)
                                     .Distinct()
                                     .ToList();

                var response = new
                {
                    CourseId = course.id,
                    CourseCode = course.course_code,
                    CourseName = course.title,
                    Course_CreditHrs = course.credit_hours,
                    CurrentSession = currentSession,
                    Teachers = string.Join(", ", teacherNames) 
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }






        [HttpPost]
        [Route("api/paper/CreateOrUpdate")]
        public HttpResponseMessage CreateOrUpdate([FromBody] paper request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

               
                if (!db.sessions.Any(s => s.id == request.session_id))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid session ID");

                if (!db.courses.Any(c => c.id == request.course_id))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid course ID");

                if (request.term != "mid" && request.term != "final")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Term must be 'mid' or 'final'");

                if (request.total_marks < 0 || request.duration < 0 || request.no_of_questions < 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Numeric fields cannot be negative");

               
                var existingPaper = db.papers.FirstOrDefault(p =>
                    p.session_id == request.session_id &&
                    p.course_id == request.course_id &&
                    p.term == request.term
                );

                if (existingPaper != null)
                {
                  
                    existingPaper.paper_Date = request.paper_Date;
                    existingPaper.Start_time = request.Start_time;
                    existingPaper.end_time = request.end_time;
                    existingPaper.duration = request.duration;
                    existingPaper.degree_programs = request.degree_programs;
                    existingPaper.total_marks = request.total_marks;
                    existingPaper.teacher_name = request.teacher_name;
                    existingPaper.no_of_questions = request.no_of_questions;

                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        message = "Paper updated successfully",
                        PaperId = existingPaper.id
                    });
                }

                
                request.status = "creation"; 
                var newPaper = db.papers.Add(request);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Paper created successfully",
                    PaperId = newPaper.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }







    [HttpPost]
    [Route("api/question/Create")]
    public HttpResponseMessage Create()
    {
        try
        {
            var httpRequest = HttpContext.Current.Request;

            // ------------------------------
            // Read JSON string
            // ------------------------------
            var jsonData = httpRequest.Form["question"];
            if (string.IsNullOrEmpty(jsonData))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Question data is required");

            var request = JsonConvert.DeserializeObject<Question>(jsonData);
            if (request == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid JSON");

            // ------------------------------
            // Validate paper
            // ------------------------------
            var paper = db.papers.FirstOrDefault(p => p.id == request.paper_id);
            if (paper == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid paper ID");

            if (paper.no_of_questions <= 0 && !(request.isextra ?? false))
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    "Cannot add regular questions. Paper's number of questions is 0.");

            if (request.marks < 0)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Marks cannot be negative");

            // ------------------------------
            // Difficulty
            // ------------------------------
            var validLevels = new[] { "easy", "medium", "tough" };
            request.difficulty_level = string.IsNullOrEmpty(request.difficulty_level)
                ? "medium"
                : request.difficulty_level.ToLower();

            if (!validLevels.Contains(request.difficulty_level))
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid difficulty level");

            // ------------------------------
            // Validate CLO
            // ------------------------------
            if (request.clo_id != null)
            {
                bool cloExists = db.cloes.Any(c => c.id == request.clo_id && c.course_id == paper.course_id);
                if (!cloExists)
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Invalid CLO ID or CLO does not belong to the same course as the paper");
            }

            // ------------------------------
            // Question limit
            // ------------------------------
            int existingCount = db.Questions.Count(q =>
                q.paper_id == request.paper_id && (q.isextra ?? false) == false);

            if (!(request.isextra ?? false) && existingCount >= paper.no_of_questions)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,
                    $"Cannot add more questions. This paper already has {paper.no_of_questions} regular questions.");
            }

            // ------------------------------
            // Handle image file
            // ------------------------------
            if (httpRequest.Files.Count > 0)
            {
                var file = httpRequest.Files[0];
                string ext = Path.GetExtension(file.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png"};

                if (!allowed.Contains(ext))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid image format");

                string fileName = $"question_{Guid.NewGuid()}{ext}";
                string path = HttpContext.Current.Server.MapPath("~/Uploads/Questions/");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                file.SaveAs(Path.Combine(path, fileName));
                request.image = "/Uploads/Questions/" + fileName;
            }

            // ------------------------------
            // Save
            // ------------------------------
            db.Questions.Add(request);
            db.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                message = "Question added successfully",
                QuestionId = request.id
            });
        }
        catch (Exception ex)
        {
            return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        }
    }








        [HttpPost]
        [Route("api/question/Edit/{id}")]
        public HttpResponseMessage Edit(int id)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // ------------------------------
                // Get existing question
                // ------------------------------
                var question = db.Questions.FirstOrDefault(q => q.id == id);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Question not found");

                // ------------------------------
                // Read JSON string
                // ------------------------------
                var jsonData = httpRequest.Form["question"];
                if (string.IsNullOrEmpty(jsonData))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question data is required");

                var request = JsonConvert.DeserializeObject<Question>(jsonData);
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid JSON");

                // ------------------------------
                // Validate paper
                // ------------------------------
                var paper = db.papers.FirstOrDefault(p => p.id == question.paper_id);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper of this question not found");

                if (request.marks < 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Marks cannot be negative");

                // ------------------------------
                // Validate difficulty
                // ------------------------------
                var validLevels = new[] { "easy", "medium", "tough" };
                request.difficulty_level = string.IsNullOrEmpty(request.difficulty_level)
                    ? "medium"
                    : request.difficulty_level.ToLower();

                if (!validLevels.Contains(request.difficulty_level))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid difficulty level");

                // ------------------------------
                // Validate CLO
                // ------------------------------
                if (request.clo_id != null)
                {
                    bool cloExists = db.cloes.Any(c => c.id == request.clo_id && c.course_id == paper.course_id);
                    if (!cloExists)
                        return Request.CreateResponse(HttpStatusCode.BadRequest,
                            "Invalid CLO ID or CLO does not belong to the same course as the paper");
                }

                // ------------------------------
                // Handle image (optional)
                // ------------------------------
                if (httpRequest.Files.Count > 0)
                {
                    var file = httpRequest.Files[0];
                    string ext = Path.GetExtension(file.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png" };

                    if (!allowed.Contains(ext))
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid image format");

                    string fileName = $"question_{Guid.NewGuid()}{ext}";
                    string path = HttpContext.Current.Server.MapPath("~/Uploads/Questions/");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    file.SaveAs(Path.Combine(path, fileName));
                    question.image = "/Uploads/Questions/" + fileName;
                }

                // ------------------------------
                // Update fields
                // ------------------------------
                question.text = request.text;
                question.description = request.description;
                question.marks = request.marks;
                question.difficulty_level = request.difficulty_level;
                question.clo_id = request.clo_id;
                question.isextra = request.isextra;
                question.editor_id = request.editor_id;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Question updated successfully",
                    QuestionId = question.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }



        [HttpPost]
        [Route("api/question/assign_editor")]
        public HttpResponseMessage AssignEditor([FromBody] AssignEditorRequest request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                bool isTeacher = db.Course_Assignment.Any(ca =>
                    ca.course_id == request.CourseId &&
                    ca.user_id == request.UserId &&
                    ca.session_id == activeSession.id
                );

                if (!isTeacher)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "User is not assigned to this course in the current session");

                var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Question not found");

                var paper = db.papers.FirstOrDefault(p => p.id == question.paper_id);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question's paper not found");

                if (paper.course_id != request.CourseId)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question does not belong to this course");

                question.editor_id = request.UserId;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Editor assigned successfully",
                    QuestionId = question.id,
                    EditorId = request.UserId
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }





        [HttpGet]
        [Route("api/paper/GetPaperDetails/{paperId}")]
        public HttpResponseMessage GetPaperDetails(int paperId)
        {
            try
            {
                // Get paper
                var paper = db.papers.FirstOrDefault(p => p.id == paperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

                // Get course details
                var course = db.courses.FirstOrDefault(c => c.id == paper.course_id);
                if (course == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Course not found");

                // Get session details
                var session = db.sessions.FirstOrDefault(s => s.id == paper.session_id);
                string sessionName = session != null ? session.name : "Unknown";

                // Get questions of this paper and parse numeric fields
                var questions = db.Questions
                                  .Where(q => q.paper_id == paperId)
                                  .Select(q => new
                                  {
                                      Id = q.id,
                                      Text = q.text,
                                      Description = q.description,
                                      Image = q.image,
                                      DifficultyLevel = q.difficulty_level,
                                      CloId = q.clo_id.HasValue ? q.clo_id.Value : 0,
                                      Marks = q.marks.HasValue ? q.marks.Value : 0,
                                      IsExtra = q.isextra.HasValue ? q.isextra.Value : false,
                                      EditorId = q.editor_id.HasValue ? q.editor_id.Value : 0
                                  }).ToList();

                var response = new PaperQuestionsResponse
                {
                    PaperId = paper.id,
                    Term = paper.term,
                    TeacherName = paper.teacher_name,
                    PaperStatus = paper.status,
                    TotalMarks = paper.total_marks.HasValue ? paper.total_marks.Value : 0,
                    NoOfQuestions = paper.no_of_questions.HasValue ? paper.no_of_questions.Value : 0,
                    DegreePrograms = paper.degree_programs,
                    CourseCode = course.course_code,
                    CourseTitle = course.title,
                    CourseCreditHours = course.credit_hours,
                    SessionId = paper.session_id,
                    SessionName = sessionName,
                    Questions = questions
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }










        [HttpPost]
        [Route("api/question/approve_reject")]
        public HttpResponseMessage ApproveOrRejectQuestion([FromBody] QuestionApproveRequestDto request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                string statusLower = request.Status?.ToLower();
                if (statusLower != "approved" && statusLower != "reject")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Status must be 'approved' or 'reject'");

                
                var user = db.Users.FirstOrDefault(u => u.id == request.UserId);
                if (user == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "User not found");

          
                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

            
                var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question not found");

                if (question.paper_id != request.PaperId)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question does not belong to this paper");

                
                var isAssigned = db.Course_Assignment.Any(ca =>
                    ca.course_id == paper.course_id &&
                    ca.user_id == request.UserId &&
                    ca.session_id == paper.session_id
                );

                if (!isAssigned)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "User is not assigned to this course for the paper's session");

                var existing = db.Question_Accept_Reject
                                 .FirstOrDefault(q => q.question_id == request.QuestionId && q.user_id == request.UserId);

                if (existing != null)
                {
                    existing.status = statusLower; 
                }
                else
                {
                    var newEntry = new Question_Accept_Reject
                    {
                        question_id = request.QuestionId,
                        user_id = request.UserId,
                        status = statusLower
                    };
                    db.Question_Accept_Reject.Add(newEntry);
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = $"Question {statusLower} successfully",
                    PaperId = request.PaperId,
                    QuestionId = request.QuestionId,
                    UserId = request.UserId
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }










    }
}
