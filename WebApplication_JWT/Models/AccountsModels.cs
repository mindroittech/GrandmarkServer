using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication_JWT.Models
{
    public class AccountsModels
    {
        public class LoginModel
        {
            [Required]
            public string UsernameOrMobile { get; set; }

            [Required]
            public string Password { get; set; }
        }

        public class ChangePasswordBindingModel
        {
            [Required]
            [Display(Name = "UserName")]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public class AddMoneyModel
        {
            public string Username { get; set; }

            public Decimal Amount { get; set; }

            public string Paymentmode { get; set; }

            public string summary { get; set; }
        }

        public class SetPasswordBindingModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public class SendMoneyModel
        {
            public string SenderUsername { get; set; }

            public string RecipientUsername { get; set; }

            public string RecipientMobileNo { get; set; }

            public Decimal Amount { get; set; }

            public string summary { get; set; }
        }
        public class RechargeRequestModel
        {
            [Required]
            [DataType(DataType.PhoneNumber)]
            public long cannumber { get; set; }

            [Required]
            public int amount { get; set; }

            public int Operator { get; set; }

            public string paymentMode { get; set; }

            public string userId { get; set; }

            public string DiscountType { get; set; }

            public string summary { get; set; }

            public string serviceName { get; set; }

            public string ReferenceId { get; set; }

            public string transactionNo { get; set; }
        }

        public class MembershipActivationRequest
        {
            public string Username { get; set; }

            public int PlanID { get; set; }

            public string Paymentmode { get; set; }

            public string summary { get; set; }

            public string franchiseid { get; set; }
        }

        public class UpgradeMemberRequest
        {
            public string Username { get; set; }

            public int SelectedPlanID { get; set; }

            public string paymentMode { get; set; }

            public string summary { get; set; }

            public string franchiseid { get; set; }
        }

        public class StoreRechargeDataModel
        {
            public string userID { get; set; }

            public Decimal amount { get; set; }

            public string DiscountType { get; set; }

            public string paymentMode { get; set; }

            public string transactionNo { get; set; }

            public string acknowledgeID { get; set; }

            public string referenceID { get; set; }

            public int Operator { get; set; }

            public string serviceName { get; set; }

            public string summary { get; set; }
        }

        public class UpdateProfileModel
        {
            [Required]
            public string firstName { get; set; }

            [Required]
            public string lastName { get; set; }

            [Required]
            public string email { get; set; }

            public string adharNo { get; set; }

            public string panNo { get; set; }

            public string address { get; set; }

            public string pinCode { get; set; }

            [Required]
            public string city { get; set; }

            [Required]
            public string state { get; set; }

            public string Photo { get; set; }
        }

        public class FranchiseePaymentModel
        {
            public string SenderUsername { get; set; }

            public string VendorUsername { get; set; }

            public Decimal Amount { get; set; }

            public string DiscountType { get; set; }
        }

        public class PaySprintMobileModel
        {
            public long number { get; set; }

            public string type { get; set; }
        }

        public class PaySprintDTHModel
        {
            public long cannumber { get; set; }

            public string op { get; set; }
        }

        public class PaySprintBrowsePlanModel
        {
            public string circle { get; set; }

            public string op { get; set; }
        }

        public class BillOperator
        {
            public string mode { get; set; }
        }

        public class FetchBillModel
        {
            public string cannumber { get; set; }

            [Required]
            public string mode { get; set; }

            [Required]
            public int Operator { get; set; }

            public string ad1 { get; set; }
            public string ad2 { get; set; }
        }

        public class FetchLICmodel
        {

            public long cannumber { get; set; }
    
            public string mode { get; set; }

            public string ad1 { get; set; }
            public string ad2 { get; set; }
        }
        public class PayLicBillModel
        {
            public string DiscountType { get; set; }

            public string Operator { get; set; }

            public string cannumber { get; set; }

            public Decimal amount { get; set; }

            public string referenceId { get; set; }

            public string latitude { get; set; }

            public string longitude { get; set; }

            public string mode { get; set; }

            public string userId { get; set; }

            public string paymentMode { get; set; }

            public string serviceName { get; set; }

            public string summary { get; set; }
            public string ad1 { get; set; }
            public string ad2 { get; set; }
            public string ad3 { get; set; }

            public object bill_fetch { get; set; }
        }

        public class PayBillRequestModel
        {
            public string DiscountType { get; set; }

            public string Operator { get; set; }

            public string cannumber { get; set; }

            public Decimal amount { get; set; }

            public string referenceId { get; set; }

            public string latitude { get; set; }

            public string longitude { get; set; }

            public string mode { get; set; }

            public string userId { get; set; }

            public string paymentMode { get; set; }

            public string serviceName { get; set; }

            public string summary { get; set; }

            public object bill_fetch { get; set; }
        }

        public class FetchConsumerDetails
        {
            public int Operator { get; set; }

            [Required]
            public string cannumber { get; set; }
            public int ad1 { get; set; }
            public int ad2 { get; set; }
            public int ad3 { get; set; }
            public int ad4 { get; set; }

        }

        public class FastagRequestModel
        {
            public string DiscountType { get; set; }

            public string Operator { get; set; }

            public string cannumber { get; set; }

            public Decimal amount { get; set; }

            public string referenceId { get; set; }

            public string latitude { get; set; }

            public string longitude { get; set; }

            public object bill_fetch { get; set; }

            public string userId { get; set; }

            public string paymentMode { get; set; }

            public string serviceName { get; set; }

            public string summary { get; set; }
        }

        public class LpgBillModel
        {
            public string cannumber { get; set; }
            public int Operator { get; set; }
            public decimal amount { get; set; }
            public int ad1 { get; set; }
            public int ad2 { get; set; }
            public long ad3 { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }

            //public string cannumber { get; set; }

            //public string Operator { get; set; }

            //public Decimal amount { get; set; }

            //public string ad1 { get; set; }

            //public string ad2 { get; set; }

            //public string ad3 { get; set; }

            //public string latitude { get; set; }

            //public string longitude { get; set; }

            public string userId { get; set; }

            public string paymentMode { get; set; }

            public string serviceName { get; set; }

            public string summary { get; set; }

            public string DiscountType { get; set; }
        }

        public class OrderRequestModel
        {
            public long amount { get; set; }

            public string currency { get; set; }
        }

        public class RazorpayPaymentInfo
        {
            public string razorpay_payment_id { get; set; }

            public string razorpay_order_id { get; set; }

            public string razorpay_signature { get; set; }
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

        public class AvailableTrips
        {
            public string source_id { get; set; }

            public string destination_id { get; set; }

            public string date_of_journey { get; set; }
        }

        public class TripDetalis
        {
            public string trip_id { get; set; }

            public string bpId { get; set; }

        }


        public class BookTicket
        {
            public string refid { get; set; }

            public decimal amount { get; set; }

            public string base_fare { get; set; }

            public string blockKey { get; set; }

            public string passenger_phone { get; set; }

            public string passenger_email { get; set; }
        }

      public class TicketModelObject
        {
            public BlockTickets tickets { get; set; }
            public Blockuser user { get; set; }
        }

        public class BlockTickets
        {
            [JsonProperty("availableTripId")]
            public long availableTripId { get; set; }

            [Required]
            [JsonProperty("boardingPointId")]
            public int boardingPointId { get; set; }

            [Required]
            [JsonProperty("droppingPointId")]
            public string droppingPointId { get; set; }

            [Required]
            [JsonProperty("source")]
            public string source { get; set; }

            [Required]
            [JsonProperty("destination")]
            public string destination { get; set; }

            [JsonProperty("inventoryItems")]
            public Dictionary<string, InventoryItem> inventoryItems { get; set; }

            [Required]
            [JsonProperty("bookingType")]
            public string bookingType { get; set; }

            [Required]
            [JsonProperty("paymentMode")]
            public string paymentMode { get; set; }

            [Required]
            [JsonProperty("serviceCharge")]
            public decimal serviceCharge { get; set; }

           
        }
        public class Blockuser
        {
            public string payment_fund_razor { get; set; }

            public string userId { get; set; }

            public string DiscountType { get; set; }

            public string summary { get; set; }

            public string serviceName { get; set; }
        }

        //public class Passenger
        //{
        //    [Required]
        //    [JsonProperty("name")]
        //    public string name { get; set; }

        //    [JsonProperty("mobile")]
        //    public long mobile { get; set; }

        //    [Required]
        //    [JsonProperty("title")]
        //    public string title { get; set; }

        //    [EmailAddress]
        //    [JsonProperty("email")]
        //    public string email { get; set; }

        //    [Required]
        //    [JsonProperty("age")]
        //    public int age { get; set; }

        //    [Required]
        //    [JsonProperty("gender")]
        //    public string gender { get; set; }

        //    [Required]
        //    [JsonProperty("address")]
        //    public string address { get; set; }

        //    [Required]
        //    [JsonProperty("idType")]
        //    public string idType { get; set; }

        //    [Required]
        //    [JsonProperty("idNumber")]
        //    public string idNumber { get; set; }

        //    [Required]
        //    [JsonProperty("primary")]
        //    public string primary { get; set; }
        //}

        //public class InventoryItem
        //{
        //    [Required]
        //    [JsonProperty("seatName")]
        //    public string seatName { get; set; }

        //    [Required]
        //    [JsonProperty("fare")]
        //    public double fare { get; set; }

        //    [Required]
        //    [JsonProperty("serviceTax")]
        //    public double serviceTax { get; set; }

        //    [JsonProperty("operatorServiceCharge")]
        //    public double operatorServiceCharge { get; set; }

        //    [Required]
        //    [JsonProperty("ladiesSeat")]
        //    public string ladiesSeat { get; set; }

        //    [Required]
        //    [JsonProperty("passenger")]
        //    public Passenger passenger { get; set; }
        //}

        // callback model

        public class SeatDetails
        {
            public string bookingFee { get; set; }
            public string busType { get; set; }
            public string cancellationCalculationTimestamp { get; set; }
            public string cancellationMessage { get; set; }
            public string cancellationPolicy { get; set; }
            public DateTime dateOfIssue { get; set; }
            public string destinationCity { get; set; }
            public string destinationCityId { get; set; }
            public DateTime doj { get; set; }
            public string dropLocation { get; set; }
            public string dropLocationAddress { get; set; }
            public string dropLocationId { get; set; }
            public string dropLocationLandmark { get; set; }
            public string dropTime { get; set; }
            public string firstBoardingPointTime { get; set; }
            public string hasRTCBreakup { get; set; }
            public string hasSpecialTemplate { get; set; }
            public string inventoryId { get; set; }
            public List<InventoryItem> inventoryItems { get; set; }
            public string MTicketEnabled { get; set; }
            public string partialCancellationAllowed { get; set; }
            public string pickUpContactNo { get; set; }
            public string pickUpLocationAddress { get; set; }
            public string pickupLocation { get; set; }
            public string pickupLocationId { get; set; }
            public string pickupLocationLandmark { get; set; }
            public string pickupTime { get; set; }
            public string pnr { get; set; }
            public string primeDepartureTime { get; set; }
            public string primoBooking { get; set; }
            public ReschedulingPolicy reschedulingPolicy { get; set; }
            public string serviceCharge { get; set; }
            public string sourceCity { get; set; }
            public string sourceCityId { get; set; }
            public string status { get; set; }
            public string tin { get; set; }
            public string travels { get; set; }
            public string vaccinatedBus { get; set; }
            public string vaccinatedStaff { get; set; }
        }

        public class InventoryItem
        {
            public string fare { get; set; }
            public string ladiesSeat { get; set; }
            public string malesSeat { get; set; }
            public string operatorServiceCharge { get; set; }
            public Passenger passenger { get; set; }
            public string seatName { get; set; }
            public string serviceTax { get; set; }
        }

        public class Passenger
        {
            public string address { get; set; }
            public string age { get; set; }
            public string email { get; set; }
            public string gender { get; set; }
            public string idNumber { get; set; }
            public string idType { get; set; }
            public string mobile { get; set; }
            public string name { get; set; }
            public string primary { get; set; }
            public string singleLadies { get; set; }
            public string title { get; set; }
        }

        public class ReschedulingPolicy
        {
            public string reschedulingCharge { get; set; }
            public string windowTime { get; set; }
        }

        public class CallbackPayload
        {
            public string Event { get; set; }
            public Param Param { get; set; }
        }

        public class Param
        {
            public string Amount { get; set; }
            public string BaseFare { get; set; }
            public string Comm { get; set; }
            public string Tds { get; set; }
            public string TotalDeduction { get; set; }
            public string CustomerMobile { get; set; }
            public string RefId { get; set; }
            public string CustomerEmail { get; set; }
            public string BlockId { get; set; }
            public string pnr_no { get; set; }
        }

        public class CheckTickets
        {
            public string refid { get; set; }
        }

        public class cancellTicket
        {
            public string refid { get; set; }
            public Dictionary<int, string> seatsToCancel { get; set; }

        }
       

    }

}