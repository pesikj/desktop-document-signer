using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using iTextSharp.text.pdf.security;


namespace DesktopDocumentSigner
{
    internal class DocumentSigner
    {
        private string location = "Location";

        private MemoryStream SignDocument(byte[] file, string document_name)
        {

            X509Certificate2 certificate = GetCertificateFromStore();
            if (certificate != null)
            {
                var arialBaseFont = BaseFont.CreateFont("ArialCE.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                IList<X509Certificate> chain = new List<X509Certificate>();
                PdfReader inputPdf = new PdfReader(file);
                using (MemoryStream signedPdf = new MemoryStream())
                {
                    IExternalSignature externalSignature = new X509Certificate2Signature(certificate, "SHA-256");
                    PdfStamper pdfStamper = PdfStamper.CreateSignature(inputPdf, signedPdf, '\0');
                    PdfSignatureAppearance signatureAppearance = pdfStamper.SignatureAppearance;
                    pdfStamper.AcroFields.AddSubstitutionFont(arialBaseFont);
                    signatureAppearance.Layer2Font = new iTextSharp.text.Font(arialBaseFont, 12f);
                    signatureAppearance.Location = location;
                    if (document_name.Contains("DRIVER_CALL"))
                    {
                        try
                        {
                            signatureAppearance.SetVisibleSignature(new iTextSharp.text.Rectangle(350, 50, 550, 200), inputPdf.NumberOfPages - 1, "Digitally signed");
                        }
                        catch (ArgumentException e)
                        {
                            return new MemoryStream(file);
                        }
                    }
                    else if (document_name.Contains("ORDER_WITH_EIF"))
                    {
                        try
                        {
                            signatureAppearance.SetVisibleSignature(new iTextSharp.text.Rectangle(375, 565, 575, 715), 1, "Digitally signed");
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"Dokument {document_name} již byl podepsán, vrácen beze změny ({DateTime.Now}).");
                            return new MemoryStream(file);
                        }
                    }
                    else
                    {
                        try
                        {
                            signatureAppearance.SetVisibleSignature(new iTextSharp.text.Rectangle(350, 50, 550, 200), inputPdf.NumberOfPages, "Digitally signed");
                        }
                        catch (ArgumentException e)
                        {
                            Console.WriteLine($"Dokument {document_name} již byl podepsán, vrácen beze změny ({DateTime.Now}).");
                            return new MemoryStream(file);
                        }

                    }
                    signatureAppearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
                    chain.Add(DotNetUtilities.FromX509Certificate(certificate));
                    MakeSignature.SignDetached(signatureAppearance, externalSignature, chain, null, null, null, 0, CryptoStandard.CMS);
                    inputPdf.Close();
                    pdfStamper.Close();

                    Console.WriteLine($"Dokument {document_name} byl podepsán ({DateTime.Now}).");
                    return signedPdf;
                }
            }
            return new MemoryStream();
        }

        private X509Certificate2 GetCertificateFromStore()
        {
            throw new NotImplementedException();
        }
    }
}
