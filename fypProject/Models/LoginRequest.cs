using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace fypProject.Models
{
    public class LoginRequest
    {
       
        public string Email { get; set; }

        
        public string Password { get; set; }
    }
}