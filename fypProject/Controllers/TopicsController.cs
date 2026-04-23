using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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
    public class TopicController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();


        ///eisha



        //        [HttpGet]
        //        [Route("api/topic/byCourseGet/{courseId}")]
        //        public IHttpActionResult GetTopics(int courseId)
        //        {


        //            var activeSession = db.sessions.Where(s => s.Active == true).FirstOrDefault();


        //            var topics = db.topics
        //                .Where(t => t.course_id == courseId && t.session_id == activeSession.id)
        //                .Select(t => new { t.id, t.description })
        //                .ToList();
        //            return Ok(topics);
        //        }

        //        [HttpPost]
        //        [Route("api/topic/addTopic/{courseId}")]
        //        public IHttpActionResult AddTopic(int courseId, topic model)
        //        {



        //            // 1️⃣ Validate body
        //            if (model == null)
        //            {
        //                return BadRequest("Request body is empty.");
        //            }

        //            // 2️⃣ Validate required fields
        //            if (string.IsNullOrWhiteSpace(model.description))
        //            {
        //                return BadRequest("CLO description is required.");
        //            }



        //            var activeSession = db.sessions.Where(s => s.Active == true).FirstOrDefault();
        //            model.session_id = activeSession.id;
        //            model.course_id = courseId;
        //            db.topics.Add(model);
        //            db.SaveChanges();
        //            return Ok("Topic Added");
        //        }




        //        [HttpPost]
        //        [Route("api/topic/editTopic/{topicId}")]
        //        public IHttpActionResult UpdateTopic(int topicId, topic model)
        //        {
        //            try
        //            {
        //                // 1️⃣ Validate request body
        //                if (model == null)
        //                    return BadRequest("Request body is empty.");

        //                // 2️⃣ Find topic
        //                var existingTopic = db.topics.Find(topicId);
        //                if (existingTopic == null)
        //                    return NotFound();

        //                // 3️⃣ Validate required fields
        //                if (string.IsNullOrWhiteSpace(model.description))
        //                    return BadRequest("Topic description is required.");

        //                // 4️⃣ Update fields
        //                existingTopic.description = model.description;

        //                // 5️⃣ Save changes
        //                var result = db.SaveChanges();
        //                if (result <= 0)
        //                    return InternalServerError(new Exception("Topic was not updated. No changes detected."));

        //                // 6️⃣ Success response
        //                return Ok(new
        //                {
        //                    success = true,
        //                    message = "Topic updated successfully",
        //                    topicId = topicId
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                return InternalServerError(ex);
        //            }
        //        }






















        //        [HttpPost] // better than POST for deletion
        //        [Route("api/topic/deleteTopic/{topicId}")]
        //        public IHttpActionResult DeleteTopic(int topicId)
        //        {
        //            try
        //            {
        //                // 1️⃣ Find Topic
        //                var topic = db.topics.Find(topicId);
        //                if (topic == null)
        //                {
        //                    return NotFound();
        //                }

        //                // 2️⃣ Remove Topic
        //                db.topics.Remove(topic);

        //                // 3️⃣ Save changes
        //                var result = db.SaveChanges();

        //                // 4️⃣ Check if deletion actually happened
        //                if (result <= 0)
        //                {
        //                    return InternalServerError(new Exception("Topic could not be deleted."));
        //                }

        //                // 5️⃣ Success response
        //                return Ok(new
        //                {
        //                    success = true,
        //                    message = "Topic deleted successfully",
        //                    topicId = topicId
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                // 6️⃣ Catch unexpected errors
        //                return InternalServerError(ex);
        //            }
        //        }




        //        // GET: api/Topics/get_Topics/1?session_id=2
        //        [HttpGet]
        //        [Route("api/Topics/get_Topics/{courseId}")]
        //        public IHttpActionResult GetTopics(int courseId, int? session_id = null)
        //        {
        //            try
        //            {
        //                int sessionIdToUse;

        //                if (session_id.HasValue)
        //                {
        //                    // Use session_id from query
        //                    sessionIdToUse = session_id.Value;
        //                }
        //                else
        //                {
        //                    // Get active session
        //                    var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);

        //                    if (activeSession != null)
        //                    {
        //                        sessionIdToUse = activeSession.id;
        //                    }
        //                    else
        //                    {
        //                        // No active session found, return empty list
        //                        return Ok(new List<object>());
        //                    }
        //                }

        //                // Fetch topics for the determined session
        //                var topics = db.topics
        //                               .Where(t => t.course_id == courseId && t.session_id == sessionIdToUse)
        //                               .Select(t => new
        //                               {
        //                                   t.id,
        //                                   t.description
        //                               })
        //                               .ToList();

        //                return Ok(topics);
        //            }
        //            catch (Exception ex)
        //            {
        //                return InternalServerError(ex);
        //            }
        //        }


















        //        [HttpPost]
        //        [Route("api/Topics/topicteach")]
        //        public IHttpActionResult teachTopics(TeachTopicRequest request)
        //        {
        //            try
        //            {
        //                if (request == null)
        //                    return BadRequest("Invalid request");

        //                // 🔹 Validate required fields
        //                if (!request.section_id.HasValue)
        //                    return BadRequest("Section ID is required");

        //                if (!request.course_id.HasValue || !request.user_id.HasValue || !request.topic_id.HasValue)
        //                    return BadRequest("Course ID, User ID, and Topic ID are required");

        //                // 🔹 Determine session
        //                int sessionId;
        //                if (request.session_id.HasValue)
        //                {
        //                    sessionId = request.session_id.Value;
        //                }
        //                else
        //                {
        //                    var activeSession = db.sessions.FirstOrDefault(s => s.Active);
        //                    if (activeSession == null)
        //                        return BadRequest("No active session found");
        //                    sessionId = activeSession.id;
        //                }

        //                // 🔹 Get course assignment for this section, course, user, and session
        //                var courseAssignment = db.Course_Assignment
        //                    .FirstOrDefault(x =>
        //                        x.course_id == request.course_id.Value &&
        //                        x.section_id == request.section_id.Value &&
        //                        x.user_id == request.user_id.Value &&
        //                        x.session_id == sessionId);

        //                if (courseAssignment == null)
        //                    return BadRequest("Course Assignment not found for this section and session");

        //                // 🔹 Check if topic_teach already exists
        //                var topicTeachExisting = db.topic_teach
        //                    .FirstOrDefault(t => t.course_assignment_id == courseAssignment.id && t.topic_id == request.topic_id.Value);



        //                if (topicTeachExisting == null)
        //                {
        //                    // Insert new topic
        //                    db.topic_teach.Add(new topic_teach
        //                    {
        //                        course_assignment_id = courseAssignment.id,
        //                        topic_id = request.topic_id.Value
        //                    });
        //                }

        //                db.SaveChanges();

        //                return Ok(new { message = "Topic saved successfully" });
        //            }
        //            catch (Exception ex)
        //            {
        //                return InternalServerError(ex);
        //            }
        //        }





        //        [HttpGet]
        //        [Route("api/Topics/getAssignedTopics")]
        //        public IHttpActionResult GetAssignedTopics(int section_id, int course_id, int user_id)
        //        {
        //            try
        //            {
        //                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
        //                if (activeSession == null)
        //                    return Ok(new List<int>());

        //                var courseAssignment = db.Course_Assignment.FirstOrDefault(x =>
        //                    x.course_id == course_id &&
        //                    x.section_id == section_id &&
        //                    x.user_id == user_id &&
        //                    x.session_id == activeSession.id);

        //                if (courseAssignment == null)
        //                    return Ok(new List<int>());

        //                var topicIds = db.topic_teach
        //                    .Where(t => t.course_assignment_id == courseAssignment.id)
        //                    .Select(t => t.topic_id)
        //                    .ToList();

        //                return Ok(topicIds);
        //            }
        //            catch (Exception ex)
        //            {
        //                return InternalServerError(ex);
        //            }
        //        }







        //        [HttpPost]
        //        [Route("api/Topics/removeTopicTeach")]
        //        public IHttpActionResult RemoveTopicTeach(TeachTopicRequest request)
        //        {
        //            try
        //            {
        //                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
        //                if (activeSession == null)
        //                    return BadRequest("No active session");

        //                var courseAssignment = db.Course_Assignment.FirstOrDefault(x =>
        //                    x.course_id == request.course_id &&
        //                    x.section_id == request.section_id &&
        //                    x.user_id == request.user_id &&
        //                    x.session_id == activeSession.id);

        //                if (courseAssignment == null)
        //                    return BadRequest("Assignment not found");

        //                var topicTeach = db.topic_teach.FirstOrDefault(t =>
        //                    t.course_assignment_id == courseAssignment.id &&
        //                    t.topic_id == request.topic_id);

        //                if (topicTeach != null)
        //                {
        //                    db.topic_teach.Remove(topicTeach);
        //                    db.SaveChanges();
        //                }

        //                return Ok(new { message = "Topic removed successfully" });
        //            }
        //            catch (Exception ex)
        //            {
        //                return InternalServerError(ex);
        //            }
        //        }






        //        [HttpGet]
        //        [Route("api/Topics/getCommonTopics/{courseId}")]
        //        public IHttpActionResult GetCommonTopics(int courseId, int? session_id = null)
        //        {
        //            // 1️⃣ Determine session
        //            int activeSessionId = session_id ?? db.sessions
        //                                                .Where(s => s.Active) // assuming Active flag exists
        //                                                .Select(s => s.id)
        //                                                .FirstOrDefault();

        //            if (activeSessionId == 0)
        //                return Json(new List<int>()); // no active session found

        //            // 2️⃣ Get all sections assigned to this course in this session
        //            var courseSections = db.Course_Assignment
        //                                   .Where(ca => ca.course_id == courseId && ca.session_id == activeSessionId)
        //                                   .Select(ca => ca.section_id)
        //                                   .Distinct()
        //                                   .ToList();

        //            if (courseSections.Count == 0)
        //                return Json(new List<int>()); // no sections assigned

        //            // 3️⃣ Get all topics for this course (session optional)
        //            var courseTopics = db.topics
        //                                 .Where(t => t.course_id == courseId && (t.session_id == null || t.session_id == activeSessionId))
        //                                 .Select(t => t.id)
        //                                 .ToList();

        //            if (courseTopics.Count == 0)
        //                return Json(new List<int>()); // no topics for this course

        //            var commonTopicIds = new List<int>();

        //            // 4️⃣ Check topics taught in all sections
        //            foreach (var topicId in courseTopics)
        //            {
        //                var taughtSectionsCount = db.topic_teach
        //                                            .Where(tt => tt.topic_id == topicId
        //                                                        && tt.Course_Assignment.session_id == activeSessionId)
        //                                            .Select(tt => tt.Course_Assignment.section_id)
        //                                            .Distinct()
        //                                            .Count();

        //                if (taughtSectionsCount == courseSections.Count)
        //                    commonTopicIds.Add(topicId);
        //            }

        //            return Json(commonTopicIds); // ✅ return JSON
        //        }
        //    }

        //}


        [HttpGet]
        [Route("api/topic/byCourseGet/{courseId}")]
        public IHttpActionResult GetTopics(int courseId)
        {


            var activeSession = db.sessions.Where(s => s.Active == true).FirstOrDefault();


            var topics = db.topics
                .Where(t => t.course_id == courseId && t.session_id == activeSession.id)
                .Select(t => new { t.id, t.description })
                .ToList();
            return Ok(topics);
        }

        [HttpPost]
        [Route("api/topic/addTopic/{courseId}")]
        public IHttpActionResult AddTopic(int courseId, topic model)
        {



            // 1️⃣ Validate body
            if (model == null)
            {
                return BadRequest("Request body is empty.");
            }

            // 2️⃣ Validate required fields
            if (string.IsNullOrWhiteSpace(model.description))
            {
                return BadRequest("CLO description is required.");
            }



            var activeSession = db.sessions.Where(s => s.Active == true).FirstOrDefault();
            model.session_id = activeSession.id;
            model.course_id = courseId;
            db.topics.Add(model);
            db.SaveChanges();
            return Ok("Topic Added");
        }




        [HttpPost]
        [Route("api/topic/editTopic/{topicId}")]
        public IHttpActionResult UpdateTopic(int topicId, topic model)
        {
            try
            {
                // 1️⃣ Validate request body
                if (model == null)
                    return BadRequest("Request body is empty.");

                // 2️⃣ Find topic
                var existingTopic = db.topics.Find(topicId);
                if (existingTopic == null)
                    return NotFound();

                // 3️⃣ Validate required fields
                if (string.IsNullOrWhiteSpace(model.description))
                    return BadRequest("Topic description is required.");

                // 4️⃣ Update fields
                existingTopic.description = model.description;

                // 5️⃣ Save changes
                var result = db.SaveChanges();
                if (result <= 0)
                    return InternalServerError(new Exception("Topic was not updated. No changes detected."));

                // 6️⃣ Success response
                return Ok(new
                {
                    success = true,
                    message = "Topic updated successfully",
                    topicId = topicId
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }






















        [HttpPost] // better than POST for deletion
        [Route("api/topic/deleteTopic/{topicId}")]
        public IHttpActionResult DeleteTopic(int topicId)
        {
            try
            {
                // 1️⃣ Find Topic
                var topic = db.topics.Find(topicId);
                if (topic == null)
                {
                    return NotFound();
                }

                // 2️⃣ Remove Topic
                db.topics.Remove(topic);

                // 3️⃣ Save changes
                var result = db.SaveChanges();

                // 4️⃣ Check if deletion actually happened
                if (result <= 0)
                {
                    return InternalServerError(new Exception("Topic could not be deleted."));
                }

                // 5️⃣ Success response
                return Ok(new
                {
                    success = true,
                    message = "Topic deleted successfully",
                    topicId = topicId
                });
            }
            catch (Exception ex)
            {
                // 6️⃣ Catch unexpected errors
                return InternalServerError(ex);
            }
        }




        // GET: api/Topics/get_Topics/1?session_id=2
        [HttpGet]
        [Route("api/Topics/get_Topics/{courseId}")]
        public IHttpActionResult GetTopics(int courseId, int? session_id = null)
        {
            try
            {
                int sessionIdToUse;

                if (session_id.HasValue)
                {
                    // Use session_id from query
                    sessionIdToUse = session_id.Value;
                }
                else
                {
                    // Get active session
                    var activeSession = db.sessions.FirstOrDefault(s => s.Active == true);

                    if (activeSession != null)
                    {
                        sessionIdToUse = activeSession.id;
                    }
                    else
                    {
                        // No active session found, return empty list
                        return Ok(new List<object>());
                    }
                }

                // Fetch topics for the determined session
                var topics = db.topics
                               .Where(t => t.course_id == courseId && t.session_id == sessionIdToUse)
                               .Select(t => new
                               {
                                   t.id,
                                   t.description
                               })
                               .ToList();

                return Ok(topics);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


















        [HttpPost]
        [Route("api/Topics/topicteach")]
        public IHttpActionResult teachTopics(TeachTopicRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Invalid request");

                // 🔹 Validate required fields
                if (!request.section_id.HasValue)
                    return BadRequest("Section ID is required");

                if (!request.course_id.HasValue || !request.user_id.HasValue || !request.topic_id.HasValue)
                    return BadRequest("Course ID, User ID, and Topic ID are required");

                // 🔹 Determine session
                int sessionId;
                if (request.session_id.HasValue)
                {
                    sessionId = request.session_id.Value;
                }
                else
                {
                    var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                    if (activeSession == null)
                        return BadRequest("No active session found");
                    sessionId = activeSession.id;
                }

                // 🔹 Get course assignment for this section, course, user, and session
                var courseAssignment = db.Course_Assignment
                    .FirstOrDefault(x =>
                        x.course_id == request.course_id.Value &&
                        x.section_id == request.section_id.Value &&
                        x.user_id == request.user_id.Value &&
                        x.session_id == sessionId);

                if (courseAssignment == null)
                    return BadRequest("Course Assignment not found for this section and session");

                // 🔹 Check if topic_teach already exists
                var topicTeachExisting = db.topic_teach
                    .FirstOrDefault(t => t.course_assignment_id == courseAssignment.id && t.topic_id == request.topic_id.Value);



                if (topicTeachExisting == null)
                {
                    // Insert new topic
                    db.topic_teach.Add(new topic_teach
                    {
                        course_assignment_id = courseAssignment.id,
                        topic_id = request.topic_id.Value
                    });
                }

                db.SaveChanges();

                return Ok(new { message = "Topic saved successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }





        [HttpGet]
        [Route("api/Topics/getAssignedTopics")]
        public IHttpActionResult GetAssignedTopics(int section_id, int course_id, int user_id)
        {
            try
            {
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                    return Ok(new List<int>());

                var courseAssignment = db.Course_Assignment.FirstOrDefault(x =>
                    x.course_id == course_id &&
                    x.section_id == section_id &&
                    x.user_id == user_id &&
                    x.session_id == activeSession.id);

                if (courseAssignment == null)
                    return Ok(new List<int>());

                var topicIds = db.topic_teach
                    .Where(t => t.course_assignment_id == courseAssignment.id)
                    .Select(t => t.topic_id)
                    .ToList();

                return Ok(topicIds);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }







        [HttpPost]
        [Route("api/Topics/removeTopicTeach")]
        public IHttpActionResult RemoveTopicTeach(TeachTopicRequest request)
        {
            try
            {
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                    return BadRequest("No active session");

                var courseAssignment = db.Course_Assignment.FirstOrDefault(x =>
                    x.course_id == request.course_id &&
                    x.section_id == request.section_id &&
                    x.user_id == request.user_id &&
                    x.session_id == activeSession.id);

                if (courseAssignment == null)
                    return BadRequest("Assignment not found");

                var topicTeach = db.topic_teach.FirstOrDefault(t =>
                    t.course_assignment_id == courseAssignment.id &&
                    t.topic_id == request.topic_id);

                if (topicTeach != null)
                {
                    db.topic_teach.Remove(topicTeach);
                    db.SaveChanges();
                }

                return Ok(new { message = "Topic removed successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }






        [HttpGet]
        [Route("api/Topics/getCommonTopics/{courseId}")]
        public IHttpActionResult GetCommonTopics(int courseId, int? session_id = null)
        {
            // 1️⃣ Determine session
            int activeSessionId = session_id ?? db.sessions
                                                .Where(s => s.Active) // assuming Active flag exists
                                                .Select(s => s.id)
                                                .FirstOrDefault();

            if (activeSessionId == 0)
                return Json(new List<int>()); // no active session found

            // 2️⃣ Get all sections assigned to this course in this session
            var courseSections = db.Course_Assignment
                                   .Where(ca => ca.course_id == courseId && ca.session_id == activeSessionId)
                                   .Select(ca => ca.section_id)
                                   .Distinct()
                                   .ToList();

            if (courseSections.Count == 0)
                return Json(new List<int>()); // no sections assigned

            // 3️⃣ Get all topics for this course (session optional)
            var courseTopics = db.topics
                                 .Where(t => t.course_id == courseId && (t.session_id == null || t.session_id == activeSessionId))
                                 .Select(t => t.id)
                                 .ToList();

            if (courseTopics.Count == 0)
                return Json(new List<int>()); // no topics for this course

            var commonTopicIds = new List<int>();

            // 4️⃣ Check topics taught in all sections
            foreach (var topicId in courseTopics)
            {
                var taughtSectionsCount = db.topic_teach
                                            .Where(tt => tt.topic_id == topicId
                                                        && tt.Course_Assignment.session_id == activeSessionId)
                                            .Select(tt => tt.Course_Assignment.section_id)
                                            .Distinct()
                                            .Count();

                if (taughtSectionsCount == courseSections.Count)
                    commonTopicIds.Add(topicId);
            }

            return Json(commonTopicIds); // ✅ return JSON
        }
    }

}

