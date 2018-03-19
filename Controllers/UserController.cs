using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using WebApiTokenAuth.Models;
using WebApiTokenAuth.Services;

namespace WebApiTokenAuth.Controllers
{
    [RoutePrefix("api/user")]
    public class UserController : ApiController
    {
        UserService us = new UserService();

        [HttpGet]
        [Route("auhenticate/{email}/{pw}")]
        public async Task<HttpResponseMessage> CheckUser(string email, string pw)
        {
            if(!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pw))
                return Request.CreateResponse(HttpStatusCode.OK, us.GetUserByCredentials(email, pw));
            else
                return Request.CreateResponse(HttpStatusCode.NotFound, "Provide email and password");
        }

        [HttpGet]
        [Route("getUserClaims")]
        [Authorize]
        public async Task<HttpResponseMessage> GetUserClaims()
        {
            var identityClaims = (ClaimsIdentity)User.Identity;
            IEnumerable<Claim> claims = identityClaims.Claims;
            RmsUser user = new RmsUser()
            {
                UFName = identityClaims.FindFirst("UFName").Value,
                ULName = identityClaims.FindFirst("ULName").Value,
                Email = identityClaims.FindFirst("Email").Value,
                AccessLevel = Convert.ToInt32(identityClaims.FindFirst("AccessLevel").Value)
            };
            return Request.CreateResponse(HttpStatusCode.OK, user);
        }
    }
}
