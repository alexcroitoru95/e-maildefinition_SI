using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace E_mail_API.Controllers
{
    public class DecryptEmailController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Decrypt E-mail Definition";

            return View("DecryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "importKey")]
        public ActionResult ImportKey()
        {


            return View("DecryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "importMessage")]
        public ActionResult ImportMessage()
        {


            return View("DecryptEmail");
        }
    }
}