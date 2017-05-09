using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Diagnostics;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Web;
using E_mail_API.Models;

namespace E_mail_API.Controllers
{
    public class EncryptEmailController : Controller
    {
        RSACryptoServiceProvider myRSA = new RSACryptoServiceProvider(2048);
        RSACryptoServiceProvider rsa_CryptoService = new RSACryptoServiceProvider();

        [HttpGet]
        public ActionResult Index()
        {
            String head, content, footer;

            ViewBag.Title = "Encrypt E-mail Definition";

            E_mailContent model = (E_mailContent)TempData["model"];

            head = model.headSection;
            content = model.contentSection;
            footer = model.footerSection;

            if(head != null && content != null && footer != null)
            {
                ViewBag.head = head;
                ViewBag.content = content;
                ViewBag.footer = footer;
            }

            Session["head"] = head;
            Session["content"] = content;
            Session["footer"] = footer;

            return View("EncryptEmail", model);
        }

        [HttpPost]
        public ActionResult createEncryptedEmail()
        {
            var head_encrypt = (Session["head"] != null) ? Session["head"] : ""; 
            var content_encrypt = (Session["content"] != null) ? Session["content"] : ""; 
            var footer_encrypt = (Session["footer"] != null) ? Session["footer"] : ""; ;

            var private_Key = myRSA.ExportParameters(true);
            var public_Key = myRSA.ExportParameters(false);

            //Convertirea cheii publice intr-un string
            string public_Key_String;
            {
                var String_Writer = new System.IO.StringWriter();
                var String_Serializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                String_Serializer.Serialize(String_Writer, public_Key);
                public_Key_String = String_Writer.ToString();
            }

            //Convertirea cheii publice inapoi in Byte
            {
                var String_Reader = new System.IO.StringReader(public_Key_String);
                var XML_Serializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                public_Key = (RSAParameters)XML_Serializer.Deserialize(String_Reader);
            }

            //Convertirea cheii private intr-un string
            string private_Key_String;
            {
                var String_Writer = new System.IO.StringWriter();
                var String_Serializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                String_Serializer.Serialize(String_Writer, private_Key);
                private_Key_String = String_Writer.ToString();
            }

            //Convertirea cheii private inapoi in Byte
            {
                var String_Reader = new System.IO.StringReader(private_Key_String);
                var XML_Serializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                public_Key = (RSAParameters)XML_Serializer.Deserialize(String_Reader);
            }

            //Importare cheie publica
            rsa_CryptoService.ImportParameters(public_Key);

            //Criptarea e-mail definition
            var bytesEmailHead = Encoding.Unicode.GetBytes(head_encrypt.ToString());
            var bytesEmailContent = Encoding.Unicode.GetBytes(content_encrypt.ToString());
            var bytesEmailFooter = Encoding.Unicode.GetBytes(footer_encrypt.ToString());

            var cypherHead = Convert.ToBase64String(bytesEmailHead);
            var cypherContent = Convert.ToBase64String(bytesEmailContent);
            var cypherFooter = Convert.ToBase64String(bytesEmailFooter);

            int y_position_content = 600;
            int y_position_footer = 250;
            
            int chunkSize = 25;
            int stringLength = cypherContent.Length;
            string[] contentSplit = new string[stringLength];

            for (int i = 0; i < stringLength; i += chunkSize)
            {
                if (i + chunkSize > stringLength) chunkSize = stringLength - i;
                    contentSplit[i] = cypherContent.Substring(i, chunkSize);

            }

            //Scriere in PDF
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
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, cypherHead, 75, 665, 0);

                for( int i = 0;i < stringLength; i += 1)
                {
                    cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, contentSplit[i], 75, y_position_content, 0);
                    y_position_content = y_position_content - 20;
                }
                                
                cb.ShowTextAligned(PdfContentByte.ALIGN_LEFT, cypherFooter, 380, y_position_footer, 0);
                y_position_footer = y_position_footer - 20;

                cb.EndText();
                cb.RestoreState();

                pdfDoc.Close();
            }

            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", "attachment;" + "filename=E-mail Encrypted Definition.pdf");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Write(pdfDoc);
            Response.End();

            return View("EncryptEmail");
        }


        //Sending e-mails function
        /*
               public static void SendEncryptedEmail(string[] to, string from, string subject, string body, string[] attachments)
               {
                   MailMessage message = new MailMessage();
                   message.From = new MailAddress(from);
                   message.Subject = subject;

                   body = "Content-Type: text/plain\r\nContent-Transfer-Encoding: 7Bit\r\n\r\n" + body;


                   byte[] messageData = Encoding.ASCII.GetBytes(body);
                   ContentInfo content = new ContentInfo(messageData);
                   EnvelopedCms envelopedCms = new EnvelopedCms(content);
                   CmsRecipientCollection toCollection = new CmsRecipientCollection();

                   foreach (string address in to)
                   {
                       message.To.Add(new MailAddress(address));
                       X509Certificate2 certificate = null; //Need to load from store or from file the client's cert
                       CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, certificate);
                       toCollection.Add(recipient);
                   }

                   envelopedCms.Encrypt(toCollection);
                   byte[] encryptedBytes = envelopedCms.Encode();

                   //add digital signature:
                   SignedCms signedCms = new SignedCms(new ContentInfo(encryptedBytes));
                   X509Certificate2 signerCertificate = null; //Need to load from store or from file the signer's cert
                   CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, signerCertificate);
                   signedCms.ComputeSignature(signer);
                   encryptedBytes = signedCms.Encode();
                   //end digital signature section

                   MemoryStream stream = new MemoryStream(encryptedBytes);
                   AlternateView view = new AlternateView(stream, "application/pkcs7-mime; smime-type=signed-data;name=smime.p7m");
                   message.AlternateViews.Add(view);

                   SmtpClient client = new SmtpClient("your.smtp.mailhost");
                   //add authentication info if required by your smtp server etc...
                   //client.Credentials = CredentialCache.DefaultCredentials;
                   client.Send(message);
               }
       */
    }
}