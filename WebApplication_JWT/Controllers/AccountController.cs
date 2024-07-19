using Microsoft.CSharp.RuntimeBinder;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Razorpay.Api;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebApplication_JWT.Models;
using static WebApplication_JWT.Models.AccountsModels;

namespace WebApplication_JWT.Controllers
{
    [CustumAuthenticationFilter]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private DataClassesdbContextDataContext db = new DataClassesdbContextDataContext();

        public AccountController()
        {

        }
        //// LIVE :

        private const string JWTtoken = "UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=";
        private const string PartnerID = "PS005115";
        private const string AuthKey = "YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=";

        ////live
        private const string PaysprintBaseUrl = "api.paysprint.in";

        ////////local
        //private const string PaysprintBaseUrl = "sit.paysprint.in/service-api";

        ////// LOCAL :

        //private const string JWTtoken = "UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==";
        //private const string PartnerID = "PS001624";
        //private const string AuthKey = "OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=";



        string cashback = null;
        decimal cashRPC = 0;
        decimal cashBooster = 0;
        public object[] PublicArray { get; set; }

        int statusCode = 0;
        string statusMessage = null;
        string message = null;
        object reward = null;
        string PNR = null;
        string REFID = null;
        
        [HttpPost]
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            // Check if the Authorization header is present
            var authorizationHeader = Request.Headers.Authorization;
            if (authorizationHeader == null)
            {
                return BadRequest("Authorization header is missing in the request.");
            }

            // Get the token parameter
            string token = authorizationHeader.Parameter;
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is missing in the Authorization header.");
            }

            // Add the token to the blacklist
            TokenManager.AddToBlacklist(token);
            return Ok("Logged out successfully.");
        }


        [HttpPost]
        [Route("changepassword")]
        public IHttpActionResult changepassword(AccountsModels.ChangePasswordBindingModel model)
        {
            try
            {
                // Find the user in the database
                var tblMember = db.tblMembers.SingleOrDefault(x => x.username == model.UserName);

                // If user not found, return 404 Not Found
                if (tblMember == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        statusMessage = "Not Found",
                        message = "User not found"
                    });
                }

                // Check if the old password is correct
                if (tblMember.password != model.OldPassword)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Old password is incorrect"
                    });
                }

                // Update the password and save changes
                tblMember.password = model.NewPassword;
                db.SubmitChanges();

                // Return success response
                return Content(HttpStatusCode.OK, new
                {
                    statusCode = 200,
                    statusMessage = "OK",
                    message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                // Return internal server error if an exception occurs
                return Content(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    statusMessage = "Internal Server Error",
                    message = "An error occurred: " + ex.Message
                });
            }
        }
        [HttpPost]
        [Route("UpdateProfile")]
        public IHttpActionResult UpdateProfile(AccountsModels.UpdateProfileModel model, string Username)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var member = db.tblMembers.SingleOrDefault(x => x.username == Username);
                if (member == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        statusMessage = "Not Found",
                        message = "User not found"
                    });
                }

                member.name = $"{model.firstName} {model.lastName}";
                member.email = model.email;
                member.aadhar = !string.IsNullOrEmpty(model.adharNo) ? model.adharNo : member.aadhar;
                member.pan = !string.IsNullOrEmpty(model.panNo) ? model.panNo : member.pan;
                member.address = !string.IsNullOrEmpty(model.address) ? model.address : member.address;
                member.pincode = !string.IsNullOrEmpty(model.pinCode) ? model.pinCode : member.pincode;
                member.city = model.city;
                member.state = model.state;

                db.SubmitChanges();

                return Content(HttpStatusCode.OK, new
                {
                    statusCode = 200,
                    statusMessage = "OK",
                    message = "Profile Updated Successfully"
                });
            }
            catch (Exception ex)
            {
                // Log the exception details (if logging is set up)
                Console.WriteLine(ex);

                return Content(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    statusMessage = "Internal Server Error",
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("getaccountdetailsbyusername")]
        public HttpResponseMessage getaccountdetailsbyusername(string username)
        {
            try
            {
                // Find the account by username
                var account = db.tblMembers.SingleOrDefault(x => x.username == username);

                if (account != null)
                {
                    // Find the referrer by referrer ID (assuming sid is the referrer ID)
                    var referrer = db.tblMembers.SingleOrDefault(x => x.username == account.sid);
                    string referrerMobile = referrer != null ? referrer.mobile : "";

                    // Create response with account details
                    var responseData = new
                    {
                        Username = account.username,
                        Email = account.email,
                        Name = account.name,
                        CurrentPlan = account.plan,
                        OldPlan = account.renewalplan,
                        City = account.city,
                        State = account.state,
                        ReferrerId = referrerMobile,
                        ReferrerName = account.sname,
                        Pan = account.pan,
                        Aadhar = account.aadhar,
                        PinCode = account.pincode,
                        MobileNumber = account.mobile,
                        GMwallet = account.credit,
                        RepurchaseWallet = account.RepurchaseWallet,
                        RewardPoints = account.rewardPoints,
                        BoosterPoints = account.boosterPoints,
                        WalletBalance = account.selfPurchaseWallet,
                        ProfilePicture = account.UploadId
                    };

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());

                    return response;
                }
                else
                {
                    // Account not found
                    HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "Account not found"
                    }, new JsonMediaTypeFormatter());

                    return responseNotFound;
                }
            }
            catch (Exception ex)
            {
                // Internal server error
                HttpResponseMessage responseServerError = Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Failed",
                    message = "Internal Server Error: " + ex.Message
                }, new JsonMediaTypeFormatter());

                return responseServerError;
            }
        }

        [HttpPost]
        [Route("addmoney")]
        public HttpResponseMessage AddMoney(AddMoneyModel model)
        {
            try
            {
                // Check if model state is valid
                if (!ModelState.IsValid)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
                }

                // Find the user in the database
                var tblMember = db.tblMembers.SingleOrDefault(x => x.username == model.Username);

                if (tblMember == null)
                {
                    // User not found
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Not Found",
                        message = "User not found"
                    });
                }

                // Check if amount is not negative
                if (model.Amount <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        statusCode = HttpStatusCode.BadRequest,
                        statusMessage = "Bad Request",
                        message = "Amount cannot be negative"
                    });
                }

                // Handle different payment modes
                if (model.Paymentmode == "gm_wallet")
                {
                    // Check if sufficient funds in GM Wallet
                    if (tblMember.credit < model.Amount)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            statusCode = HttpStatusCode.BadRequest,
                            statusMessage = "Bad Request",
                            message = "Insufficient funds in GM Wallet"
                        });
                    }

                    // Deduct amount from GM Wallet and update selfPurchaseWallet
                    tblMember.credit -= model.Amount;
                    tblMember.selfPurchaseWallet += model.Amount;

                    // Insert record into FundWallets and Payouts
                    var fundWallet = new FundWallet
                    {
                        username = model.Username,
                        desc = "Credit",
                        date = DateTime.UtcNow.AddHours(5.5),
                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                        credit = model.Amount,
                        status = "Send",
                        remarks = "GM Wallet Transfer",
                        summury = model.summary
                    };
                    db.FundWallets.InsertOnSubmit(fundWallet);
                    db.SubmitChanges();
                    var payout = new Payout
                    {
                        username = model.Username,
                        desc = "GM Wallet Debit",
                        date = DateTime.UtcNow.AddHours(5.5),
                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                        status = "Send",
                        debit = model.Amount,
                        summury = model.summary
                    };
                    db.Payouts.InsertOnSubmit(payout);

                    db.SubmitChanges();
                }
                else if (model.Paymentmode == "razor_pay")
                {
                    tblMember.selfPurchaseWallet += model.Amount;

                    var fundWallet = new FundWallet
                    {
                        username = model.Username,
                        desc = "Credit",
                        date = DateTime.UtcNow.AddHours(5.5),
                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                        credit = model.Amount,
                        status = "Send",
                        remarks = "Added To Fund Wallet",
                        summury = model.summary
                    };
                    db.FundWallets.InsertOnSubmit(fundWallet);

                    db.SubmitChanges();
                }

                // Return success response
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    statusCode = HttpStatusCode.OK,
                    statusMessage = "Success",
                    message = "Money added successfully"
                });
            }
            catch (Exception ex)
            {
                // Return internal server error if an exception occurs
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Internal Server Error",
                    message = "An error occurred: " + ex.Message
                });
            }
        }

        [HttpGet]
        [Route("Getname")]
        public HttpResponseMessage Getname(string mobileNo)
        {
            try
            {
                // Get the name of the authenticated user
                string username = this.User.Identity.Name;

                // Find the member with the provided mobile number
                var tblMember = this.db.tblMembers.SingleOrDefault(x => x.mobile == mobileNo);

                if (tblMember != null)
                {
                    // Return the name and username of the found member
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            Name = tblMember.name,
                            Username = tblMember.username
                        }
                    });
                }
                else
                {
                    // Return a 404 response if the member is not found
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "User not found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Return a 500 response if an exception occurs
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Failed",
                    message = "Internal Server Error: " + ex.Message
                });
            }

        }

        [HttpPost]
        [Route("sendmoney")]
        public HttpResponseMessage sendmoney(AccountsModels.SendMoneyModel model)
        {

            try
            {
                tblMember sender = db.tblMembers.SingleOrDefault(x => x.username == model.SenderUsername);
                tblMember recipient = db.tblMembers.SingleOrDefault(x => x.mobile == model.RecipientMobileNo || x.username == model.RecipientUsername);

                if (sender == null || recipient == null)
                {
                    return NotFoundResponse("User not found");
                }

                if (model.Amount <= 0)
                {
                    return BadRequestResponse("Amount cannot be negative");
                }

                if (sender.selfPurchaseWallet < model.Amount)
                {
                    return BadRequestResponse("Insufficient funds in Sender's Fund Wallet");
                }

                sender.selfPurchaseWallet -= model.Amount;
                recipient.selfPurchaseWallet += model.Amount;

                // Save changes only once
                db.SubmitChanges();

                // Log transactions
                LogTransaction(sender, recipient, model.Amount);

                return SuccessResponse("Money Sent successfully");
            }
            catch (Exception ex)
            {
                return InternalServerErrorResponse("An error occurred: " + ex.Message);
            }
        }

        private HttpResponseMessage BadRequestResponse(string message)
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.BadRequest);
            response.Content = new ObjectContent<object>(new
            {
                statusCode = HttpStatusCode.BadRequest,
                statusMessage = "Bad Request",
                message
            }, new JsonMediaTypeFormatter());
            return response;
        }

        private HttpResponseMessage NotFoundResponse(string message)
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.NotFound);
            response.Content = new ObjectContent<object>(new
            {
                statusCode = HttpStatusCode.NotFound,
                statusMessage = "Not Found",
                message
            }, new JsonMediaTypeFormatter());
            return response;
        }

        private HttpResponseMessage SuccessResponse(string message)
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new ObjectContent<object>(new
            {
                statusCode = HttpStatusCode.OK,
                statusMessage = "Success",
                message
            }, new JsonMediaTypeFormatter());
            return response;
        }

        private HttpResponseMessage InternalServerErrorResponse(string message)
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
            response.Content = new ObjectContent<object>(new
            {
                statusCode = HttpStatusCode.InternalServerError,
                statusMessage = "Internal Server Error",
                message
            }, new JsonMediaTypeFormatter());
            return response;
        }

        private void LogTransaction(tblMember sender, tblMember recipient, decimal amount)
        {
            DateTime utcNow = DateTime.UtcNow.AddDays(5.5);
            decimal roundedAmount = Math.Round(amount, 2);

            // Log debit transaction for sender
            db.FundWallets.InsertOnSubmit(new FundWallet()
            {
                username = sender.username,
                desc = "Debit",
                date = utcNow,
                debit = roundedAmount,
                status = "Send",
                incomefrom = recipient.username,
                remarks = "Transfer Out-" + recipient.username,
                summury = "Money Sent To " + recipient.name + " (" + roundedAmount + ")"
            });

            // Log credit transaction for recipient
            db.FundWallets.InsertOnSubmit(new FundWallet()
            {
                username = recipient.username,
                credit = roundedAmount,
                date = utcNow,
                desc = "Credit",
                remarks = "Transfer In",
                incomefrom = sender.username,
                status = "Send",
                summury = "Money Received From " + sender.name + " (" + roundedAmount + ")"
            });

            // Save changes once after logging both transactions
            db.SubmitChanges();
        }

        [HttpGet]
        [Route("getplandetails")]
        public HttpResponseMessage getplandetails()
        {
            HttpResponseMessage response;
            try
            {
                List<tblPlan> activePlans = db.tblPlans.Where(p => p.Description == "Active").ToList();

                if (activePlans.Any())
                {
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = activePlans
                    }, new JsonMediaTypeFormatter());
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"not found\"}", Encoding.UTF8, "application/json");
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
            }
            return response;
        }

        [HttpGet]
        [Route("getgmwalletbyusername")]
        public HttpResponseMessage getgmwalletbyusername(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
                List<Payout> payouts = db.Payouts
                    .Where(x => x.username == Username && (x.desc.Trim() == "GM Wallet Transfer" ||
                                            x.desc.Trim() == "GM Wallet Debit" ||
                                            x.desc.Trim()=="GM Wallet" ||
                                            x.remarks.Trim() == "GM Wallet" ||
                                            x.remarks.Trim() == "Pair Match Commission"))
                    .OrderByDescending(x => x.confirmdate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (payouts.Any())
                {
                    var responseData = payouts.Select(wallet => new
                    {
                        Credit = wallet.credit.HasValue ? wallet.credit : null,
                        Date = wallet.confirmdate,
                        Debit = wallet.debit.HasValue ? wallet.debit : null,
                        Remark = wallet.desc
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    { 
                        statusCode = 200,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;
            }
            catch (Exception ex)
            {
                HttpResponseMessage responseInternalServerError = Request.CreateResponse(HttpStatusCode.InternalServerError);
                responseInternalServerError.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return responseInternalServerError;
            }
        }

        [HttpGet]
        [Route("getrepurchasewalletbyusername")]
        public HttpResponseMessage getrepurchasewalletbyusername(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
                List<Payout> payouts = db.Payouts
                    .Where(x => x.username == Username && (x.desc == "Repurchase Income" || x.desc=="Repurchase Wallet"))
                    .OrderByDescending(x => x.confirmdate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (payouts.Any())
                {
                    var responseData = payouts.Select(wallet => new
                    {
                        Credit = wallet.credit ?? 0,
                        Date = wallet.confirmdate,
                        Debit = wallet.debit ?? 0,
                        Remark = wallet.remarks
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;
            }
            catch (Exception ex)
            {
                HttpResponseMessage responseInternalServerError = Request.CreateResponse(HttpStatusCode.InternalServerError);
                responseInternalServerError.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return responseInternalServerError;
            }
        }

        [HttpGet]
        [Route("getBoosterpointsbyusername")]
        public HttpResponseMessage getBoosterpointsbyusername(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
                List<Payout> payouts = db.Payouts
                    .Where(x => x.username == Username && x.remarks == "Booster Points")
                    .OrderByDescending(x => x.confirmdate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (payouts.Any())
                {
                    var responseData = payouts.Select(wallet => new
                    {
                        Credit = wallet.credit ?? 0,
                        Date = wallet.confirmdate,
                        Debit = wallet.debit ?? 0,
                        Remark = wallet.remarks,
                        Summary = wallet.summury
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;
            }
            catch (Exception ex)
            {
                HttpResponseMessage responseInternalServerError = Request.CreateResponse(HttpStatusCode.InternalServerError);
                responseInternalServerError.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return responseInternalServerError;
            }
        }

        [HttpGet]
        [Route("getFundwalletbyusername")]
        public HttpResponseMessage getFundwalletbyusername(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
                List<FundWallet> wallets = db.FundWallets
                    .Where(x => x.username == Username)
                    .OrderByDescending(x => x.date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (wallets.Any())
                {
                    var responseData = wallets.Select(wallet => new
                    {
                        Credit = wallet.credit,
                        Date = wallet.date,
                        Debit = wallet.credit > 0 ? null : wallet.debit,
                        Remark = wallet.remarks ?? wallet.desc,
                        IncomeFrom = wallet.incomefrom,
                        Summary = wallet.summury
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;
            }
            catch (Exception ex)
            {
                HttpResponseMessage responseInternalServerError = Request.CreateResponse(HttpStatusCode.InternalServerError);
                responseInternalServerError.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return responseInternalServerError;
            }
        }

        [HttpGet]
        [Route("GetGiftvoucherTotal")]
        public HttpResponseMessage GetGiftvoucherTotal(string Username)
        {
            try
            {
                decimal? totalCredit = db.FundWallets
                    .Where(x => x.username == Username && x.remarks == "Gift Voucher")
                    .Sum(x => x.credit);

                if (totalCredit.HasValue && totalCredit > 0)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = totalCredit
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;
            }
            catch (Exception ex)
            {
                HttpResponseMessage responseInternalServerError = Request.CreateResponse(HttpStatusCode.InternalServerError);
                responseInternalServerError.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return responseInternalServerError;
            }
        }

        [HttpGet]
        [Route("GetUserData")]
        public HttpResponseMessage GetUserData(string username, int page = 1, int pageSize = 10)
        {
            try
            {
                string loggedInUsername = User.Identity.Name;

                // Call API 1 endpoint logic
                var signUpData = GetSignUpDateByUsername(username);

                // Call API 2 endpoint logic
                var joinReferData = GetJoinDateReferByUsername(username, page, pageSize);

                // Call API 3 endpoint logic
                var activationData = GetActivationDateByUsername(username);

                var rewardpointscredit = GetRewardPointsByUsername(username, page, pageSize);

                // Combine results into a single array
                List<object> combinedData = new List<object>();
                if (signUpData != null && page == 1)
                {
                    combinedData.Add(signUpData);
                }
                if (joinReferData != null)
                {
                    combinedData.AddRange((IEnumerable<object>)joinReferData);
                }
                if (activationData != null && page == 1)
                {
                    combinedData.AddRange((IEnumerable<object>)activationData);
                }
                if (rewardpointscredit != null)
                {
                    combinedData.AddRange((IEnumerable<object>)rewardpointscredit);
                }


                combinedData.Sort((a, b) => DateTime.Compare(GetDateFromObject(b), GetDateFromObject(a)));

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ObjectContent<object>(new
                {
                    statusCode = 200,
                    statusMessage = "Success",
                    data = combinedData
                }, new JsonMediaTypeFormatter());

                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }
        private DateTime GetDateFromObject(object obj)
        {
            // Check if the object is null or not of the expected type
            if (obj == null || !(obj is dynamic))
            {
                throw new ArgumentException("Invalid object or object does not contain a valid date property.");
            }

            // Assuming each object has a DateTime property named Date
            try
            {
                dynamic dynamicObj = obj;
                return dynamicObj.Date;
            }
            catch (RuntimeBinderException ex)
            {
                throw new ArgumentException("Object does not contain a valid date property.", ex);
            }
        }

        // Helper methods for each individual API endpoint
        private object GetSignUpDateByUsername(string username)
        {
            var user = db.tblMembers.SingleOrDefault(x => x.username == username);

            if (user != null)
            {
                return new
                {
                    Date = user.joindate,
                    Credit = 50,
                    Description = "Registration"
                };
            }
            else
            {
                return null;
            }
        }

        private object GetJoinDateReferByUsername(string username, int page, int pageSize)
        {
            var user = db.tblMembers.FirstOrDefault(x => x.sid == username);


            if (user != null)
            {
                var users = db.tblMembers.Where(x => x.sid == username)
                                         .OrderByDescending(x => x.joindate)
                                         .Skip((page - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToList();

                if (users != null && users.Any())
                {
                    List<object> responseData = new List<object>();

                    foreach (var u in users)
                    {
                        responseData.Add(new
                        {
                            Name = u.name,
                            Date = u.joindate,
                            Credit = 50,
                            Description = "Refer"
                        });
                    }

                    return responseData;
                }
                else
                {
                    return null;
                }

            }
            else
            {
                return null;
            }
        }

        private object GetActivationDateByUsername(string username)
        {
            var users = db.tblActivations.Where(x => x.username == username && x.newPlan != null);

            if (users != null && users.Any())
            {
                List<object> responseData = new List<object>();

                foreach (var user in users)
                {
                    var plan = db.tblPlans.FirstOrDefault(p => p.PlanName == user.newPlan);
                    if (plan != null)
                    {
                        responseData.Add(new
                        {
                            Date = user.activatedDate,
                            Credit = plan.points, // Fetching the amount from tblPlans based on PlanName
                            Description = user.newPlan
                        });
                    }
                    else
                    {
                        // Handle case where plan is not found
                    }
                }
                return responseData;
            }
            else
            {
                return null;
            }
        }

        private object GetRewardPointsByUsername(string username, int page, int pageSize)
        {
            var user = db.Payouts.FirstOrDefault(x => x.username == username);

            if (user != null)
            {
                var transactions = db.Payouts.Where(x => x.username == username && x.remarks == "Reward Points")
                                             .OrderByDescending(x => x.confirmdate)
                                             .Skip((page - 1) * pageSize)
                                             .Take(pageSize)
                                             .ToList();

                if (transactions != null && transactions.Any())
                {
                    List<object> responseData = new List<object>();

                    foreach (var transaction in transactions)
                    {
                        if (transaction.credit > 0) // Check if it's a credit transaction
                        {
                            responseData.Add(new
                            {
                                Date = transaction.confirmdate,
                                Credit = transaction.credit,
                                Description = transaction.remarks,
                                Summary = transaction.summury
                            });
                        }
                        else if (transaction.debit > 0) // Check if it's a debit transaction
                        {
                            responseData.Add(new
                            {
                                Date = transaction.confirmdate,
                                Debit = transaction.debit,
                                Description = transaction.remarks,
                                Summary = transaction.summury
                            });
                        }
                    }

                    return responseData;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        [HttpGet]
        [Route("GetCashback")]
        public HttpResponseMessage GetCashback(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
                // Retrieve cashback transactions for the specified user
                List<FundWallet> cashbackTransactions = db.FundWallets
                    .Where(x => x.username == Username && x.remarks == "Cashback")
                    .OrderByDescending(x => x.confirmdate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (cashbackTransactions.Any())
                {
                    // Create a success response
                    var responseData = cashbackTransactions.Select(wallet => new
                    {
                        Credit = wallet.credit,
                        Date = wallet.confirmdate,
                        Remark = wallet.desc,
                        Summary = wallet.summury
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    // Create a not found response
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "Data not found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Create an internal server error response
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetCashbackEarned")]
        public HttpResponseMessage GetCashbackEarned(string Username)
        {
            try
            {

                decimal? totalcashbackEarned = db.FundWallets
                    .Where(x => x.username == Username && x.remarks == "Cashback")
                   .Sum(x => x.credit);

                if (totalcashbackEarned.HasValue && totalcashbackEarned > 0)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = totalcashbackEarned
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;

            }
            catch (Exception ex)
            {
                // Create an internal server error response
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetTotalPayout")]
        public HttpResponseMessage GetTotalPayout(string Username)
        {
            try
            {

                decimal? totalpayout = db.Payouts
                    .Where(x => x.username == Username && (x.remarks != "Booster Points" && x.remarks != "Reward Points"))
                   .Sum(x => x.credit);

                if (totalpayout.HasValue && totalpayout > 0)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = 200,
                        statusMessage = "Success",
                        data = totalpayout
                    }, new JsonMediaTypeFormatter());
                    return response;
                }

                HttpResponseMessage responseNotFound = Request.CreateResponse(HttpStatusCode.NotFound);
                responseNotFound.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Data not found\"}", Encoding.UTF8, "application/json");
                return responseNotFound;

            }
            catch (Exception ex)
            {
                // Create an internal server error response
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GeAffiliateIncome")]
        public HttpResponseMessage GeAffiliateIncome(string Username, int page = 1, int pageSize = 10)
        {
            try
            {


                // Retrieve affiliate income and generation income payouts for the specified user
                List<Payout> payouts = this.db.Payouts
                    .Where(x => x.username == Username && (x.remarks == "Generation Income" || x.remarks == "Affiliate Income"))
                    .OrderByDescending(x => x.confirmdate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (payouts.Any())
                {
                    // Create a success response
                    var responseData = payouts.Select(wallet => new
                    {
                        Credit = wallet.credit,
                        Date = wallet.confirmdate,
                        Remark = wallet.remarks,
                        Summary = wallet.summury
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    // Create a not found response
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "Data not found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Create an internal server error response
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetDiscountDetails")]
        public HttpResponseMessage GetDiscountDetails(int operatorID, string serviceName)
        {
            try
            {
                var serviceProvider = db.tblServiceProviders.SingleOrDefault(sp => sp.value == operatorID);
                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == serviceName);
                var response = Request.CreateResponse(HttpStatusCode.OK);

                if (discountMaster != null)
                {
                    if (discountMaster.status == 1)
                    {
                        var discounts = new List<object>();

                        if (serviceName == "mobile_recharge" || serviceName == "dth_recharge")
                        {
                            if (serviceProvider.value == 11 || serviceProvider.value == 18 || serviceProvider.value == 8)
                            {
                                discounts.Add(new
                                {
                                    discountType = "reward_points",
                                    discount = discountMaster.rew_2
                                });
                            }
                            else
                            {
                                discounts.Add(new
                                {
                                    discountType = "reward_points",
                                    discount = discountMaster.rew_1
                                });
                            }
                        }

                        discounts.Add(new
                        {
                            discountType = "booster_points",
                            discount = discountMaster.booster
                        });

                        response.Content = new ObjectContent<object>(new
                        {
                            statusCode = HttpStatusCode.OK,
                            statusMessage = "Success",
                            data = discounts
                        }, new JsonMediaTypeFormatter());
                    }
                    else
                    {
                        response.Content = new StringContent("{\"statusCode\": 404, \"status\": \"failed\", \"message\": \"Sorry !! Service Not available \"}", System.Text.Encoding.UTF8, "application/json");
                    }
                }
                else
                {
                    response.Content = new StringContent("{\"statusCode\": 404, \"status\": \"failed\", \"message\": \"No Discount\"}", System.Text.Encoding.UTF8, "application/json");
                }

                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"status\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetUtilityDetailsbyUsername")]
        public HttpResponseMessage GetUtilityDetailsbyUsername(string username, string serviceName = null, int page = 1, int pageSize = 10)
        {
            HttpResponseMessage response;

            try
            {
                string Username = User.Identity.Name;
                var user = db.tblMembers.SingleOrDefault(x => x.username == username);
                IQueryable<tblRecharge> rechargeQuery = db.tblRecharges.Where(x => x.userID == username);

                if (!string.IsNullOrEmpty(serviceName))
                {
                    rechargeQuery = rechargeQuery.Where(x => x.remarks == serviceName);
                }

                var recharge = rechargeQuery.OrderByDescending(x => x.date)
                                           .Skip((page - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToList();

                if (recharge != null && recharge.Any())
                {
                    List<object> responseData = new List<object>();

                    foreach (var reg in recharge)
                    {
                        responseData.Add(new
                        {
                            Name = reg.summury,
                            Date = reg.date,
                            Amount = reg.amount,
                            Discount = reg.discountType,
                            Mode = reg.paymentMode,
                            Operator = reg.Operator,
                            Description = reg.remarks
                        });
                    }

                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData

                    }, new JsonMediaTypeFormatter());

                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"status\": \"failed\", \"message\": \"Not found\"}", System.Text.Encoding.UTF8, "application/json");
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"status\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
            }

            return response;

        }

        [HttpGet]
        [Route("GetNews")]
        public HttpResponseMessage GetNews()
        {
            try
            {

                // Retrieve list of news
                List<New1> newsList = db.New1s.ToList();

                // Filter active news
                List<New1> activeNews = newsList.Where(n => n.status == "Active").ToList();

                // If there are active news
                if (activeNews.Any())
                {
                    // Create success response
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = activeNews
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    // Create not found response
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "No active news found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Create internal server error response
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Failed",
                    message = "Internal Server Error: " + ex.Message
                });
            }
        }

        [HttpGet]
        [Route("GetRechargeBillsUsername")]
        public HttpResponseMessage GetRechargeBillsUsername(string username, string serviceName = null, int page = 1, int pageSize = 10)
        {
            HttpResponseMessage response;

            try
            {
                string Username = User.Identity.Name;
                IQueryable<tblRecharge> rechargeQuery = db.tblRecharges.Where(x => x.userID == username);


                var recharge = rechargeQuery.OrderByDescending(x => x.date)
                                           .Skip((page - 1) * pageSize)
                                           .Take(pageSize)
                                           .ToList();

                if (recharge != null && recharge.Any())
                {
                    List<object> responseData = new List<object>();

                    foreach (var reg in recharge)
                    {
                        responseData.Add(new
                        {
                            Name = reg.userID,
                            Date = reg.date,
                            Amount = reg.amount,
                            Discount = reg.discountType,
                            Mode = reg.paymentMode,
                            Operator = reg.Operator,
                            Description = reg.remarks,
                            Summary = reg.summury
                        });
                    }

                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData

                    }, new JsonMediaTypeFormatter());

                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"status\": \"failed\", \"message\": \"Not found\"}", System.Text.Encoding.UTF8, "application/json");
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"status\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
            }

            return response;

        }

        [HttpGet]
        [Route("GetUpgradationDetailsbyUsername")]
        public HttpResponseMessage GetUpgradationDetailsbyUsername(string username)
        {
            try
            {
                var members = db.tblMembers.Where(x => x.username == username).ToList();
                if (members.Any())
                {
                    var objectList = new List<object>();

                    foreach (var member in members)
                    {
                        if (string.IsNullOrEmpty(member.plan) && string.IsNullOrEmpty(member.renewalplan))
                        {
                            var response = Request.CreateResponse(HttpStatusCode.NotFound, new
                            {
                                statusCode = 404,
                                status = "Not Found",
                                message = "No active plan for this user"
                            });
                            return response;
                        }

                        objectList.Add(new
                        {
                            Username = member.username,
                            Name = member.name,
                            Date = member.renewal,
                            CurrentPlan = member.plan,
                            OldPlan = member.renewalplan
                        });
                    }

                    var successResponse = Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = objectList
                    });
                    return successResponse;
                }
                else
                {
                    var notFoundResponse = Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        status = "Failed",
                        message = "Account Not Found"
                    });
                    return notFoundResponse;
                }
            }
            catch (Exception ex)
            {
                var errorResponse = Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    status = "Failed",
                    message = $"Internal Server Error: {ex.Message}"
                });
                return errorResponse;
            }
        }


        [HttpGet]
        [Route("ScanQRCode")]
        public HttpResponseMessage ScanQRCode(string vendorname)
        {
            HttpResponseMessage response;
            try
            {
                // Fetch vendor data from the database based on vendorId
                var vendor = db.tblVendors.FirstOrDefault(v => v.Username == vendorname);
                if (vendor == null)
                {
                    // return BadRequest("Vendor data not found for the specified vendor ID");
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Vendor data not found for the specified vendor ID\"}", System.Text.Encoding.UTF8, "application/json");
                }

                if (vendor != null)
                {
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            Username = vendor.Username,

                            Name = vendor.VendorName,
                            CompanyName = vendor.CompanyName,
                            City = vendor.City,
                            State = vendor.State,

                        }
                    }, new JsonMediaTypeFormatter());
                }

                else
                {
                    //return BadRequest("Vendor ID does not match");
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Vendor ID does not match\"}", System.Text.Encoding.UTF8, "application/json");
                }

            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
            }
            return response;
        }

        [HttpGet]
        [Route("GetPayment")]
        public HttpResponseMessage GetPayment(string username)
        {
            HttpResponseMessage response;
            try
            {

                var user = db.tblMembers.FirstOrDefault(v => v.username == username);
                if (user == null)
                {
                    // return BadRequest("Vendor data not found for the specified vendor ID");
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"data not found\"}", System.Text.Encoding.UTF8, "application/json");
                }

                if (user != null && user.isActive == 1 && (user.status.Equals("paid") || user.status.Equals("Paid")))
                {
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            AffiliateBalance = user.RepurchaseWallet,
                            Fundwallet = user.selfPurchaseWallet,
                            Boosterpoints = user.boosterPoints

                        }
                    }, new JsonMediaTypeFormatter());
                }

                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            AffiliateBalance = 0,
                            Fundwallet = user.selfPurchaseWallet,
                            Boosterpoints = user.boosterPoints

                        }
                    }, new JsonMediaTypeFormatter());
                }

            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
            }
            return response;
        }

        [HttpPost]
        [Route("FranchiseePayment")]
        public IHttpActionResult FranchiseePayment(FranchiseePaymentModel model)
        {
            if (!ModelState.IsValid)
            {
                //return BadRequest(ModelState);
                return Content(HttpStatusCode.BadRequest, new { statusCode = 400, statusMessage = "Bad Request", message = ModelState });

            }

            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.SenderUsername);
                var vendor = db.tblVendors.SingleOrDefault(x => x.Username == model.VendorUsername);
                //var role = db.tblRoles.Single(x => x.RoleId == vendor.RoleId);

                decimal totalAmt = Convert.ToDecimal(model.Amount);

                decimal wallet = Convert.ToDecimal(user.RepurchaseWallet);
                decimal cash = Convert.ToDecimal(user.selfPurchaseWallet);


                if (user == null || vendor == null)
                {
                    // return NotFound();
                    return Content(HttpStatusCode.NotFound, new { statusCode = 404, statusMessage = "Not Found", message = "User not found" });

                }


                DateTime date = DateTime.UtcNow.AddHours(5.5);
                decimal debit = wallet + cash;

                if (debit >= totalAmt)
                {
                    // Debit the totalAmt from wallet if possible, otherwise deduct only available amount
                    if (wallet >= totalAmt)
                    {
                        user.RepurchaseWallet -= totalAmt;
                    }
                    else
                    {
                        decimal remainingAmt = totalAmt - user.RepurchaseWallet;
                        user.RepurchaseWallet = 0;
                        if (cash >= remainingAmt)
                        {
                            user.selfPurchaseWallet -= remainingAmt;

                            var fund1 = new FundWallet
                            {
                                username = user.username,
                                desc = "Purchase",
                                credit = 0, // Assuming boosterPointsAmount should be positive
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = remainingAmt,
                                remarks = "Franchisee Purchase",
                                summury = "Debit for Payment Of " + vendor.Username

                            };
                            db.FundWallets.InsertOnSubmit(fund1);
                            db.SubmitChanges();

                        }
                        else
                        {
                            throw new Exception("Insufficient funds");
                        }
                    }

                    vendor.CashWallet += model.Amount;

                    var history = new Payout
                    {
                        username = user.username,
                        credit = 0,
                        debit = totalAmt,
                        date = DateTime.UtcNow.AddHours(5.5),
                        confirmdate = date,
                        desc = "Repurchase Wallet Debit",
                        remarks = "Towards Repurchase",
                        status = "Send",
                        summury = "Debit for Payment Of " + vendor.Username
                    };


                    db.Payouts.InsertOnSubmit(history);
                    db.SubmitChanges();
                }

                else
                {
                    return Content(HttpStatusCode.BadRequest, new { statusCode = 400, statusMessage = "Bad Request", message = "You don't have sufficient balance in Wallet" });
                }

                // 5% calculating of amount
                decimal repurchaseIncome = model.Amount * 5 / 100;
                decimal franchCommission = 0;

                int number = 10000;
                var getLast = db.tblInvoices.Where(x => x.InvoiceNo != null).OrderByDescending(x => x.InvoiceNo).FirstOrDefault();
                if (getLast != null)
                {
                    number = Convert.ToInt32(getLast.InvoiceNo);
                }

                string purchaseType = "Genmed";
                string address = null;
                string dispatched = user.name;
                // 5% from vender debit
                vendor.franchiseeWallet -= repurchaseIncome;
                Debit(vendor.Username, repurchaseIncome, "Franchisee Wallet Debit", "Towards Genmed Order");
                // 10% to company wallet
                decimal companyWallet = repurchaseIncome * 10 / 100;
                decimal selftPurchaseIncome = repurchaseIncome * 25 / 100;
                var invoice = new tblInvoice
                {
                    username = user.username,
                    Amount = totalAmt,
                    PurchaseDate = date.Date,
                    Name = purchaseType,
                    DeliveredOn = date.Date,
                    DispachedOn = date,
                    status = "Delivered",
                    DeliveryType = 1,
                    FranchiseeId = vendor.Id,
                    InvoiceNo = (number + 1).ToString(),
                    userType = "Member",
                    RepurchaseCommission = franchCommission, // franchisee commission
                    Commission = companyWallet,
                    dispatchedTo = dispatched,
                    CourierAddress = address,
                    invType = "Paid"
                };
                db.tblInvoices.InsertOnSubmit(invoice);
                db.SubmitChanges();

                // Payout(franchisee.Username, debit, "Repurchase Income", "Franchisee Income", user.username, date, invoice.InvoiceId);
                // adding to repurchase 85%
                user.RepurchaseWallet += (selftPurchaseIncome * 85 / 100);
                if (selftPurchaseIncome > 0)
                {
                    Payout(user.username, selftPurchaseIncome, "Repurchase Income", "Self Purchase", user.username, date, invoice.InvoiceId);
                }

                string sponsor = user.sid;
                int i = 0;
                List<string> usernames = new List<string>();

                while (sponsor != null)
                {

                    var bv = (from c in db.tblMembers
                              where c.username == sponsor
                              select c).SingleOrDefault();
                    if (bv.status == "Paid")
                    {
                        i++;
                        usernames.Add(bv.username);
                    }
                    sponsor = bv.sid;
                    if (i == 15)
                        break;
                }

                if (i > 0)
                {
                    decimal incomeShare = (repurchaseIncome * 65 / 100) / 15;
                    foreach (var u in usernames)
                    {
                        var mem = db.tblMembers.SingleOrDefault(x => x.username == u);
                        mem.RepurchaseWallet += (incomeShare * 85 / 100);
                        if (incomeShare > 0)
                        {
                            Payout(u, incomeShare, "Repurchase Income", "Generation Income", user.username, date, invoice.InvoiceId);
                        }
                    }
                }

                if (model.DiscountType == "booster_points")
                {
                    decimal booster = model.Amount * 25m / 100;
                    if (user.boosterPoints >= booster)
                    {
                        user.boosterPoints -= booster;
                        user.selfPurchaseWallet += booster;


                    }
                    else
                    {

                        decimal remainingAmt = Math.Abs(booster - (decimal)user.boosterPoints);
                        booster -= remainingAmt;

                        user.boosterPoints = 0;

                        user.selfPurchaseWallet += booster - remainingAmt;

                    }
                    if (booster > 0)
                    {
                        var payout = new Payout
                        {
                            username = model.SenderUsername,
                            desc = "Franchisee Purchase",
                            credit = 0, // Assuming boosterPointsAmount should be positive
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = booster,
                            remarks = "Booster Points",
                            summury = "Debit for Payment Of " + vendor.Username

                        };
                        db.Payouts.InsertOnSubmit(payout);
                        db.SubmitChanges();

                        var fund1 = new FundWallet
                        {
                            username = model.SenderUsername,
                            desc = "Repurchase Cashback",
                            credit = booster, // Assuming boosterPointsAmount should be positive
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = 0,
                            remarks = "Franchisee Purchase",
                            summury = "Cashback From " + vendor.Username

                        };
                        db.FundWallets.InsertOnSubmit(fund1);
                        db.SubmitChanges();

                        PublicArray = new object[]
                           {

                                new
                                {
                                     label="You got a Cashback Credit of ",
                                    value= booster
                                }
                           };


                        // cashback = "You received a cashback of ₹ " + booster;

                    }

                }

                Payout(vendor.Username, model.Amount, "Repurchase Income", "Franchisee Income", user.username, date, invoice.InvoiceId);
                var smsService = new SendSms();
                string msg = "Dear " + user.name + ", You have purchased products worth Rs " + totalAmt + ". Thanks for shopping with" + vendor.CompanyName;
                smsService.sms(user.mobile, msg, "1407169769926420610");

                // return Ok("Money sent successfully!");
                return Content(HttpStatusCode.OK, new { statusCode = 200, statusMessage = "OK", message = "Payment Successfull", reward = PublicArray });

            }
            catch (Exception ex)
            {
                //return InternalServerError(ex);
                return Content(HttpStatusCode.InternalServerError, new { statusCode = 500, statusMessage = "Internal Server Error", message = "An error occurred: " + ex.Message });

            }

        }
        private void Debit(string uname, decimal Amount, string desc, string remarks)
        {
            var history = new FranchiseeWallet
            {
                username = uname,
                credit = 0,
                debit = Amount,
                date = DateTime.UtcNow.AddHours(5.5),
                confirmdate = DateTime.UtcNow.AddHours(5.5),
                desc = desc,
                remarks = remarks,
                status = "Send",
            };
            db.FranchiseeWallets.InsertOnSubmit(history);
            db.SubmitChanges();
        }
        private void Payout(string uname, Decimal amt, string desc, string remarks, string incomefrom, DateTime date, Decimal invoiceId, decimal tax = 0)
        {
            var history = new Payout
            {
                username = uname,
                credit = amt,
                debit = 0,
                tax = amt * 5 / 100,
                sc = amt * 10 / 100,
                deduction = amt * 5 / 100,
                netpayout = amt * 80 / 100,
                date = date,
                confirmdate = date,
                desc = desc,
                remarks = remarks,
                incomefrom = incomefrom,
                status = "Send",
                reward = invoiceId,
                summury = "Income From " + incomefrom
            };

            db.Payouts.InsertOnSubmit(history);
            db.SubmitChanges();
        }

        [HttpGet]
        [Route("GetFranchiseeName")]
        public HttpResponseMessage GetFranchiseeName()
        {
            try
            {

                var franchisee = db.tblVendors.Where(x => x.IsActive == true && x.RoleId == 3).ToList();

                if (franchisee != null && franchisee.Any())
                {
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = franchisee.Select(franchisees => new
                        {
                            Name = franchisees.VendorName,
                            Username = franchisees.Username
                        })
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    var response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"User not found\"}", System.Text.Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }


        [HttpGet]
        [Route("GetAccountBalance")]
        public HttpResponseMessage GetAccountBalance(string username)
        {
            HttpResponseMessage response;

            try
            {
                var account = db.tblMembers.SingleOrDefault(x => x.username == username);

                if (account != null)
                {
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = new
                        {
                            Username = account.username,
                            FundWallet = account.selfPurchaseWallet
                        }
                    }, new JsonMediaTypeFormatter());
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"Account not found\"}", System.Text.Encoding.UTF8, "application/json");
                }
            }
            catch (Exception ex)
            {
                response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
            }

            return response;
        }


        [HttpPost]
        [Route("ActivateMembership")]
        public IHttpActionResult ActivateMembership(AccountsModels.MembershipActivationRequest request)
        {
            try
            {
                tblMember insert = this.db.tblMembers.FirstOrDefault(x => x.username == request.Username);
                if (insert == null)
                {
                    return Content(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        statusMessage = "Not Found",
                        message = "User not found"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Username))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "MemberId is required"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.PlanID.ToString()))
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Plan is required"
                    });
                }

                tblPlan findPlan = this.db.tblPlans.FirstOrDefault(x => x.PlanId == request.PlanID);
                if (findPlan == null)
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Invalid plan"
                    });
                }

                if (insert.status == "Paid")
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "ID already activated"
                    });
                }

                // Check payment mode and balance in fund wallet
                if (request.Paymentmode == "fund_wallet")
                {
                    // Handle fund wallet payment
                    if (insert.selfPurchaseWallet < findPlan.PlanAmount)
                    {
                        return Content(HttpStatusCode.BadRequest, new
                        {
                            statusCode = 400,
                            statusMessage = "Bad Request",
                            message = "You don't have sufficient balance in Fund Wallet"
                        });
                    }
                    else
                    {
                       
                        insert.selfPurchaseWallet -= (decimal)findPlan.PlanAmount;

                        // Insert transaction details into FundWallets table
                        db.FundWallets.InsertOnSubmit(new FundWallet()
                        {
                            username = insert.username,
                            desc = "Debit",
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            debit = findPlan.PlanAmount,
                            status = "Send",
                            remarks = "Member Activation",
                            summury = request.summary
                        });

                       
                        insert.status = "Paid";
                        insert.plan = findPlan.PlanName;
                        insert.amount = findPlan.PlanAmount;
                        insert.renewal = DateTime.UtcNow.AddHours(5.5);

                        // 10-04 Re implemenred self booster points
                        if (findPlan.booster.Value > 0)
                        {
                            insert.boosterPoints += findPlan.booster.Value;
                            payout(findPlan.booster.Value, insert.username, insert.username, "Self-Booster Points", "Booster Points",request.summary);
                        }
                        if (findPlan.points > 0)
                        {
                            insert.rewardPoints += (decimal)findPlan.points;
                            payout((decimal)findPlan.points, insert.username, insert.username, "Self-Reward Points", "Reward Points", request.summary);
                        }

                        var topup = new tblActivation
                        {
                            username = insert.username,
                            activatedDate = insert.renewal,
                            amount = insert.amount,
                            activatedBy = request.Username,
                            isactive = 1,
                            status = "Activation",
                            newPlan = findPlan.PlanName
                        };
                        db.tblActivations.InsertOnSubmit(topup);
                        db.SubmitChanges();
                     
                        //Referral Income
                        var sid = db.tblMembers.SingleOrDefault(x => x.username == insert.sid);
                        if (sid.status == "Paid")
                        {
                            decimal credit = 0;
                            if (insert.plan.Contains("Prime Membership"))
                            {
                                //if (insert.plan.Contains("Super"))
                                //{
                                //    if (sid.plan.Contains("Super"))
                                //    {
                                //        sid.boosterPoints += 100;
                                //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                                //    }
                                //}
                                //else
                                //{
                                //    if (!sid.plan.Contains("Super"))
                                //    {
                                //        sid.boosterPoints += 100;
                                //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                                //    }
                                //}
                                // Booster Points to Sponsorer
                                sid.boosterPoints += 100;
                                payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points", request.summary);

                                credit = 100;
                            }
                            else if (insert.plan == "Super Membership")
                            {
                                sid.boosterPoints += 50;
                                payout(50, sid.username, insert.username, "Referral Bonus", "Booster Points", request.summary);

                                credit = 100;
                            }
                            else if (insert.plan == "Platinum Membership")
                            {
                                credit = 500;

                            }
                            if (credit > 0)
                            {
                                sid.credit += (credit * 80 / 100);
                                sid.RepurchaseWallet += (credit * 5 / 100);
                                payout(credit, sid.username, insert.username, "Referral Bonus", "GM Wallet", request.summary);
                            }
                        }

                        if (findPlan.product == true)
                        {
                            ProductRequest(insert.username, findPlan.PlanName);
                        }
                        int refid = (int)insert.refid;
                        string leg = insert.leg;
                        int i = 0;
                        Decimal bvAmount = (decimal)findPlan.BVAmount;

                        // franchisee Referral Commission
                        if (!string.IsNullOrEmpty(insert.franchise))
                        {
                            decimal franchRefCommission = insert.amount.Value * 1 / 100;
                            var franchisee = db.tblVendors.SingleOrDefault(x => x.Username == insert.franchise);
                            franchisee.KitWallet += (franchRefCommission * 80 / 100);
                            payout(franchRefCommission, franchisee.Username, request.Username, "Referral Commission", "Franchisee Income", request.summary);

                            if (franchisee != null)
                            {
                                decimal masterRefCommission = insert.amount.Value * 0.5m / 100;
                                var master = db.tblVendors.SingleOrDefault(x => x.Id == franchisee.ReportingId);
                                master.KitWallet += (masterRefCommission * 80 / 100);
                                payout(masterRefCommission, master.Username, request.Username, "Referral Commission", "Franchisee Income", request.summary);
                            }
                        }

                        while (refid != 0)
                        {
                            i++;
                            var bv = (from c in db.tblMembers
                                      where c.id == refid
                                      select c).SingleOrDefault();

                            if (leg == "Left")
                            {
                                if (bv.status == "Paid")
                                {
                                    bv.lcarry = bv.lcarry + bvAmount;
                                    bv.tlcount = bv.tlcount + bvAmount;
                                    bv.silverLeft += 1;
                                }

                            }
                            if (leg == "Right")
                            {
                                if (bv.status == "Paid")
                                {
                                    bv.rcarry = bv.rcarry + bvAmount;
                                    bv.trcount = bv.trcount + bvAmount;
                                    bv.silverRight += 1;
                                }


                            }
                            if (bv.TourPackageEligible == false)   // 2:1 or 1:2 update  for Enable flag
                            {
                                if (bv.silverLeft >= 1 && bv.silverRight >= 2)
                                {
                                    bv.TourPackageEligible = true;
                                }
                                else if (bv.silverLeft >= 2 && bv.silverRight >= 1)
                                {
                                    bv.TourPackageEligible = true;
                                }
                            }
                            db.SubmitChanges();
                            AddBv(bv.username, bvAmount, insert.renewal.Value.Date, leg, insert.username, "Binary");

                            refid = (int)bv.refid;
                            leg = bv.leg;
                        }

                        var smsService = new SendSms();
                        string msg = "Dear " + insert.name + ", Congratulations! Your User ID:" + insert.username + " is successfully activated. " + ConfigurationManager.AppSettings["companyname"];
                        smsService.sms(insert.mobile, msg, "1407169769920372126");

                    }
                }
                else if (request.Paymentmode == "razor_pay")
                {

                    insert.status = "Paid";
                    insert.plan = findPlan.PlanName;
                    insert.amount = findPlan.PlanAmount;
                    insert.renewal = DateTime.UtcNow.AddHours(5.5);

                    // 10-04 Re implemenred self booster points
                    if (findPlan.booster.Value > 0)
                    {
                        insert.boosterPoints += findPlan.booster.Value;
                        payout(findPlan.booster.Value, insert.username, insert.username, "Self-Booster Points", "Booster Points", request.summary);
                    }
                    if (findPlan.points > 0)
                    {
                        insert.rewardPoints += (decimal)findPlan.points;
                        payout((decimal)findPlan.points, insert.username, insert.username, "Self-Reward Points", "Reward Points", request.summary);
                    }

                    var topup = new tblActivation
                    {
                        username = insert.username,
                        activatedDate = insert.renewal,
                        amount = insert.amount,
                        activatedBy = request.Username,
                        isactive = 1,
                        status = "Activation",
                        newPlan = findPlan.PlanName
                    };
                    db.tblActivations.InsertOnSubmit(topup);
                    db.SubmitChanges();

                    //Referral Income
                    var sid = db.tblMembers.SingleOrDefault(x => x.username == insert.sid);
                    if (sid.status == "Paid")
                    {
                        decimal credit = 0;
                        if (insert.plan.Contains("Prime Membership"))
                        {
                            //if (insert.plan.Contains("Super"))
                            //{
                            //    if (sid.plan.Contains("Super"))
                            //    {
                            //        sid.boosterPoints += 100;
                            //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                            //    }
                            //}
                            //else
                            //{
                            //    if (!sid.plan.Contains("Super"))
                            //    {
                            //        sid.boosterPoints += 100;
                            //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                            //    }
                            //}
                            // Booster Points to Sponsorer
                            sid.boosterPoints += 100;
                            payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points", request.summary);

                            credit = 100;
                        }
                        else if (insert.plan == "Super Membership")
                        {
                            sid.boosterPoints += 50;
                            payout(50, sid.username, insert.username, "Referral Bonus", "Booster Points", request.summary);

                            credit = 100;
                        }
                        else if (insert.plan == "Platinum Membership")
                        {
                            credit = 500;

                        }
                        if (credit > 0)
                        {
                            sid.credit += (credit * 80 / 100);
                            sid.RepurchaseWallet += (credit * 5 / 100);
                            payout(credit, sid.username, insert.username, "Referral Bonus", "GM Wallet", request.summary);
                        }
                    }

                    if (findPlan.product == true)
                    {
                        ProductRequest(insert.username, findPlan.PlanName);
                    }
                    int refid = (int)insert.refid;
                    string leg = insert.leg;
                    int i = 0;
                    Decimal bvAmount = (decimal)findPlan.BVAmount;

                    // franchisee Referral Commission
                    if (!string.IsNullOrEmpty(insert.franchise))
                    {
                        decimal franchRefCommission = insert.amount.Value * 1 / 100;
                        var franchisee = db.tblVendors.SingleOrDefault(x => x.Username == insert.franchise);
                        franchisee.KitWallet += (franchRefCommission * 80 / 100);
                        payout(franchRefCommission, franchisee.Username, request.Username, "Referral Commission", "Franchisee Income", request.summary);

                        if (franchisee != null)
                        {
                            decimal masterRefCommission = insert.amount.Value * 0.5m / 100;
                            var master = db.tblVendors.SingleOrDefault(x => x.Id == franchisee.ReportingId);
                            master.KitWallet += (masterRefCommission * 80 / 100);
                            payout(masterRefCommission, master.Username, request.Username, "Referral Commission", "Franchisee Income", request.summary);
                        }
                    }

                    while (refid != 0)
                    {
                        i++;
                        var bv = (from c in db.tblMembers
                                  where c.id == refid
                                  select c).SingleOrDefault();

                        if (leg == "Left")
                        {
                            if (bv.status == "Paid")
                            {
                                bv.lcarry = bv.lcarry + bvAmount;
                                bv.tlcount = bv.tlcount + bvAmount;
                                bv.silverLeft += 1;
                            }

                        }
                        if (leg == "Right")
                        {
                            if (bv.status == "Paid")
                            {
                                bv.rcarry = bv.rcarry + bvAmount;
                                bv.trcount = bv.trcount + bvAmount;
                                bv.silverRight += 1;
                            }


                        }
                        if (bv.TourPackageEligible == false)   // 2:1 or 1:2 update  for Enable flag
                        {
                            if (bv.silverLeft >= 1 && bv.silverRight >= 2)
                            {
                                bv.TourPackageEligible = true;
                            }
                            else if (bv.silverLeft >= 2 && bv.silverRight >= 1)
                            {
                                bv.TourPackageEligible = true;
                            }
                        }
                        db.SubmitChanges();
                        AddBv(bv.username, bvAmount, insert.renewal.Value.Date, leg, insert.username, "Binary");

                        refid = (int)bv.refid;
                        leg = bv.leg;
                    }

                    var smsService = new SendSms();
                    string msg = "Dear " + insert.name + ", Congratulations! Your User ID:" + insert.username + " is successfully activated. " + ConfigurationManager.AppSettings["companyname"];
                    smsService.sms(insert.mobile, msg, "1407169769920372126");

                }
                else
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Invalid payment mode"
                    });
                }

                return Content(HttpStatusCode.OK, new
                {
                    statusCode = 200,
                    statusMessage = "OK",
                    message = "Membership activated successfully"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    statusMessage = "Internal Server Error",
                    Message = ex.Message
                });
            }
        }

        private void payout(Decimal amt, string uname, string incomefrom, string desc, string remarks, string summary)
        {
            var insert = new Payout();
            insert.username = uname;
            insert.date = DateTime.UtcNow.AddHours(5.5);
            insert.confirmdate = DateTime.UtcNow.AddHours(5.5);
            insert.desc = desc;
            insert.remarks = remarks;
            insert.tax = amt * 5 / 100;
            insert.sc = amt * 10 / 100;
            insert.deduction = amt * 5 / 100;
            insert.netpayout = amt * 80 / 100;
            insert.credit = amt;
            insert.debit = 0;
            insert.status = "Send";
            insert.incomefrom = incomefrom;
            insert.summury = summary;

            db.Payouts.InsertOnSubmit(insert);
            db.SubmitChanges();
        }

        private void AddBv(string uname, decimal pv, DateTime date, string desc, string bvfrom, string type)
        {
            var history = new tblBV
            {
                username = uname,
                Bv = 0,
                leftBv = 0,
                rightBv = 0,
                bvFrom = bvfrom,
                date = date.Date,
                desc = desc,
                status = "Pending",
                lcarry = 0,
                rcarry = 0,
                bvType = type
            };

            if (desc == "Left")
                history.lcarry = pv;
            else if (desc == "Right")
                history.rcarry = pv;

            db.tblBVs.InsertOnSubmit(history);
            db.SubmitChanges();
        }

        private void HandleProductRequest(string uname, string plan)
        {
            RequestProduct up = new RequestProduct();

            up.username = uname;
            up.status = "Pending";
            up.date = DateTime.UtcNow.AddHours(5.5);
            up.name = plan;
            db.RequestProducts.InsertOnSubmit(up);
            db.SubmitChanges();
        }


        [HttpPost]
        [Route("UpgradeMember")]
        public IHttpActionResult UpgradeMember(AccountsModels.UpgradeMemberRequest request)
        {
            try
            {
                tblMember insert = db.tblMembers.FirstOrDefault(x => x.username == request.Username);
                tblPlan plan = db.tblPlans.SingleOrDefault(x => x.PlanId == request.SelectedPlanID);

                if (insert == null)
                    return Content(HttpStatusCode.NotFound, new
                    {
                        statusCode = 404,
                        statusMessage = "Not Found",
                        message = "User not found"
                    });

                if (string.IsNullOrWhiteSpace(request.Username))
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "MemberId is required"
                    });

                if (plan == null)
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Invalid plan"
                    });

                if (insert.plan == plan.PlanName)
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Cannot upgrade to the same plan"
                    });
                Decimal amt = plan.PlanAmount ?? 0;
                if (request.paymentMode == "fund_wallet")
                {


                    if (insert.selfPurchaseWallet < amt)
                        return Content(HttpStatusCode.BadRequest, new
                        {
                            statusCode = 400,
                            statusMessage = "Bad Request",
                            message = "You don't have sufficient balance in Fund Wallet"
                        });

                    insert.selfPurchaseWallet -= amt;

                    FundWallet fundWallet = new FundWallet()
                    {
                        username = request.Username,
                        desc = "Debit",
                        date = DateTime.UtcNow.AddHours(5.5),
                        debit = amt,
                        status = "Send",
                        remarks = "Member Activation",
                        summury = request.summary
                    };

                    db.FundWallets.InsertOnSubmit(fundWallet);
                    db.SubmitChanges();

                    ProcessMemberUpdateAndUpgrade(insert, plan, request);
                }
                else if (request.paymentMode == "razor_pay")
                {

                    ProcessMemberUpdateAndUpgrade(insert, plan, request);

                }
                else
                {
                    return Content(HttpStatusCode.BadRequest, new
                    {
                        statusCode = 400,
                        statusMessage = "Bad Request",
                        message = "Invalid payment mode"
                    });
                }

                return Content(HttpStatusCode.OK, new
                {
                    statusCode = 200,
                    statusMessage = "OK",
                    message = "Successfully upgraded"
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    statusCode = 500,
                    statusMessage = "Internal Server Error",
                    message = ex.Message
                });
            }
        }

        private void ProcessMemberUpdateAndUpgrade(tblMember insert, tblPlan findPlan, AccountsModels.UpgradeMemberRequest request)
        {
            insert.renewalplan = insert.plan;
            insert.plan = findPlan.PlanName;
            insert.amount = findPlan.PlanAmount;
            if (findPlan.PlanName.Contains("Super") || insert.renewalplan.Contains("Super"))
                insert.isUpgradedSuper = true;

            // 10-04 Re implemenred self booster points
            if (findPlan.booster.Value > 0)
            {
                insert.boosterPoints += findPlan.booster.Value;
                payout(findPlan.booster.Value, insert.username, insert.username, "Self-Booster Points", "Booster Points",request.Username);
            }
            if (findPlan.points > 0)
            {
                insert.rewardPoints += (decimal)findPlan.points;
                payout((decimal)findPlan.points, insert.username, insert.username, "Self-Reward Points", "Reward Points",request.Username);
            }

            var topup = new tblActivation
            {
                username = insert.username,
                activatedDate = DateTime.UtcNow.AddHours(5.5),
                amount = insert.amount,
                activatedBy = request.Username,
                isactive = 1,
                status = "Upgradation",
                oldPlan = insert.renewalplan,
                newPlan = findPlan.PlanName
            };
            db.tblActivations.InsertOnSubmit(topup);
            db.SubmitChanges();

            //Referral Income
            var sid = db.tblMembers.SingleOrDefault(x => x.username == insert.sid);
            if (sid.status == "Paid")
            {
                decimal credit = 0;
                if (insert.plan.Contains("Prime Membership"))
                {
                    //if (insert.plan.Contains("Super"))
                    //{
                    //    if (sid.plan.Contains("Super") || sid.isUpgradedSuper == true)
                    //    {
                    //        sid.boosterPoints += 100;
                    //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                    //    }
                    //}
                    //else
                    //{
                    //    if (!sid.plan.Contains("Super") || sid.isUpgradedSuper == true)
                    //    {
                    //        sid.boosterPoints += 100;
                    //        payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points");
                    //    }
                    //}

                    // Booster Points to Sponsorer
                    sid.boosterPoints += 100;
                    payout(100, sid.username, insert.username, "Referral Bonus", "Booster Points",request.summary);

                    credit = 100;
                }
                else if (insert.plan == "Super Membership")
                {
                    sid.boosterPoints += 50;
                    payout(50, sid.username, insert.username, "Referral Bonus", "Booster Points", request.summary);

                    credit = 100;
                }
                else if (insert.plan == "Platinum Membership")
                {
                    credit = 500;

                }
                if (credit > 0)
                {
                    sid.credit += (credit * 80 / 100);
                    sid.RepurchaseWallet += (credit * 5 / 100);
                    payout(credit, sid.username, insert.username, "Referral Bonus", "GM Wallet", request.summary);

                }
            }

            if (findPlan.product == true)
            {
                ProductRequest(insert.username, findPlan.PlanName);
            }
            int refid = (int)insert.refid;
            string leg = insert.leg;
            int i = 0;
            Decimal bvAmount = (decimal)findPlan.BVAmount;

            // franchisee Referral Commission
            if (!string.IsNullOrEmpty(insert.franchise))
            {
                decimal franchRefCommission = insert.amount.Value * 1 / 100;
                var franchisee = db.tblVendors.SingleOrDefault(x => x.Username == insert.franchise);
                franchisee.KitWallet += (franchRefCommission * 80 / 100);
                payout(franchRefCommission, franchisee.Username, request.Username, "Referral Commission", "Franchisee Income",request.summary);

                if (franchisee != null)
                {
                    decimal masterRefCommission = insert.amount.Value * 0.5m / 100;
                    var master = db.tblVendors.SingleOrDefault(x => x.Id == franchisee.ReportingId);
                    master.KitWallet += (masterRefCommission * 80 / 100);
                    payout(masterRefCommission, master.Username, request.Username, "Referral Commission", "Franchisee Income",request.summary);
                }
            }

            while (refid != 0)
            {
                i++;
                var bv = (from c in db.tblMembers
                          where c.id == refid
                          select c).SingleOrDefault();

                if (leg == "Left")
                {
                    if (bv.status == "Paid")
                    {
                        bv.lcarry = bv.lcarry + bvAmount;
                        bv.tlcount = bv.tlcount + bvAmount;
                        bv.silverLeft += 1;
                    }
                }
                if (leg == "Right")
                {
                    if (bv.status == "Paid")
                    {
                        bv.rcarry = bv.rcarry + bvAmount;
                        bv.trcount = bv.trcount + bvAmount;
                        bv.silverRight += 1;
                    }
                }
                if (bv.TourPackageEligible == false)   // 2:1 or 1:2 update  for Enable flag
                {
                    if (bv.silverLeft >= 1 && bv.silverRight >= 2)
                    {
                        bv.TourPackageEligible = true;
                    }
                    else if (bv.silverLeft >= 2 && bv.silverRight >= 1)
                    {
                        bv.TourPackageEligible = true;
                    }
                }
                db.SubmitChanges();
                AddBv(bv.username, bvAmount, insert.renewal.Value.Date, leg, insert.username, "Binary");

                refid = (int)bv.refid;
                leg = bv.leg;
            }
           
            var smsService = new SendSms();
            string msg = "Dear " + insert.name + ", Congratulations! Your User ID:" + insert.username + " is successfully upgraded. " + ConfigurationManager.AppSettings["companyname"];
            smsService.sms(insert.mobile, msg, "1407169769926420610");

        }
      
        private void ProductRequest(string uname, string plan)
        {
            RequestProduct requestProduct = new RequestProduct()
            {
                username = uname,
                status = "Pending",
                date = DateTime.UtcNow.AddHours(5.5),
                name = plan
            };

            db.RequestProducts.InsertOnSubmit(requestProduct);
            db.SubmitChanges();
        }

        [HttpGet]
        [Route("GetLevelDetails")]
        public HttpResponseMessage GetLevelDetails(string username)
        {
            HttpResponseMessage response;
            try
            {
                List<tblMember> membersBySid = GetMembersBySid(username);
                if (membersBySid != null && membersBySid.Any())
                {
                    List<AccountController.AutoPoolTree> autoPoolTreeList = new List<AccountController.AutoPoolTree>();

                    for (int i = 1; i <= 15; i++)
                    {
                        var membersAtLevel = membersBySid.Where(x => x.level == i);
                        int activeCount = membersAtLevel.Count(x => x.status == "Paid");
                        int inactiveCount = membersAtLevel.Count(x => x.status == "Free");

                        autoPoolTreeList.Add(new AccountController.AutoPoolTree
                        {
                            level = i,
                            count = (activeCount + inactiveCount).ToString(),
                            active = activeCount.ToString(),
                            inactive = inactiveCount.ToString()
                        });
                    }

                    response = Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = autoPoolTreeList
                    });
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "Account not found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception details (if logging is set up)
                Console.WriteLine(ex);

                response = Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Failed",
                    message = $"Internal Server Error: {ex.Message}"
                });
            }
            return response;
        }

        public List<tblMember> GetMembersBySid(string sid)
        {
            List<tblMember> members = new List<tblMember>();
            GetMembersRecursive(sid, 1, members);
            return members;
        }

        private void GetMembersRecursive(string username, int level, List<tblMember> members)
        {
            tblMember member = db.tblMembers.FirstOrDefault(x => x.username == username);
            if (member == null)
                return;

            member.level = level;
            members.Add(member);

            var childMembers = db.tblMembers.Where(x => x.refid == member.id).Select(x => x.username).ToList();
            foreach (string childUsername in childMembers)
            {
                GetMembersRecursive(childUsername, level + 1, members);
            }
        }
        public class AutoPoolTree
        {
            public int level { get; set; }

            public string count { get; set; }

            public string active { get; set; }

            public string inactive { get; set; }

            public string amount { get; set; }

            public string color { get; set; }
        }


        private string GenerateAutoGeneratedReferenceId()
        {
            // Concatenate current timestamp with a random number
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var random = new Random().Next(1000, 9999).ToString(); // Adjust range as needed

            return timestamp + random;
        }
        private string GenerateAutoGeneratedReferenceId1(string user)
        {
            // Concatenate current timestamp with a random number
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var random = new Random().Next(1000, 9999).ToString(); // Adjust range as needed

            return user + "G" + timestamp + random;
        }

        [HttpGet]
        [Route("GetPaySprintToken")]
        public async Task<HttpResponseMessage> GetPaysprintToken()
        {

            try
            {
                string key = JWTtoken;

                //string key = "UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=";   
                var referenceId = GenerateAutoGeneratedReferenceId();


                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


                var header = new JwtHeader(credentials);


                var payload = new JwtPayload
                {
                {"timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds()},
                {"partnerId",PartnerID }, //PS001624  //PS005115
                { "reqid",referenceId},
                 };

                //
                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();


                var tokenString = handler.WriteToken(secToken);

                var response = Request.CreateResponse(HttpStatusCode.NotFound);
                response.Content = new ObjectContent<object>(new
                {
                    statusCode = HttpStatusCode.OK,
                    statusMessage = "success",
                    data = tokenString
                }, new JsonMediaTypeFormatter());

                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"status\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }

        }

        [HttpGet]
        [Route("getoperator")]
        public async Task<HttpResponseMessage> getoperator()
        {
            try
            {
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/recharge/getoperator");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                var response = await client.ExecutePostAsync(request);


                //Console.WriteLine("{0}", response.Content);


                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("hlrcheck")]
        public async Task<HttpResponseMessage> hlrcheck(PaySprintMobileModel model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/recharge/hlrapi/hlrcheck
                //local = https://sit.paysprint.in/service-api/api/v1/service/recharge/hlrapi/hlrcheck
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/hlrapi/hlrcheck");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"number\":\"" + model.number + "\",\"type\":\"" + model.type + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }


        [HttpPost]
        [Route("dthinfo")]
        public async Task<HttpResponseMessage> dthinfo(PaySprintDTHModel model)
        {
            try
            {
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/recharge/hlrapi/dthinfo
                //local = https://sit.paysprint.in/service-api/api/v1/service/recharge/hlrapi/dthinfo
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/hlrapi/dthinfo");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"canumber\":\"" + model.cannumber + "\",\"op\":\"" + model.op + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }


        [HttpPost]
        [Route("browseplan")]
        public async Task<HttpResponseMessage> browseplan(PaySprintBrowsePlanModel model)
        {
            try
            {
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                string circle = model.circle;

                if (circle == "Andhra Pradesh")
                {
                    circle = "Andhra Pradesh Telangana";
                }
                else if (circle == "Maharashtra and Goa")
                {
                    circle = "Maharashtra Goa";
                }
                else if (circle == "Jammu and Kashmir")
                {
                    circle = "Jammu Kashmir";
                }
                //else if (model.circle == "")
                //{
                //    circle = "Bihar Jharkhand";
                //}

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/hlrapi/browseplan");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"circle\":\"" + circle + "\",\"op\":\"" + model.op + "\"}";
                // request.AddJsonBody(jsonBody, false);
                //var response = await client.PostAsync(request);
                request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);


                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }


        [HttpGet]
        [Route("getoperatorbill")]
        public async Task<HttpResponseMessage> getoperatorbill(string mode)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/recharge/hlrapi/hlrcheck
                //local = https://sit.paysprint.in/service-api/api/v1/service/bill-payment/bill/getoperator
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/getoperator");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                // request.AddJsonBody("{\"mode\":\"online/offline\"}", false);
                string jsonBody = "{\"mode\":\"" + mode + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }


        [HttpPost]
        [Route("fetchbill")]
        public async Task<HttpResponseMessage> fetchbill(FetchBillModel model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/bill-payment/bill/fetchbill
                //local =https://sit.paysprint.in/service-api/api/v1/service/bill-payment/bill/fetchbill
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/fetchbill");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                // request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                // string jsonBody = $"{{\"operator\":\"{model.Operator}\",\"canumber\":\"{model.cannumber}\",\"mode\":\"{model.mode}\"}}";
                string jsonBody = $"{{\"operator\":\"{model.Operator}\",\"canumber\":\"{model.cannumber}\",\"mode\":\"{model.mode}\",\"ad1\":\"{model.ad1}\",\"ad2\":\"{model.ad2}\"}}";

                request.AddJsonBody(jsonBody, false);


                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpGet]
        [Route("fastagoperatorsList")]
        public async Task<HttpResponseMessage> fastagoperatorsList()
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/fastag/Fastag/operatorsList
                //local =https://sit.paysprint.in/api/v1/service/fastag/Fastag/operatorsList
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/fastag/Fastag/operatorsList");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                // request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("fetchConsumerDetails")]
        public async Task<HttpResponseMessage> fetchConsumerDetails(FetchConsumerDetails model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/v1/service/fastag/Fastag/fetchConsumerDetails
                //local =https://sit.paysprint.in/service-api/api/v1/service/fastag/Fastag/fetchConsumerDetails
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/fastag/Fastag/fetchConsumerDetails");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                // request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);


                string jsonBody = "{\"operator\":\"" + model.Operator + "\",\"canumber\":\"" + model.cannumber + "\"}";

                request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);


                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpGet]
        [Route("getoperatorlpg")]
        public async Task<HttpResponseMessage> getoperatorlpg(string mode)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // key Live =UFMwMDUxMTU5YTlmMDcxYWFlODA0MTY1NDEyZjRkOGI2MzI2ZTRhYTE3MTA3Njk2MDk=
                //key Local = UFMwMDE2MjQzNWJkNzEyYzBhZWNiYmEyOTI4ZGI5Yjc4YmY2ZjEwNA==

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                // live = PS005115
                //local = PS001624

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                // live = https://api.paysprint.in/api/api/v1/service/bill-payment/lpg/getoperator
                //local = https://sit.paysprint.in/service-api/api/v1/service/bill-payment/lpg/getoperator
                // Auth Live =YzZiZjgzODAzMGIzODAwNTRmNDRhZDgxMjkxMjUxMGM=
                // Auth Local =OTY2YzVkNmQwOTY3YjBiMTc0OWFhMTNkMDkwMmU5ZTI=

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/lpg/getoperator");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                // request.AddJsonBody("{\"mode\":\"online/offline\"}", false);
                string jsonBody = "{\"mode\":\"" + mode + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("fetchbilllpg")]
        public async Task<HttpResponseMessage> fetchbilllpg(FetchConsumerDetails model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };


                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/lpg/fetchbill");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                // request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                string latitude = "28.65521";
                string longitude = "77.14343";

                if (model.ad1 == 1)
                {
                    string jsonBody = "{\"operator\":\"" + model.Operator + "\",\"canumber\":\"" + model.cannumber + "\",\"ad1\":\"" + model.ad1 + "\",\"ad2\":\"" + model.ad2 + "\",\"ad3\":\"" + model.ad3 + "\",\"ad4\":\"" + model.ad4 + "\",\"referenceid\":\"" + referenceId + "\",\"latitude\":\"" + latitude + "\",\"longitude\":\"" + longitude + "\"}";
                    request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);
                }
                if (model.ad1 == 2)
                {
                    string jsonBody = "{\"operator\":\"" + model.Operator + "\",\"canumber\":\"" + model.cannumber + "\",\"ad1\":\"" + model.ad1 + "\",\"ad2\":\"" + model.ad2 + "\",\"referenceid\":\"" + referenceId + "\",\"latitude\":\"" + latitude + "\",\"longitude\":\"" + longitude + "\"}";
                    request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);

                }
                if (model.ad1 == 3)
                {
                    string jsonBody = "{\"operator\":\"" + model.Operator + "\",\"canumber\":\"" + model.cannumber + "\",\"ad1\":\"" + model.ad1 + "\",\"ad2\":\"" + model.ad2 + "\",\"referenceid\":\"" + referenceId + "\",\"latitude\":\"" + latitude + "\",\"longitude\":\"" + longitude + "\"}";
                    request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);

                }
                else
                {
                    string jsonBody = "{\"operator\":\"" + model.Operator + "\",\"canumber\":\"" + model.cannumber + "\"}";
                    request.AddParameter("application/json", jsonBody, RestSharp.ParameterType.RequestBody);

                }

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }


        [HttpPost]
        [Route("payRecharge")]
        public async Task<HttpResponseMessage> payRecharge(RechargeRequestModel model)
        {
            var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);
            decimal initialBalance = user.selfPurchaseWallet;

            try
            {
               // var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);
                if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)
                {

                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Invalid authorization",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                    };
                }

                var log = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Initiated"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (model.amount < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Insert valid amount\"}", Encoding.UTF8, "application/json")
                    };
                }
                if (model.paymentMode == "Fund_wallet")
                {
                    if (model.amount > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }

                    if (user.selfPurchaseWallet >= model.amount)
                    {
                        //decimal initialBalance = user.selfPurchaseWallet;

                        user.selfPurchaseWallet -= model.amount;

                        db.SubmitChanges();

                        var fund = new FundWallet
                        {
                            username = model.userId,
                            desc = "Recharge",
                            credit = 0, // 
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = model.amount,
                            remarks = "Towards Recharge",
                            summury = model.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();


                        var logdebit = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = model.serviceName + " " + model.paymentMode,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();


                        string key = JWTtoken;


                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);
                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid",referenceId },
                };

                        // Create JWT token
                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();


                        var tokenString = handler.WriteToken(secToken);


                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/recharge/dorecharge");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest("");
                        request.AddHeader("accept", "text/plain");
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);

                        string jsonBody = $"{{\"operator\":{model.Operator},\"canumber\":{model.cannumber},\"amount\":{model.amount},\"referenceid\":\"{referenceId}\"}}";

                        request.AddJsonBody(jsonBody, false);


                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");

                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {
                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Mobile Recharge-from Paysprint-Fundwallet",
                                status = "Success",
                                message = referenceId
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            var secondApiResponse = await CallSecondAPI(model, referenceId, paysprintResponse);

                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Recharge is Succesfull",
                                reward = PublicArray
                            };

                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "OK"
                            };
                        }
                        else
                        {

                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Mobile Recharge-from Paysprint-Fundwallet",
                                status = "Failed",
                                message=paysprintResponse.message

                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            decimal selfPurchaseWallet = user.selfPurchaseWallet;

                            if (initialBalance != selfPurchaseWallet)
                            {
                                Decimal refundAmount = initialBalance - selfPurchaseWallet;
                             
                                if(refundAmount == model.amount)
                                {
                                    user.selfPurchaseWallet += refundAmount;
                                    db.SubmitChanges();

                                    var refundTransaction = new FundWallet
                                    {
                                        username = model.userId,
                                        desc = model.serviceName,
                                        credit = refundAmount,
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Refund",
                                        debit = 0,
                                        remarks = "Towards Recharge",
                                        summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                                    };
                                    db.FundWallets.InsertOnSubmit(refundTransaction);
                                    db.SubmitChanges();

                                    var logTransaction = new tblpaysprintLog
                                    {
                                        username = model.userId,
                                        amount = refundAmount,
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                        status = "Failed & Refunded",
                                        message = paysprintResponse.message
                                    };
                                    db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                                    db.SubmitChanges();
                                }                            
                               
                            }

                            // Log transaction in tblpaysprintLog table

                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Refund Processed",
                            };
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(response.StatusCode)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = response.StatusDescription,

                            };
                        }

                    }
                }
                else if (model.paymentMode == "razor_pay")
                {
                    string key = JWTtoken;

                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);
                    var header = new JwtHeader(credentials);
                    var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid",referenceId },
                };

                    // Create JWT token
                    var secToken = new JwtSecurityToken(header, payload);
                    var handler = new JwtSecurityTokenHandler();


                    var tokenString = handler.WriteToken(secToken);


                    var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/recharge/recharge/dorecharge");
                    var client = new RestSharp.RestClient(options);
                    var request = new RestSharp.RestRequest("");
                    request.AddHeader("accept", "text/plain");
                    request.AddHeader("Token", tokenString);
                    request.AddHeader("AuthorisedKey", AuthKey);

                    string jsonBody = $"{{\"operator\":{model.Operator},\"canumber\":{model.cannumber},\"amount\":{model.amount},\"referenceid\":\"{referenceId}\"}}";

                    request.AddJsonBody(jsonBody, false);


                    var response = await client.ExecutePostAsync(request);
                    // Log response status and content
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {response.Content}");

                    var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Mobile Recharge-from Paysprint-RazorPay",
                            status = "Success",
                            message = referenceId
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var secondApiResponse = await CallSecondAPI(model, referenceId, paysprintResponse);

                        var responseObject = new
                        {
                            statusCode = 200,
                            statusMessage = "success",
                            message = "Recharge is Succesfull",
                            reward = PublicArray
                        };

                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = "OK"
                        };
                    }
                    else
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Mobile Recharge-from Paysprint-RazorPay",
                            status = "Failed",
                            message = paysprintResponse.message
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var responseObject = new
                        {
                            statusCode = 400,
                            statusMessage = "Failed",
                            message = paysprintResponse.message + ".Debited Amount Will be Refunded Shortly !"
                        };
                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = response.StatusDescription,

                        };
                    }

                }

                var log2 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log2);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Bad Request",
                    Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
                };

            }
            catch (Exception ex)
            {
                var log3 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ex.Message +"due to Exception",
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log3);
                db.SubmitChanges();

                decimal selfPurchaseWallet = user.selfPurchaseWallet;
                 
                  if (model.paymentMode == "Fund_wallet")
                  {
                        if (initialBalance != selfPurchaseWallet)
                        {
                            Decimal refundAmount = initialBalance - selfPurchaseWallet;

                            if (refundAmount == model.amount)
                            {
                                user.selfPurchaseWallet += refundAmount;
                                db.SubmitChanges();

                                var refundTransaction = new FundWallet
                                {
                                    username = model.userId,
                                    desc = model.serviceName,
                                    credit = refundAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Towards Recharge",
                                    summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                                };
                                db.FundWallets.InsertOnSubmit(refundTransaction);
                                db.SubmitChanges();

                                var logTransaction = new tblpaysprintLog
                                {
                                    username = model.userId,
                                    amount = refundAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                    status = "Failed & Refunded"
                                };
                                db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                                db.SubmitChanges();
                            }

                        }
                  }

                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }

        }
        private async Task<HttpResponseMessage> CallSecondAPI(RechargeRequestModel model, string referenceId, dynamic response)
        {

            try
            {
                bool status = response.status;
                int responseCode = response.response_code;
                string operatorId = response.operatorid;
                int ackno = response.ackno;
                string refid = response.refid;
                string message = response.message;
                //transaction 
                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (model.DiscountType == "booster_points")
                {
                    if (discountMaster.booster != null)
                    {
                        // Convert rew_1 to decimal
                        decimal boostervalue;
                        if (decimal.TryParse(discountMaster.booster.ToString(), out boostervalue))
                        {

                            // Calculate the percentage to add based on the value fetched from the database
                            decimal boosterPointsAmount = model.amount * (boostervalue / 100);
                            // Use percentageToAdd as needed
                            if (user.boosterPoints >= Math.Abs(boosterPointsAmount))
                            {
                                // If user has enough booster points, deduct the full amount from boosterPoints
                                user.boosterPoints -= Math.Abs(boosterPointsAmount);
                                user.selfPurchaseWallet += Math.Abs(boosterPointsAmount);
                            }
                            else
                            {

                                decimal remainingAmount = Math.Abs(boosterPointsAmount - (decimal)user.boosterPoints);
                                user.selfPurchaseWallet += Math.Abs(boosterPointsAmount) - remainingAmount;
                                boosterPointsAmount -= remainingAmount;
                                user.boosterPoints = 0; // Deduct all available booster points

                            }
                            if (boosterPointsAmount > 0)
                            {
                                var payout = new Payout
                                {
                                    username = model.userId,
                                    desc = "Recharge",
                                    credit = 0, // Assuming boosterPointsAmount should be positive
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = boosterPointsAmount,
                                    remarks = "Booster Points",
                                    summury = model.summary
                                };
                                db.Payouts.InsertOnSubmit(payout);
                                db.SubmitChanges();

                                var fund1 = new FundWallet
                                {
                                    username = model.userId,
                                    desc = "Recharge",
                                    credit = boosterPointsAmount, // Assuming boosterPointsAmount should be positive
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = 0,
                                    remarks = "Cashback",
                                    summury = model.summary
                                };
                                db.FundWallets.InsertOnSubmit(fund1);
                                db.SubmitChanges();

                                PublicArray = new object[]
                         {
                                        new
                                        {
                                             label="You got a Cashback Credit of ",
                                            value=boosterPointsAmount
                                        }
                         };


                            }
                        }
                    }

                }
                else if (model.DiscountType == "reward_points")
                {
                    DateTime date = DateTime.UtcNow.AddHours(5.5);

                    var operatordetail = new OperatorDetails();


                    if (model.Operator == (int)OperatorDetails.vodafone || model.Operator == (int)OperatorDetails.bsnl || model.Operator == (int)OperatorDetails.idea || model.Operator == (int)OperatorDetails.mtnl || model.Operator == (int)OperatorDetails.mtnl_delhi || model.Operator == (int)OperatorDetails.mtnl_mumbai || model.Operator == (int)OperatorDetails.airtel_digital_tv || model.Operator == (int)OperatorDetails.dish_tv || model.Operator == (int)OperatorDetails.sun_direct || model.Operator == (int)OperatorDetails.videocon_D2H)
                    {
                        if (discountMaster.rew_1 != null)
                        {
                            decimal rew_1_decimal;
                            if (decimal.TryParse(discountMaster.rew_1.ToString(), out rew_1_decimal))
                            {

                                decimal percentageToAdd = model.amount * (rew_1_decimal / 100);

                                if (user.rewardPoints >= percentageToAdd)
                                {

                                    user.rewardPoints -= percentageToAdd;

                                    user.selfPurchaseWallet += percentageToAdd;

                                }
                                else
                                {
                                    decimal remainingAmount = Math.Abs(percentageToAdd - (decimal)user.rewardPoints);
                                    user.selfPurchaseWallet += Math.Abs(percentageToAdd) - remainingAmount;
                                    percentageToAdd -= remainingAmount;
                                    user.rewardPoints = 0; // Deduct a
                                }
                                if (percentageToAdd > 0)
                                {
                                    // Payout1(model.userID, rewardCredit, "Recharge Income", "self purchase", user.username, DateTime.UtcNow, 0);

                                    var payout = new Payout
                                    {
                                        username = model.userId,
                                        desc = "Recharge",
                                        credit = 0, // Assuming boosterPointsAmount should be positive
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Send",
                                        debit = percentageToAdd,
                                        remarks = "Reward Points",
                                        summury = model.summary
                                    };
                                    db.Payouts.InsertOnSubmit(payout);
                                    db.SubmitChanges();

                                    var fund1 = new FundWallet
                                    {
                                        username = model.userId,
                                        desc = "Recharge",
                                        credit = percentageToAdd, // Assuming boosterPointsAmount should be positive
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Send",
                                        debit = 0,
                                        remarks = "Cashback",
                                        summury = model.summary
                                    };
                                    db.FundWallets.InsertOnSubmit(fund1);
                                    db.SubmitChanges();

                                    PublicArray = new object[]
                       {
                                new
                                {
                                   label="You got a Cashback Points Credit of ",
                                    value=percentageToAdd
                                }
                       };

                                    //cashback = "You a Got a Reward Points Credit of ₹ " + percentageToAdd;
                                }

                            }
                            db.SubmitChanges();
                        }

                    }
                    else if (model.Operator == (int)OperatorDetails.airtel || model.Operator == (int)OperatorDetails.jio || model.Operator == (int)OperatorDetails.tata_sky)
                    {
                        if (discountMaster.rew_2 != null)
                        {

                            decimal rew_2_decimal;
                            if (decimal.TryParse(discountMaster.rew_2.ToString(), out rew_2_decimal))
                            {

                                decimal percentageToAdd = model.amount * (rew_2_decimal / 100);

                                if (user.rewardPoints >= percentageToAdd)
                                {

                                    user.rewardPoints -= percentageToAdd;

                                    user.selfPurchaseWallet += percentageToAdd;

                                }
                                else
                                {
                                    decimal remainingAmount = Math.Abs(percentageToAdd - (decimal)user.rewardPoints);
                                    user.selfPurchaseWallet += Math.Abs(percentageToAdd) - remainingAmount;
                                    percentageToAdd -= remainingAmount;
                                    user.rewardPoints = 0; // Deduct a
                                }
                                if (percentageToAdd > 0)
                                {
                                    // Payout1(model.userID, rewardCredit, "Recharge Income", "self purchase", user.username, DateTime.UtcNow, 0);

                                    var payout = new Payout
                                    {
                                        username = model.userId,
                                        desc = "Recharge",
                                        credit = 0, // Assuming boosterPointsAmount should be positive
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Send",
                                        debit = percentageToAdd,
                                        remarks = "Reward Points",
                                        summury = model.summary
                                    };
                                    db.Payouts.InsertOnSubmit(payout);
                                    db.SubmitChanges();

                                    var fund1 = new FundWallet
                                    {
                                        username = model.userId,
                                        desc = "Recharge",
                                        credit = percentageToAdd, // Assuming boosterPointsAmount should be positive
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Send",
                                        debit = 0,
                                        remarks = "Cashback",
                                        summury = model.summary
                                    };
                                    db.FundWallets.InsertOnSubmit(fund1);
                                    db.SubmitChanges();

                                    PublicArray = new object[]
                       {
                                new
                                {
                                     label="You got a Cashback Points Credit of ",
                                    value=percentageToAdd
                                }
                       };

                                    //cashback = "You a Got a Reward Points Credit of ₹ " + percentageToAdd;
                                }

                            }

                            db.SubmitChanges();
                        }
                    }

                    string sponsor = user.sid;
                    int i = 0;
                    List<string> usernames = new List<string>();

                    while (sponsor != null)
                    {
                        var bv = db.tblMembers.SingleOrDefault(x => x.username == sponsor);
                        if (bv != null && bv.status == "Paid")
                        {
                            i++;
                            usernames.Add(bv.username);
                        }
                        sponsor = bv?.sid;
                        if (i == 15)
                            break;
                    }
                    if (i > 0)
                    {
                        decimal incomeShare = 0;

                        // Adjust income share percentage for Vodafone operator
                        if (model.Operator == (int)OperatorDetails.vodafone || model.Operator == (int)OperatorDetails.bsnl || model.Operator == (int)OperatorDetails.idea || model.Operator == (int)OperatorDetails.mtnl || model.Operator == (int)OperatorDetails.mtnl_delhi || model.Operator == (int)OperatorDetails.mtnl_mumbai || model.Operator == (int)OperatorDetails.airtel_digital_tv || model.Operator == (int)OperatorDetails.dish_tv || model.Operator == (int)OperatorDetails.sun_direct || model.Operator == (int)OperatorDetails.videocon_D2H)
                        {

                            incomeShare = (model.amount * 1 / 100) / 15; // 1% of repurchaseIncome distributed to 15 levels

                        }
                        else
                        {
                            // For other operators, continue with the existing distribution logic (0.5% to 15 levels)
                            incomeShare = (model.amount * 0.5m / 100) / 15; // 0.5% of repurchaseIncome distributed to 15 levels

                        }

                        foreach (var u in usernames)
                        {
                            var mem = db.tblMembers.SingleOrDefault(x => x.username == u);
                            if (mem != null)
                            {
                                // Distribute incomeShare accordingly
                                mem.RepurchaseWallet += (incomeShare * 85 / 100); // 85% of income share added to Repurchase Wallet
                                if (incomeShare > 0)
                                {
                                    Payout(u, incomeShare, "Recharge Income", "Affiliate Income", user.username, DateTime.UtcNow.AddHours(5.5), 0);
                                }

                            }
                        }

                    }

                }
                else
                {

                }
                var recharge = new tblRecharge
                {
                    userID = model.userId,
                    amount = model.amount,
                    discountType = model.DiscountType,
                    paymentMode = model.paymentMode,
                    transactionNo = model.transactionNo,
                    aknowledgeId = response.ackno,
                    referenceID = response.refid,
                    remarks = model.serviceName,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summury = response.message,
                    Operator = OperatorDetailsExtensions.GetNameFromId(model.Operator)
                };
                db.tblRecharges.InsertOnSubmit(recharge);
                db.SubmitChanges();

                var log4 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " - booster / rewards",
                    status = "Succesfull",
                    message = referenceId
                };
                db.tblpaysprintLogs.InsertOnSubmit(log4);
                db.SubmitChanges();

                var responseObject = new
                {
                    statusCode = 200,
                    statusMessage = "success",
                    message = "Second API call successful",
                    reward = PublicArray // Assuming PublicArray is a dynamic property in your class
                };

                // Serialize the response object to JSON string
                string jsonResponse = JsonConvert.SerializeObject(responseObject);
                 
                // Return the success response with JSON content
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                    ReasonPhrase = "Successful"
                };
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the second API call
                Console.WriteLine($"Error calling second API: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error",
                    Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Error calling second API\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("payBill")]
        public async Task<HttpResponseMessage> payBill(PayBillRequestModel model)
        {
            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)
                {

                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Invalid authorization",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                    };
                }

                var log = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Initiated"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (model.amount < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Insert valid amount\"}", Encoding.UTF8, "application/json")
                    };
                }
                if (model.paymentMode == "Fund_wallet")
                {
                    if (model.amount > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }
                    if (user.selfPurchaseWallet >= model.amount)
                    {
                        decimal initialBalance = user.selfPurchaseWallet;
                        user.selfPurchaseWallet -= model.amount;

                        var fund = new FundWallet
                        {
                            username = model.userId,
                            desc = "Recharge",
                            credit = 0, // 
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = model.amount,
                            remarks = "Towards Recharge",
                            summury = model.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();

                        var logdebit = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = model.serviceName + " " + model.paymentMode,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();

                        string key = JWTtoken;
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                        {
                            { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                            { "partnerId",PartnerID },
                            { "reqid", referenceId },
                        };

                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenString = handler.WriteToken(secToken);

                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/paybill");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest("");
                        request.AddHeader("accept", "text/plain");
                        // request.AddHeader("User-Agent", PartnerID);
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);
                        string billFetchJson = JsonConvert.SerializeObject(model.bill_fetch);
                        string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"mode\": \"{model.mode}\", \"bill_fetch\": {billFetchJson}}}";

                        request.AddJsonBody(jsonString);

                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");
                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {
                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Bill Payment-from Paysprint-Fundwallet",
                                status = "Success",
                                message = referenceId
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            var secondApiResponse = await CallpaybillDiscount(model, referenceId, paysprintResponse);

                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Recharge is Succesfull",
                                reward = PublicArray
                            };

                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "OK"
                            };
                        }
                        else
                        {

                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Bill Payment-from Paysprint-Fundwallet",
                                status = "Failed",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            decimal selfPurchaseWallet = user.selfPurchaseWallet;

                            if (initialBalance != selfPurchaseWallet)
                            {
                                Decimal refundAmount = initialBalance - selfPurchaseWallet;
                                user.selfPurchaseWallet += refundAmount;
                                db.SubmitChanges();

                                // Log refund transaction in FundWallet table
                                var refundTransaction = new FundWallet
                                {
                                    username = model.userId,
                                    desc = model.serviceName,
                                    credit = refundAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Towards Recharge",
                                    summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                                };
                                db.FundWallets.InsertOnSubmit(refundTransaction);
                                db.SubmitChanges();
                            }

                            // Log transaction in tblpaysprintLog table
                            var logTransaction = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = (decimal)model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                status = "Failed & Refunded",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                            db.SubmitChanges();

                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Refund Processed",
                            };
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(response.StatusCode)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = response.StatusDescription,

                            };
                        }
                    }


                }
                else if (model.paymentMode == "razor_pay")
                {
                    string key = JWTtoken;
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                    var header = new JwtHeader(credentials);
                    var payload = new JwtPayload
                    {
                        { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                        { "partnerId",PartnerID },
                        { "reqid", referenceId },
                    };

                    var secToken = new JwtSecurityToken(header, payload);
                    var handler = new JwtSecurityTokenHandler();
                    var tokenString = handler.WriteToken(secToken);

                    var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/paybill");
                    var client = new RestSharp.RestClient(options);
                    var request = new RestSharp.RestRequest("");
                    request.AddHeader("accept", "text/plain");
                    // request.AddHeader("User-Agent", PartnerID);
                    request.AddHeader("Token", tokenString);
                    request.AddHeader("AuthorisedKey", AuthKey);
                    string billFetchJson = JsonConvert.SerializeObject(model.bill_fetch);
                    string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"mode\": \"{model.mode}\", \"bill_fetch\": {billFetchJson}}}";

                    request.AddJsonBody(jsonString);

                    var response = await client.ExecutePostAsync(request);
                    // Log response status and content
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {response.Content}");

                    var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Bill Payment-from Paysprint-RazorPay",
                            status = "Success",
                            message = referenceId
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var secondApiResponse = await CallpaybillDiscount(model, referenceId, paysprintResponse);

                        var responseObject = new
                        {
                            statusCode = 200,
                            statusMessage = "success",
                            message = "Recharge is Succesfull",
                            reward = PublicArray
                        };

                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = "OK"
                        };
                    }
                    else
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Bill Payment-from Paysprint-RazorPay",
                            status = "Failed",
                            message = paysprintResponse.message
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var responseObject = new
                        {
                            statusCode = 400,
                            statusMessage = "Failed",
                            message = paysprintResponse.message + ".Debited Amount Will be Refunded Shortly !"
                        };
                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = response.StatusDescription,

                        };
                    }

                }

                var log2 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log2);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Bad Request",
                    Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
                };

            }
            catch (Exception ex)
            {
                var log3 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary =  ex.Message+"Bill Payment - due to Exception",
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log3);
                db.SubmitChanges();

                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }
        private async Task<HttpResponseMessage> CallpaybillDiscount(AccountsModels.PayBillRequestModel model, string referenceId, dynamic response)
        {
            try
            {
                bool status = response.status;
                int responseCode = response.response_code;
                string operatorId = response.operatorid;
                int ackno = response.ackno;
                string refid = response.refid;
                string message = response.message;

                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var member = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (discountMaster != null && member != null)
                {
                    if (Decimal.TryParse(discountMaster.rew_1.ToString(), out Decimal rewardPercentage))
                    {
                        Decimal rewardAmount = model.amount * (rewardPercentage / 100M);
                        member.rewardPoints = member.rewardPoints.HasValue ? member.rewardPoints.Value + rewardAmount : rewardAmount;
                        db.SubmitChanges();

                        if (rewardAmount > 0M)
                        {
                            var payout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = rewardAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Reward Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(payout);
                            db.SubmitChanges();
                            cashRPC = rewardAmount;
                        }
                    }

                    if (model.DiscountType == "booster_points" && discountMaster.booster.HasValue && Decimal.TryParse(discountMaster.booster.ToString(), out Decimal boosterPercentage))
                    {
                        Decimal boosterAmount = model.amount * (boosterPercentage / 100M);
                        Decimal availableBoosterPoints = member.boosterPoints.GetValueOrDefault();

                        if (availableBoosterPoints >= Math.Abs(boosterAmount))
                        {
                            member.boosterPoints -= Math.Abs(boosterAmount);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount);
                        }
                        else
                        {
                            Decimal remainingAmount = Math.Abs(boosterAmount - availableBoosterPoints);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount) - remainingAmount;
                            member.boosterPoints = 0M;
                        }

                        db.SubmitChanges();

                        if (boosterAmount > 0M)
                        {
                            var boosterPayout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = 0M,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = boosterAmount,
                                remarks = "Booster Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(boosterPayout);

                            var fundWallet = new FundWallet
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = boosterAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Cashback",
                                summury = model.summary
                            };
                            db.FundWallets.InsertOnSubmit(fundWallet);

                            db.SubmitChanges();
                            cashBooster = boosterAmount;
                        }
                    }

                    PublicArray = string.IsNullOrEmpty(model.DiscountType) ?
                        new object[]
                        {
                    new { label = "You a Got a Reward Points Credit of ", value = cashRPC }
                        } :
                        new object[]
                        {
                    new { label = "You got a Reward Points Credit of ", value = cashRPC },
                    new { label = "You got a Cashback Credit of ", value = cashBooster }
                        };

                    var recharge = new tblRecharge
                    {
                        userID = model.userId,
                        amount = model.amount,
                        discountType = model.DiscountType,
                        paymentMode = model.paymentMode,
                        transactionNo = referenceId,
                        aknowledgeId = ackno.ToString(),
                        referenceID = operatorId,
                        remarks = model.serviceName,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summury = model.summary,
                        Operator = model.serviceName
                    };
                    db.tblRecharges.InsertOnSubmit(recharge);
                    db.SubmitChanges();

                    var paysprintLog = new tblpaysprintLog
                    {
                        username = model.userId,
                        amount = model.amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = $"{model.serviceName} booster / rewards",
                        status = "Succesfull",
                        message = referenceId
                    };
                    db.tblpaysprintLogs.InsertOnSubmit(paysprintLog);
                    db.SubmitChanges();

                    var content = JsonConvert.SerializeObject(new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = "Second API call successful",
                        reward = PublicArray
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = "Invalid User or Discount Service"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling second API: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error",
                    Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"data\": \"Error calling second API\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("payTag")]
        public async Task<HttpResponseMessage> payTag(FastagRequestModel model)
        {
            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);
                if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)
                {

                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Invalid authorization",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                    };
                }

                var log = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Initiated"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (model.amount < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Insert valid amount\"}", Encoding.UTF8, "application/json")
                    };
                }
                if (model.paymentMode == "Fund_wallet")
                {
                    if (model.amount > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }

                    if (user.selfPurchaseWallet >= model.amount)
                    {
                        decimal initialBalance = user.selfPurchaseWallet;

                        user.selfPurchaseWallet -= model.amount;

                        var fund = new FundWallet
                        {
                            username = model.userId,
                            desc = "Recharge",
                            credit = 0, // 
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = model.amount,
                            remarks = "Towards Recharge",
                            summury = model.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();

                        var logdebit = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = model.serviceName + " " + model.paymentMode,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();

                        string key = JWTtoken;
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                        {
                            { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                            { "partnerId",PartnerID },
                            { "reqid", referenceId },
                        };

                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenString = handler.WriteToken(secToken);

                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/fastag/Fastag/recharge");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest("");
                        request.AddHeader("accept", "text/plain");
                        // request.AddHeader("User-Agent", PartnerID);
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);
                        string billFetchJson = JsonConvert.SerializeObject(model.bill_fetch);
                        // string jsonBody = $"{{\"operator\":\"{model.Operator}\",\"canumber\":\"{model.cannumber}\",\"mode\":\"{model.mode}\"}}";
                        string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"bill_fetch\": {billFetchJson}}}";


                        request.AddJsonBody(jsonString);


                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");

                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {
                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Fastag Payment-from Paysprint-Fundwallet",
                                status = "Success",
                                message = referenceId
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            var secondApiResponse = await CallFastagDiscount(model, referenceId, paysprintResponse);

                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Recharge is Succesfull",
                                reward = PublicArray
                            };

                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "OK"
                            };
                        }
                        else
                        {

                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Fastag Payment-from Paysprint-Fundwallet",
                                status = "Failed",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            decimal selfPurchaseWallet = user.selfPurchaseWallet;

                            if (initialBalance != selfPurchaseWallet)
                            {
                                Decimal refundAmount = initialBalance - selfPurchaseWallet;
                                user.selfPurchaseWallet += refundAmount;
                                db.SubmitChanges();

                                // Log refund transaction in FundWallet table
                                var refundTransaction = new FundWallet
                                {
                                    username = model.userId,
                                    desc = model.serviceName,
                                    credit = refundAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Towards Recharge",
                                    summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                                };
                                db.FundWallets.InsertOnSubmit(refundTransaction);
                                db.SubmitChanges();
                            }

                            // Log transaction in tblpaysprintLog table
                            var logTransaction = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = (decimal)model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                status = "Failed & Refunded",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                            db.SubmitChanges();

                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Refund Processed",
                            };
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(response.StatusCode)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = response.StatusDescription,

                            };
                        }

                    }
                }
                else if (model.paymentMode == "razor_pay")
                {
                    string key = JWTtoken;
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                    var header = new JwtHeader(credentials);
                    var payload = new JwtPayload
                    {
                        { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                        { "partnerId",PartnerID },
                        { "reqid", referenceId },
                    };

                 
                    var secToken = new JwtSecurityToken(header, payload);
                    var handler = new JwtSecurityTokenHandler();
                    var tokenString = handler.WriteToken(secToken);

                   
                    var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/fastag/Fastag/recharge");
                    var client = new RestSharp.RestClient(options);
                    var request = new RestSharp.RestRequest("");
                    request.AddHeader("accept", "text/plain");
                    // request.AddHeader("User-Agent", PartnerID);
                    request.AddHeader("Token", tokenString);
                    request.AddHeader("AuthorisedKey", AuthKey);
                    string billFetchJson = JsonConvert.SerializeObject(model.bill_fetch);
                    string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"bill_fetch\": {billFetchJson}}}";

                    request.AddJsonBody(jsonString);

                    var response = await client.ExecutePostAsync(request);
                    // Log response status and content
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {response.Content}");

                    var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Fastag Payment-from Paysprint-RazorPay",
                            status = "Success",
                            message = referenceId
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var secondApiResponse = await CallFastagDiscount(model, referenceId, paysprintResponse);

                        var responseObject = new
                        {
                            statusCode = 200,
                            statusMessage = "success",
                            message = "Recharge is Succesfull",
                            reward = PublicArray
                        };

                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = "OK"
                        };
                    }
                    else
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Fastag Payment-from Paysprint-RazorPay",
                            status = "Failed",
                            message = paysprintResponse.message

                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var responseObject = new
                        {
                            statusCode = 400,
                            statusMessage = "Failed",
                            message = paysprintResponse.message + ".Debited Amount Will be Refunded Shortly !"
                        };
                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = response.StatusDescription,

                        };
                    }

                }

                var log2 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log2);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Bad Request",
                    Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
                };

            }
            catch (Exception ex)
            {
                var log3 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ex.Message+"Fastag Payment - due to Exception",
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log3);
                db.SubmitChanges();

                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }

        }
        private async Task<HttpResponseMessage> CallFastagDiscount(AccountsModels.FastagRequestModel model, string referenceId, dynamic response)
        {
            try
            {
                bool status = response.status;
                int responseCode = response.response_code;
                string operatorId = response.operatorid;
                int ackno = response.ackno;
                string refid = response.refid;
                string message = response.message;

                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var member = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (discountMaster != null && member != null)
                {
                    if (Decimal.TryParse(discountMaster.rew_1.ToString(), out Decimal rewardPercentage))
                    {
                        Decimal rewardAmount = model.amount * (rewardPercentage / 100M);
                        member.rewardPoints = member.rewardPoints.HasValue ? member.rewardPoints.Value + rewardAmount : rewardAmount;
                        db.SubmitChanges();

                        if (rewardAmount > 0M)
                        {
                            var payout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = rewardAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Reward Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(payout);
                            db.SubmitChanges();
                            cashRPC = rewardAmount;
                        }
                    }

                    if (model.DiscountType == "booster_points" && discountMaster.booster.HasValue && Decimal.TryParse(discountMaster.booster.ToString(), out Decimal boosterPercentage))
                    {
                        Decimal boosterAmount = model.amount * (boosterPercentage / 100M);
                        Decimal availableBoosterPoints = member.boosterPoints.GetValueOrDefault();

                        if (availableBoosterPoints >= Math.Abs(boosterAmount))
                        {
                            member.boosterPoints -= Math.Abs(boosterAmount);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount);
                        }
                        else
                        {
                            Decimal remainingAmount = Math.Abs(boosterAmount - availableBoosterPoints);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount) - remainingAmount;
                            member.boosterPoints = 0M;
                        }

                        db.SubmitChanges();

                        if (boosterAmount > 0M)
                        {
                            var boosterPayout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = 0M,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = boosterAmount,
                                remarks = "Booster Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(boosterPayout);

                            var fundWallet = new FundWallet
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = boosterAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Cashback",
                                summury = model.summary
                            };
                            db.FundWallets.InsertOnSubmit(fundWallet);

                            db.SubmitChanges();
                            cashBooster = boosterAmount;
                        }
                    }

                    PublicArray = string.IsNullOrEmpty(model.DiscountType) ?
                        new object[]
                        {
                    new { label = "You a Got a Reward Points Credit of ", value = cashRPC }
                        } :
                        new object[]
                        {
                    new { label = "You got a Reward Points Credit of ", value = cashRPC },
                    new { label = "You got a Cashback Credit of ", value = cashBooster }
                        };

                    var recharge = new tblRecharge
                    {
                        userID = model.userId,
                        amount = model.amount,
                        discountType = model.DiscountType,
                        paymentMode = model.paymentMode,
                        transactionNo = referenceId,
                        aknowledgeId = ackno.ToString(),
                        referenceID = operatorId,
                        remarks = model.serviceName,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summury = model.summary,
                        Operator = model.serviceName
                    };
                    db.tblRecharges.InsertOnSubmit(recharge);
                    db.SubmitChanges();

                    var paysprintLog = new tblpaysprintLog
                    {
                        username = model.userId,
                        amount = model.amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = $"{model.serviceName} booster / rewards",
                        status = "Succesfull",
                        message = referenceId
                    };
                    db.tblpaysprintLogs.InsertOnSubmit(paysprintLog);
                    db.SubmitChanges();

                    var content = JsonConvert.SerializeObject(new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = "Second API call successful",
                        reward = PublicArray
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = "Invalid User or Discount Service"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling second API: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error",
                    Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"data\": \"Error calling second API\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("payGasBill")]
        public async Task<HttpResponseMessage> payGasBill(LpgBillModel model)
        {
            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)
                {

                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Invalid authorization",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                    };
                }

                var log = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Initiated"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (model.amount < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Insert valid amount\"}", Encoding.UTF8, "application/json")
                    };
                }
                if (model.paymentMode == "Fund_wallet")
                {
                    if (model.amount > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }
                    if (user.selfPurchaseWallet >= model.amount)
                    {
                        decimal initialBalance = user.selfPurchaseWallet;
                        user.selfPurchaseWallet -= model.amount;

                        var fund = new FundWallet
                        {
                            username = model.userId,
                            desc = "Recharge",
                            credit = 0, // 
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = model.amount,
                            remarks = "Towards Recharge",
                            summury = model.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();


                        var logdebit = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = model.serviceName + " " + model.paymentMode,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();

                        string key = JWTtoken;
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                        {
                            { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                            { "partnerId",PartnerID },
                            { "reqid", referenceId },
                        };

                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenString = handler.WriteToken(secToken);

                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/lpg/paybill");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest("");
                        request.AddHeader("accept", "text/plain");
                        request.AddHeader("User-Agent", PartnerID);
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);

                            string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"ad1\":{model.ad1},\"ad2\":{model.ad2},\"ad3\":{model.ad3}}}";

                          request.AddJsonBody(jsonString, false);
                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");
                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {
                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "LPG Payment-from Paysprint-Fundwallet",
                                status = "Success",
                                message = referenceId
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            var secondApiResponse = await CallLPGDiscount(model, referenceId, paysprintResponse);

                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Gas Booking is Succesfull",
                                reward = PublicArray
                            };

                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "OK"
                            };
                        }
                        else
                        {

                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "LPG Payment-from Paysprint-Fundwallet",
                                status = "Failed",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                decimal selfPurchaseWallet = user.selfPurchaseWallet;

                if (initialBalance != selfPurchaseWallet)
                {
                    Decimal refundAmount = initialBalance - selfPurchaseWallet;
                    user.selfPurchaseWallet += refundAmount;
                    db.SubmitChanges();

                    // Log refund transaction in FundWallet table
                    var refundTransaction = new FundWallet
                    {
                        username = model.userId,
                        desc = model.serviceName,
                        credit = refundAmount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                        status = "Refund",
                        debit = 0,
                        remarks = "Towards Recharge",
                        summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                    };
                    db.FundWallets.InsertOnSubmit(refundTransaction);
                    db.SubmitChanges();
                }

                // Log transaction in tblpaysprintLog table
                var logTransaction = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = (decimal)model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                status = "Failed & Refunded",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                            db.SubmitChanges();

                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Refund Processed",
                            };
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(response.StatusCode)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = response.StatusDescription,

                            };
                        }
                    }


                }
                else if (model.paymentMode == "razor_pay")
                {
                    string key = JWTtoken;
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                    var header = new JwtHeader(credentials);
                    var payload = new JwtPayload
                    {
                        { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                        { "partnerId",PartnerID },
                        { "reqid", referenceId },
                    };

                    var secToken = new JwtSecurityToken(header, payload);
                    var handler = new JwtSecurityTokenHandler();
                    var tokenString = handler.WriteToken(secToken);

                    var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/lpg/paybill");
                    var client = new RestSharp.RestClient(options);
                    var request = new RestSharp.RestRequest("");
                    request.AddHeader("accept", "text/plain");
                    request.AddHeader("User-Agent", PartnerID);
                    request.AddHeader("Token", tokenString);
                    request.AddHeader("AuthorisedKey", AuthKey);

                    string jsonString = $"{{\"operator\": \"{model.Operator}\", \"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"ad1\":{model.ad1},\"ad2\":{model.ad2},\"ad3\":{model.ad3}}}";

                    request.AddJsonBody(jsonString, false);

                    var response = await client.ExecutePostAsync(request);
                    // Log response status and content
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {response.Content}");

                    var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                    if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "LPG Payment-from Paysprint-RazorPay",
                            status = "Success",
                            message = referenceId
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var secondApiResponse = await CallLPGDiscount(model, referenceId, paysprintResponse);

                        var responseObject = new
                        {
                            statusCode = 200,
                            statusMessage = "success",
                            message = "Gas Booking is Succesfull",
                            reward = PublicArray
                        };

                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = "OK"
                        };
                    }
                    else
                    {
                        var log1 = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "LPG Payment-from Paysprint-RazorPay",
                            status = "Failed",
                            message = paysprintResponse.message
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var responseObject = new
                        {
                            statusCode = 400,
                            statusMessage = "Failed",
                            message = paysprintResponse.message + ".Debited Amount Will be Refunded Shortly !"
                        };
                        string jsonResponse = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                            ReasonPhrase = response.StatusDescription,

                        };
                    }

                }

                var log2 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log2);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Bad Request",
                    Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
                };

            }
            catch (Exception ex)
            {
                var log3 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ex.Message + "LPG Payment- due to Exception",
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log3);
                db.SubmitChanges();

                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }
        private async Task<HttpResponseMessage> CallLPGDiscount(AccountsModels.LpgBillModel model, string referenceId, dynamic response)
        {
            try
            {
                bool status = response.status;
                int responseCode = response.response_code;
                string operatorId = response.operatorid;
                int ackno = response.ackno;
                string refid = response.refid;
                string message = response.message;

                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var member = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (discountMaster != null && member != null)
                {
                    if (Decimal.TryParse(discountMaster.rew_1.ToString(), out Decimal rewardPercentage))
                    {
                        Decimal rewardAmount = model.amount * (rewardPercentage / 100M);
                        member.rewardPoints = member.rewardPoints.HasValue ? member.rewardPoints.Value + rewardAmount : rewardAmount;
                        db.SubmitChanges();

                        if (rewardAmount > 0M)
                        {
                            var payout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = rewardAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Reward Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(payout);
                            db.SubmitChanges();
                            cashRPC = rewardAmount;
                        }
                    }

                    if (model.DiscountType == "booster_points" && discountMaster.booster.HasValue && Decimal.TryParse(discountMaster.booster.ToString(), out Decimal boosterPercentage))
                    {
                        Decimal boosterAmount = model.amount * (boosterPercentage / 100M);
                        Decimal availableBoosterPoints = member.boosterPoints.GetValueOrDefault();

                        if (availableBoosterPoints >= Math.Abs(boosterAmount))
                        {
                            member.boosterPoints -= Math.Abs(boosterAmount);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount);
                        }
                        else
                        {
                            Decimal remainingAmount = Math.Abs(boosterAmount - availableBoosterPoints);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount) - remainingAmount;
                            member.boosterPoints = 0M;
                        }

                        db.SubmitChanges();

                        if (boosterAmount > 0M)
                        {
                            var boosterPayout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = 0M,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = boosterAmount,
                                remarks = "Booster Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(boosterPayout);

                            var fundWallet = new FundWallet
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = boosterAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Cashback",
                                summury = model.summary
                            };
                            db.FundWallets.InsertOnSubmit(fundWallet);

                            db.SubmitChanges();
                            cashBooster = boosterAmount;
                        }
                    }

                    PublicArray = string.IsNullOrEmpty(model.DiscountType) ?
                        new object[]
                        {
                    new { label = "You a Got a Reward Points Credit of ", value = cashRPC }
                        } :
                        new object[]
                        {
                    new { label = "You got a Reward Points Credit of ", value = cashRPC },
                    new { label = "You got a Cashback Credit of ", value = cashBooster }
                        };

                    var recharge = new tblRecharge
                    {
                        userID = model.userId,
                        amount = model.amount,
                        discountType = model.DiscountType,
                        paymentMode = model.paymentMode,
                        transactionNo = referenceId,
                        aknowledgeId = ackno.ToString(),
                        referenceID = operatorId,
                        remarks = model.serviceName,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summury = model.summary,
                        Operator = model.serviceName
                    };
                    db.tblRecharges.InsertOnSubmit(recharge);
                    db.SubmitChanges();

                    var paysprintLog = new tblpaysprintLog
                    {
                        username = model.userId,
                        amount = model.amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = $"{model.serviceName} booster / rewards",
                        status = "Succesfull",
                        message = referenceId
                    };
                    db.tblpaysprintLogs.InsertOnSubmit(paysprintLog);
                    db.SubmitChanges();

                    var content = JsonConvert.SerializeObject(new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = "Second API call successful",
                        reward = PublicArray
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = "Invalid User or Discount Service"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling second API: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error",
                    Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"data\": \"Error calling second API\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        [HttpPost]
        [Route("CreateOrderRpay")]
        public IHttpActionResult CreateOrderRpay(AccountsModels.OrderRequestModel model)
        {
            RazorpayClient razorpayClient = new RazorpayClient("rzp_live_84qcZWqWSlAuhk", "GAtoPwXXL4Vl9sihBKTKsTNX");
            //RazorpayClient razorpayClient = new RazorpayClient("rzp_test_gpxhtt1nNUrPAX", "2lmlv3FPbN2IQOeh3cPqRXDU");

            Dictionary<string, object> data1 = new Dictionary<string, object>
            {
                { "amount", model.amount}, // Amount in paise (minor currency unit)
                { "currency", model.currency },
                { "receipt", GenerateReceiptID() }
            };

            try
            {
                Order order = razorpayClient.Order.Create(data1);
                var data2 = new
                {
                    statusCode = HttpStatusCode.OK,
                    statusMessage = "Success",
                    data = order
                };
                return Content(HttpStatusCode.OK, data2);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while creating the order: " + ex.Message);
                var errorResponse = new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "Error",
                    message = ex.Message
                };
                return Content(HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        private string GenerateReceiptID()
        {
            return $"rcptid_{new Random().Next(10000000, 99999999):D8}";
        }

        [HttpPost]
        [Route("VerifyPaymentSignatureRpay")]
        public IHttpActionResult VerifyPaymentSignatureRpay(AccountsModels.RazorpayPaymentInfo info)
        {
            try
            {
                 RazorpayClient razorpayClient = new RazorpayClient("rzp_live_84qcZWqWSlAuhk", "GAtoPwXXL4Vl9sihBKTKsTNX");
              //  RazorpayClient razorpayClient = new RazorpayClient("rzp_test_gpxhtt1nNUrPAX", "2lmlv3FPbN2IQOeh3cPqRXDU");

                Dictionary<string, string> attributes = new Dictionary<string, string>
        {
            { "razorpay_order_id", info.razorpay_order_id },
            { "razorpay_payment_id", info.razorpay_payment_id },
            { "razorpay_signature", info.razorpay_signature }
        };

                Utils.verifyPaymentSignature(attributes);

                var data = new
                {
                    statusCode = HttpStatusCode.OK,
                    statusMessage = "Success",
                    data = "Payment signature verified successfully."
                };

                return Content(HttpStatusCode.OK, data);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    statusCode = HttpStatusCode.BadRequest,
                    statusMessage = "Error",
                    message = "Failed to verify payment signature: " + ex.Message
                };

                return Content(HttpStatusCode.BadRequest, errorResponse);
            }
        }

        /* bus Ticket BOOKING */ 

        [HttpGet]
        [Route("getSourceCity")]
        public async Task<HttpResponseMessage> getSourceCity()
        {
            try
            {
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId", PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/source");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                var response = await client.ExecutePostAsync(request);


                //Console.WriteLine("{0}", response.Content);


                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("availabletrips")]
        public async Task<HttpResponseMessage> availabletrips(AvailableTrips model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/availabletrips");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"source_id\":\"" + model.source_id + "\",\"destination_id\":\"" + model.destination_id + "\",\"date_of_journey\":\"" + model.date_of_journey + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("tripdetails")]
        public async Task<HttpResponseMessage> tripdetails(TripDetalis model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/tripdetails");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"trip_id\":\"" + model.trip_id + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("boardingPoint")]
        public async Task<HttpResponseMessage> boardingPoint(TripDetalis model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/boardingPoint");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"trip_id\":\"" + model.trip_id + "\",\"bpId\":\"" + model.bpId + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("bookTicket")]
        public async Task<HttpResponseMessage> bookTicket(BookTicket model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId2();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/bookticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"refid\":\"" + referenceId + "\",\"amount\":\"" + model.amount + "\",\"base_fare\":\"" + model.base_fare + "\",\"blockKey\":\"" + model.blockKey + "\",\"passenger_phone\":\"" + model.passenger_phone + "\",\"passenger_email\":\"" + model.passenger_email + "\"}";

                request.AddJsonBody(jsonBody);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");

                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        private string GenerateAutoGeneratedReferenceId2()
        {
            // Concatenate current timestamp with a random number
            // var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var random = new Random().Next(1000, 9999).ToString();

            return "GM_BUS_" + random;
        }
       
        [HttpPost]
        [Route("blockTicket")]
        public async Task<HttpResponseMessage> blockTicket(TicketModelObject ticketModel)
        {
            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == ticketModel.user.userId);

                //if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)         // TOKEN AUthentication
                //{

                //    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                //    {
                //        ReasonPhrase = "Invalid authorization",
                //        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                //    };
                //}
                var totals = CalculateTotals(ticketModel.tickets);

                Console.WriteLine($"Base Total: {totals.BaseTotal}");
                Console.WriteLine($"Service Tax Total: {totals.ServiceTaxTotal}");
             // Console.WriteLine($"Operator Service Charge Total: {totals.OperatorServiceChargeTotal}");
                Console.WriteLine($"Grand Total: {totals.GrandTotal}");


                var log = new tblBusBookingPaysprintLog
                {
                    username = user.username,
                    amount = totals.GrandTotal,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ticketModel.user.serviceName + " " + ticketModel.user.payment_fund_razor,
                    status = "Initiated"
                };
                db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (totals.GrandTotal < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Amount Can't be Empty\"}", Encoding.UTF8, "application/json")
                    };
                }
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/blockticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                request.AddJsonBody(ticketModel.tickets);

                var response = await client.ExecutePostAsync(request);

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                {
                    var passengerdetails = GetPrimaryPassengerInfo(ticketModel.tickets);
                    string primaryPassengerEmail = string.Empty;
                    string primaryPassengerPhoneNo = string.Empty;

                    foreach (var passenger in passengerdetails)
                    {
                        Console.WriteLine($"Name: {passenger.Email}, Phone No: {passenger.PhoneNo}");

                        primaryPassengerEmail = passenger.Email;
                        primaryPassengerPhoneNo = passenger.PhoneNo;
                    }

                    var referenceIdbook = GenerateAutoGeneratedReferenceId2();

                    var bookTicketResponse = await BookTicketAPI(ticketModel.tickets, ticketModel.user, paysprintResponse, totals, primaryPassengerEmail, primaryPassengerPhoneNo, referenceIdbook,user.username);

                    if (bookTicketResponse.IsSuccessStatusCode)
                    {
                        var TicketDetails = await TicketDetailsAPI(ticketModel.user, referenceIdbook, totals.GrandTotal, primaryPassengerPhoneNo, primaryPassengerEmail);

                        var BusDiscount = await BusDiscountAPI(ticketModel.user, totals.GrandTotal);

                        if (BusDiscount.IsSuccessStatusCode && TicketDetails.IsSuccessStatusCode)
                        {
                            statusCode = 200;
                            statusMessage = "Success";
                            message = "Congratulations..! Ticket Booked successfully.";
                            REFID = referenceIdbook;
                           
                        }
                     
                    }
                    else
                    {
                        var log1 = new tblBusBookingPaysprintLog
                        {
                            username = user.username,
                            amount = totals.GrandTotal,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Bus Booking-from Paysprint-Fundwallet",
                            status = "Failed"
                        };
                        db.tblBusBookingPaysprintLogs.InsertOnSubmit(log1);
                        db.SubmitChanges();

                        var responseObject = new
                        {
                            statusCode = 400,
                            statusMessage = "Failed",
                            message = paysprintResponse.message + "Refund Processed",
                        };
                        string jsonResponse1 = JsonConvert.SerializeObject(responseObject);
                        return new HttpResponseMessage(response.StatusCode)
                        {
                            Content = new StringContent(jsonResponse1, Encoding.UTF8, "application/json"),
                            ReasonPhrase = response.StatusDescription,

                        };
                    }

                    var responseObject1 = new
                    {
                        statusCode,
                        statusMessage,
                        message,
                        PNR,
                        REFID,
                        reward
                    };

                    string jsonResponse = JsonConvert.SerializeObject(responseObject1);

                    // Return the response with JSON content
                    return new HttpResponseMessage((HttpStatusCode)bookTicketResponse.StatusCode)
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Success"
                    };
                }
                else
                {
                    var logt = new tblBusBookingPaysprintLog
                    {
                        username = user.username,
                        amount = totals.GrandTotal,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = "Bus Booking-from Paysprint-Fundwallet",
                        status = "Refunded",
                        message = paysprintResponse.message
                    };
                    db.tblBusBookingPaysprintLogs.InsertOnSubmit(logt);
                    db.SubmitChanges();

                    var responseObject = new
                    {
                        statusCode = 400,
                        statusMessage = "Failed",
                        message = paysprintResponse.message + "Not booked",
                    };
                   
                    var jsonResponse = JsonConvert.SerializeObject(responseObject);

                    var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Bad Request"
                    };

                    return responseMessage;
                }
          

            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");

                statusCode = 500;
                statusMessage = "Failed";
                message = ex.Message + "Exception";

                var log = new tblBusBookingPaysprintLog
                {
                    username = ticketModel.user.userId,
                   // amount = totals.GrandTotal,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ex.Message + "Exception",
                status = ticketModel.user.serviceName + " " + ticketModel.user.payment_fund_razor
                };
                db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        private async Task<HttpResponseMessage> BookTicketAPI(BlockTickets model,Blockuser blockuser,dynamic pay, Totals totals, string pEmail,string Pphone,string referenceId,string uname)  
        {
            try
            {

                var user = db.tblMembers.SingleOrDefault(x => x.username == uname);

                var blockid = pay.data;

                if (blockuser.payment_fund_razor == "Fund_wallet")
                {
                    if (totals.GrandTotal > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }
                    if (user.selfPurchaseWallet >= totals.GrandTotal)
                    {
                        decimal initialBalance = user.selfPurchaseWallet;

                        user.selfPurchaseWallet -= totals.GrandTotal;
                        db.SubmitChanges();

                        var fund = new FundWallet
                        {
                            username = user.username,
                            desc = "Bus Booking",
                            credit = 0,
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = totals.GrandTotal,
                            remarks = "Towards Bus Booking",
                            summury = blockuser.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();

                        var logdebit = new tblBusBookingPaysprintLog
                        {
                            username = user.username,
                            amount = totals.GrandTotal,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = blockuser.serviceName + " " + blockuser.payment_fund_razor,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblBusBookingPaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();

                        string key = JWTtoken;
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        // var referenceId = GenerateAutoGeneratedReferenceId2();

                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },  
                    { "reqid", referenceId },
                };

                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenString = handler.WriteToken(secToken);

                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/bookticket");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                        request.AddHeader("accept", "application/json");
                        request.AddHeader("User-Agent", PartnerID);
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);
                        string jsonBody = "{\"refid\":\"" + referenceId + "\",\"amount\":\"" + totals.GrandTotal + "\",\"base_fare\":\"" + totals.BaseTotal + "\",\"blockKey\":\"" + blockid + "\",\"passenger_phone\":\"" + Pphone + "\",\"passenger_email\":\"" + pEmail + "\"}";

                        request.AddJsonBody(jsonBody);
                        // var response = await client.PostAsync(request);

                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");

                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {

                            var log = new tblBusBookingPaysprintLog
                            {
                                username = blockuser.userId,
                                amount = totals.GrandTotal,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Ticket Booked - paysprint",
                                status = "Booked",
                                message = paysprintResponse.data.pnr

                            };
                            db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                            db.SubmitChanges();


                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Ticket Booked succesfully"

                            };
                            PNR = paysprintResponse.data.pnr;
                            REFID = referenceId;
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);

                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "Successful"
                            };
                        }
                        else
                        {

                            decimal selfPurchaseWallet = user.selfPurchaseWallet;

                            if (initialBalance != selfPurchaseWallet)
                            {
                                Decimal refundAmount = initialBalance - selfPurchaseWallet;

                                if (refundAmount == totals.GrandTotal)
                                {
                                    user.selfPurchaseWallet += refundAmount;
                                    db.SubmitChanges();

                                    var refundTransaction = new FundWallet
                                    {
                                        username = user.username,
                                        desc = "Bus Booking",
                                        credit = refundAmount,
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        confirmdate = DateTime.UtcNow.AddHours(5.5),
                                        status = "Refund",
                                        debit = 0,
                                        remarks = "Towards Bus Booking",
                                        summury = $"Amount Refunded towards Bus Booking"
                                    };
                                    db.FundWallets.InsertOnSubmit(refundTransaction);
                                    db.SubmitChanges();

                                    var logt = new tblBusBookingPaysprintLog
                                    {
                                        username = user.username,
                                        amount = refundAmount,
                                        date = DateTime.UtcNow.AddHours(5.5),
                                        summary = "Bus Booking-from Paysprint-Fundwallet",
                                        status = "Refunded",
                                        message= paysprintResponse.message
                                    };
                                    db.tblBusBookingPaysprintLogs.InsertOnSubmit(logt);
                                    db.SubmitChanges();
                                }

                            }
                          
                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Not booked",
                            };
                            statusCode = 400;
                            statusMessage = "Failed";
                            message = paysprintResponse.message + "Not booked";
                            var jsonResponse = JsonConvert.SerializeObject(responseObject);

                            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "Bad Request"
                            };

                            return responseMessage;
                        }
                    }
                
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");

                statusCode = 500;
                statusMessage = "Failed";
                message = ex.Message + "Exception";
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request",
                Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
            };
        }

        private async Task<HttpResponseMessage> BusDiscountAPI(Blockuser model, decimal amount)   
        {
            try
            {
                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (model.DiscountType == "booster_points")
                {
                    if (discountMaster.booster != null)
                    {
                        // Convert rew_1 to decimal
                        decimal boostervalue;
                        if (decimal.TryParse(discountMaster.booster.ToString(), out boostervalue))
                        {

                            decimal boosterPointsAmount = amount * (boostervalue / 100);
                            // Use percentageToAdd as needed
                            if (user.boosterPoints >= Math.Abs(boosterPointsAmount))
                            {
                                // If user has enough booster points, deduct the full amount from boosterPoints
                                user.boosterPoints -= Math.Abs(boosterPointsAmount);
                                user.selfPurchaseWallet += Math.Abs(boosterPointsAmount);
                            }
                            else
                            {

                                decimal remainingAmount = Math.Abs(boosterPointsAmount - (decimal)user.boosterPoints);
                                user.selfPurchaseWallet += Math.Abs(boosterPointsAmount) - remainingAmount;
                                boosterPointsAmount -= remainingAmount;
                                user.boosterPoints = 0;

                            }
                            if (boosterPointsAmount > 0)
                            {
                                var payout = new Payout
                                {
                                    username = model.userId,
                                    desc = "Bus Booking",
                                    credit = 0,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = boosterPointsAmount,
                                    remarks = "Booster Points",
                                    summury = model.summary
                                };
                                db.Payouts.InsertOnSubmit(payout);
                                db.SubmitChanges();

                                var fund1 = new FundWallet
                                {
                                    username = model.userId,
                                    desc = "Bus Booking",
                                    credit = boosterPointsAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = 0,
                                    remarks = "Cashback",
                                    summury = model.summary
                                };
                                db.FundWallets.InsertOnSubmit(fund1);
                                db.SubmitChanges();

                                PublicArray = new object[]
                         {
                                        new
                                        {
                                             label="You got a Cashback Credit of ",
                                            value=boosterPointsAmount
                                        }
                         };

                                var log = new tblBusBookingPaysprintLog
                                {
                                    username = model.userId,
                                    amount = amount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    summary = "Ticket Booked-booster points",
                                    status = "Discount",
                                     
                                };
                                db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                                db.SubmitChanges();
                            }
                            db.SubmitChanges();

                        }
                    }

                }
                else if (model.DiscountType == "reward_points")
                {
                    decimal self_decimal;
                    if (decimal.TryParse(discountMaster.rew_1.ToString(), out self_decimal))
                    {

                        decimal percentageToAdd = amount * (self_decimal / 100);

                        if (user.rewardPoints >= percentageToAdd)
                        {
                            user.rewardPoints -= percentageToAdd;

                            user.selfPurchaseWallet += percentageToAdd;
                        }
                        else
                        {
                            decimal remainingAmount = Math.Abs(percentageToAdd - (decimal)user.rewardPoints);
                            user.selfPurchaseWallet += Math.Abs(percentageToAdd) - remainingAmount;
                            percentageToAdd -= remainingAmount;
                            user.rewardPoints = 0; // Deduct a
                        }
                        if (percentageToAdd > 0)
                        {

                            var payout = new Payout
                            {
                                username = model.userId,
                                desc = "Bus Booking",
                                credit = 0,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = percentageToAdd,
                                remarks = "Reward Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(payout);
                            db.SubmitChanges();

                            var fund1 = new FundWallet
                            {
                                username = model.userId,
                                desc = "Bus Booking",
                                credit = percentageToAdd,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0,
                                remarks = "Cashback",
                                summury = model.summary
                            };
                            db.FundWallets.InsertOnSubmit(fund1);
                            db.SubmitChanges();

                            PublicArray = new object[]
               {
                                new
                                {
                                     label="You got a Cashback Credit of ",
                                    value=percentageToAdd
                                }
               };

                            var log = new tblBusBookingPaysprintLog
                            {
                                username = model.userId,
                                amount = amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Ticket Booked-Reward points",
                                status = "Booked",

                            };
                            db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                            db.SubmitChanges();

                        }
                    }
                    // TEAM INCOME
                    decimal team_decimal;
                    if (decimal.TryParse(discountMaster.rew_2.ToString(), out team_decimal))
                    {
                        string sponsor = user.sid;
                        int i = 0;
                        List<string> usernames = new List<string>();

                        while (sponsor != null)
                        {
                            var bv = db.tblMembers.SingleOrDefault(x => x.username == sponsor);
                            if (bv != null && bv.status == "Paid")
                            {
                                i++;
                                usernames.Add(bv.username);
                            }
                            sponsor = bv?.sid;
                            if (i == 15)
                                break;
                        }
                        if (i > 0)
                        {
                            decimal incomeShare = 0;
                            incomeShare = (amount * team_decimal / 100) / 15;

                            foreach (var u in usernames)
                            {
                                var mem = db.tblMembers.SingleOrDefault(x => x.username == u);
                                if (mem != null)
                                {
                                    // Distribute incomeShare accordingly
                                    mem.RepurchaseWallet += (incomeShare * 85 / 100); // 85% of income share added to Repurchase Wallet
                                    if (incomeShare > 0)
                                    {
                                        Payout(u, incomeShare, "Booking Income", "Affiliate Income", user.username, DateTime.UtcNow.AddHours(5.5), 0);
                                    }

                                }
                            }

                        }
                    }
                    db.SubmitChanges();

                }
                else
                {

                }
                var responseObject = new
                {
                    statusCode = 200,
                    statusMessage = "success",
                    message = "Discount successful",
                    reward = PublicArray
                };
           
                reward = PublicArray;

                string jsonResponse = JsonConvert.SerializeObject(responseObject);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                    ReasonPhrase = "Successful"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                statusCode = 500;
                statusMessage = "Failed";
                message = ex.Message + "Exception";
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };

            }
        }

        private async Task<HttpResponseMessage> TicketDetailsAPI(Blockuser model, string refid,decimal amount,string phon, string mail)
        {
            try
            {
                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/get_ticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"refid\":\"" + refid + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                {
                    var booking = new tblBusBooking
                    {
                        username=model.userId,
                        pnr_no=PNR,
                        dateOfIssue=DateTime.UtcNow.AddDays(5.5),
                        refid=refid,
                        sourceCity=paysprintResponse.data.sourceCity,
                        destinationCity=paysprintResponse.data.destinationCity,
                        amount=amount,
                        customer_mobile=phon,
                        customer_email=mail,
                        MTicketEnabled=paysprintResponse.data.MTicketEnabled,
                        dateOfJourney=paysprintResponse.data.doj,
                        status= paysprintResponse.data.status,
                        discount=model.DiscountType,
                        paymentmode=model.payment_fund_razor
                    };
                    db.tblBusBookings.InsertOnSubmit(booking);
                    db.SubmitChanges();

                    var log = new tblBusBookingPaysprintLog
                    {
                        username = model.userId,
                        amount = amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = "Ticket Booked-booster/reward - ticket checked",
                        status = "Checked",
                        message= paysprintResponse.data.pnr 

                    };
                    db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                    db.SubmitChanges();

                    var responseObject = new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = "Ticket booked successful",
                        
                    };
                    //statusCode = 200;
                    //statusMessage = "success";
                    //message = "Ticket checked successful";
                    string jsonResponse = JsonConvert.SerializeObject(responseObject);

                    // Return the success response with JSON content
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
                else
                {
                    var responseObject = new
                    {
                        statusCode = 400,
                        statusMessage = "Failed",
                        message = paysprintResponse.message + "Ticket NOT Checked",
                    };
                    var log = new tblBusBookingPaysprintLog
                    {
                        username = model.userId,
                        amount = amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = paysprintResponse.message + "Failed to check ticket",
                        status = "Booked",

                    };
                    db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                    db.SubmitChanges();
                    statusCode = 400;
                    statusMessage = "Failed";
                    message = paysprintResponse.message + "Ticket NOT Checked";

                    var jsonResponse = JsonConvert.SerializeObject(responseObject);

                    var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest) 
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Bad Request"
                    };

                    return responseMessage;
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                statusCode = 500;
                statusMessage = "Failed";
                message = ex.Message + "Exception";
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error" 
                };
            }
        }

        public class Totals
        {
            public decimal BaseTotal { get; set; }
            public decimal ServiceTaxTotal { get; set; }         
            public decimal GrandTotal { get; set; }
        }

        public Totals CalculateTotals(BlockTickets blockTickets)
        {
            var totalAmount = blockTickets.inventoryItems.Values.Aggregate(new Totals(), (acc, item) =>
            {
                var fare = ParseDecimal(item.fare);
                var serviceTax = ParseDecimal(item.serviceTax);
                           
                acc.ServiceTaxTotal += serviceTax;
              
                acc.GrandTotal += fare ;
                acc.BaseTotal += fare - serviceTax;

                // Round each total to 2 decimal places
                acc.BaseTotal = Math.Round(acc.BaseTotal, 2);
                acc.ServiceTaxTotal = Math.Round(acc.ServiceTaxTotal, 2);              
                acc.GrandTotal = Math.Round(acc.GrandTotal, 2);

                return acc;
            });

            return totalAmount;
        }
        public class PassengerInfo
        {
            public string Email { get; set; }
            public string PhoneNo { get; set; }
        }

        public List<PassengerInfo> GetPrimaryPassengerInfo(BlockTickets blockTickets)
        {
            var primaryPassengers = blockTickets.inventoryItems.Values
                .Where(item => item.passenger.primary == "1")
                .Select(item => new PassengerInfo
                {
                    Email = item.passenger.email,
                    PhoneNo = item.passenger.mobile
                })
                .ToList();

            return primaryPassengers;
        }
        private decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, out var result) ? result : 0;
        }

  
        [HttpPost]
        [Route("CheckBookedTicket")]
        public async Task<HttpResponseMessage> CheckBookedTicket(CheckTickets model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/check_booked_ticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"refid\":\"" + model.refid + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("GetBookedTicket")]
        public async Task<HttpResponseMessage> GetBookedTicket(CheckTickets model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/get_ticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"refid\":\"" + model.refid + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("GetCancellationData")]
        public async Task<HttpResponseMessage> GetCancellationData(CheckTickets model) 
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/get_cancellation_data");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                string jsonBody = "{\"refid\":\"" + model.refid + "\"}";
                request.AddJsonBody(jsonBody, false);
                // var response = await client.PostAsync(request);

                var response = await client.ExecutePostAsync(request);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("CancellTicket")]
        public async Task<HttpResponseMessage> CancellTicket(cancellTicket model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds()},
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bus/ticket/cancel_ticket");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest(RestSharp.Method.Post.ToString());
                request.AddHeader("accept", "application/json");
                request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);
                 
                 request.AddJsonBody(model);

                var response = await client.ExecutePostAsync(request);
                
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");


                var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                {

                    var cancel = db.tblBusBookings.FirstOrDefault(x => x.refid == model.refid);
                    decimal refund = paysprintResponse.data.refund_amount;
                    var user = db.tblMembers.SingleOrDefault(x => x.username == cancel.username);

                    cancel.status = "CANCELLED";
                    db.SubmitChanges();

                    if (cancel.paymentmode == "Fund_wallet")
                    {
                       
                        user.selfPurchaseWallet += refund;

                        if(refund > 0)
                        {
                            var fund1 = new FundWallet
                            {
                                username = cancel.username,
                                desc = "Bus Booking",
                                credit = refund,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Refund",
                                debit = 0,
                                remarks = "Booking Refund",
                                summury = paysprintResponse.message
                            };
                            db.FundWallets.InsertOnSubmit(fund1);
                            db.SubmitChanges();
                        }

                        var log = new tblBusBookingPaysprintLog
                        {
                            username = user.username,
                            amount = refund,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = "Fund wallet refunded towards cancellation",
                            status = "Refund",
                            message = "CANCELLATION"

                        };
                        db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                        db.SubmitChanges();

                        var RefundDiscount = await BusDiscountRefund(user, cancel, refund);
                    }
                    else if (cancel.paymentmode == "razor_pay")
                    {
                        var RefundDiscount = await BusDiscountRefund(user, cancel, refund);
                    }

                    var responseObject = new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = paysprintResponse.message,
                        data=paysprintResponse.data
                    };
                  
                    string jsonResponse = JsonConvert.SerializeObject(responseObject);

                    // Return the success response with JSON content
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };

                }
                else
                {
                    var responseObject = new
                    {
                        statusCode = 400,
                        statusMessage = "success",
                        message = paysprintResponse.message,
                      
                    };

                    string jsonResponse = JsonConvert.SerializeObject(responseObject);

                    // Return the success response with JSON content
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
              
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        public async Task<HttpResponseMessage> BusDiscountRefund(tblMember user, tblBusBooking cancel,decimal refund)
        {
            try
            {
                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == "bus_booking");

                decimal amt = (decimal)cancel.amount;

                if (cancel.discount == "booster_points")
                {
                    if (discountMaster.booster != null)
                    {
                        decimal boostervalue;
                        if (decimal.TryParse(discountMaster.booster.ToString(), out boostervalue))
                        {
                            decimal boosterPointsAmount = amt * (boostervalue / 100);

                            user.selfPurchaseWallet -= Math.Abs(boosterPointsAmount);
                            user.boosterPoints += Math.Abs(boosterPointsAmount);

                            if (boosterPointsAmount > 0)
                            {
                                var payout = new Payout
                                {
                                    username = user.username,
                                    desc = "Bus Booking",
                                    credit = boosterPointsAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Booster Points",
                                    summury = "Refund Toward Cancellation Bus Ticket"
                                };
                                db.Payouts.InsertOnSubmit(payout);
                                db.SubmitChanges();

                                var fund1 = new FundWallet
                                {
                                    username = user.username,
                                    desc = "Bus Booking",
                                    credit = 0,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = boosterPointsAmount,
                                    remarks = "Booster Points Debit",
                                    summury = "Debit Toward Cancellation Bus Ticket"
                                };
                                db.FundWallets.InsertOnSubmit(fund1);
                                db.SubmitChanges();

                                var log = new tblBusBookingPaysprintLog
                                {
                                    username = user.username,
                                    amount = refund,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    summary = "Booster Points refunded towards cancellation",
                                    status = "Refund",
                                    message = "CANCELLATION"

                                };
                                db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                                db.SubmitChanges();
                            }
                            db.SubmitChanges();

                        }
                    }

                }
                else if (cancel.discount == "reward_points")
                {
                    if (discountMaster.rew_2 != null)
                    {
                        // SELF INCOME
                        decimal self_decimal;
                        if (decimal.TryParse(discountMaster.rew_1.ToString(), out self_decimal))
                        {

                            decimal percentageToAdd = amt * (self_decimal / 100);

                            user.selfPurchaseWallet -= percentageToAdd;
                            user.rewardPoints += percentageToAdd;

                            if (percentageToAdd > 0)
                            {

                                var payout = new Payout
                                {
                                    username = user.username,
                                    desc = "Bus Booking",
                                    credit = percentageToAdd,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Reward Points",
                                    summury = "Refund Toward Cancellation Bus Ticket"
                                };
                                db.Payouts.InsertOnSubmit(payout);
                                db.SubmitChanges();

                                var fund1 = new FundWallet
                                {
                                    username = user.username,
                                    desc = "Bus Booking",
                                    credit = 0,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Send",
                                    debit = percentageToAdd,
                                    remarks = "Reward Points Debit",
                                    summury = "Debit Toward Cancellation Bus Ticket"
                                };
                                db.FundWallets.InsertOnSubmit(fund1);
                                db.SubmitChanges();


                                var log = new tblBusBookingPaysprintLog
                                {
                                    username = user.username,
                                    amount = refund,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    summary = "Reward Points refunded towards cancellation",
                                    status = "Refund",
                                    message = "CANCELLATION"

                                };
                                db.tblBusBookingPaysprintLogs.InsertOnSubmit(log);
                                db.SubmitChanges();

                            }


                        }

                        // TEAM INCOME
                        decimal team_decimal;
                        if (decimal.TryParse(discountMaster.rew_2.ToString(), out team_decimal))
                        {
                            string sponsor = user.sid;
                            int i = 0;
                            List<string> usernames = new List<string>();

                            while (sponsor != null)
                            {
                                var bv = db.tblMembers.SingleOrDefault(x => x.username == sponsor);
                                if (bv != null && bv.status == "Paid")
                                {
                                    i++;
                                    usernames.Add(bv.username);
                                }
                                sponsor = bv?.sid;
                                if (i == 15)
                                    break;
                            }
                            if (i > 0)
                            {
                                decimal incomeShare = 0;
                                incomeShare = (amt * team_decimal / 100) / 15;

                                foreach (var u in usernames)
                                {
                                    var mem = db.tblMembers.SingleOrDefault(x => x.username == u);
                                    if (mem != null)
                                    {
                                        // Distribute incomeShare accordingly
                                        mem.RepurchaseWallet -= (incomeShare * 85 / 100); // 85% of income share added to Repurchase Wallet
                                        if (incomeShare > 0)
                                        {
                                            Payout(u, incomeShare, "Booking Refund Debit", "Affiliate Income Debit", user.username, DateTime.UtcNow.AddHours(5.5), 0);
                                        }

                                    }
                                }

                            }
                        }
                        db.SubmitChanges();
                    }
                }
                else
                {

                }
                var responseObject = new
                {
                    statusCode = 200,
                    statusMessage = "success",
                    message = "Refund successful",
                  
                };     
                string jsonResponse = JsonConvert.SerializeObject(responseObject);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                    ReasonPhrase = "Successful"
                };
            }
            catch( Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpGet]
        [Route("GetBookingList")]
        public HttpResponseMessage GetBookingList(string Username, int page = 1, int pageSize = 10)
        {
            try
            {
               
                List<tblBusBooking> bus = db.tblBusBookings
                    .Where(x => x.username == Username )
                    .OrderByDescending(x => x.dateOfIssue)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (bus.Any())
                {
                    // Create a success response
                    var responseData = bus.Select(book => new
                    {
                       Amount=book.amount,
                       PNR=book.pnr_no,
                       Source=book.sourceCity,
                       Destination=book.destinationCity,
                       Status=book.status,
                       DateOfjourney=book.dateOfJourney,
                       ReferenceID=book.refid
                    });

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = responseData
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    // Create a not found response
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "Failed",
                        message = "Data not found"
                    });
                }
            }
            catch (Exception ex)
            {
                // Create an internal server error response
                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetStates")]
        public HttpResponseMessage GetStates()
        {
            try
            {

                var states = db.States.ToList();

                if (states != null && states.Any())
                {
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = states.Select(franchisees => new
                        {
                            Id=franchisees.state_id,
                            Name = franchisees.state_name,
                           
                        })
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    var response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"User not found\"}", System.Text.Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetDistrictBystateID")]
        public HttpResponseMessage GetDistrictBystateID(int stateId)
        {
            try
            {

                var district = db.Districts.Where(x => x.state_id==stateId).ToList();

                if (district != null && district.Any())
                {
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent<object>(new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = district.Select(franchisees => new
                        {
                            Id=franchisees.state_id,
                            D_id=franchisees.districtID,
                            Name = franchisees.dist_name,
                           
                        })
                    }, new JsonMediaTypeFormatter());
                    return response;
                }
                else
                {
                    var response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("{\"statusCode\": 404, \"statusMessage\": \"failed\", \"message\": \"User not found\"}", System.Text.Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                response.Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"message\": \"Internal Server Error: " + ex.Message + "\"}", System.Text.Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        [Route("GetDistributorBydistrictID")]
        public HttpResponseMessage GetDistributorBydistrictID(int D_id)
        {
            try
            {
                var distributors = db.Distributors.Where(x => x.distributor_id == D_id);

                if (distributors.Any())
                {
                    var data = distributors.Select(distributor => new
                    {
                        Id = distributor.state_id,
                        D_id = distributor.distributor_id,
                        Name = distributor.distributor_name,
                        Value = distributor.distributor_value
                    });

                    var responseContent = new
                    {
                        statusCode = HttpStatusCode.OK,
                        statusMessage = "Success",
                        data = data
                    };
               

                    return Request.CreateResponse(HttpStatusCode.OK, responseContent, new JsonMediaTypeFormatter());
                }
                else
                {
                    var responseContent = new
                    {
                        statusCode = HttpStatusCode.NotFound,
                        statusMessage = "failed",
                        message = "Distributor not found"
                    };

                    return Request.CreateResponse(HttpStatusCode.NotFound, responseContent, new JsonMediaTypeFormatter());
                }
            }
            catch (Exception ex)
            {
                var responseContent = new
                {
                    statusCode = HttpStatusCode.InternalServerError,
                    statusMessage = "failed",
                    message = $"Internal Server Error: {ex.Message}"
                };

                return Request.CreateResponse(HttpStatusCode.InternalServerError, responseContent, new JsonMediaTypeFormatter());
            }
        }


        [HttpPost]
        [Route("fetchlicbill")]
        public async Task<HttpResponseMessage> fetchlicbill(FetchLICmodel model)
        {
            try
            {

                string key = JWTtoken;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var referenceId = GenerateAutoGeneratedReferenceId();

                var header = new JwtHeader(credentials);
                var payload = new JwtPayload
                {
                    { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                    { "partnerId",PartnerID },
                    { "reqid", referenceId },
                };

                

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(secToken);

              

                var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/fetchlicbill");
                var client = new RestSharp.RestClient(options);
                var request = new RestSharp.RestRequest("");
                request.AddHeader("accept", "text/plain");
                // request.AddHeader("User-Agent", PartnerID);
                request.AddHeader("Token", tokenString);
                request.AddHeader("AuthorisedKey", AuthKey);

                // string jsonBody = $"{{\"operator\":\"{model.Operator}\",\"canumber\":\"{model.cannumber}\",\"mode\":\"{model.mode}\"}}";
                string jsonBody = $"{{\"canumber\":\"{model.cannumber}\",\"mode\":\"{model.mode}\",\"ad1\":\"{model.ad1}\",\"ad2\":\"{model.ad2}\"}}";

                request.AddJsonBody(jsonBody, false);


                var response = await client.ExecutePostAsync(request);
                // Log response status and content
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");

                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content),
                    ReasonPhrase = response.StatusDescription
                };
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        [HttpPost]
        [Route("payLicBill")]
        public async Task<HttpResponseMessage> payLicBill(PayLicBillModel model)
        {
            try
            {
                var user = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (user == null || user.accesstoken != Request.Headers.Authorization.Parameter)
                {

                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Invalid authorization",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid authorization\"}", Encoding.UTF8, "application/json")
                    };
                }
                var log = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Initiated"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log);
                db.SubmitChanges();

                if (model.amount < 0)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Bad Request",
                        Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Insert valid amount\"}", Encoding.UTF8, "application/json")
                    };
                }
                if (model.paymentMode == "Fund_wallet")
                {
                    if (model.amount > user.selfPurchaseWallet)
                    {
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            ReasonPhrase = "Bad Request",
                            Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"You Don't Have sufficient Balance\"}", Encoding.UTF8, "application/json")
                        };
                    }

                    if (user.selfPurchaseWallet >= model.amount)
                    {
                        decimal initialBalance = user.selfPurchaseWallet;
                        user.selfPurchaseWallet -= model.amount;

                        var fund = new FundWallet
                        {
                            username = model.userId,
                            desc = "Recharge",
                            credit = 0, // 
                            date = DateTime.UtcNow.AddHours(5.5),
                            confirmdate = DateTime.UtcNow.AddHours(5.5),
                            status = "Send",
                            debit = model.amount,
                            remarks = "Towards Insurance",
                            summury = model.summary
                        };
                        db.FundWallets.InsertOnSubmit(fund);
                        db.SubmitChanges();

                        var logdebit = new tblpaysprintLog
                        {
                            username = model.userId,
                            amount = model.amount,
                            date = DateTime.UtcNow.AddHours(5.5),
                            summary = model.serviceName + " " + model.paymentMode,
                            status = "Debited from Fund Wallet"
                        };
                        db.tblpaysprintLogs.InsertOnSubmit(logdebit);
                        db.SubmitChanges();

                        string key = JWTtoken;
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var referenceId = GenerateAutoGeneratedReferenceId1(model.userId);

                        var header = new JwtHeader(credentials);
                        var payload = new JwtPayload
                        {
                            { "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds() },
                            { "partnerId",PartnerID },
                            { "reqid", referenceId },
                        };

                        var secToken = new JwtSecurityToken(header, payload);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenString = handler.WriteToken(secToken);

                        var options = new RestClientOptions($"https://{PaysprintBaseUrl}/api/v1/service/bill-payment/bill/paylicbill");
                        var client = new RestSharp.RestClient(options);
                        var request = new RestSharp.RestRequest("");
                        request.AddHeader("accept", "text/plain");
                        // request.AddHeader("User-Agent", PartnerID);
                        request.AddHeader("Token", tokenString);
                        request.AddHeader("AuthorisedKey", AuthKey);
                        string billFetchJson = JsonConvert.SerializeObject(model.bill_fetch);
                        string jsonString = $"{{\"canumber\": \"{model.cannumber}\", \"amount\": \"{model.amount}\", \"referenceid\": \"{referenceId}\", \"latitude\": \"{model.latitude}\", \"longitude\": \"{model.longitude}\", \"mode\": \"{model.mode}\", \"ad1\": \"{model.ad1}\", \"ad2\": \"{model.ad2}\", \"ad3\": \"{model.ad3}\", \"bill_fetch\": {billFetchJson}}}";

                        request.AddJsonBody(jsonString);

                        var response = await client.ExecutePostAsync(request);
                        // Log response status and content
                        Console.WriteLine($"Response Status: {response.StatusCode}");
                        Console.WriteLine($"Response Content: {response.Content}");
                        var paysprintResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);

                        if (paysprintResponse.status == true && paysprintResponse.response_code == 1)
                        {
                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "LIC Bill Payment-from Paysprint-Fundwallet",
                                status = "Success",
                                message = referenceId
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            var secondApiResponse = await CallpayLICbillDiscount(model, referenceId, paysprintResponse);

                            var responseObject = new
                            {
                                statusCode = 200,
                                statusMessage = "success",
                                message = "Recharge is Succesfull",
                                reward = PublicArray
                            };

                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = "OK"
                            };
                        }
                        else
                        {

                            var log1 = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = "Bill Payment-from Paysprint-Fundwallet",
                                status = "Failed",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(log1);
                            db.SubmitChanges();

                            decimal selfPurchaseWallet = user.selfPurchaseWallet;

                            if (initialBalance != selfPurchaseWallet)
                            {
                                Decimal refundAmount = initialBalance - selfPurchaseWallet;
                                user.selfPurchaseWallet += refundAmount;
                                db.SubmitChanges();

                                // Log refund transaction in FundWallet table
                                var refundTransaction = new FundWallet
                                {
                                    username = model.userId,
                                    desc = model.serviceName,
                                    credit = refundAmount,
                                    date = DateTime.UtcNow.AddHours(5.5),
                                    confirmdate = DateTime.UtcNow.AddHours(5.5),
                                    status = "Refund",
                                    debit = 0,
                                    remarks = "Towards Insurance",
                                    summury = $"Amount Refunded towards {model.serviceName}({model.cannumber})"
                                };
                                db.FundWallets.InsertOnSubmit(refundTransaction);
                                db.SubmitChanges();
                            }

                            // Log transaction in tblpaysprintLog table
                            var logTransaction = new tblpaysprintLog
                            {
                                username = model.userId,
                                amount = (decimal)model.amount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                summary = $"{model.serviceName} from Paysprint-Fundwallet",
                                status = "Failed & Refunded",
                                message = paysprintResponse.message
                            };
                            db.tblpaysprintLogs.InsertOnSubmit(logTransaction);
                            db.SubmitChanges();

                            var responseObject = new
                            {
                                statusCode = 400,
                                statusMessage = "Failed",
                                message = paysprintResponse.message + "Refund Processed",
                            };
                            string jsonResponse = JsonConvert.SerializeObject(responseObject);
                            return new HttpResponseMessage(response.StatusCode)
                            {
                                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json"),
                                ReasonPhrase = response.StatusDescription,

                            };
                        }
                    }

                }
                var log2 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = model.serviceName + " " + model.paymentMode,
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log2);
                db.SubmitChanges();
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Bad Request",
                    Content = new StringContent("{\"statusCode\": 400, \"statusMessage\": \"failed\", \"message\": \"Invalid request\"}", Encoding.UTF8, "application/json")
                };

            }
            catch(Exception ex)
            {
                var log3 = new tblpaysprintLog
                {
                    username = model.userId,
                    amount = model.amount,
                    date = DateTime.UtcNow.AddHours(5.5),
                    summary = ex.Message + "Bill Payment - due to Exception",
                    status = "Failed"
                };
                db.tblpaysprintLogs.InsertOnSubmit(log3);
                db.SubmitChanges();

                Console.WriteLine($"An error occurred: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error"
                };
            }
        }

        private async Task<HttpResponseMessage> CallpayLICbillDiscount(AccountsModels.PayLicBillModel model, string referenceId, dynamic response)
        {
            try
            {
                bool status = response.status;
                int responseCode = response.response_code;
                string operatorId = response.operatorid;
                int ackno = response.ackno;
                string refid = response.refid;
                string message = response.message;

                var discountMaster = db.tblDiscountMasters.SingleOrDefault(dm => dm.service == model.serviceName);
                var member = db.tblMembers.SingleOrDefault(x => x.username == model.userId);

                if (discountMaster != null && member != null)
                {
                    if (Decimal.TryParse(discountMaster.rew_1.ToString(), out Decimal rewardPercentage))
                    {
                        Decimal rewardAmount = model.amount * (rewardPercentage / 100M);
                        member.rewardPoints = member.rewardPoints.HasValue ? member.rewardPoints.Value + rewardAmount : rewardAmount;
                        db.SubmitChanges();

                        if (rewardAmount > 0M)
                        {
                            var payout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = rewardAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Reward Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(payout);
                            db.SubmitChanges();
                            cashRPC = rewardAmount;
                        }
                    }

                    if (model.DiscountType == "booster_points" && discountMaster.booster.HasValue && Decimal.TryParse(discountMaster.booster.ToString(), out Decimal boosterPercentage))
                    {
                        Decimal boosterAmount = model.amount * (boosterPercentage / 100M);
                        Decimal availableBoosterPoints = member.boosterPoints.GetValueOrDefault();

                        if (availableBoosterPoints >= Math.Abs(boosterAmount))
                        {
                            member.boosterPoints -= Math.Abs(boosterAmount);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount);
                        }
                        else
                        {
                            Decimal remainingAmount = Math.Abs(boosterAmount - availableBoosterPoints);
                            member.selfPurchaseWallet += Math.Abs(boosterAmount) - remainingAmount;
                            member.boosterPoints = 0M;
                        }

                        db.SubmitChanges();

                        if (boosterAmount > 0M)
                        {
                            var boosterPayout = new Payout
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = 0M,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = boosterAmount,
                                remarks = "Booster Points",
                                summury = model.summary
                            };
                            db.Payouts.InsertOnSubmit(boosterPayout);

                            var fundWallet = new FundWallet
                            {
                                username = model.userId,
                                desc = model.serviceName,
                                credit = boosterAmount,
                                date = DateTime.UtcNow.AddHours(5.5),
                                confirmdate = DateTime.UtcNow.AddHours(5.5),
                                status = "Send",
                                debit = 0M,
                                remarks = "Cashback",
                                summury = model.summary
                            };
                            db.FundWallets.InsertOnSubmit(fundWallet);

                            db.SubmitChanges();
                            cashBooster = boosterAmount;
                        }
                    }

                    PublicArray = string.IsNullOrEmpty(model.DiscountType) ?
                        new object[]
                        {
                    new { label = "You a Got a Reward Points Credit of ", value = cashRPC }
                        } :
                        new object[]
                        {
                    new { label = "You got a Reward Points Credit of ", value = cashRPC },
                    new { label = "You got a Cashback Credit of ", value = cashBooster }
                        };

                    var recharge = new tblRecharge
                    {
                        userID = model.userId,
                        amount = model.amount,
                        discountType = model.DiscountType,
                        paymentMode = model.paymentMode,
                        transactionNo = referenceId,
                        aknowledgeId = ackno.ToString(),
                        referenceID = operatorId,
                        remarks = model.serviceName,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summury = model.summary,
                        Operator = model.serviceName
                    };
                    db.tblRecharges.InsertOnSubmit(recharge);
                    db.SubmitChanges();

                    var paysprintLog = new tblpaysprintLog
                    {
                        username = model.userId,
                        amount = model.amount,
                        date = DateTime.UtcNow.AddHours(5.5),
                        summary = $"{model.serviceName} booster / rewards",
                        status = "Succesfull",
                        message = referenceId
                    };
                    db.tblpaysprintLogs.InsertOnSubmit(paysprintLog);
                    db.SubmitChanges();

                    var content = JsonConvert.SerializeObject(new
                    {
                        statusCode = 200,
                        statusMessage = "success",
                        message = "Second API call successful",
                        reward = PublicArray
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content, Encoding.UTF8, "application/json"),
                        ReasonPhrase = "Successful"
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        ReasonPhrase = "Invalid User or Discount Service"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling second API: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "Internal Server Error",
                    Content = new StringContent("{\"statusCode\": 500, \"statusMessage\": \"failed\", \"data\": \"Error calling second API\"}", Encoding.UTF8, "application/json")
                };
            }
        }

    }
}

   



