using System.Security.Cryptography.X509Certificates;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using iTextSharp.text.pdf.security;
using System.Text;


namespace DesktopDocumentSigner
{
    internal class DocumentSigner
    {
        private const string FIELD_NAME = "Digitally signed";
        private string _certificateThumbprint;
        private string _encoded_file; 
        private string _documentName;
        private string _location;
        private PdfReader _inputPdf;
        private readonly iTextSharp.text.Rectangle PAGE_RECT = new iTextSharp.text.Rectangle(375, 565, 575, 715);
        private BaseFont _base_font;
        private byte[] file;

        public DocumentSigner(string certificateThumbprint, string encodedFile, string documentName, string location)
        {
            _certificateThumbprint = certificateThumbprint;
            _encoded_file = encodedFile;
            _documentName = documentName;
            _location = location;
            file = Convert.FromBase64String(_encoded_file);
            _inputPdf = new PdfReader(file);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _base_font = BaseFont.CreateFont("OpenSans-Regular.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        }

        public static X509Certificate2? GetCertificateFromStore(string certificateThumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certificate2Collection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);
            if (certificate2Collection.Count == 0)
            {
                return null;
            }
            else
            {
                foreach (X509Certificate2 certificate in certificate2Collection)
                {
                    if (certificate.HasPrivateKey && certificate.Thumbprint.ToLower() == certificateThumbprint)
                    {
                        return certificate;
                    }
                }
            }
            store.Close();
            return null;
        }

        private int CalculatePage()
        {
            if (_documentName.Contains("ORDER_WITH_EIF")) 
            { 
                return 1;
            }
            else
            {
                return _inputPdf.NumberOfPages;
            }
        }

        public SignatureResult SignDocument()
        {
            X509Certificate2? certificate = GetCertificateFromStore(_certificateThumbprint);
            if (certificate != null)
            {
                string resultText = "";
                IList<X509Certificate> chain = new List<X509Certificate>();
                MemoryStream signedPdf = new MemoryStream();
                IExternalSignature externalSignature = new X509Certificate2Signature(certificate, "SHA-256");
                PdfStamper pdfStamper = PdfStamper.CreateSignature(_inputPdf, signedPdf, '\0');
                PdfSignatureAppearance signatureAppearance = pdfStamper.SignatureAppearance;
                pdfStamper.AcroFields.AddSubstitutionFont(_base_font);
                signatureAppearance.Layer2Font = new iTextSharp.text.Font(_base_font, 12f);
                signatureAppearance.Location = _location;
                try
                {
                    signatureAppearance.SetVisibleSignature(PAGE_RECT, CalculatePage(), FIELD_NAME);
                }
                catch (ArgumentException e)
                {
                    resultText = $"Dokument {_documentName} již byl podepsán, vrácen beze změny ({DateTime.Now}).";
                    signedPdf = new MemoryStream(file);
                }
                signatureAppearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
                chain.Add(DotNetUtilities.FromX509Certificate(certificate));
                MakeSignature.SignDetached(signatureAppearance, externalSignature, chain, null, null, null, 0, CryptoStandard.CMS);
                _inputPdf.Close();
                pdfStamper.Close();
                string signedPDF_base64 = Convert.ToBase64String(signedPdf.ToArray());
                SignatureResult signatureResult = new SignatureResult{
                    ResultText = resultText,
                    SignedDocumentBase64 = signedPDF_base64

                };
                return signatureResult;
            }
            else
            {
                return new SignatureResult{ ResultText = $"Dokument {_documentName} není možné podepsat." };
            }
        }
    }
}
