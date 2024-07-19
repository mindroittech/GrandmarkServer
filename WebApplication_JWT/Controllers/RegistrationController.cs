using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using WebApplication_JWT.Models;

namespace WebApplication_JWT.Controllers
{
    [RoutePrefix("api/Registration")]
    public class RegistrationController : ApiController
    {
        private DataClassesdbContextDataContext db = new DataClassesdbContextDataContext();

        public RegistrationController()
        {

        }

        [HttpGet]
        [Route("Getname")]
        public IHttpActionResult Getname(string mobileno)
        {
            try
            {
                tblMember tblMember = db.tblMembers.SingleOrDefault(x => x.username == mobileno || x.mobile == mobileno);
                if (tblMember != null)
                {
                    return Ok(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new { Name = tblMember.name }
                    });
                }
                else
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        statusMessage = "failed",
                        message = "User not found"
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.NotFound, new
                {
                    statusCode = 404,
                    statusMessage = "failed",
                    message = "User not found"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    status = "failed",
                    message = "Internal Server Error: " + ex.Message
                });
            }
        }

        [HttpPost]
        [Route("Register")]
        public HttpResponseMessage Register(RegistrationModel model)
        {
            try
            {
                var sponsor = db.tblMembers.FirstOrDefault(u => u.mobile == model.SponserId);

                if (sponsor == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        message = "Failed",
                        data = "Sponsor ID not found"
                    });
                }
                else
                {
                    if ((string.IsNullOrEmpty(model.SponserId) || string.IsNullOrEmpty(model.FullName) ||
                          string.IsNullOrEmpty(model.MobileNumber.ToString()) || string.IsNullOrEmpty(model.Team)))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            statusCode = 400,
                            message = "Failed",
                            data = "All fields are required"
                        });
                    }
                    if (!IsValidMobile(model.MobileNumber.ToString()))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            statusCode = 400,
                            message = "Failed",
                            data = "Invalid mobile number"
                        });

                    }
                    var existingUser = db.tblMembers.FirstOrDefault(u => u.mobile == model.MobileNumber.ToString());
                    if (existingUser != null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            statusCode = 400,
                            message = "Failed",
                            data = "Mobile number already exists"
                        });
                    }

                    string loginId = GenerateUserID();

                    // Generate password
                    // string password = GeneratePassword();

                    string Team = model.Team;

                    if (Team == "left")
                    {
                        Team = "Left";
                    }
                    else if (Team == "right")
                    {
                        Team = "Right";
                    }

                    int referid = sponsor.id;
                    int lev = sponsor.level.Value;

                    var newSponsor = db.tblMembers.FirstOrDefault(c => c.refid == sponsor.id && c.leg == model.Team);

                    while (newSponsor != null)
                    {
                        referid = newSponsor.id;
                        lev = newSponsor.level.Value;

                        newSponsor = db.tblMembers.FirstOrDefault(c => c.refid == referid && c.leg == model.Team);
                    }


                    // Proceed with registration logic
                    var newUser = new tblMember
                    {
                        username = loginId,
                        password = model.Password,
                        leg = Team,
                        joindate = DateTime.UtcNow.AddHours(5.5),
                        level = lev + 1,
                        refid = referid,
                        boosterPoints = 0,
                        name = model.FullName,
                        mobile = model.MobileNumber.ToString(),
                        status = "Free",
                        sid = sponsor.username,
                        sname = sponsor.name,
                        state = model.State,
                        district = model.District,
                        pincode = model.Pincode,
                        credit = 0,
                        email = "",
                        debit = 0,
                        lcarry = 0,
                        rcarry = 0,
                        tlcount = 0,
                        trcount = 0,
                        goldlcarry = 0,
                        goldrcarry = 0,
                        diamondlcarry = 0,
                        diamondrcarry = 0,
                        silverLeft = 0,
                        silverRight = 0,
                        selfBv = 0,
                        leftBv = 0,
                        rightBv = 0,
                        leftTotalBv = 0,
                        rightTotalBv = 0,
                        maintenanceBv = 0,
                        RepurchaseWallet = 0,
                        flushOut = 0,
                        rewardPoints = 50,
                        isActive = 1,
                        franchise = "",
                        TourPackageEligible = false
                    };

                    int refid = (int)newUser.refid;
                    string leg = newUser.leg;

                    while (refid != 0)
                    {
                        var bv = db.tblMembers.SingleOrDefault(c => c.id == refid);

                        if (leg == "Left")
                        {
                            bv.diamondlcarry += 1;
                        }
                        else if (leg == "Right")
                        {
                            bv.diamondrcarry += 1;
                        }

                        db.SubmitChanges();

                        refid = (int)bv.refid;
                        leg = bv.leg;
                    }


                    sponsor.rewardPoints += 50;
                    db.tblMembers.InsertOnSubmit(newUser);
                    db.SubmitChanges();
                    var smsService = new SendSms();

                    var mob = (from c in db.tblMembers
                               where c.mobile == model.MobileNumber.ToString()
                               select c).SingleOrDefault();

                    string msg = "Dear " + mob.name + ", Your login password for the User ID " + mob.username + " is " + mob.password + "  " + ConfigurationManager.AppSettings["companyname"];
                    smsService.sms(mob.mobile, msg, "1407169769915124365");

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        statusCode = 200,
                        statusMessage = "Successful",
                        data = $"Congratulations! You have successfully registered to Grandmark Shoppe. Please login to activate your account. Your login ID: {loginId}, Password: {model.Password}"
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    statusCode = 400,
                    statusMessage = "Failed",
                    data = $"Registration failed: {ex.Message}"
                });
            }

        }

        private bool IsValidMobile(string mobile)
        {
            long result;
            return long.TryParse(mobile, out result) && mobile.Length == 10;
        }

        //private string GeneratePassword()
        //{
        //    string strPwdchar = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //    string strPwd = "";
        //    Random rnd = new Random();
        //    for (int i = 0; i <= 5; i++)
        //    {
        //        int iRandom = rnd.Next(0, strPwdchar.Length - 1);
        //        strPwd += strPwdchar.Substring(iRandom, 1);
        //    }
        //    return strPwd;
        //}

        private string GenerateUserID()
        {
            string strPwdchar = "1234567890";
            string username = "";
            Random rnd = new Random();
            for (int i = 0; i <= 7; i++)
            {
                int iRandom = rnd.Next(0, strPwdchar.Length - 1);
                username += strPwdchar.Substring(iRandom, 1);
            }

            var existingUser = db.tblMembers.FirstOrDefault(c => c.username == username);
            if (existingUser != null)
            {
                // If a user with the generated username already exists, recursively generate a new one
                return GenerateUserID();
            }

            return username;
        }

        [HttpPost]
        [Route("ForgotPassword")]
        public HttpResponseMessage ForgotPassword(ForgotPasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNumber))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    statusCode = HttpStatusCode.BadRequest,
                    message = "Failed",
                    data = "Mobile number is required"
                });
            }

            var user = db.tblMembers.FirstOrDefault(u => u.mobile == request.MobileNumber);
            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new
                {
                    statusCode = HttpStatusCode.NotFound,
                    message = "Failed",
                    data = "User not found"
                });
            }

            var smsService = new SendSms();
            string msg = $"Dear {user.name}, Your login password for the User ID {user.username} is {user.password} {ConfigurationManager.AppSettings["companyname"]}";
            smsService.sms(user.mobile, msg, "1407169769915124365");

            var response = Request.CreateResponse(HttpStatusCode.OK, new
            {
                statusCode = HttpStatusCode.OK,
                message = "Success",
                data = "Password sent successfully"
            });
            return response;
        }

        [HttpGet]
        [Route("GetNamebyUsername")]
        public HttpResponseMessage GetNamebyUsername(string Username)
        {
            try
            {
                string username = User.Identity.Name;
                var account = db.tblMembers.SingleOrDefault(x => x.username == Username);

                if (account != null)
                {
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    // response.Content = new StringContent("{\"statusCode\": 200, \"status Message\": \"success\", \"Username\": \"" + account.username + "\", \"Name\": \"" + account.name + "\"}", System.Text.Encoding.UTF8, "application/json");
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            Name = account.name
                        }
                    }, new JsonMediaTypeFormatter());

                    return response;
                }
                else
                {
                    var response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"status Message\": \"failed\", \"message\": \"User not found\"}", System.Text.Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"status\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }

    }
}
