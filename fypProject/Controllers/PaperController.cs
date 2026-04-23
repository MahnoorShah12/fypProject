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
            // 1️⃣ Get session (requested OR active)
            var session = sessionId.HasValue
       ? db.sessions.FirstOrDefault(s => s.id == sessionId.Value)
       : db.sessions.FirstOrDefault(s => s.Active);

            if (session == null)
                return BadRequest("No valid session found");

            var courses = db.Course_Assignment
                .Where(ca => ca.user_id == teacherId &&
                    (ca.session_id == session.id)) // remove '|| ca.session_id == null' if session_id is int
                .Join(db.courses,
                      ca => ca.course_id,
                      c => c.id,
                      (ca, c) => new
                      {
                          CourseId = c.id,
                          CourseTitle = c.title,
                          SessionId = session.id
                      })
                .Distinct()
                .ToList();


            if (!courses.Any())
                return BadRequest("No courses found for this teacher in this session");

            // 3️⃣ Return result
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
                          SessionId = session.id
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





















































        [HttpPost]
        [Route("api/paper/CreateOrUpdate")]
        public HttpResponseMessage CreateOrUpdate([FromBody] paper request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                // ------------------------------
                // Normalize term
                // ------------------------------
                if (string.IsNullOrWhiteSpace(request.term))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Term is required");

                request.term = request.term.ToLower();
                if (request.term != "mid" && request.term != "final")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Term must be 'mid' or 'final'");

                // ------------------------------
                // Get Session (Requested OR Active)
                // ------------------------------
                var session = request.session_id != 0
                    ? db.sessions.FirstOrDefault(s => s.id == request.session_id)
                    : db.sessions.FirstOrDefault(s => s.Active);

                if (session == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No active session found");

                request.session_id = session.id;

                // ------------------------------
                // Validate course and numeric fields
                // ------------------------------
                if (!db.courses.Any(c => c.id == request.course_id))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid course ID");

                if (request.total_marks < 0 || request.duration < 0 || (request.no_of_questions ?? 0) < 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Numeric fields cannot be negative");

                // ------------------------------
                // Check existing paper
                // ------------------------------
                var existingPaper = db.papers.FirstOrDefault(p =>
                    p.session_id == session.id &&
                    p.course_id == request.course_id &&
                    p.term.ToLower() == request.term
                );

                int mainCount = request.no_of_questions ?? 0;

                if (existingPaper != null)
                {
                    // ------------------------------
                    // Update existing paper
                    // ------------------------------
                    existingPaper.paper_Date = request.paper_Date;
                    existingPaper.Start_time = request.Start_time;
                    existingPaper.end_time = request.end_time;
                    existingPaper.duration = request.duration;
                    existingPaper.degree_programs = request.degree_programs;
                    existingPaper.total_marks = request.total_marks;
                    existingPaper.teacher_name = request.teacher_name;
                    existingPaper.no_of_questions = mainCount;

                    db.SaveChanges();

                    // ------------------------------
                    // Handle main and extra questions
                    // ------------------------------
                    var allQuestions = db.Questions
                        .Where(q => q.paper_id == existingPaper.id)
                        .OrderBy(q => q.id)
                        .ToList();

                    var mainQuestions = allQuestions.Where(q => q.isextra == false).ToList();
                    var extraQuestions = allQuestions.Where(q => q.isextra == true).ToList();

                    // Increase main questions if needed
                    if (mainCount > mainQuestions.Count)
                    {
                        int toAdd = mainCount - mainQuestions.Count;
                        for (int i = 0; i < toAdd; i++)
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
                    // Decrease main questions if needed → convert last main to extra
                    else if (mainCount < mainQuestions.Count)
                    {
                        // Keep the first 'mainCount' main questions
                        var questionsToKeepMain = mainQuestions
                            .OrderBy(q => q.id)
                            .Take(mainCount)
                            .ToList();

                        // All others (including existing main beyond the first 'mainCount' and any existing extra) become extra
                        foreach (var q in allQuestions)
                        {
                            if (!questionsToKeepMain.Contains(q))
                                q.isextra = true;
                            else
                                q.isextra = false; // ensure the first 'mainCount' remain main
                        }
                    }

                    db.SaveChanges();

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        message = "Paper updated successfully",
                        PaperId = existingPaper.id,
                        paperExists = true
                    });
                }

                // ------------------------------
                // Create new paper
                // ------------------------------
                request.status = "creation";
                var newPaper = db.papers.Add(request);
                db.SaveChanges();

                // ------------------------------
                // Automatically create main questions
                // ------------------------------
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
                    paperExists = false
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
                // Validate paper, marks, difficulty, CLO
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

                if (request.clo_id != null)
                {
                    bool cloExists = db.cloes.Any(c => c.id == request.clo_id && c.course_id == paper.course_id);
                    if (!cloExists)
                        return Request.CreateResponse(HttpStatusCode.BadRequest,
                            "Invalid CLO ID or CLO does not belong to the same course as the paper");
                }

                // ------------------------------
                // Handle image
                // ------------------------------
                string removeImageFlag = httpRequest.Form["removeImage"];

                if (httpRequest.Files.Count > 0)
                {
                    // New file uploaded → delete old file if exists
                    if (!string.IsNullOrEmpty(question.image))
                    {
                        string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
                        if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
                    }

                    // Save new image
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
                    // Delete existing image
                    if (!string.IsNullOrEmpty(question.image))
                    {
                        string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
                        if (File.Exists(oldImagePath)) File.Delete(oldImagePath);
                    }

                    question.image = null; // Set DB column to null
                }
                // Case 2: Remove existing image
                else if (!string.IsNullOrEmpty(removeImageFlag) && removeImageFlag.ToLower() == "true")
                {
                    if (!string.IsNullOrEmpty(question.image))
                    {
                        string oldImagePath = HttpContext.Current.Server.MapPath("~" + question.image.TrimStart('/'));
                        if (File.Exists(oldImagePath))
                            File.Delete(oldImagePath);
                    }
                    question.image = null; // Set DB column to null
                }

                // ------------------------------
                // Update other fields
                // ------------------------------
                question.text = request.text;
                question.marks = request.marks;
                question.difficulty_level = request.difficulty_level;
                question.clo_id = request.clo_id;



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
                    paperStatus = paper.status // ✅ send updated paper status
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
        public HttpResponseMessage GetPaperInfoDetails(int courseId, string term = "mid")
        {
            try
            {
                // 1️⃣ Get the course
                var course = db.courses.FirstOrDefault(c => c.id == courseId);
                if (course == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { message = "Course not found" });

                // 2️⃣ Get the paper for this course and term
                var paper = db.papers
                    .Where(p => p.course_id == courseId && p.term.ToLower() == term.ToLower())
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
                        noOfQuestions = p.no_of_questions,
                        paperExists = true
                    })
                    .FirstOrDefault();

                // 3️⃣ Check if paper exists AND has meaningful data
                if (paper != null && !string.IsNullOrEmpty(paper.teachersName) && (paper.noOfQuestions ?? 0) > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, paper);
                }

                // 4️⃣ Fallback → fetch teachers from Course_Assignment for active session
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                int? activeSessionId = activeSession?.id;

                var teacherNames = db.Course_Assignment
                    .Where(ca => ca.course_id == courseId && (activeSessionId == null || ca.session_id == activeSessionId))
                    .Join(db.Users,
                          ca => ca.user_id,
                          u => u.id,
                          (ca, u) => u.name)
                    .Distinct()
                    .ToList();

                string teachersString = teacherNames.Any() ? string.Join(", ", teacherNames) : "";

                // 5️⃣ Return default paper info with teacher names
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
        [Route("api/paper/directorApprove/{paperId}")]
        public IHttpActionResult DirectorApprove(int paperId)
        {
            var paper = db.papers.Find(paperId);

            if (paper == null)
                return NotFound();

            // ✅ Only allow change from Submitted
            if (paper.status != "Submitted")
                return BadRequest("Paper is not in Submitted state.");

            // ✅ Change status
            paper.status = "Approved";

            db.SaveChanges();

            return Ok(new
            {
                message = "Paper approved successfully by Director.",
                PaperStatus = paper.status
            });
        }











    }



















}
