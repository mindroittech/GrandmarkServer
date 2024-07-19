using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using WebApplication_JWT.Models;

namespace WebApplication_JWT.Controllers
{
    public class TokenController : ApiController
    {
        private DataClassesdbContextDataContext db = new DataClassesdbContextDataContext();

        [HttpPost]
        [Route("Token")]
        public async Task<HttpResponseMessage> GenerateToken()
        {
            NameValueCollection result = await Request.Content.ReadAsFormDataAsync();
            string username = result.Get("username");
            string password = result.Get("password");
            string grantType = result.Get("grant_type");

            if (grantType != "password")
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    statusCode = HttpStatusCode.BadRequest,
                    statusMessage = "invalid_grant",
                    errorMessage = "Invalid grant_type"
                }, new JsonMediaTypeFormatter());
            }

            var user = db.tblMembers.FirstOrDefault(m => (m.username == username || m.mobile == username) && m.password == password);

            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new
                {
                    statusCode = HttpStatusCode.Unauthorized,
                    statusMessage = "invalid_grant",
                    errorMessage = "Provided username and password is incorrect"
                }, new JsonMediaTypeFormatter());
            }

            if (user.isActive == 0)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    statusCode = HttpStatusCode.Unauthorized,
                    statusMessage = "invalid_grant",
                    errorMessage = "Your Account has been Deactivated"
                }, new JsonMediaTypeFormatter());
            }

            string token = TokenManager.GenerateToken(username);

            user.accesstoken = token;
            db.SubmitChanges();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                statusCode = HttpStatusCode.OK,
                message = "Success",
                access_token = token,
                username = user.username
            }, new JsonMediaTypeFormatter());
        }
    }
}

