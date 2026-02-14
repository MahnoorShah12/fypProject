using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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
    public class TopicController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();


        ///eisha




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










    }
}
