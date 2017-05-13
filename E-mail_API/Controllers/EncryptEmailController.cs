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
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.html;
using System.Reflection;
using System.Activities;

namespace E_mail_API.Controllers
{
    public class EncryptEmailController : Controller
    {
        RSACryptoServiceProvider myRSA = new RSACryptoServiceProvider(2048);
        RSACryptoServiceProvider rsa_CryptoService = new RSACryptoServiceProvider();

        [HttpGet]
        public ActionResult Index()
        {
            if (TempData["clicked"] == null)
            {
                TempData["sErrMsg"] = "Press 'Create encrypted e-mail' on Home Page!";

                return View("~/Views/Home/Index.cshtml");
            }

            String head, content, footer;

            ViewBag.Title = "Encrypted E-mail Definition";

            E_mailContent model = (E_mailContent)TempData["model"];
            head = model.headSection;
            content = model.contentSection;
            footer = model.footerSection;

            var head_encrypt = head;
            var content_encrypt = content;
            var footer_encrypt = footer;

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

            EncodedEmail encodedModel = new EncodedEmail();
            encodedModel.encodedheadSection = cypherHead;
            encodedModel.encodedcontentSection = cypherContent;
            encodedModel.encodedfooterSection = cypherFooter;

            Session["encodedModel"] = encodedModel;

            ViewBag.head = cypherHead;
            ViewBag.content = cypherContent;
            ViewBag.footer = cypherFooter;

            return View("EncryptEmail", model);
        }

        [HttpPost]
        [MultipleButton(MatchFormKey= "action", MatchFormValue= "exportKey")] 
        public ActionResult exportKey()
        {
            var parameters = myRSA.ExportParameters(true);

            if (myRSA.PublicOnly)
            {
                TempData["sErrMsg"] = "RSACryptoServiceProvider does not contain private key!";
            }

            StringWriter oStringWriter = new StringWriter();

            Response.ContentType = "application/txt";
            Response.AddHeader("content-disposition", "attachment;filename=Private_Key.txt");
            Response.Clear();

            using (TextWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                ExportPrivateKey(myRSA, writer);
            }

            Response.End();

            return View("EncryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "exportEncodedMessage")]
        public ActionResult exportEncodedMessage()
        {
            EncodedEmail encodedModel = (EncodedEmail)Session["encodedModel"];
            string headSection = encodedModel.encodedheadSection;
            string contentSection = encodedModel.encodedcontentSection;
            string footerSection = encodedModel.encodedfooterSection;

            StringWriter oStringWriter = new StringWriter();

            Response.ContentType = "application/txt";
            Response.AddHeader("content-disposition", "attachment;filename=Encoded_Message.txt");
            Response.Clear();

            using (TextWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                writer.WriteLine("Header: " + headSection);
                writer.WriteLine("Content: " + contentSection);
                writer.WriteLine("Footer: " + footerSection);
            }

            Response.End();

            return View("EncryptEmail");
        }

        private static void ExportPrivateKey(RSACryptoServiceProvider csp, TextWriter outputStream)
        {
            var parameters = csp.ExportParameters(true);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30);
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 });
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.WriteLine(base64, i, Math.Min(64, base64.Length - i));
                }
                outputStream.WriteLine("-----END RSA PRIVATE KEY-----");
            }
        }
        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }
        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02);
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
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