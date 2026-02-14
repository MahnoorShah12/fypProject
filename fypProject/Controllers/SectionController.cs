using fypProject.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using WebGrease.Css.Ast.Selectors;
using System;
using Microsoft.Ajax.Utilities;

namespace fypProject.Controllers
{
    public class SectionController : ApiController
    {



        private DirectorDashboardEntities db = new DirectorDashboardEntities();




        [HttpGet]
        [Route("api/section/get_all_sections")]
        public HttpResponseMessage GetAllSections()
        {
            try
            {
                var sections = db.sections.Select(s=> new{ s.id,s.name}).ToList();
                return Request.CreateResponse(HttpStatusCode.OK,new{
                    Sections= sections});

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }





        [HttpPost]
        [Route("api/section/add_section")]
        public HttpResponseMessage AddSection([FromBody] section request)
        {
            try
            {
                if (request == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid request data");

                if (string.IsNullOrWhiteSpace(request.name))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Name is required");

                
           
                if (db.sections.Any(u => u.name == request.name))
                    return Request.CreateResponse(HttpStatusCode.Conflict, "section name must be unique");

               
                db.sections.Add(request);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    message = "Section added successfully",
                    SectionId = request.id
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
















        [HttpGet]
        [Route("api/section/get_section_by_name/{name}")]
        public HttpResponseMessage GetSectionByName(string name)
        {
            try
            {

               

                var section = db.sections.Where(s=>s.name==name).Select(s => new {s.id}).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Section = section
                });

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

























    }
}
