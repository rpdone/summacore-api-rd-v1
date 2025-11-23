// Models/CsvEcfDto.cs
using System.Collections.Generic;

namespace SummaCore.Models
{
    public class CsvEcfDto
    {
        // === IDENTIFICACIÓN ===
        public string? Encf { get; set; }
        public int TipoeCF { get; set; } = 31;
        
        public string? FechaVencimientoSecuencia { get; set; }
        public int? TipoIngresos { get; set; }
        public int? TipoPago { get; set; }
        public string? FechaLimitePago { get; set; }
        public string? NCFModificado { get; set; }
public string? FechaNCFModificado { get; set; }

        // === EMISOR ===
        public string? RNCEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
        public string? NombreComercial { get; set; }
        public string? DireccionEmisor { get; set; }
        public string? Municipio { get; set; }
        public string? Provincia { get; set; }
        public string? FechaEmision { get; set; }

        // === COMPRADOR ===
        public string? RNCComprador { get; set; }
        public string? IdentificadorExtranjero { get; set; }
        public string? RazonSocialComprador { get; set; }

        // === TOTALES ===
        public decimal? MontoGravadoTotal { get; set; }
        public decimal? MontoGravadoI1 { get; set; }
        public decimal? MontoGravadoI2 { get; set; }
        public decimal? MontoExento { get; set; }
        public decimal? TotalITBIS { get; set; }
        public decimal? TotalITBIS1 { get; set; }
        public decimal? TotalITBIS2 { get; set; }
        public decimal? MontoTotal { get; set; }

        // === MÚLTIPLES ÍTEMS ===
        public List<ItemDto> Items { get; set; } = new();
    }

    public class ItemDto
    {
        public string? Nombre { get; set; }
        public decimal? Cantidad { get; set; }
        public decimal? Precio { get; set; }
        public decimal Monto { get; set; }
    }
}