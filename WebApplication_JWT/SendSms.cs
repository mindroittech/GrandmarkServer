using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WebApplication_JWT
{
    public class SendSms
    {
         public SendSms()
        {


            //
            // TODO: Add constructor logic here
            //
        }
        public string sms(string mob, string msg, string templateId)
        {
            Task.Factory.StartNew(() =>
            {
                if (ConfigurationManager.AppSettings["env"] == "prod")
                {
                    string sURL;

                    string sender = "GMSHPP";
                    string key = "8d68ae6b01fad26c17cc4dadaad244fc";
                    WebRequest wrGETURL;
                    StreamReader objReader;

                    //red sms2
                    sURL = string.Format("http://login.redsms.in/api/smsapi?key={0}&route=2&sender={1}&number={2}&sms={3}&templateid={4}", key, sender, mob, msg, templateId);

                    wrGETURL = WebRequest.Create(sURL);
                    try
                    {
                        Stream objStream;
                        objStream = wrGETURL.GetResponse().GetResponseStream();
                        objReader = new StreamReader(objStream);
                        objReader.Close();

                    }
                    catch
                    {

                    }
                }
            });
            return "Sms has been sent ";
        }

        internal Task<string> sms(long phoneNumber, string message, string templateId)
        {
            throw new NotImplementedException();
        }
    }
}