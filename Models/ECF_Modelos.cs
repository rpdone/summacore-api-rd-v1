// Models/ECF_Modelos.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;

namespace SummaCore.Models
{
    [XmlRoot("ECF")]
    public class ECF
    {
        [XmlElement(Order = 1)] public Encabezado Encabezado { get; set; } = new();
        [XmlElement("DetallesItems", Order = 2)] public DetallesItems DetallesItems { get; set; } = new();
        [XmlElement(Order = 3)] public Subtotales? Subtotales { get; set; }
        [XmlElement(Order = 4)] public DescuentosORecargos? DescuentosORecargos { get; set; }
        [XmlElement(Order = 5)] public Paginacion? Paginacion { get; set; }
        [XmlElement(Order = 6)] public InformacionReferencia? InformacionReferencia { get; set; }
        [XmlElement(Order = 7)] public List<TablaAdicional>? TablaAdicional { get; set; }
        [XmlElement(Order = 8)] [Required] public string FechaHoraFirma { get; set; } = string.Empty;
        [XmlElement(Order = 99)] public Signature Signature { get; set; } = new();

        public bool ShouldSerializeDetallesItems() => DetallesItems?.Item?.Any() == true;
        public bool ShouldSerializeSubtotales() => Subtotales?.Subtotal?.Any() == true;
        public bool ShouldSerializeDescuentosORecargos() => DescuentosORecargos?.DescuentoORecargo?.Any() == true;
        public bool ShouldSerializePaginacion() => Paginacion?.Pagina?.Any() == true;
        public bool ShouldSerializeInformacionReferencia() => InformacionReferencia != null && !string.IsNullOrEmpty(InformacionReferencia.NCFModificado);
        public bool ShouldSerializeTablaAdicional() => TablaAdicional?.Any() == true;
    }

    public class Encabezado
    {
        [XmlElement(Order = 1)] public VersionType Version { get; set; } = new();
        [XmlElement(Order = 2)] public IdDoc IdDoc { get; set; } = new();
        [XmlElement(Order = 3)] public Emisor Emisor { get; set; } = new();
        [XmlElement(Order = 4)] public Comprador Comprador { get; set; } = new();
        [XmlElement(Order = 5)] public InformacionesAdicionales? InformacionesAdicionales { get; set; }
        [XmlElement(Order = 6)] public Transporte? Transporte { get; set; }
        [XmlElement(Order = 7)] public Totales Totales { get; set; } = new();
        [XmlElement(Order = 8)] public OtraMoneda? OtraMoneda { get; set; }
        [XmlElement(Order = 9)] public string? CodigoSeguridadeCF { get; set; }
       
    }

    public class VersionType { [XmlText] public string Value { get; set; } = "1.0"; }

    public class IdDoc
    {
        [XmlElement(Order = 1)] [Required] public int TipoeCF { get; set; }
        [XmlElement(Order = 2)] [Required] public string eNCF { get; set; } = string.Empty;
        [XmlElement(Order = 3)] public string FechaEmision { get; set; } = string.Empty;
        [XmlElement(Order = 4)] public string? FechaVencimientoSecuencia { get; set; }
        [XmlElement(Order = 5)] public string? TipoIngresos { get; set; }
        [XmlElement(Order = 6)] public int? TipoPago { get; set; }
    }

    public class Emisor
    {
        [XmlElement(Order = 1)] [Required] public string RNCEmisor { get; set; } = string.Empty;
        [XmlElement(Order = 2)] [Required] public string RazonSocialEmisor { get; set; } = string.Empty;
        [XmlElement(Order = 3)] public string? NombreComercial { get; set; }
        [XmlElement(Order = 4)] public string? DireccionEmisor { get; set; }
        [XmlElement(Order = 5)] public string Municipio { get; set; } = string.Empty;
        [XmlElement(Order = 6)] public string Provincia { get; set; } = string.Empty;
    }

    public class Comprador
    {
        [XmlElement(Order = 1)] public string? RNCComprador { get; set; }
        [XmlElement(Order = 2)] public string? IdentificadorExtranjero { get; set; }
        [XmlElement(Order = 3)] public string? RazonSocialComprador { get; set; }
    }

    public class Totales
    {
        [XmlElement(Order = 1)] public decimal? MontoGravadoTotal { get; set; }
        [XmlElement(Order = 2)] public decimal? MontoGravadoI1 { get; set; }
        [XmlElement(Order = 4)] public decimal? MontoExento { get; set; }
        [XmlElement(Order = 5)] public decimal? TotalITBIS { get; set; }
        [XmlElement(Order = 6)] public decimal? TotalITBIS1 { get; set; }
        [XmlElement(Order = 8)] public decimal? MontoTotal { get; set; }
    }

    public class DetallesItems
    {
        [XmlElement("Item")] public List<Item> Item { get; set; } = new();
    }

    public class Item
    {
        [XmlElement(Order = 1)] public int NumeroLinea { get; set; }
        [XmlElement("TablaCodigosItem", Order = 2)] public TablaCodigosItem TablaCodigosItem { get; set; } = new();
        [XmlElement(Order = 3)] public int IndicadorFacturacion { get; set; } = 1;
        [XmlElement(Order = 4)] public string? NombreItem { get; set; }
        [XmlElement(Order = 5)] public decimal? CantidadItem { get; set; }
        [XmlElement(Order = 6)] public decimal? PrecioUnitarioItem { get; set; }
        [XmlElement(Order = 7)] public decimal MontoItem { get; set; }
    }

    public class TablaCodigosItem
    {
        [XmlElement("CodigosItem")] public List<CodigoItem> CodigosItem { get; set; } = new();
    }

    public class CodigoItem
    {
        [XmlText] public string Value { get; set; } = "001";
    }

    public class InformacionReferencia
    {
        [XmlElement(Order = 1)] public string? NCFModificado { get; set; }
        [XmlElement(Order = 3)] public string? FechaNCFModificado { get; set; }
        [XmlElement(Order = 4)] public int? CodigoModificacion { get; set; }
    }

    public class Subtotales { public List<object> Subtotal { get; set; } = new(); }
    public class DescuentosORecargos { public List<object> DescuentoORecargo { get; set; } = new(); }
    public class Paginacion { public List<object> Pagina { get; set; } = new(); }
    public class TablaAdicional { }
    public class InformacionesAdicionales { }
    public class Transporte { }
    public class OtraMoneda { }
    public class Signature { [XmlAttribute] public string Id { get; set; } = "Signature"; }
}