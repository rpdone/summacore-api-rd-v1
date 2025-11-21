using System;
using System.IO;
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
    doc.PreserveWhitespace = false; // Importante: Mantiene la limpieza igual que TS
    
    var settings = new XmlReaderSettings { IgnoreComments = true };
    using (var reader = XmlReader.Create(new StringReader(xmlSinFirma), settings))
    {
        doc.Load(reader);
    }

    // --- Lógica de Firma (Igual que antes) ---
    var signedXml = new SignedXml(doc)
    {
        SigningKey = certificado.GetRSAPrivateKey()
    };

    signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
    signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

    var reference = new Reference("");
    reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
    reference.AddTransform(new XmlDsigC14NTransform());
    
    signedXml.AddReference(reference);

    var keyInfo = new KeyInfo();
    var keyInfoData = new KeyInfoX509Data(certificado);
    keyInfoData.AddCertificate(certificado);
    keyInfo.AddClause(keyInfoData);
    signedXml.KeyInfo = keyInfo;

    signedXml.ComputeSignature();
    var xmlSignature = signedXml.GetXml();
    
    if (doc.DocumentElement != null)
    {
        doc.DocumentElement.AppendChild(doc.ImportNode(xmlSignature, true));
        
        // =========================================================
        // CORRECCIÓN FINAL: CONDICIONAL PARA SEMILLA VS ECF
        // =========================================================
        
        // Caso 1: Si es SEMILLA, NO queremos declaración XML (rompe la validación DGII)
        if (doc.DocumentElement.Name == "SemillaModel")
        {
            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }
        }
        // Caso 2: Si es ECF (Factura), SÍ necesitamos UTF-8 forzado
        else 
        {
            if (doc.FirstChild is XmlDeclaration dec)
            {
                dec.Encoding = "UTF-8";
            }
            else
            {
                var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.InsertBefore(xmlDeclaration, doc.DocumentElement);
            }
        }
    }

    return doc.OuterXml;
}
    }
}