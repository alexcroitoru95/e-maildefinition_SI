using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Text.RegularExpressions;
using E_mail_API.Models;
using System.Security.Cryptography;
using System.Text;

namespace E_mail_API.Controllers
{
    public class HomeController : Controller
    {
        public String head, content, footer;

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Title = "E-mail Definition";

            return View();
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "goTo")]
        public ActionResult goTo()
        {
            TempData["clicked"] = "clicked";

            return RedirectToAction("Index", "EncryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "createTXT")]
        public ActionResult createTXT(string headSection, string contentSection, string footerSection)
        {
            head = headSection;
            content = contentSection;
            footer = footerSection;

            StringWriter oStringWriter = new StringWriter();

            Response.ContentType = "application/txt";
            Response.AddHeader("content-disposition", "attachment;filename=Message.txt");
            Response.Clear();

            using (TextWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                writer.WriteLine("Header: " + head);
                writer.WriteLine("Content: " + content);
                writer.WriteLine("Footer: " + footer);
            }

            Response.End();

            return View("Index");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "createPDF")]
        public ActionResult createPDF(string headSection, string contentSection, string footerSection)
        {
            head = headSection;
            content = contentSection;
            footer = footerSection;

            int y_position_content = 600;
            int y_position_footer = 250;

            string[] contentSplit = Regex.Split(content, @"(?<=[.?!])", RegexOptions.None);
            string[] footerSplit = Regex.Split(footer, @"(?<=[,])", RegexOptions.None);

            Document pdfDoc = new Document(PageSize.A4, 10, 10, 10, 10);

            using (PdfWriter writer = PdfWriter.GetInstance(pdfDoc, Response.OutputStream))
            {
                pdfDoc.Open();

                PdfContentByte cb = writer.DirectContent;
                cb.SaveState();
                cb.SetColorFill(BaseColor.BLACK);
                cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 12f);
                cb.BeginText();

                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, "E-mail Definition", 250, 770, 0);
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, head, 75, 665, 0);

                foreach (string line in contentSplit)
                {
                    string contentLine = line.TrimStart(' ');
                    cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, contentLine, 75, y_position_content, 0);
                    y_position_content = y_position_content - 20;

                }

                foreach (string line in footerSplit)
                {
                    string footerLine = line.TrimStart(' ');
                    cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, footerLine, 380, y_position_footer, 0);
                    y_position_footer = y_position_footer - 20;
                }

                cb.EndText();
                cb.RestoreState();

                pdfDoc.Close();
            }

            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;" + "filename=E-mail Definition.pdf");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Write(pdfDoc);
            Response.End();

            return View("Index");
        }

        public PartialViewResult ShowError(String sErrorMessage)
        {
            return PartialView("ErrorMessageView");
        }

    }
}
