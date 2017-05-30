using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace E_mail_API.Controllers
{
    public class DecryptEmailController : Controller
    {
        RSACryptoServiceProvider myRSA = new RSACryptoServiceProvider();

        public ActionResult Index()
        {
            ViewBag.Title = "Decrypt E-mail Definition";

            return View("DecryptEmail");
        }

        [HttpPost]
        public ActionResult ImportKey()
        {
            String decryptedHead, decryptedContent, decryptedFooter;

            DecodeMessage();

            decryptedHead = (string)Session["decrytpedHead"];
            decryptedContent = (string)Session["decryptedContent"];
            decryptedFooter = (string)Session["decryptedFooter"];

            ViewBag.head = decryptedHead;
            ViewBag.content = decryptedContent;
            ViewBag.footer = decryptedFooter;

            return View("DecryptEmail");
        }

        [HttpPost]
        public ActionResult ImportMessage()
        {
            String lineReader, encodedHead, encodedContent, encodedFooter;
            String searchByHead = "Header: ", searchByContent = "Content: ", searchByFooter = "Footer: ";

            foreach (string file in Request.Files)
            {
                var hpf = this.Request.Files[file];
                if (hpf.ContentLength == 0)
                {
                     TempData["sErrMsg"] = "Empty file!";
                }

                var path = Path.Combine(Server.MapPath("~/App_Data/Upload"), Path.GetFileName(hpf.FileName));
                hpf.SaveAs(path);

                StreamReader readKey = new StreamReader(path);

                while ((lineReader = readKey.ReadLine()) != null)
                {
                    if (lineReader.Contains(searchByHead))
                    {
                        encodedHead = lineReader.Substring(lineReader.IndexOf(searchByHead) + searchByHead.Length);
                        ViewBag.head = encodedHead;
                        Session["encryptedHead"] = encodedHead;
                    }

                    if (lineReader.Contains(searchByContent))
                    {
                        encodedContent = lineReader.Substring(lineReader.IndexOf(searchByContent) + searchByContent.Length);
                        ViewBag.content = encodedContent;
                        Session["encryptedContent"] = encodedContent;
                    }

                    if (lineReader.Contains(searchByFooter))
                    {
                        encodedFooter = lineReader.Substring(lineReader.IndexOf(searchByFooter) + searchByFooter.Length);
                        ViewBag.footer = encodedFooter;
                        Session["encryptedFooter"] = encodedFooter;
                    }
                }
            }

            return View("DecryptEmail");
        }

        public void DecodeMessage()
        {
            var private_key = (RSAParameters)TempData["private_key"];

            myRSA.ImportParameters(private_key);

            String encodedHead, encodedContent, encodedFooter;

            encodedHead = (string)Session["encryptedHead"];
            encodedContent = (string)Session["encryptedContent"];
            encodedFooter = (string)Session["encryptedFooter"];

            var decryptHead = Convert.FromBase64String(encodedHead);
            var decryptContent = Convert.FromBase64String(encodedContent);
            var decryptFooter = Convert.FromBase64String(encodedFooter);

            var decryptedHead = myRSA.Decrypt(decryptHead, false);
            var decryptedContent = myRSA.Decrypt(decryptContent, false);
            var decryptedFooter = myRSA.Decrypt(decryptFooter, false);

            var headTextData = Encoding.Unicode.GetString(decryptedHead);
            var contentTextData = Encoding.Unicode.GetString(decryptedContent);
            var footerTextData = Encoding.Unicode.GetString(decryptedFooter);

            Session["decryptedHead"] = headTextData;
            Session["decryptedContent"] = contentTextData;
            Session["decryptedFooter"] = footerTextData;
        }
    }
}