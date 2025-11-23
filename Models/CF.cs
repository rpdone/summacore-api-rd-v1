using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    /// <summary>
    /// Aprobación Comercial de e-CF (ACECF)
    /// </summary>
    [XmlRoot("ACECF")]
    public class ACECF
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
        public string FechaAprobacion { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 7)]
        public int EstadoAprobacion { get; set; } // 1 = Aprobado, 2 = Rechazado

        [XmlElement(Order = 8)]
        public string? MotivoRechazo { get; set; }

        [XmlElement(Order = 9)]
        public string FechaHoraFirma { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 99)]
        public Signature Signature { get; set; } = new Signature();

        public bool ShouldSerializeMotivoRechazo() => !string.IsNullOrEmpty(MotivoRechazo);
    }

    /// <summary>
    /// Anulación de e-NCF (ANECF)
    /// </summary>
    [XmlRoot("ANECF")]
    public class ANECF
    {
        [XmlElement(Order = 1)]
        public VersionType Version { get; set; } = new VersionType { Value = "1.0" };

        [XmlElement(Order = 2)]
        public string RNCEmisor { get; set; } = string.Empty;

        [XmlElement(Order = 3)]
        public int TipoAnulacion { get; set; } // 1 = Rango, 2 = Individual

        [XmlElement(Order = 4)]
        public List<RangoAnulacion> RangosAnulacion { get; set; } = new();

        [XmlElement(Order = 5)]
        public string FechaAnulacion { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 6)]
        public string FechaHoraFirma { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 99)]
        public Signature Signature { get; set; } = new Signature();
    }

    public class RangoAnulacion
    {
        [XmlElement(Order = 1)]
        public string eNCFDesde { get; set; } = string.Empty;

        [XmlElement(Order = 2)]
        public string eNCFHasta { get; set; } = string.Empty;

        [XmlElement(Order = 3)]
        public string MotivoAnulacion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resumen Factura de Consumo Electrónica (RFCE)
    /// Para facturas < DOP $250,000
    /// </summary>
    [XmlRoot("RFCE")]
    public class RFCE
    {
        [XmlElement(Order = 1)]
        public VersionType Version { get; set; } = new VersionType { Value = "1.0" };

        [XmlElement(Order = 2)]
        public EncabezadoRFCE Encabezado { get; set; } = new();

        [XmlElement(Order = 3)]
        public List<DetalleRFCE> DetalleVentas { get; set; } = new();

        [XmlElement(Order = 4)]
        public string FechaHoraFirma { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        [XmlElement(Order = 99)]
        public Signature Signature { get; set; } = new Signature();
    }

    public class EncabezadoRFCE
    {
        [XmlElement(Order = 1)]
        public string RNCEmisor { get; set; } = string.Empty;

        [XmlElement(Order = 2)]
        public string PeriodoResumen { get; set; } = string.Empty; // Formato: YYYYMM

        [XmlElement(Order = 3)]
        public string FechaEmision { get; set; } = string.Empty;

        [XmlElement(Order = 4)]
        public int CantidadRegistros { get; set; }

        [XmlElement(Order = 5)]
        public decimal MontoTotalGravado { get; set; }

        [XmlElement(Order = 6)]
        public decimal MontoTotalExento { get; set; }

        [XmlElement(Order = 7)]
        public decimal MontoTotalITBIS { get; set; }

        [XmlElement(Order = 8)]
        public decimal MontoTotal { get; set; }
    }

    public class DetalleRFCE
    {
        [XmlElement(Order = 1)]
        public int NumeroLinea { get; set; }

        [XmlElement(Order = 2)]
        public string eNCF { get; set; } = string.Empty;

        [XmlElement(Order = 3)]
        public string FechaEmision { get; set; } = string.Empty;

        [XmlElement(Order = 4)]
        public decimal MontoGravado { get; set; }

        [XmlElement(Order = 5)]
        public decimal MontoExento { get; set; }

        [XmlElement(Order = 6)]
        public decimal MontoITBIS { get; set; }

        [XmlElement(Order = 7)]
        public decimal MontoTotal { get; set; }
    }
}