using System;

namespace SummaCore.Models
{
    public class CsvEcfDto
    {
        // Identificación
        public string Encf { get; set; }
        public int TipoeCF { get; set; }
        public string FechaVencimientoSecuencia { get; set; }
        public int IndicadorMontoGravado { get; set; }
        public int TipoIngresos { get; set; }
        public int TipoPago { get; set; }
        public string FechaLimitePago { get; set; }

        // Partes
        public string RNCEmisor { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string DireccionEmisor { get; set; } // <--- AGREGADO
        public string FechaEmision { get; set; }
        
        public string RNCComprador { get; set; }
        public string RazonSocialComprador { get; set; }
        public string CodigoInternoComprador { get; set; }

        // Totales
        public decimal MontoTotal { get; set; }
        public decimal MontoGravadoTotal { get; set; }
        public decimal MontoGravadoI1 { get; set; }
        public decimal MontoGravadoI2 { get; set; }
        public decimal MontoExento { get; set; }
        
        // Impuestos
        public decimal ITBIS1 { get; set; }
        public decimal ITBIS2 { get; set; }
        public decimal TotalITBIS { get; set; }
        public decimal TotalITBIS1 { get; set; }
        public decimal TotalITBIS2 { get; set; }
    }
}