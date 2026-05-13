using DocumentFormat.OpenXml.Office2016.Excel;
using fypProject.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        [Route("api/paper/Get_Teacher_Courses/{teacherId}")]
        public IHttpActionResult GetTeacherCourses(int teacherId, int? sessionId = null)
        {
            // 1️⃣ Get session
            var session = sessionId.HasValue
                ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
                : db.sessions.FirstOrDefault(s => s.Active);

            if (session == null)
                return BadRequest("No valid session found");

            // 2️⃣ Get courses with paper info
            var courses = db.Course_Assignment
                .Where(ca => ca.user_id == teacherId && ca.session_id == session.id)
                .Join(db.courses,
                      ca => ca.course_id,
                      c => c.id,
                      (ca, c) => new
                      {
                          CourseId = c.id,
                          CourseTitle = c.title,
                          CourseCode = c.course_code,
                          SessionId = session.id,

                          // ✅ Check if teacher created paper
                          CreatePaper = db.paper_Assignment.Any(p =>
                              p.user_id == teacherId &&
                              p.course_id == c.id &&
                              p.session_id == session.id
                          ),


                      })
                .ToList()
                .Select(c => new
                {
                    c.CourseId,
                    c.CourseTitle,
                    c.CourseCode,
                    c.SessionId,
                    c.CreatePaper,

                    // ✅ If paper exists but not created by this teacher → view only
                    ViewOnly = c.CreatePaper
                })
                .Distinct()
                .ToList();

            if (!courses.Any())
                return BadRequest("No courses found for this teacher in this session");

            return Ok(new
            {
                Message = "Teacher courses fetched successfully",
                Session = session.id,
                Courses = courses
            });
        }




















        [HttpGet]
        [Route("api/paper/verify-teacher-teach-course/{teacherId}")]



        public IHttpActionResult VerifyTeacherTeachCourse(int teacherId, int courseId, int? sessionId = null)
        {
            var session = sessionId.HasValue
     ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
     : db.sessions.FirstOrDefault(s => s.Active);

            if (session == null)
                return BadRequest("No valid session found");

            var assignedCourse = db.Course_Assignment
                .Where(ca => ca.user_id == teacherId &&
                             ca.course_id == courseId &&
                             ca.session_id == session.id) // remove null check if session_id is int
                .Join(db.courses,
                      ca => ca.course_id,
                      c => c.id,
                      (ca, c) => new
                      {
                          CourseId = c.id,
                          CourseTitle = c.title,
                          CourseCode = c.course_code,
                          SessionId = session.id,
                          CreditHours = c.credit_hours
                      })
                .FirstOrDefault();

            if (assignedCourse == null)
                return BadRequest("This teacher is not assigned to this course in this session");

            // ✅ Proper paper check
            var CreatePaper = db.paper_Assignment
                .Any(p => p.user_id == teacherId
                       && p.course_id == courseId
                       && p.session_id == session.id);

            return Ok(new
            {
                Message = "Teacher is assigned to this course",
                Session = session.id,
                Course = assignedCourse,
                CreatePaper = CreatePaper
            });
        }

























































        //[HttpPost]
        //[Route("api/paper/CreateOrUpdate")]
        //public HttpResponseMessage CreateOrUpdate([FromBody] paper request)
        //{
        //    try
        //    {
        //        if (request == null)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

        //        // ------------------------------
        //        // Normalize term
        //        // ------------------------------
        //        if (string.IsNullOrWhiteSpace(request.term))
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Term is required");

        //        request.term = request.term.ToLower();
        //        if (request.term != "mid" && request.term != "final")
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Term must be 'mid' or 'final'");

        //        // ------------------------------
        //        // Get Session (Requested OR Active)
        //        // ------------------------------
        //        var session = request.session_id != 0
        //            ? db.sessions.FirstOrDefault(s => s.id == request.session_id)
        //            : db.sessions.FirstOrDefault(s => s.Active);

        //        if (session == null)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

        //        request.session_id = session.id;

        //        // ------------------------------
        //        // Validate course and numeric fields
        //        // ------------------------------
        //        if (!db.courses.Any(c => c.id == request.course_id))
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid course ID");

        //        if (request.total_marks < 0 || request.duration < 0 || (request.no_of_questions ?? 0) < 0)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Numeric fields cannot be negative");

        //        // ------------------------------
        //        // Check existing paper
        //        // ------------------------------
        //        var existingPaper = db.papers.FirstOrDefault(p =>
        //            p.session_id == session.id &&
        //            p.course_id == request.course_id &&
        //            p.term.ToLower() == request.term
        //        );

        //        int mainCount = request.no_of_questions ?? 0;

        //        if (existingPaper != null)
        //        {
        //            // ------------------------------
        //            // Update existing paper
        //            // ------------------------------
        //            existingPaper.paper_Date = request.paper_Date;
        //            existingPaper.Start_time = request.Start_time;
        //            existingPaper.end_time = request.end_time;
        //            existingPaper.duration = request.duration;
        //            existingPaper.degree_programs = request.degree_programs;
        //            existingPaper.total_marks = request.total_marks;
        //            existingPaper.teacher_name = request.teacher_name;
        //            existingPaper.no_of_questions = mainCount;

        //            db.SaveChanges();

        //            // ------------------------------
        //            // Handle main and extra questions
        //            // ------------------------------
        //            var allQuestions = db.Questions
        //                .Where(q => q.paper_id == existingPaper.id)
        //                .OrderBy(q => q.id)
        //                .ToList();

        //            var mainQuestions = allQuestions.Where(q => q.isextra == false).ToList();
        //            var extraQuestions = allQuestions.Where(q => q.isextra == true).ToList();

        //            // Increase main questions if needed
        //            if (mainCount > mainQuestions.Count)
        //            {
        //                int toAdd = mainCount - mainQuestions.Count;
        //                for (int i = 0; i < toAdd; i++)
        //                {
        //                    db.Questions.Add(new Question
        //                    {
        //                        paper_id = existingPaper.id,
        //                        text = "",
        //                        marks = 0,
        //                        difficulty_level = "medium",
        //                        isextra = false
        //                    });
        //                }
        //            }
        //            // Decrease main questions if needed → convert last main to extra
        //            else if (mainCount < mainQuestions.Count)
        //            {
        //                // Keep the first 'mainCount' main questions
        //                var questionsToKeepMain = mainQuestions
        //                    .OrderBy(q => q.id)
        //                    .Take(mainCount)
        //                    .ToList();

        //                // All others (including existing main beyond the first 'mainCount' and any existing extra) become extra
        //                foreach (var q in allQuestions)
        //                {
        //                    if (!questionsToKeepMain.Contains(q))
        //                        q.isextra = true;
        //                    else
        //                        q.isextra = false; // ensure the first 'mainCount' remain main
        //                }
        //            }

        //            db.SaveChanges();

        //            return Request.CreateResponse(HttpStatusCode.OK, new
        //            {
        //                message = "Paper updated successfully",
        //                PaperId = existingPaper.id,
        //                paperExists = true
        //            });
        //        }

        //        // ------------------------------
        //        // Create new paper
        //        // ------------------------------
        //        request.status = "creation";
        //        var newPaper = db.papers.Add(request);
        //        db.SaveChanges();

        //        // ------------------------------
        //        // Automatically create main questions
        //        // ------------------------------
        //        for (int i = 0; i < mainCount; i++)
        //        {
        //            db.Questions.Add(new Question
        //            {
        //                paper_id = newPaper.id,
        //                text = "",
        //                marks = 0,
        //                difficulty_level = "medium",
        //                isextra = false
        //            });
        //        }

        //        db.SaveChanges();

        //        return Request.CreateResponse(HttpStatusCode.OK, new
        //        {
        //            message = "Paper created successfully",
        //            PaperId = newPaper.id,
        //            paperExists = false
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}


        [HttpPost]
        [Route("api/paper/CreateOrUpdate")]
        public HttpResponseMessage CreateOrUpdate([FromBody] paper request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                // =========================
                // TERM
                // =========================
                if (string.IsNullOrWhiteSpace(request.term))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Term is required");

                request.term = request.term.ToLower().Trim();

                if (request.term != "mid" && request.term != "final")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Term must be mid or final");

                // =========================
                // TYPE
                // =========================
                request.type = string.IsNullOrWhiteSpace(request.type)
                    ? "theory"
                    : request.type.ToLower().Trim();

                if (request.type != "theory" && request.type != "lab")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Type must be theory or lab");

                // =========================
                // SESSION
                // =========================
                var session = request.session_id != 0
                    ? db.sessions.FirstOrDefault(s => s.id == request.session_id)
                    : db.sessions.FirstOrDefault(s => s.Active);

                if (session == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                request.session_id = session.id;

                // =========================
                // COURSE CHECK
                // =========================
                if (!db.courses.Any(c => c.id == request.course_id))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid course ID");

                int mainCount = request.no_of_questions ?? 0;

                if ((request.total_marks ?? 0) < 0 ||
                    (request.duration ?? 0) < 0 ||
                    mainCount < 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Numeric fields cannot be negative");
                }

                // =========================
                // FIND EXISTING PAPER (STRICT UNIQUE RULE)
                // =========================
                var existingPaper = db.papers.FirstOrDefault(p =>
                    p.session_id == request.session_id &&
                    p.course_id == request.course_id &&
                    (p.term ?? "").ToLower() == request.term &&
                    (p.type ?? "theory").ToLower() == request.type
                );

                // =========================
                // UPDATE
                // =========================
                if (existingPaper != null)
                {
                    existingPaper.paper_Date = request.paper_Date;
                    existingPaper.Start_time = request.Start_time;
                    existingPaper.end_time = request.end_time;
                    existingPaper.duration = request.duration;
                    existingPaper.degree_programs = request.degree_programs;
                    existingPaper.total_marks = request.total_marks;
                    existingPaper.teacher_name = request.teacher_name;
                    existingPaper.no_of_questions = mainCount;
                    existingPaper.term = request.term;
                    existingPaper.type = request.type;

                    db.SaveChanges();

                    var questions = db.Questions
                        .Where(q => q.paper_id == existingPaper.id)
                        .OrderBy(q => q.id)
                        .ToList();

                    var mainQuestions = questions.Where(q => q.isextra == false).ToList();

                    if (mainCount > mainQuestions.Count)
                    {
                        int add = mainCount - mainQuestions.Count;

                        for (int i = 0; i < add; i++)
                        {
                            db.Questions.Add(new Question
                            {
                                paper_id = existingPaper.id,
                                text = "",
                                marks = 0,
                                difficulty_level = "medium",
                                isextra = false
                            });
                        }
                    }
                    else if (mainCount < mainQuestions.Count)
                    {
                        var keep = mainQuestions.Take(mainCount).ToList();

                        foreach (var q in questions)
                            q.isextra = !keep.Contains(q);
                    }

                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        message = "Paper updated successfully",
                        PaperId = existingPaper.id,
                        type = existingPaper.type,
                        term = existingPaper.term,
                        paperExists = true
                    });
                }

                // =========================
                // CREATE (SAFE CHECK BEFORE INSERT)
                // =========================

                var duplicateCheck = db.papers.Any(p =>
                    p.session_id == request.session_id &&
                    p.course_id == request.course_id &&
                    p.term.ToLower() == request.term &&
                    p.type.ToLower() == request.type
                );

                if (duplicateCheck)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new
                    {
                        message = "Paper already exists for this session, course, term and type"
                    });
                }

                var newPaper = new paper
                {
                    course_id = request.course_id,
                    session_id = request.session_id,
                    term = request.term,
                    type = request.type,
                    paper_Date = request.paper_Date,
                    Start_time = request.Start_time,
                    end_time = request.end_time,
                    duration = request.duration,
                    degree_programs = request.degree_programs,
                    total_marks = request.total_marks,
                    teacher_name = request.teacher_name,
                    no_of_questions = mainCount,
                    status = "creation"
                };

                db.papers.Add(newPaper);
                db.SaveChanges();

                for (int i = 0; i < mainCount; i++)
                {
                    db.Questions.Add(new Question
                    {
                        paper_id = newPaper.id,
                        text = "",
                        marks = 0,
                        difficulty_level = "medium",
                        isextra = false
                    });
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Paper created successfully",
                    PaperId = newPaper.id,
                    type = newPaper.type,
                    term = newPaper.term,
                    paperExists = false
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
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
                // Read JSON string from FormData
                // ------------------------------
                var jsonData = httpRequest.Form["question"];

                if (string.IsNullOrEmpty(jsonData))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question data is required");

                var request = JsonConvert.DeserializeObject<Question>(jsonData);
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid JSON");

                // ------------------------------
                // Validate Paper ID
                // ------------------------------
                if (request.paper_id <= 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper ID is required");
                var paper = db.papers.FirstOrDefault(p => p.id == request.paper_id);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid paper ID");

                // ------------------------------
                // Check number of regular questions
                // ------------------------------
                if (!(request.isextra ?? false) && paper.no_of_questions <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Cannot add regular questions. Paper's number of questions is 0.");
                }

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
                // Limit regular questions
                // ------------------------------
                int existingCount = db.Questions.Count(q =>
                    q.paper_id == request.paper_id && (q.isextra ?? false) == false);

                if (!(request.isextra ?? false) && existingCount >= paper.no_of_questions)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        $"Cannot add more questions. This paper already has {paper.no_of_questions} regular questions.");
                }

                // ------------------------------
                // Handle image upload
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
                    request.image = "/Uploads/Questions/" + fileName;
                }

                // ------------------------------
                // Save Question
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
        //[HttpPost]
        //[Route("api/question/Edit/{id}")]
        //public HttpResponseMessage Edit(int id)
        //{
        //    try
        //    {
        //        var httpRequest = HttpContext.Current.Request;

        //        // ------------------------------
        //        // Get existing question
        //        // ------------------------------
        //        var question = db.Questions.FirstOrDefault(q => q.id == id);
        //        if (question == null)
        //            return Request.CreateResponse(HttpStatusCode.NotFound, "Question not found");

        //        // ------------------------------
        //        // Read JSON string
        //        // ------------------------------
        //        var jsonData = httpRequest.Form["question"];
        //        if (string.IsNullOrEmpty(jsonData))
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Question data is required");

        //        var request = JsonConvert.DeserializeObject<Question>(jsonData);
        //        if (request == null)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid JSON");

        //        // ------------------------------
        //        // Validate paper, marks, difficulty, CLO
        //        // ------------------------------
        //        var paper = db.papers.FirstOrDefault(p => p.id == question.paper_id);
        //        if (paper == null)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper of this question not found");

        //        if (request.marks < 0)
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Marks cannot be negative");

        //        var validLevels = new[] { "easy", "medium", "tough" };
        //        request.difficulty_level = string.IsNullOrEmpty(request.difficulty_level)
        //            ? "medium"
        //            : request.difficulty_level.ToLower();

        //        if (!validLevels.Contains(request.difficulty_level))
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid difficulty level");

        //        if (request.clo_id != null)
        //        {
        //            bool cloExists = db.cloes.Any(c => c.id == request.clo_id && c.course_id == paper.course_id);
        //            if (!cloExists)
        //                return Request.CreateResponse(HttpStatusCode.BadRequest,
        //                    "Invalid CLO ID or CLO does not belong to the same course as the paper");
        //        }

        //        // ------------------------------
        //        // Check Director Flag
        //        // ------------------------------
        //        var isDirectorFlag = httpRequest.Form["isDirector"];
        //        bool isDirector = !string.IsNullOrEmpty(isDirectorFlag) && isDirectorFlag.ToLower() == "true";

        //        // ------------------------------
        //        // Director vs Normal Logic
        //        // ------------------------------
        //        if (isDirector)
        //        {
        //            // ------------------------------
        //            // Check if edited record already exists
        //            // ------------------------------
        //            var edited = db.Question_edited
        //                           .FirstOrDefault(x => x.question_id == question.id);


        //            // ➕ INSERT new record (first time edit)
        //            var newEdited = new Question_edited
        //            {
        //                question_id = question.id,
        //                text = question.text,
        //                image = question.image,
        //                difficulty_level = question.difficulty_level,
        //                clo_id = question.clo_id,
        //                marks = question.marks,
        //                paper_id = question.paper_id,
        //                isextra = question.isextra,
        //                editor_id = question.editor_id
        //            };

        //            db.Question_edited.Add(newEdited);


        //            // ✅ Only allow text update in main question
        //            question.text = request.text;
        //        }
        //        else
        //        {
        //            // Normal update
        //            question.text = request.text;
        //            question.marks = request.marks;
        //            question.difficulty_level = request.difficulty_level;
        //            question.clo_id = request.clo_id;
        //        }

        //        // ------------------------------
        //        // Handle image (ONLY once, no duplicate)
        //        // ------------------------------
        //        string removeImageFlag = httpRequest.Form["removeImage"];

        //        if (httpRequest.Files.Count > 0)
        //        {
        //            // Delete old image
        //            if (!string.IsNullOrEmpty(question.image))
        //            {
        //                string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
        //                if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
        //            }

        //            // Save new image
        //            var file = httpRequest.Files[0];
        //            string ext = Path.GetExtension(file.FileName).ToLower();
        //            string fileName = $"question_{Guid.NewGuid()}{ext}";
        //            string path = HttpContext.Current.Server.MapPath("~/Uploads/Questions/");
        //            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        //            file.SaveAs(Path.Combine(path, fileName));
        //            question.image = "/Uploads/Questions/" + fileName;
        //        }
        //        else if (!string.IsNullOrEmpty(removeImageFlag) && removeImageFlag.ToLower() == "true")
        //        {
        //            if (!string.IsNullOrEmpty(question.image))
        //            {
        //                string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
        //                if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
        //            }

        //            question.image = null;
        //        }

        //        // ------------------------------
        //        // Version update (safe add)
        //        // ------------------------------
        //        question.version = (question.version ?? 0) + 1;

        //        // ------------------------------
        //        // Save changes
        //        // ------------------------------
        //        db.SaveChanges();

        //        return Request.CreateResponse(HttpStatusCode.OK, new
        //        {
        //            message = "Question updated successfully",
        //            QuestionId = question.id
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}
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
                // Paper validation
                // ------------------------------
                var paper = db.papers.FirstOrDefault(p => p.id == question.paper_id);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper of this question not found");

                if (request.marks < 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Marks cannot be negative");

                var validLevels = new[] { "easy", "medium", "tough" };
                request.difficulty_level = string.IsNullOrEmpty(request.difficulty_level)
                    ? "medium"
                    : request.difficulty_level.ToLower();

                if (!validLevels.Contains(request.difficulty_level))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid difficulty level");

                // =====================================================
                // ✅ FIXED CLO HANDLING (MULTIPLE CLO SAFE)
                // =====================================================
                List<int> cloIds = new List<int>();

                try
                {
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonData);

                    if (jsonObj.clo_ids != null)
                    {
                        cloIds = ((IEnumerable<object>)jsonObj.clo_ids)
                            .Select(x =>
                            {
                                int val;
                                return int.TryParse(x?.ToString(), out val) ? val : -1;
                            })
                            .Where(x => x > 0) // remove 0/null/invalid
                            .Distinct()
                            .ToList();
                    }
                }
                catch
                {
                    cloIds = new List<int>();
                }

                // ------------------------------
                // CLO VALIDATION
                // ------------------------------
                if (cloIds.Any())
                {
                    var validCloIds = db.cloes
                        .Where(c => cloIds.Contains(c.id) && c.course_id == paper.course_id)
                        .Select(c => c.id)
                        .ToList();

                    if (validCloIds.Count != cloIds.Count)
                        return Request.CreateResponse(HttpStatusCode.BadRequest,
                            "Invalid CLO IDs or CLOs not in same course");
                }

                // ------------------------------
                // Topic validation
                // ------------------------------
                if (request.topic_id != null)
                {
                    bool topicExists = db.topics.Any(t =>
                        t.id == request.topic_id &&
                        t.course_id == paper.course_id);

                    if (!topicExists)
                        return Request.CreateResponse(HttpStatusCode.BadRequest,
                            "Invalid Topic ID");
                }

                // ------------------------------
                // Director flag
                // ------------------------------
                var isDirectorFlag = httpRequest.Form["isDirector"];
                bool isDirector = !string.IsNullOrEmpty(isDirectorFlag) &&
                                  isDirectorFlag.ToLower() == "true";

                // ------------------------------
                // MAIN UPDATE LOGIC
                // ------------------------------
                if (isDirector)
                {
                    var edited = db.Question_edited
                        .FirstOrDefault(x => x.question_id == question.id);

                    if (edited == null)
                    {
                        db.Question_edited.Add(new Question_edited
                        {
                            question_id = question.id,
                            text = question.text,
                            image = question.image,
                            difficulty_level = question.difficulty_level,
                            clo_id = question.clo_id,
                            topic_id = question.topic_id,
                            marks = question.marks,
                            paper_id = question.paper_id,
                            isextra = question.isextra,
                            editor_id = question.editor_id
                        });
                    }

                    question.text = request.text;
                }
                else
                {
                    question.text = request.text;
                    question.marks = request.marks;
                    question.difficulty_level = request.difficulty_level;
                    question.topic_id = request.topic_id;
                }

                // =====================================================
                // ✅ CLO MAPPING FIX (THIS FIXES YOUR ERROR)
                // =====================================================

                var existingClo = db.question_clo
                    .Where(x => x.question_id == question.id)
                    .ToList();

                if (existingClo.Any())
                {
                    db.question_clo.RemoveRange(existingClo);
                }

                if (cloIds.Any())
                {
                    db.question_clo.AddRange(
                        cloIds.Select(cloId => new question_clo
                        {
                            question_id = question.id,
                            clo_id = cloId
                        })
                    );
                }

                // ------------------------------
                // IMAGE HANDLING (UNCHANGED)
                // ------------------------------
                string removeImageFlag = httpRequest.Form["removeImage"];

                if (httpRequest.Files.Count > 0)
                {
                    if (!string.IsNullOrEmpty(question.image))
                    {
                        string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
                        if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
                    }

                    var file = httpRequest.Files[0];
                    string ext = Path.GetExtension(file.FileName).ToLower();
                    string fileName = $"question_{Guid.NewGuid()}{ext}";
                    string path = HttpContext.Current.Server.MapPath("~/Uploads/Questions/");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    file.SaveAs(Path.Combine(path, fileName));
                    question.image = "/Uploads/Questions/" + fileName;
                }
                else if (!string.IsNullOrEmpty(removeImageFlag) && removeImageFlag.ToLower() == "true")
                {
                    if (!string.IsNullOrEmpty(question.image))
                    {
                        string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
                        if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
                    }

                    question.image = null;
                }

                // ------------------------------
                // Version update
                // ------------------------------

                // ------------------------------
                // SAVE
                // ------------------------------
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Question updated successfully",
                    QuestionId = question.id,
                    clo_ids = cloIds
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message
                });
            }
        }













        [HttpPost]
        [Route("api/question/assign_editor")]
        public HttpResponseMessage AssignEditor([FromBody] AssignEditorRequest request)
        {
            try
            {
                if (request == null || request.QuestionId <= 0 || request.UserId <= 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                // Get Question
                var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Question not found");

                // Get Paper
                var paper = db.papers.FirstOrDefault(p => p.id == question.paper_id);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

                // Validate Teacher belongs to this course in active session
                bool isTeacherAssigned = db.Course_Assignment.Any(ca =>
                    ca.course_id == paper.course_id &&
                    ca.user_id == request.UserId &&
                    ca.session_id == activeSession.id
                );

                if (!isTeacherAssigned)
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        "Teacher is not assigned to this course in current session");

                // Assign Editor
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
                // ------------------------------
                // 1️⃣ Get Paper
                // ------------------------------
                var paper = db.papers.FirstOrDefault(p => p.id == paperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Paper not found");

                int courseId = paper.course_id;
                int sessionId = paper.session_id;

                // ------------------------------
                // 2️⃣ Get Teachers
                // ------------------------------
                var teachers = db.Course_Assignment
                    .Where(ca => ca.course_id == courseId && ca.session_id == sessionId)
                    .Join(db.Users,
                          ca => ca.user_id,
                          u => u.id,
                          (ca, u) => new
                          {
                              TeacherId = u.id,
                              TeacherName = u.name
                          })
                    .Distinct()
                    .ToList();

                // ------------------------------
                // 3️⃣ Course & Session
                // ------------------------------
                var course = db.courses.FirstOrDefault(c => c.id == paper.course_id);
                if (course == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Course not found");

                var session = db.sessions.FirstOrDefault(s => s.id == paper.session_id);
                string sessionName = session != null ? session.name : "Unknown";

                // ------------------------------
                // 4️⃣ Question IDs
                // ------------------------------
                var questionIds = db.Questions
                    .Where(q => q.paper_id == paperId)
                    .Select(q => q.id)
                    .ToList();

                // ------------------------------
                // 5️⃣ Approvals
                // ------------------------------
                var approvals = db.Question_Accept_Reject
                    .Where(ar => questionIds.Contains(ar.question_id))
                    .ToList();

                var approvalLookup = approvals
                    .GroupBy(a => a.question_id)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // ------------------------------
                // 6️⃣ EDITED QUESTIONS (ALL RECORDS PER QUESTION)
                // ------------------------------
                var editedDict = db.Question_edited
                 .Where(e => e.question_id.HasValue && questionIds.Contains(e.question_id.Value))
                    .GroupBy(e => e.question_id)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(x => x.id).ToList() // ALL VERSIONS
                    );

                // ------------------------------question
                // 7️⃣ QUESTIONS MAPPING
                // ------------------------------
                var questions = db.Questions
                        .Where(q => q.paper_id == paperId)
                        .ToList()
                        .Select(q =>
                        {
                            editedDict.TryGetValue(q.id, out var edits);
                            approvalLookup.TryGetValue(q.id, out var questionApprovals);

                            return new
                            {
                                Id = q.id,

                                // 🔹 ORIGINAL
                                Text = q.text,
                                Image = q.image,

                                // 🔥 ALL EDITED VERSIONS (FIXED)
                                EditedVersions = edits?.Select(e => new
                                {
                                    e.id,
                                    e.question_id,
                                    e.text,
                                    e.image,
                                    e.editor_id,

                                }).ToList(),

                                // 🔹 LATEST EDIT (optional UI convenience)
                                EditedText = edits?.FirstOrDefault()?.text,

                                // 🔹 FLAG
                                IsEdited = edits != null && edits.Any(),

                                DifficultyLevel = q.difficulty_level,
                                CloId = q.clo_id ?? 0,
                                Marks = q.marks ?? 0,
                                IsExtra = q.isextra ?? false,
                                EditorId = q.editor_id ?? 0,

                                // 🔹 TEACHER STATUSES
                                TeacherStatuses = teachers.Select(t =>
                                {
                                    var status = questionApprovals?
                                        .FirstOrDefault(a => a.user_id == t.TeacherId);

                                    return new
                                    {
                                        TeacherId = t.TeacherId,
                                        TeacherName = t.TeacherName,
                                        Status = status != null ? status.status : "pending"
                                    };
                                }).ToList()
                            };
                        })
                        .ToList();

                // ------------------------------
                // 8️⃣ FINAL RESPONSE
                // ------------------------------
                var response = new PaperQuestionsResponse
                {
                    PaperId = paper.id,
                    Term = paper.term,
                    TeacherName = paper.teacher_name,
                    PaperStatus = paper.status,
                    TotalMarks = paper.total_marks ?? 0,
                    NoOfQuestions = paper.no_of_questions ?? 0,
                    DegreePrograms = paper.degree_programs,
                    PaperSolution = paper.paper_solution,
                    CourseCode = course.course_code,
                    CourseTitle = course.title,
                    CourseCreditHours = course.credit_hours,
                    SessionId = paper.session_id,
                    SessionName = sessionName,
                    Questions = questions,
                    CourseId = paper.course_id
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

                // ✅ Save current user's approval/rejection
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

                // 🔹 NEW LOGIC: Check if all questions are approved by all assigned teachers
                var questionsForPaper = db.Questions.Where(q => q.paper_id == request.PaperId).ToList();
                bool allApproved = true;

                foreach (var q in questionsForPaper)
                {
                    // Get all assignments (teachers) for this course/session
                    var assignedUsers = db.Course_Assignment
                        .Where(ca => ca.course_id == paper.course_id && ca.session_id == paper.session_id)
                        .Select(ca => ca.user_id)
                        .ToList();

                    // Get approvals for this question
                    var approvals = db.Question_Accept_Reject
                        .Where(ar => ar.question_id == q.id && ar.status == "approved")
                        .Select(ar => ar.user_id)
                        .ToList();

                    // If any assigned user has NOT approved, set allApproved = false
                    if (!assignedUsers.All(u => approvals.Contains(u)))
                    {
                        allApproved = false;
                        break;
                    }
                }

                if (allApproved && paper.status != "Submitted")
                {
                    paper.status = "Submitted";
                    db.SaveChanges();
                }

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = $"Question {statusLower} successfully",
                    PaperId = request.PaperId,
                    QuestionId = request.QuestionId,
                    UserId = request.UserId,
                    paperStatus = paper.status, // ✅ send updated paper status
                    allApproved
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpDelete]
        [Route("api/question/Delete/{id}")]
        public HttpResponseMessage DeleteQuestion(int id)
        {
            // ✅ Validate ID
            if (id <= 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid question ID. ID must be greater than zero.");
            }

            // ✅ Fetch question from database
            var question = db.Questions.FirstOrDefault(q => q.id == id);

            if (question == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, $"Question with ID {id} not found.");
            }

            // ✅ Remove question
            db.Questions.Remove(question);
            db.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                message = "Question deleted successfully.",
                questionId = id
            });
        }



























        [HttpGet]
        [Route("api/paper/get_PaperInFo_details/{courseId}")]
        public HttpResponseMessage GetPaperInfoDetails(int courseId, string term = "mid", string type = "theory")
        {
            try
            {
                // 1️⃣ Active session
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                int? activeSessionId = activeSession?.id;

                // 2️⃣ Get course
                var course = db.courses.FirstOrDefault(c => c.id == courseId);
                if (course == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { message = "Course not found" });

                // 3️⃣ Get paper (SAFE + NULL PROTECTED)
                var paper = db.papers
                    .Where(p =>
                        p.course_id == courseId &&
                        (p.term ?? "").ToLower() == term.ToLower() &&
                        (p.type ?? "theory").ToLower() == type.ToLower() &&
                        (!activeSessionId.HasValue || p.session_id == activeSessionId)
                    )
                    .Select(p => new
                    {
                        paperId = p.id,
                        paperStatus = p.status,
                        courseTitle = course.title,
                        courseCode = course.course_code,
                        sessionId = p.session_id,
                        sessionName = p.session != null ? p.session.name : "",
                        examDate = p.paper_Date,
                        startTime = p.Start_time,
                        endTime = p.end_time,
                        duration = p.duration,
                        degreeProgram = p.degree_programs,
                        totalMarks = p.total_marks,
                        teachersName = p.teacher_name,
                        term = p.term,
                        type = p.type,
                        noOfQuestions = p.no_of_questions,
                        paperExists = true
                    })
                    .FirstOrDefault();

                // 4️⃣ If paper exists and valid → return it
                if (paper != null &&
                    !string.IsNullOrEmpty(paper.teachersName) &&
                    (paper.noOfQuestions ?? 0) > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, paper);
                }

                // 5️⃣ Fallback teachers
                var teacherNames = db.Course_Assignment
                    .Where(ca =>
                        ca.course_id == courseId &&
                        (!activeSessionId.HasValue || ca.session_id == activeSessionId)
                    )
                    .Join(db.Users,
                        ca => ca.user_id,
                        u => u.id,
                        (ca, u) => u.name)
                    .Distinct()
                    .ToList();

                string teachersString = teacherNames.Any()
                    ? string.Join(", ", teacherNames)
                    : "";

                // 6️⃣ Default response (SAFE)
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    paperId = paper?.paperId ?? 0,
                    courseTitle = course.title,
                    courseCode = course.course_code,
                    sessionId = activeSessionId,
                    sessionName = activeSession?.name ?? "",
                    examDate = "",
                    startTime = "",
                    endTime = "",
                    duration = "",
                    degreeProgram = "",
                    totalMarks = "",
                    teachersName = teachersString,
                    term = term.ToLower(),

                    // 🔥 IMPORTANT FIX (NO NULL CRASH)
                    type = paper?.type ?? type,

                    noOfQuestions = 0,
                    paperExists = false
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { message = ex.Message });
            }
        }



        [HttpPost]
        [Route("api/paper/ReorderQuestionsNoOrder/{paperId}")]
        public IHttpActionResult ReorderQuestionsNoOrder(int paperId, [FromBody] List<int> newOrderIds)
        {
            try
            {
                // Fetch all questions for the paper
                var questions = db.Questions
                                  .Where(q => q.paper_id == paperId)
                                  .OrderBy(q => q.id)
                                  .ToList();

                if (questions.Count != newOrderIds.Count)
                    return BadRequest("Mismatch in questions count.");

                // Build dictionary for fast lookup
                var questionMap = questions.ToDictionary(q => q.id, q => q);

                // Create a temporary list to hold the source data safely
                var tempDataList = new List<Question>();
                foreach (var id in newOrderIds)
                {
                    if (!questionMap.ContainsKey(id))
                        return BadRequest($"Question ID {id} not found.");

                    var q = questionMap[id];
                    tempDataList.Add(new Question
                    {
                        text = q.text,
                        image = q.image,
                        difficulty_level = q.difficulty_level,
                        clo_id = q.clo_id,
                        marks = q.marks,
                        isextra = q.isextra,
                        editor_id = q.editor_id,
                        version = q.version,
                        paper_id = q.paper_id,
                        // id is intentionally left as-is (do not change)
                    });
                }

                // Now assign temp data to the original questions safely
                for (int i = 0; i < questions.Count; i++)
                {
                    var target = questions[i];
                    var source = tempDataList[i];

                    target.text = source.text;
                    target.image = source.image;
                    target.difficulty_level = source.difficulty_level;
                    target.clo_id = source.clo_id;
                    target.marks = source.marks;
                    target.isextra = source.isextra;
                    target.editor_id = source.editor_id;
                    target.version = source.version;
                }

                db.SaveChanges();

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }










        [HttpGet]
        [Route("api/paper/get-teachers/{courseId}")]
        public HttpResponseMessage GetTeachersByCourse(int courseId)
        {
            try
            {
                // 1️⃣ Find the active session
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                int activeSessionId = activeSession.id;

                // 2️⃣ Get teacher IDs who already have a paper assigned for this course/session
                var paperAssignedTeacherIds = db.paper_Assignment
                    .Where(pa => pa.course_id == courseId && pa.session_id == activeSessionId)
                    .Select(pa => pa.user_id)
                    .ToList();

                // 3️⃣ Get teachers assigned to this course in the active session excluding those with paper
                var teachers = db.Course_Assignment
                    .Where(ca => ca.course_id == courseId &&
                                 ca.session_id == activeSessionId &&
                                 !paperAssignedTeacherIds.Contains(ca.user_id))
                    .Join(db.Users,
                          ca => ca.user_id,
                          u => u.id,
                          (ca, u) => new { u.id, u.name })
                    .Distinct()
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, teachers);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { message = ex.Message });
            }
        }






        [HttpGet]
        [Route("api/question/get_assigned_editor/{questionId}")]
        public IHttpActionResult GetAssignedEditor(int questionId)
        {
            var question = db.Questions.FirstOrDefault(q => q.id == questionId);
            if (question == null) return NotFound();

            return Ok(new { editorId = question.editor_id ?? 0 }); // 0 means not assigned
        }


































































































        [HttpPost]
        [Route("api/paper/updateStatus/{paperId}")]
        public IHttpActionResult UpdateStatus(int paperId)
        {
            var paper = db.papers.Find(paperId);

            if (paper == null)
                return NotFound();

            // Only allow change from Creation
            if (paper.status != "creation")
                return BadRequest("Paper is not in Creation state.");

            paper.status = "ReadyForFacultyApprover";

            db.SaveChanges();

            return Ok(new { message = "Paper moved to ReadyForFacultyApprover" });
        }




        [HttpPost]
        [Route("api/paper/sendToFacultyApprover/{paperId}")]
        public IHttpActionResult SendToFacultyApprover(int paperId)
        {
            var paper = db.papers.Find(paperId);

            if (paper == null)
                return NotFound();

            // ✅ Only allow change from ReadyForFacultyApprover
            if (paper.status != "ReadyForFacultyApprover")
                return BadRequest("Paper is not ready to be sent to Faculty Approver.");

            // ✅ Change status
            paper.status = "WaitingForFacultyApprover";

            db.SaveChanges();

            return Ok(new
            {
                message = "Paper sent to Faculty Approver successfully.",
                PaperStatus = paper.status
            });
        }
















        [HttpPost]
        [Route("api/question/director_approve_reject")]
        public HttpResponseMessage DirectorApproveRejectQuestion([FromBody] Director_Question_Accept_RejectDTO request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                // Validate status
                string statusLower = request.Status?.ToLower();
                if (statusLower != "approved" && statusLower != "reject")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Status must be 'approved' or 'reject'");

                // Get user
                var user = db.Users.FirstOrDefault(u => u.id == request.UserId);
                if (user == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "User not found");

                // ✅ Check Director role using Role_Assignment table (your pattern)
                bool isDirector = db.Role_Assignment
                    .Where(ra => ra.user_id == request.UserId)
                    .Join(db.Roles, ra => ra.role_id, r => r.id, (ra, r) => r.name)
                    .Any(role => role == "Director");

                if (!isDirector)
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Only Director can perform this action");

                // Get paper details
                var paper = db.papers.FirstOrDefault(p => p.id == request.PaperId);
                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

                // Get question details
                var question = db.Questions.FirstOrDefault(q => q.id == request.QuestionId);
                if (question == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question not found");

                // Verify question belongs to paper
                if (question.paper_id != request.PaperId)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Question does not belong to this paper");


                // Save Director's approval/rejection
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
                        status = statusLower,

                    };
                    db.Question_Accept_Reject.Add(newEntry);
                }

                db.SaveChanges();



                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    message = $"Question {statusLower} by Director successfully",
                    data = new
                    {
                        PaperId = request.PaperId,
                        QuestionId = request.QuestionId,
                        UserId = request.UserId,
                        Status = statusLower,

                        paperStatus = paper.status,

                    }
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }


























        public class DirectorApproveRequestDto
        {
            public int UserId { get; set; }
        }



        [HttpPost]
        [Route("api/paper/directorApprove/{paperId}")]
        public IHttpActionResult DirectorApprove(int paperId, [FromBody] DirectorApproveRequestDto request)
        {
            if (request == null)
                return BadRequest("Invalid request data");

            try
            {
                var paper = db.papers.Find(paperId);
                if (paper == null)
                    return NotFound();

                // Validate director
                var director = db.Users.FirstOrDefault(u => u.id == request.UserId);
                if (director == null)
                    return BadRequest("User not found");

                // Check Director role
                bool isDirector = db.Role_Assignment
                    .Where(ra => ra.user_id == request.UserId)
                    .Join(db.Roles, ra => ra.role_id, r => r.id, (ra, r) => r.name)
                    .Any(role => role == "Director");

                if (!isDirector)
                    return Unauthorized();

                // Check paper status
                if (paper.status != "Submitted")
                    return BadRequest($"Paper is not in Submitted state. Current state: {paper.status}");

                // Get questions
                var questions = db.Questions.Where(q => q.paper_id == paperId).ToList();
                if (!questions.Any())
                    return BadRequest("No questions found for this paper.");


                var questionIds = db.Questions
    .Where(q => q.paper_id == paperId)
    .Select(q => q.id)
    .ToList();
                // ✅ Check if Director has rejected any question
                bool hasRejection = db.Question_Accept_Reject
         .Any(qar => questionIds.Contains(qar.question_id)
                  && qar.user_id == request.UserId
                  && qar.status == "reject");

                if (hasRejection)
                {
                    paper.status = "Rejected";
                    db.SaveChanges();

                    return Ok(new
                    {
                        message = "Paper rejected because Director rejected one or more questions.",
                        PaperStatus = paper.status,
                        RejectedBy = director.name,
                        RejectedDate = DateTime.Now
                    });
                }

                // ✅ Check if Director has approved ALL questions
                var directorApprovals = db.Question_Accept_Reject
                    .Where(qar => qar.user_id == request.UserId)
                    .ToList();

                var allQuestionsApprovedByDirector = questions.All(q =>
                    directorApprovals.Any(da => da.question_id == q.id && da.status == "approved")
                );

                if (!allQuestionsApprovedByDirector)
                {
                    // Find which questions are not approved by Director
                    var unapprovedQuestions = questions
                        .Where(q => !directorApprovals.Any(da => da.question_id == q.id && da.status == "approved"))
                        .Select(q => new { q.id, q.text })
                        .ToList();

                    return BadRequest(
                    "Cannot approve paper. Director has not approved all questions."
                     );
                }

                // ✅ Director has approved ALL questions - Approve the paper
                paper.status = "Approved";
                db.SaveChanges();

                return Ok(new
                {
                    message = "Paper approved successfully by Director. All questions have been approved.",
                    PaperStatus = paper.status,
                    ApprovedBy = director.name,
                    ApprovedDate = DateTime.Now,
                    TotalQuestions = questions.Count,
                    TotalDirectorApprovals = directorApprovals.Count(a => a.status == "approved")
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

































































        [HttpPost]
        [Route("api/paper/upload_solution")]
        public HttpResponseMessage UploadSolution()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // 🔹 Get paper_id from FormData
                var paperIdStr = httpRequest.Form["paper_id"];

                if (string.IsNullOrEmpty(paperIdStr))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper ID is required");

                int paperId = Convert.ToInt32(paperIdStr);

                var paper = db.papers.FirstOrDefault(p => p.id == paperId);

                if (paper == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Paper not found");

                // 🔹 Check file exists
                if (httpRequest.Files.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No file uploaded");

                var file = httpRequest.Files[0];

                // 🔹 Validate extension
                string ext = Path.GetExtension(file.FileName).ToLower();
                var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };

                if (!allowed.Contains(ext))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid file type");

                // 🔥 DELETE OLD FILE (optional but recommended)
                if (!string.IsNullOrEmpty(paper.paper_solution))
                {
                    string oldPath = HttpContext.Current.Server.MapPath("~" + paper.paper_solution);
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }

                // 🔹 Create folder
                string folderPath = HttpContext.Current.Server.MapPath("~/Uploads/PaperSolutions/");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // 🔹 Unique file name
                string fileName = $"solution_{Guid.NewGuid()}{ext}";

                // 🔹 Full path
                string fullPath = Path.Combine(folderPath, fileName);

                // 🔹 Save file
                file.SaveAs(fullPath);

                // 🔹 Save path in DB
                paper.paper_solution = "/Uploads/PaperSolutions/" + fileName;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "File uploaded successfully",
                    path = paper.paper_solution
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }





}