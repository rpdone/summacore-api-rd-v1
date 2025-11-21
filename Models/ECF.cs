using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    [XmlRoot(ElementName = "ECF", Namespace = "")]
    public class ECF
    {
        [XmlElement(Order = 1)]
        public Encabezado Encabezado { get; set; } = new Encabezado();

        [XmlElement(Order = 2)]
        public DetallesItems DetallesItems { get; set; } = new DetallesItems();

        [XmlElement(Order = 3)]
        public Subtotales Subtotales { get; set; } = new Subtotales();

        [XmlElement(Order = 4)]
        public string FechaHoraFirma { get; set; } = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
    }

    public class Encabezado
    {
        [XmlElement(Order = 1)]
        public string Version { get; set; } = "1.0";

        [XmlElement(Order = 2)]
        public IdDoc IdDoc { get; set; } = new IdDoc();

        [XmlElement(Order = 3)]
        public Emisor Emisor { get; set; } = new Emisor();

        [XmlElement(Order = 4)]
        public Comprador Comprador { get; set; } = new Comprador();

        [XmlElement(Order = 5)]
        public Totales Totales { get; set; } = new Totales();
    }

    public class IdDoc
    {
        [XmlElement(Order = 1)]
        public int TipoeCF { get; set; }
        
        [XmlElement(Order = 2)]
        public string eNCF { get; set; }
        
        [XmlElement(Order = 3)]
        public string FechaVencimientoSecuencia { get; set; }
        
        [XmlElement(Order = 4)]
        public int IndicadorMontoGravado { get; set; }
        
        [XmlElement(Order = 5)]
        public int TipoIngresos { get; set; }
        
        [XmlElement(Order = 6)]
        public int TipoPago { get; set; }
        
        [XmlElement(Order = 7)]
        public string FechaLimitePago { get; set; }
    }

    public class Emisor
    {
        [XmlElement(Order = 1)]
        public string RNCEmisor { get; set; }

        [XmlElement(Order = 2)]
        public string RazonSocialEmisor { get; set; }

        [XmlElement(Order = 3)]
        public string NombreComercial { get; set; }

        [XmlElement(Order = 4)]
        public string Sucursal { get; set; }

        [XmlElement(Order = 5)]
        public string DireccionEmisor { get; set; } = "Dirección Principal"; // Obligatorio

        [XmlElement(Order = 6)]
        public string Municipio { get; set; } = "10101"; // Valor por defecto para evitar error (Sto Dgo)

        [XmlElement(Order = 7)]
        public string Provincia { get; set; } = "10100"; // Valor por defecto para evitar error

        [XmlElement(Order = 18)] // Fecha va al final de la sección Emisor
        public string FechaEmision { get; set; }
    }

    public class Comprador
    {
        [XmlElement(Order = 1)]
        public string RNCComprador { get; set; }
        
        [XmlElement(Order = 2)]
        public string RazonSocialComprador { get; set; }
    }

    public class Totales
    {
        [XmlElement(Order = 1)]
        public decimal MontoGravadoTotal { get; set; }

        [XmlElement(Order = 2)]
        public decimal MontoGravadoI1 { get; set; }

        [XmlElement(Order = 3)]
        public decimal MontoGravadoI2 { get; set; }

        [XmlElement(Order = 5)]
        public decimal MontoExento { get; set; }

        // RECUPERADOS: ITBIS1 e ITBIS2 son necesarios antes de los totales
        [XmlElement(Order = 6)]
        public decimal ITBIS1 { get; set; }

        [XmlElement(Order = 7)]
        public decimal ITBIS2 { get; set; }

        [XmlElement(Order = 9)]
        public decimal TotalITBIS { get; set; }

        [XmlElement(Order = 10)]
        public decimal TotalITBIS1 { get; set; }

        [XmlElement(Order = 11)]
        public decimal TotalITBIS2 { get; set; }

        // MontoTotal DEBE ir después de todos los impuestos (posición 15 según XSD)
        [XmlElement(Order = 15)]
        public decimal MontoTotal { get; set; }
    }

    public class DetallesItems
    {
        [XmlElement("Item")]
        public List<Item> Item { get; set; } = new List<Item>();
    }

    public class Item
    {
        [XmlElement(Order = 1)]
        public int NumeroLinea { get; set; }

        [XmlElement(Order = 3)]
        public int IndicadorFacturacion { get; set; }

        [XmlElement(Order = 5)]
        public string NombreItem { get; set; }

        [XmlElement(Order = 6)]
        public int IndicadorBienoServicio { get; set; }

        [XmlElement(Order = 8)]
        public decimal CantidadItem { get; set; }

        [XmlElement(Order = 15)] 
        public decimal PrecioUnitarioItem { get; set; }

        [XmlArray("TablaImpuestoAdicional", Order = 19)]
        [XmlArrayItem("ImpuestoAdicional")]
        public List<ImpuestoAdicional> TablaImpuestoAdicional { get; set; }

        [XmlElement(Order = 21)]
        public decimal MontoItem { get; set; }
    }

    public class ImpuestoAdicional
    {
        public int TipoImpuesto { get; set; }
    }

    public class Subtotales
    {
        [XmlElement("Subtotal")]
        public List<Subtotal> Subtotal { get; set; } = new List<Subtotal>();
    }

    public class Subtotal
    {
        [XmlElement(Order = 1)]
        public int NumeroSubTotal { get; set; }
        
        [XmlElement(Order = 2)]
        public string DescripcionSubtotal { get; set; }
        
        [XmlElement(Order = 3)]
        public int Orden { get; set; }
        
        [XmlElement(Order = 4)]
        public decimal SubTotalMontoGravadoTotal { get; set; }
        
        [XmlElement(Order = 5)]
        public decimal SubTotalMontoGravadoI1 { get; set; }
        
        [XmlElement(Order = 6)]
        public decimal SubTotalMontoGravadoI2 { get; set; }
        
        [XmlElement(Order = 13)]
        public decimal SubTotalExento { get; set; }
        
        [XmlElement(Order = 8)]
        public decimal SubTotaITBIS { get; set; }
        
        [XmlElement(Order = 9)]
        public decimal SubTotaITBIS1 { get; set; }
        
        [XmlElement(Order = 10)]
        public decimal SubTotaITBIS2 { get; set; }
        
        [XmlElement(Order = 14)]
        public decimal MontoSubTotal { get; set; }
    }
}