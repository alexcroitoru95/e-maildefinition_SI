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
        RSACryptoServiceProvider myRSA = new RSACryptoServiceProvider();
        CspParameters csp = new CspParameters();

        const string EncrFolder = @"F:\University\SI_2\Proiect SI\e-maildefinition_SI\E-mail_API\App_Data\Encrypt\";

        [HttpGet]
        public ActionResult Index()
        {
            String keyName;

            ViewBag.Title = "Encrypted E-mail Definition";

            keyName = "Key01";

            csp.KeyContainerName = keyName;
            myRSA = new RSACryptoServiceProvider(csp);
            myRSA.PersistKeyInCsp = true;

            return View("EncryptEmail");
        }

        [HttpPost]
        public ActionResult ImportMessage()
        {
            var fileName = "";
            var path = "";

            foreach (string file in Request.Files)
            {
                var hpf = this.Request.Files[file];
                if (hpf.ContentLength == 0)
                {
                    TempData["sErrMsg"] = "Empty file!";
                }

                path = Path.Combine(Server.MapPath("~/App_Data/Upload"), Path.GetFileName(hpf.FileName));
                hpf.SaveAs(path);

                fileName = Path.GetFileName(hpf.FileName);

            }

            if (fileName != null)
            {
                EncryptFile(path);
            }
            
            return View("EncryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "exportEncodedMessage")]
        public ActionResult exportEncodedMessage()
        {
            return View("EncryptEmail");
        }

        [HttpPost]
        [MultipleButton(MatchFormKey = "action", MatchFormValue = "exportRSAPublicKey")]
        public ActionResult exportRSAPublicKey()
        {
            StringWriter oStringWriter = new StringWriter();

            Response.ContentType = "application/txt";
            Response.AddHeader("content-disposition", "attachment;filename=PublicKey.txt");
            Response.Clear();

            using (TextWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                writer.Write(myRSA.ToXmlString(false));
                writer.Close();
            }

            Response.End();

            return View("EncryptEmail");
        }

        private void EncryptFile(string inFile)
        {
            var private_key_RSA = myRSA.ExportParameters(true);

            Session["private_key"] = private_key_RSA;

            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjndl.CreateEncryptor();

            byte[] keyEncrypted = myRSA.Encrypt(rjndl.Key, false);

            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);
            int lIV = rjndl.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            int startFileName = inFile.LastIndexOf("\\") + 1;
            string outFile = EncrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".enc";

            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {
                outFs.Write(LenK, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(rjndl.IV, 0, lIV);

                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {
                    int count = 0;
                    int offset = 0;

                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using (FileStream inFs = new FileStream(inFile, FileMode.Open))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
            }
        }

    }
}