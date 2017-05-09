using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace E_mail_API.Models
{
    public class E_mailContent
    {
        public string headSection { get; set; }
        public string contentSection { get; set; }
        public string footerSection { get; set; }
    }
}