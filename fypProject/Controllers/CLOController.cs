using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;
using Antlr.Runtime;
using WebGrease.Css.Ast;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;

namespace fypProject.Controllers
{
    public class CLOController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();











        [HttpGet]
        [Route("api/clos/get_Clos/{courseId}")]
        public IHttpActionResult GetCLOs(int courseId)
        {
            var activeSession = db.sessions.Where(s => s.Active == true).FirstOrDefault();



            var clos = db.cloes
                .Where(c => c.course_id == courseId && c.session_id == activeSession.id)
                .Select(c => new
                {
                    c.id,
                    c.description
                }).ToList();

            return Ok(clos);
        }


        [HttpPost]
        [Route("api/clos/add_clos/{courseId}")]
        public IHttpActionResult AddCLO(int courseId, clo model)
        {
            try
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

                // 3️⃣ Assign course
                model.course_id = courseId;

                model.session_id = activeSession.id;

                // 4️⃣ Save
                db.cloes.Add(model);

                var result = db.SaveChanges();

                if (result <= 0)
                {
                    return InternalServerError(
                        new Exception("CLO was not added.")
                    );
                }

                // 5️⃣ Success response
                return Ok(new
                {
                    success = true,
                    message = "CLO added successfully",
                    cloId = model.id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("api/clos/update/{cloId}")]
        public IHttpActionResult UpdateCLO(int cloId, clo model)
        {
            try
            {
                // 1️⃣ Validate request body
                if (model == null)
                {
                    return BadRequest("Request body is empty.");
                }

                // 2️⃣ Find CLO
                var existingClo = db.cloes.Find(cloId);
                if (existingClo == null)
                {
                    return NotFound();
                }


                // 3️⃣ Validate required fields
                if (string.IsNullOrWhiteSpace(model.description))
                {
                    return BadRequest("Description is required.");
                }

                // 4️⃣ Update fields
                existingClo.description = model.description;

                // 5️⃣ Save changes
                var result = db.SaveChanges();

                // 6️⃣ Check if update actually happened
                if (result <= 0)
                {
                    return InternalServerError(
                        new Exception("CLO was not updated. No changes detected.")
                    );
                }

                // 7️⃣ Success response
                return Ok(new
                {
                    success = true,
                    message = "CLO updated successfully",
                    cloId = cloId
                });
            }
            catch (Exception ex)
            {
                // 8️⃣ Catch unexpected errors
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("api/clos/delete/{cloId}")]
        public IHttpActionResult DeleteCLO(int cloId)
        {
            try
            {
                // 1️⃣ Find CLO
                var clo = db.cloes.Find(cloId);
                if (clo == null)
                {
                    return NotFound();
                }

                // 2️⃣ Remove CLO
                db.cloes.Remove(clo);

                // 3️⃣ Save changes
                var result = db.SaveChanges();

                // 4️⃣ Check if deletion actually happened
                if (result <= 0)
                {
                    return InternalServerError(
                        new Exception("CLO could not be deleted.")
                    );
                }


                // 5️⃣ Success response
                return Ok(new
                {
                    success = true,
                    message = "CLO deleted successfully",
                    cloId = cloId
                });
            }
            catch (Exception ex)
            {
                // 6️⃣ Catch unexpected errors
                return InternalServerError(ex);

            }
        }





































        [HttpGet]
        [Route("api/clos/get_ClosWithWeightage/{courseId}")]
        public IHttpActionResult ClosWithWeightage(int courseId)
        {
            try
            {
                // 1️⃣ Get active session
                var activeSession = db.sessions.FirstOrDefault(s => s.Active);
                if (activeSession == null)
                {
                    return Content(HttpStatusCode.NotFound, "No active session found.");
                }

                // 2️⃣ Get CLOs for course + session
                var clos = db.cloes
                    .Where(c => c.course_id == courseId && c.session_id == activeSession.id)
                    .ToList();

                // 3️⃣ Extract CLO IDs
                var cloIds = clos.Select(c => c.id).ToList();

                // 4️⃣ Get weightages only for these CLOs
                var weightages = db.Clo_Weightage
                    .Where(w => cloIds.Contains(w.clo_id))
                    .ToList();

                // 5️⃣ Prepare result
                var result = clos.Select(c => new
                {
                    Id = c.id,
                    Description = c.description,

                    MidTermWeight = weightages
                        .Where(w =>
                            w.clo_id == c.id &&
                            w.term != null &&
                            w.term.Equals("mid", StringComparison.OrdinalIgnoreCase)
                        )
                        .Select(w => (int?)w.weightage)
                        .FirstOrDefault() ?? 0,

                    FinalTermWeight = weightages
                        .Where(w =>
                            w.clo_id == c.id &&
                            w.term != null &&
                            w.term.Equals("final", StringComparison.OrdinalIgnoreCase)
                        )
                        .Select(w => (int?)w.weightage)
                        .FirstOrDefault() ?? 0
                }).ToList();

                // 6️⃣ Return success
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    $"An error occurred: {ex.Message}"
                );
            }
        }











        [HttpPost]
        [Route("api/clos/UpdateAndAddCloWeightage")]
        public IHttpActionResult UpdateAndAddCloWeightage([FromBody] CloWeightageDTO model)
        {



            try
            {
                var clo = db.cloes.Find(model.CloId);
                if (clo == null)
                    return NotFound();

                var cloWeight = db.Clo_Weightage
                    .FirstOrDefault(w => w.clo_id == model.CloId && w.term == model.Term);

                if (cloWeight == null)
                {
                    db.Clo_Weightage.Add(new Clo_Weightage
                    {
                        clo_id = model.CloId,
                        term = model.Term,
                        weightage = model.Weightage
                    });
                }
                else
                {
                    cloWeight.weightage = model.Weightage;
                }

                db.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Weightage saved successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }







    }






















}