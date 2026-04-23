using ClosedXML.Excel;
using fypProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace fypProject.Controllers
{
    public class MeetingScheduleByTimeTableController : ApiController
    {
        private DirectorDashboardEntities db = new DirectorDashboardEntities();

        // ✅ IMPORT EXCEL (Teacher Free Slots)
        [HttpPost]
        [Route("api/TeacherFreeSlots/import")]
        public IHttpActionResult ImportTeacherSlots(int? sessionId = null)
        {
            try
            {
                var session = sessionId.HasValue
                    ? db.sessions.FirstOrDefault(s => s.id == sessionId)
                    : db.sessions.FirstOrDefault(s => s.Active == true);

                if (session == null)
                    return BadRequest("No valid or active session found");

                int finalSessionId = session.id;
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded");

                var file = httpRequest.Files[0];

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    var freeSlots = new List<TeacherFreeSlot>();

                    string lastTeacherName = ""; // Keep track of the last non-empty teacher

                    foreach (var row in rows)
                    {
                        string teacherName = row.Cell(1).GetString().Trim();

                        // If empty, use the last teacher
                        if (string.IsNullOrEmpty(teacherName))
                            teacherName = lastTeacherName;
                        else
                            lastTeacherName = teacherName;

                        string timeRange = row.Cell(2).GetString().Trim();

                        // Skip invalid time
                        if (string.IsNullOrEmpty(timeRange) || !timeRange.Contains("-"))
                            continue;

                        var parts = timeRange.Split('-');
                        if (parts.Length != 2)
                            continue;

                        string startTime = parts[0].Trim();
                        string endTime = parts[1].Trim();

                        var user = db.Users.FirstOrDefault(u => u.name == teacherName);
                        if (user == null)
                            continue;

                        // Loop through Mon-Fri (columns 3-7)
                        for (int col = 3; col <= 7; col++)
                        {
                            string cellValue = row.Cell(col).GetString().Trim();

                            // ✅ Treat empty or "-" as free slot
                            if (!string.IsNullOrEmpty(cellValue) && cellValue != "-")
                                continue; // busy

                            string day = "";
                            switch (col)
                            {
                                case 3: day = "Mon"; break;
                                case 4: day = "Tue"; break;
                                case 5: day = "Wed"; break;
                                case 6: day = "Thu"; break;
                                case 7: day = "Fri"; break;
                            }

                            // Avoid duplicates
                            bool exists = db.TeacherFreeSlots.Any(x =>
                                x.UserId == user.id &&
                                x.Day == day &&
                                x.StartTime.ToString() == startTime &&
                                x.EndTime.ToString() == endTime &&
                                x.SessionId == finalSessionId
                            );

                            if (!exists)
                            {
                                freeSlots.Add(new TeacherFreeSlot
                                {
                                    UserId = user.id,
                                    SessionId = finalSessionId,
                                    Day = day,
                                    StartTime = TimeSpan.Parse(startTime),   // ✅ FIX
                                    EndTime = TimeSpan.Parse(endTime),
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }

                    if (freeSlots.Count > 0)
                        db.TeacherFreeSlots.AddRange(freeSlots);

                    db.SaveChanges();
                }

                return Ok(new
                {
                    message = "Teacher free slots imported successfully ",
                    sessionUsed = finalSessionId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // ✅ GET ALL SLOTS
        [HttpGet]
        [Route("api/TeacherFreeSlots")]
        public HttpResponseMessage GetSlots()
        {
            try
            {
                var data = (from slot in db.TeacherFreeSlots
                            join u in db.Users on slot.UserId equals u.id
                            join s in db.sessions on slot.SessionId equals s.id
                            select new
                            {
                                slot.Id,
                                TeacherName = u.name,
                                slot.Day,
                                slot.StartTime,
                                slot.EndTime,
                                slot.SessionId
                            }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }






        [HttpPost]
        [Route("api/Meetings/GenerateSchedule")]
        public IHttpActionResult GenerateOptimizedSchedule(MeetingRequest request)
        {
            try
            {
                var startDate = request.StartDate.Date;
                var endDate = request.EndDate.Date;

                if (endDate < startDate)
                    return BadRequest("End date must be after start date");

                int slotDuration = request.SlotDuration;
                TimeSpan directorStart = TimeSpan.Parse(request.StartTime);
                TimeSpan directorEnd = TimeSpan.Parse(request.EndTime);

                var allMeetings = new List<GeneratedMeeting>();

                // Get all teachers in alphabetical order
                var teachers = db.Users.OrderBy(u => u.name).ToList();

                // Pull all teacher slots into memory
                var allSlots = db.TeacherFreeSlots.ToList();

                // Build a map: teacherId => list of free slots
                var teacherFreeSlots = new Dictionary<int, List<TeacherFreeSlot>>();
                foreach (var teacher in teachers)
                {
                    teacherFreeSlots[teacher.id] = allSlots
                        .Where(s => s.UserId == teacher.id)
                            .OrderBy(s => s.StartTime)
                        .ToList();
                }

                var currentDate = startDate;
                // ✅ Get session id
                var session = db.sessions.FirstOrDefault(s => s.Active);
                // ✅ Get teacher courses (papers)
                var teacherCoursesMap = (
                    from pa in db.paper_Assignment
                    join c in db.courses on pa.course_id equals c.id
                    where pa.session_id == session.id
                    select new
                    {
                        TeacherId = pa.user_id,
                        CourseName = c.title
                    }
                )
                .AsEnumerable()
                .GroupBy(x => x.TeacherId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.CourseName).Distinct().OrderBy(x => x).ToList()
                );
                // Queue of unscheduled teachers
                var unscheduledTeachers = new Queue<int>(
      teachers
          .Where(t => teacherCoursesMap.ContainsKey(t.id)
                      && teacherCoursesMap[t.id] != null
                      && teacherCoursesMap[t.id].Any())
          .Select(t => t.id)
  );
                while (unscheduledTeachers.Count > 0 && currentDate <= endDate)
                {
                    string day = currentDate.DayOfWeek.ToString().Substring(0, 3);

                    int totalTeachersToday = unscheduledTeachers.Count;
                    for (int i = 0; i < totalTeachersToday; i++)
                    {
                        int teacherId = unscheduledTeachers.Dequeue();
                        var freeSlots = teacherFreeSlots[teacherId]
                            .Where(s => s.Day == day)
                            .ToList(); // safe to parse now

                        bool scheduled = false;

                        foreach (var slot in freeSlots)
                        {
                            TimeSpan slotStart = slot.StartTime ?? TimeSpan.Zero;
                            TimeSpan slotEnd = slot.EndTime ?? TimeSpan.Zero;

                            var meetingStart = slotStart < directorStart ? directorStart : slotStart;
                            var meetingEnd = slotEnd > directorEnd ? directorEnd : slotEnd;

                            if (meetingStart + TimeSpan.FromMinutes(slotDuration) <= meetingEnd)
                            {
                                // Check for conflicts
                                bool conflict = allMeetings.Any(m =>
                                    m.Date == currentDate &&
                                    m.StartTime == meetingStart.ToString(@"hh\:mm"));

                                if (!conflict)
                                {
                                    var teacher = db.Users.First(u => u.id == teacherId);

                                    var meetingStartStr = meetingStart.ToString(@"hh\:mm");
                                    var meetingEndStr = (meetingStart + TimeSpan.FromMinutes(slotDuration)).ToString(@"hh\:mm");

                                    // ✅ Get courses for this teacher
                                    var courses = teacherCoursesMap.ContainsKey(teacherId)
                                        ? teacherCoursesMap[teacherId]
                                        : new List<string>();

                                    allMeetings.Add(new GeneratedMeeting
                                    {
                                        TeacherId = teacherId,
                                        TeacherName = teacher.name,
                                        Date = currentDate,
                                        StartTime = meetingStartStr,
                                        EndTime = meetingEndStr,

                                        // ✅ NEW FIELD
                                        Courses = courses
                                    });
                                    // ✅ ADD ALERT HERE
                                    db.Alerts.Add(new Alert
                                    {
                                        sender_id = request.senderId,
                                        reciever_id = teacher.id,
                                        description = $"Meeting on {currentDate:dd-MM-yyyy} | {meetingStartStr}-{meetingEndStr} | Papers: {string.Join(", ", courses)}"
                                    });

                                    scheduled = true;
                                    break; // Only one meeting per teacher per day
                                }
                            }
                        }

                        if (!scheduled)
                            unscheduledTeachers.Enqueue(teacherId); // try next day
                    }

                    currentDate = currentDate.AddDays(1);
                }

                db.SaveChanges();

                return Ok(new
                {
                    message = "Optimized schedule generated",
                    Meetings = allMeetings
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }





























        // ✅ DELETE ALL PREVIOUS TIMETABLE (Free Slots)
        [HttpDelete]
        [Route("api/TeacherFreeSlots/clear")]
        public IHttpActionResult ClearTeacherSlots(int? sessionId = null)
        {
            try
            {
                var session = sessionId.HasValue
                    ? db.sessions.FirstOrDefault(s => s.id == sessionId)
                    : db.sessions.FirstOrDefault(s => s.Active);

                if (session == null)
                    return BadRequest("No active session found");

                int finalSessionId = session.id;

                var slots = db.TeacherFreeSlots
                    .Where(s => s.SessionId == finalSessionId)
                    .ToList();

                if (slots.Any())
                {
                    db.TeacherFreeSlots.RemoveRange(slots);
                    db.SaveChanges();
                }

                return Ok(new
                {
                    message = "Previous timetable deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }





















        public class MeetingRequest
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string StartTime { get; set; } // "10:00"
            public string EndTime { get; set; }   // "16:00"
            public int SlotDuration { get; set; } // 20 or 30
            public int senderId { get; set; }
        }

        public class GeneratedMeeting
        {
            public int TeacherId { get; set; }
            public string TeacherName { get; set; }
            public DateTime Date { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }

            public List<string> Courses { get; set; }
        }
    }
}