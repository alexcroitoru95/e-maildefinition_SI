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
        CspParameters csp = new CspParameters();

        const string DecrFolder = @"F:\University\SI_2\Proiect SI\e-maildefinition_SI\E-mail_API\App_Data\Decrypt\";
        const string EncrFolder = @"F:\University\SI_2\Proiect SI\e-maildefinition_SI\E-mail_API\App_Data\Encrypt\";

        public ActionResult Index()
        {
            ViewBag.Title = "Decrypt E-mail Definition";

            return View("DecryptEmail");
        }

        [HttpPost]
        public ActionResult ImportKey()
        {
            var fileName = "";
            var path = "";

            String keytxt, keyName;

            TempData["sErrMsg"] = "RSACryptoServiceProvider does not contain private key!";

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

            if(fileName != null)
            {
                StreamReader stream = new StreamReader(path);
                keytxt = stream.ReadToEnd();

                keyName = "Key01";

                csp.KeyContainerName = keyName;

                myRSA = new RSACryptoServiceProvider(csp);

                myRSA.PersistKeyInCsp = true;

                myRSA.FromXmlString(keytxt);

            }

            return View("DecryptEmail");
        }

        [HttpPost]
        public ActionResult ImportMessage()
        {
            var fileName = "";
            var path = "";

            TempData["sErrMsg"] = "RSACryptoServiceProvider does not contain private key!";

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

            if(fileName != null)
            {
                DecryptFile(fileName);
            }

            return View("DecryptEmail");
        }

        private void DecryptFile(string inFile)
        {
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;

            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            string outFile = DecrFolder + inFile.Substring(0, inFile.LastIndexOf(".")) + ".txt";

            using (FileStream inFs = new FileStream(EncrFolder + inFile, FileMode.Open))
            {
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);

                byte[] KeyDecrypted = myRSA.Decrypt(KeyEncrypted, false);

                ICryptoTransform transform = rjndl.CreateDecryptor(KeyDecrypted, IV);

                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {

                    int count = 0;
                    int offset = 0;

                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];

                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);

                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }
        }
    }
}