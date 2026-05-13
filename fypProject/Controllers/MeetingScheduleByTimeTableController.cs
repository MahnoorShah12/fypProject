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

        //private bool TryParseTimeRange(string input, out string start, out string end)
        //{
        //    start = "";
        //    end = "";

        //    if (string.IsNullOrWhiteSpace(input))
        //        return false;

        //    var parts = input.Split('-');

        //    if (parts.Length != 2)
        //        return false;

        //    start = parts[0].Trim();
        //    end = parts[1].Trim();

        //    return TimeSpan.TryParse(start, out _) && TimeSpan.TryParse(end, out _);
        //}

        private bool TryParseTimeRange(string input, out TimeSpan start, out TimeSpan end)
        {
            start = default;
            end = default;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Split('-');
            if (parts.Length != 2)
                return false;

            return TimeSpan.TryParse(parts[0].Trim(), out start) &&
                   TimeSpan.TryParse(parts[1].Trim(), out end);
        }
        // ✅ IMPORT EXCEL (Teacher Free Slots)
        [HttpPost]
        [Route("api/TeacherFreeSlots/import")]
        public IHttpActionResult ImportTeacherSlots(int? sessionId = null)
        {
            try
            {
                // ================= SESSION =================
                var session = sessionId.HasValue
                    ? db.sessions.FirstOrDefault(s => s.id == sessionId)
                    : db.sessions.FirstOrDefault(s => s.Active == true);

                if (session == null)
                    return BadRequest("No valid or active session found");

                int finalSessionId = session.id;

                // ================= FILE CHECK =================
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.Files.Count == 0)
                    return BadRequest("No file uploaded");

                var file = httpRequest.Files[0];

                // ================= REPORTING LISTS =================
                var missingTeachersInDB = new List<object>();
                var invalidRows = new List<object>();
                var duplicateSkipped = new List<object>();

                // 🔥 FIX: prevent duplicate missing teacher reports
                var missingTeacherSet = new HashSet<string>();

                var freeSlots = new List<TeacherFreeSlot>();
                int successRows = 0;

                string lastTeacherName = "";
                int rowIndex = 1;

                using (var workbook = new XLWorkbook(file.InputStream))
                {
                    var worksheet = workbook.Worksheet(1);

                    var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
                    if (rows == null)
                        return BadRequest("Excel file is empty or invalid format");

                    foreach (var row in rows)
                    {
                        rowIndex++;

                        try
                        {
                            // ================= TEACHER NAME =================
                            string teacherName = row.Cell(1).GetString().Trim();

                            if (string.IsNullOrWhiteSpace(teacherName))
                                teacherName = lastTeacherName;
                            else
                                lastTeacherName = teacherName;

                            // ================= TIME RANGE =================
                            string timeRange = row.Cell(2).GetString().Trim();

                            if (!TryParseTimeRange(timeRange, out TimeSpan startTime, out TimeSpan endTime))
                            {
                                invalidRows.Add(new
                                {
                                    row = rowIndex,
                                    teacher = teacherName,
                                    error = "Invalid time format (expected HH:mm-HH:mm)",
                                    value = timeRange
                                });
                                continue;
                            }

                            // ================= USER CHECK =================
                            var user = db.Users.FirstOrDefault(u => u.name == teacherName);

                            if (user == null)
                            {
                                // 🔥 ADD ONLY ONCE PER TEACHER
                                if (!missingTeacherSet.Contains(teacherName))
                                {
                                    missingTeacherSet.Add(teacherName);

                                    missingTeachersInDB.Add(new
                                    {
                                        teacherName,
                                        issue = "Teacher exists in timetable but NOT in Users table",
                                        action = "Fix name or add teacher in Users table"
                                    });
                                }

                                continue;
                            }

                            // ================= DAYS LOOP =================
                            for (int col = 3; col <= 7; col++)
                            {
                                string cellValue = row.Cell(col).GetString().Trim();

                                // skip busy slot
                                if (!string.IsNullOrWhiteSpace(cellValue) && cellValue != "-")
                                    continue;

                                string day = "";

                                switch (col)
                                {
                                    case 3: day = "Mon"; break;
                                    case 4: day = "Tue"; break;
                                    case 5: day = "Wed"; break;
                                    case 6: day = "Thu"; break;
                                    case 7: day = "Fri"; break;
                                    default: day = ""; break;
                                }

                                if (string.IsNullOrEmpty(day))
                                    continue;

                                bool exists = db.TeacherFreeSlots.Any(x =>
                                    x.UserId == user.id &&
                                    x.Day == day &&
                                    x.StartTime == startTime &&
                                    x.EndTime == endTime &&
                                    x.SessionId == finalSessionId
                                );

                                if (exists)
                                {
                                    duplicateSkipped.Add(new
                                    {
                                        teacher = teacherName,
                                        day,
                                        time = $"{startTime}-{endTime}"
                                    });
                                    continue;
                                }

                                freeSlots.Add(new TeacherFreeSlot
                                {
                                    UserId = user.id,
                                    SessionId = finalSessionId,
                                    Day = day,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    CreatedAt = DateTime.Now
                                });

                                successRows++;
                            }
                        }
                        catch (Exception exRow)
                        {
                            invalidRows.Add(new
                            {
                                row = rowIndex,
                                error = exRow.Message
                            });
                        }
                    }

                    if (freeSlots.Count > 0)
                        db.TeacherFreeSlots.AddRange(freeSlots);

                    db.SaveChanges();
                }

                // ================= RESPONSE =================
                return Ok(new
                {
                    message = "Import completed successfully with full validation report",
                    sessionUsed = finalSessionId,

                    summary = new
                    {
                        successRows,
                        missingTeachers = missingTeacherSet.Count,
                        invalidRows = invalidRows.Count,
                        duplicateSkipped = duplicateSkipped.Count
                    },

                    missingTeachersInDB,
                    invalidRows,
                    duplicateSkipped
                });
            }
            catch (Exception ex)
            {
                return BadRequest("System Error: " + ex.Message);
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

                // ✅ NEW: Track issues
                var teacherIssues = new List<object>();

                var teachers = db.Users.OrderBy(u => u.name).ToList();
                var allSlots = db.TeacherFreeSlots.ToList();

                var teacherFreeSlots = new Dictionary<int, List<TeacherFreeSlot>>();
                foreach (var teacher in teachers)
                {
                    teacherFreeSlots[teacher.id] = allSlots
                        .Where(s => s.UserId == teacher.id)
.OrderBy(s => s.StartTime ?? TimeSpan.MaxValue)
    .ToList();
                }

                var currentDate = startDate;

                var session = db.sessions.FirstOrDefault(s => s.Active);

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

                        // ✅ Check teacher exists
                        var teacher = db.Users.FirstOrDefault(u => u.id == teacherId);
                        if (teacher == null)
                        {
                            teacherIssues.Add(new
                            {
                                TeacherId = teacherId,
                                Issue = "Teacher not found"
                            });
                            continue;
                        }

                        // ✅ Check courses
                        if (!teacherCoursesMap.ContainsKey(teacherId) || !teacherCoursesMap[teacherId].Any())
                        {
                            teacherIssues.Add(new
                            {
                                TeacherId = teacherId,
                                TeacherName = teacher.name,
                                Issue = "No papers assigned"
                            });

                            db.Alerts.Add(new Alert
                            {
                                sender_id = request.senderId,
                                reciever_id = teacher.id,
                                description = "No papers assigned for scheduling"
                            });

                            continue;
                        }

                        // ✅ Safe free slot fetch
                        var freeSlots = teacherFreeSlots.ContainsKey(teacherId)
                            ? teacherFreeSlots[teacherId].Where(s => s.Day == day).ToList()
                            : new List<TeacherFreeSlot>();

                        if (!freeSlots.Any())
                        {
                            teacherIssues.Add(new
                            {
                                TeacherId = teacherId,
                                TeacherName = teacher.name,
                                Issue = "No timetable / free slots available"
                            });

                            db.Alerts.Add(new Alert
                            {
                                sender_id = request.senderId,
                                reciever_id = teacher.id,
                                description = "No timetable available for scheduling"
                            });

                            continue;
                        }

                        bool scheduled = false;

                        foreach (var slot in freeSlots)
                        {
                            TimeSpan slotStart = slot.StartTime ?? TimeSpan.Zero;
                            TimeSpan slotEnd = slot.EndTime ?? TimeSpan.Zero;

                            var meetingStart = slotStart < directorStart ? directorStart : slotStart;
                            var meetingEnd = slotEnd > directorEnd ? directorEnd : slotEnd;

                            if (meetingStart + TimeSpan.FromMinutes(slotDuration) <= meetingEnd)
                            {
                                bool conflict = allMeetings.Any(m =>
                                    m.Date == currentDate &&
                                    m.StartTime == meetingStart.ToString(@"hh\:mm"));

                                if (!conflict)
                                {
                                    var meetingStartStr = meetingStart.ToString(@"hh\:mm");
                                    var meetingEndStr = (meetingStart + TimeSpan.FromMinutes(slotDuration)).ToString(@"hh\:mm");

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
                                        Courses = courses
                                    });

                                    db.Alerts.Add(new Alert
                                    {
                                        sender_id = request.senderId,
                                        reciever_id = teacher.id,
                                        description = $"Meeting on {currentDate:dd-MM-yyyy} | {meetingStartStr}-{meetingEndStr} | Papers: {string.Join(", ", courses)}"
                                    });

                                    scheduled = true;
                                    break;
                                }
                            }
                        }

                        // ✅ If not scheduled
                        if (!scheduled)
                        {
                            teacherIssues.Add(new
                            {
                                TeacherId = teacherId,
                                TeacherName = teacher.name,
                                Issue = "Could not schedule due to time conflict or slot limitation"
                            });

                            db.Alerts.Add(new Alert
                            {
                                sender_id = request.senderId,
                                reciever_id = teacher.id,
                                description = "Could not schedule meeting due to time conflict"
                            });

                            unscheduledTeachers.Enqueue(teacherId);
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                }

                db.SaveChanges();

                return Ok(new
                {
                    message = "Optimized schedule generated",
                    Meetings = allMeetings,
                    Issues = teacherIssues // ✅ NEW RESPONSE
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