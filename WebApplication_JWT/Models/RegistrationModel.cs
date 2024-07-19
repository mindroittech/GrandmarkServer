using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication_JWT.Models
{
    public class RegistrationModel
    {
        [Required]
        public string SponserId { get; set; }

        [Required]
        public string Team { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [DataType(DataType.PhoneNumber)]
        public long MobileNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string Pincode { get; set; }

        public string State { get; set; }

        public string District { get; set; }
    }

    public class ForgotPasswordRequest
    {
     
        [DataType(DataType.PhoneNumber)]
        [Required]
        public string MobileNumber { get; set; }
    }
}