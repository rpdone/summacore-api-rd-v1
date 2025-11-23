using System;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    /// <summary>
    /// Acuse de Recibo de e-CF (ARECF)
    /// Se genera al recibir un e-CF como Receptor
    /// </summary>
    [XmlRoot("ARECF")]
    public class ARECF
    {
        [XmlElement(Order = 1)]
        public VersionType Version { get; set; } = new VersionType { Value = "1.0" };

        [XmlElement(Order = 2)]
        public string RNCEmisor { get; set; } = string.Empty;

        [XmlElement(Order = 3)]
        public string RNCComprador { get; set; } = string.Empty;

        [XmlElement(Order = 4)]
        public string eNCF { get; set; } = string.Empty;

        [XmlElement(Order = 5)]
        public string FechaEmision { get; set; } = string.Empty;

        [XmlElement(Order = 6)]
        public string FechaRecepcion { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 7)]
        public int Estado { get; set; } = 1; // 1 = Recibido, 2 = Rechazado

        [XmlElement(Order = 8)]
        public string? MotivoRechazo { get; set; }

        [XmlElement(Order = 9)]
        public string CodigoSeguridadAR { get; set; } = string.Empty;

        [XmlElement(Order = 10)]
        public string FechaHoraFirma { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 99)]
        public Signature Signature { get; set; } = new Signature();

        public bool ShouldSerializeMotivoRechazo() => !string.IsNullOrEmpty(MotivoRechazo);
    }
}