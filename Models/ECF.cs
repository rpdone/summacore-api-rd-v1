using System;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    [XmlRoot(ElementName = "ECF", Namespace = "")]
    public class ECF
    {
        public Encabezado Encabezado { get; set; } = new Encabezado();
        // Detalles, Subtotales, etc. pueden agregarse según necesidad
        // Por ahora nos enfocamos en que la estructura base compile y se firme
    }

    public class Encabezado
    {
        public IdDoc IdDoc { get; set; } = new IdDoc();
        public Emisor Emisor { get; set; } = new Emisor();
        public Comprador Comprador { get; set; } = new Comprador();
        public Totales Totales { get; set; } = new Totales();
    }

    public class IdDoc
    {
        public int TipoeCF { get; set; } // Ej: 31
        public string eNCF { get; set; } = string.Empty; // Ej: E310000000001
        public string FechaVencimientoSecuencia { get; set; } = string.Empty; // dd-MM-yyyy
        public int IndicadorMontoGravado { get; set; } = 0;
        public int TipoIngresos { get; set; } = 1; // 1: Ingresos Operacionales
    }

    public class Emisor
    {
        public string RNCEmisor { get; set; } = string.Empty;
        public string RazonSocialEmisor { get; set; } = string.Empty;
        public string FechaEmision { get; set; } = string.Empty; // dd-MM-yyyy
    }

    public class Comprador
    {
        public string RNCComprador { get; set; } = string.Empty;
        public string RazonSocialComprador { get; set; } = string.Empty;
    }

    public class Totales
    {
        public decimal MontoTotal { get; set; }
    }
}