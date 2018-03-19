using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using WebApiTokenAuth.Models;
using WebApiTokenAuth.Services;

namespace WebApiTokenAuth.Provider
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            UserService us = new UserService();
            if (!string.IsNullOrEmpty(context.UserName) || !string.IsNullOrEmpty(context.Password))
            {
                RmsUser _user = us.GetUserByCredentials(context.UserName, context.Password);
                if (_user != null)
                {
                    var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                    identity.AddClaim(new Claim("UFName", _user.UFName));
                    identity.AddClaim(new Claim("ULName", _user.ULName));
                    identity.AddClaim(new Claim("Email", _user.Email));
                    identity.AddClaim(new Claim("AccessLevel", _user.AccessLevel.ToString()));
                    context.Validated(identity);
                }
            }
        }
    }
}