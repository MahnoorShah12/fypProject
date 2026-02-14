using System.Web.Http;
using System.Web.Http.Cors; // ✅ Needed for EnableCorsAttribute

namespace fypProject
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // 1️⃣ Enable CORS globally for your React app
            var cors = new EnableCorsAttribute("http://localhost:5175", "*", "*");
            config.EnableCors(cors);
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
