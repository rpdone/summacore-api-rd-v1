using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using SummaCore.Models;

namespace SummaCore.Services
{
    public class SignerService
    {
        public string FirmarECF(string xmlSinFirma, X509Certificate2 certificado)
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true; // VITAL: Si borras espacios, la firma se rompe
            doc.LoadXml(xmlSinFirma);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = certificado.GetRSAPrivateKey()
            };

            // Referencia al documento entero
            var reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform()); // La firma va DENTRO del XML
            signedXml.AddReference(reference);

            // Información de la llave (KeyInfo) - Requerido por DGII
            var keyInfo = new KeyInfo();
            // DGII pide X509Data con SubjectName y Certificate
            var keyInfoData = new KeyInfoX509Data(certificado);
            keyInfoData.AddSubjectName(certificado.Subject); 
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // Calcular firma
            signedXml.ComputeSignature();

            // Insertar la firma en el XML
            var xmlSignature = signedXml.GetXml();
            doc.DocumentElement!.AppendChild(doc.ImportNode(xmlSignature, true));

            return doc.OuterXml;
        }
    }
}