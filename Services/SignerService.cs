using System;
using System.Xml;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;

namespace SummaCore.Services
{
    public class SignerService
    {
        public string FirmarECF(string xmlSinFirma, X509Certificate2 certificado)
        {
            var doc = new XmlDocument();
            // CRÍTICO: PreserveWhitespace debe ser true
            doc.PreserveWhitespace = true; 
            doc.LoadXml(xmlSinFirma);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = certificado.GetRSAPrivateKey()
            };

            // *** FIX 1: Usar C14N ESTÁNDAR (no exclusivo) ***
            // La DGII requiere: http://www.w3.org/TR/2001/REC-xml-c14n-20010315
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            
            // *** FIX 2: Especificar RSA-SHA256 explícitamente ***
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

            // *** FIX 3: Crear Reference con URI vacío (firma todo el documento) ***
            var reference = new Reference("");
            
            // DigestMethod SHA256 explícito
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
            
            // Transform 1: Enveloped signature
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            
            // Transform 2: C14N estándar (igual que SignedInfo)
            reference.AddTransform(new XmlDsigC14NTransform());
            
            signedXml.AddReference(reference);

            // *** FIX 4: KeyInfo completo con X509Data ***
            var keyInfo = new KeyInfo();
            var keyInfoData = new KeyInfoX509Data(certificado);
            
            // Incluir SOLO el certificado, sin SubjectName ni otros datos
            // que puedan causar conflictos con el validador de DGII
            keyInfoData.AddCertificate(certificado);
            
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // *** FIX 5: Calcular firma ***
            signedXml.ComputeSignature();

            // *** FIX 6: Insertar firma en el documento ***
            var xmlSignature = signedXml.GetXml();
            doc.DocumentElement!.AppendChild(doc.ImportNode(xmlSignature, true));

            // *** FIX 7: Retornar SIN declaración XML ***
            // El OuterXml del DocumentElement no incluye <?xml version...?>
            return doc.DocumentElement.OuterXml;
        }
    }
}