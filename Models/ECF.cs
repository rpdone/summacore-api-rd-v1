using System.Xml.Serialization;

namespace SummaCore.Models
{
    [XmlRoot(ElementName = "ECF", Namespace = "")]
    public class ECF
    {
        public Encabezado Encabezado { get; set; }
        // Detalles, Subtotales, etc. pueden agregarse según necesidad
        // Por ahora nos enfocamos en que la estructura base compile y se firme
    }

    public class Encabezado
    {
        public IdDoc IdDoc { get; set; }
        public Emisor Emisor { get; set; }
        public Comprador Comprador { get; set; }
        public Totales Totales { get; set; }
    }

    public class IdDoc
    {
        public int TipoeCF { get; set; } // Ej: 31
        public string eNCF { get; set; } // Ej: E310000000001
        public string FechaVencimientoSecuencia { get; set; } // dd-MM-yyyy
        public int IndicadorMontoGravado { get; set; } = 0;
        public int TipoIngresos { get; set; } = 1; // 1: Ingresos Operacionales
    }

    public class Emisor
    {
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string FechaEmision { get; set; } // dd-MM-yyyy
    }

    public class Comprador
    {
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }
    }

    public class Totales
    {
        public decimal MontoTotal { get; set; }
    }
}