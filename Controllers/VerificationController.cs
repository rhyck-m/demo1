using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebApiTokenAuth.Controllers
{
    public class VerificationController : ApiController
    {
        [Authorize]
        public IHttpActionResult Authorize()
        {
            return Ok("Authorized");
        }
    }
}
