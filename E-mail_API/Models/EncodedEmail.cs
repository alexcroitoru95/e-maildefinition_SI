using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E_mail_API.Models
{
    public class EncodedEmail
    {
        public string encodedheadSection { get; set; }
        public string encodedcontentSection { get; set; }
        public string encodedfooterSection { get; set; }
    }
}