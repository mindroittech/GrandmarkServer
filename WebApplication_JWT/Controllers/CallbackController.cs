using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApplication_JWT.Models;

namespace WebApplication_JWT.Controllers
{
    [RoutePrefix("api/callback")]
    public class CallbackController : ApiController
    {
        public CallbackController()
        {

        }

        [HttpPost]
        [Route("BusResponse")]
        public async Task<IHttpActionResult> BusResponse(dynamic payload)
        {
            try
            {
                if (payload == null)
                {
                    return BadRequest("Invalid payload");
                }

                if (payload.@event == "BUS_TICKET_BOOKING_DEBIT_CONFIRMATION")
                {
                    return Ok(new { status = 200, message = "Debit completed successfully" });
                }
     
                if (payload.@event == "BUS_TICKET_BOOKING_CONFIRMATION")
                {
                     return Ok(new { status = 200, message = "Transaction completed successfully" });
                    
                }
                else if (payload.@event == "BUS_TICKET_BOOKING_CREDIT_CONFIRMATION")
                {
                    return Ok(new { status = 200, message = "Debit completed successfully" });
                }
                else
                {
                    return BadRequest("Unknown event type");
                }

            }
            catch (Exception ex)
            {
                // Handle any exceptions thrown during callback processing
                Console.WriteLine($"Error handling callback: {ex.Message}");
                return InternalServerError(); // Internal Server Error
            }

        }
    }
}
