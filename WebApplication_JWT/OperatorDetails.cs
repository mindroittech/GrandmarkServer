using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication_JWT
{
    public enum OperatorDetails
    {
        vodafone = 22,
        airtel = 11,
        airtel_digital_tv = 12,
        bsnl = 13,
        dish_tv = 14,
        idea = 4,
        jio = 18,
        mtnl = 35,
        mtnl_delhi = 33,
        mtnl_mumbai = 34,
        sun_direct = 27,
        tata_sky = 8,
        videocon_D2H=10,
        
    }
    public static class OperatorDetailsExtensions
    {
        public static string GetNameFromId(int id)
        {
            foreach (OperatorDetails op in Enum.GetValues(typeof(OperatorDetails)))
            {
                if ((int)op == id)
                {
                    return op.ToString();
                }
            }
            return null; // Or throw exception if desired
        }
    }

}
