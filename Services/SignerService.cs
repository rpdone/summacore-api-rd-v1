using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace SummaCore.Services
{
    public class SignerService
    {
        public string FirmarECF(string xmlSinFirma, X509Certificate2 certificado)
        {
            var doc = new XmlDocument();
            // IMPORTANTE: PreserveWhitespace true es vital para que la firma sea matemáticamente válida
            doc.PreserveWhitespace = true; 
            doc.LoadXml(xmlSinFirma);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = certificado.GetRSAPrivateKey()
            };

            // 1. Configurar Canonicalización Exclusiva (Estándar DGII)
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

            // 2. Referencia (Firmar todo el documento)
            var reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform()); 
            signedXml.AddReference(reference);

            // 3. KeyInfo (Solo X509Data limpio, sin SubjectName para evitar conflictos)
            var keyInfo = new KeyInfo();
            var keyInfoData = new KeyInfoX509Data(certificado);
            // No agregamos SubjectName para reducir riesgo de "Archivo no válido"
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;

            // 4. Calcular firma
            signedXml.ComputeSignature();

            // 5. Insertar firma
            var xmlSignature = signedXml.GetXml();
            doc.DocumentElement!.AppendChild(doc.ImportNode(xmlSignature, true));

            // TRUCO: Retornar OuterXml limpio.
            // Si hubiera declaración <?xml...?> al principio, a veces conviene quitarla para endpoints SOAP/REST viejos,
            // pero OuterXml del elemento raíz suele ser seguro.
            return doc.OuterXml;
        }
    }
}