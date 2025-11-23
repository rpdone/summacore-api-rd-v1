using System.Collections.Generic;

namespace SummaCore.Validation
{
    public static class ECF_Reglas
    {
        public static readonly Dictionary<string, Dictionary<int, int>> Matriz = new Dictionary<string, Dictionary<int, int>>
        {
            { "IdDoc.FechaVencimientoSecuencia", new Dictionary<int, int> {
                {31, 1}, {32, 0}, {33, 1}, {34, 0}, {41, 1}, {43, 1}, {44, 1}, {45, 1}, {46, 1}, {47, 1}
            }},
            { "IdDoc.IndicadorNotaCredito", new Dictionary<int, int> {
                {31, 0}, {32, 0}, {33, 0}, {34, 2}, {41, 0}, {43, 0}, {44, 0}, {45, 0}, {46, 0}, {47, 0}
            }},
            { "IdDoc.IndicadorEnvioDiferido", new Dictionary<int, int> {
                {31, 0}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 2}, {45, 2}, {46, 2}, {47, 0}
            }},
            { "IdDoc.IndicadorMontoGravado", new Dictionary<int, int> {
                {31, 0}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 2}, {44, 0}, {45, 0}, {46, 2}, {47, 0}
            }},
            { "IdDoc.TipoIngresos", new Dictionary<int, int> {
                {31, 1}, {32, 1}, {33, 1}, {34, 1}, {41, 1}, {43, 0}, {44, 1}, {45, 1}, {46, 1}, {47, 0}
            }},
            { "IdDoc.TipoPago", new Dictionary<int, int> {
                {31, 1}, {32, 1}, {33, 1}, {34, 1}, {41, 1}, {43, 1}, {44, 3}, {45, 1}, {46, 1}, {47, 3}
            }},
            { "IdDoc.FechaLimitePago", new Dictionary<int, int> {
                {31, 0}, {32, 2}, {33, 3}, {34, 2}, {41, 3}, {43, 2}, {44, 0}, {45, 2}, {46, 3}, {47, 3}
            }},
            { "IdDoc.TerminoPago", new Dictionary<int, int> {
                {31, 0}, {32, 2}, {33, 0}, {34, 0}, {41, 3}, {43, 2}, {44, 0}, {45, 2}, {46, 3}, {47, 3}
            }},
            { "Emisor.NombreComercial", new Dictionary<int, int> {
                {31, 1}, {32, 3}, {33, 3}, {34, 3}, {41, 3}, {43, 3}, {44, 3}, {45, 3}, {46, 3}, {47, 3}
            }},
            { "Emisor.Municipio", new Dictionary<int, int> {
                {31, 1}, {32, 3}, {33, 3}, {34, 3}, {41, 3}, {43, 3}, {44, 3}, {45, 3}, {46, 3}, {47, 3}
            }},
            { "Emisor.Provincia", new Dictionary<int, int> {
                {31, 1}, {32, 3}, {33, 3}, {34, 3}, {41, 3}, {43, 3}, {44, 3}, {45, 3}, {46, 3}, {47, 3}
            }},
            { "Comprador.RNCComprador", new Dictionary<int, int> {
                {31, 1}, {32, 2}, {33, 2}, {34, 2}, {41, 1}, {43, 0}, {44, 1}, {45, 1}, {46, 2}, {47, 3}
            }},
            { "Comprador.IdentificadorExtranjero", new Dictionary<int, int> {
                {31, 3}, {32, 2}, {33, 2}, {34, 2}, {41, 0}, {43, 0}, {44, 2}, {45, 0}, {46, 2}, {47, 3}
            }},
            { "Comprador.RazonSocialComprador", new Dictionary<int, int> {
                {31, 1}, {32, 2}, {33, 2}, {34, 2}, {41, 1}, {43, 0}, {44, 1}, {45, 1}, {46, 1}, {47, 3}
            }},
            { "InformacionReferencia.NCFModificado", new Dictionary<int, int> {
                {31, 0}, {32, 0}, {33, 1}, {34, 1}, {41, 0}, {43, 0}, {44, 0}, {45, 0}, {46, 0}, {47, 0}
            }},
            { "InformacionReferencia.FechaNCFModificado", new Dictionary<int, int> {
                {31, 0}, {32, 0}, {33, 2}, {34, 2}, {41, 0}, {43, 0}, {44, 0}, {45, 0}, {46, 0}, {47, 0}
            }},
            { "Totales.MontoGravadoTotal", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 0}, {43, 0}, {44, 2}, {45, 2}, {46, 2}, {47, 0}
            }},
            { "Totales.MontoGravadoI1", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 2}, {47, 0}
            }},
            { "Totales.MontoGravadoI2", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 2}, {47, 0}
            }},
            { "Totales.MontoExento", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 2}, {44, 2}, {45, 2}, {46, 0}, {47, 2}
            }},
            { "Totales.ITBIS1", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 0}, {47, 0}
            }},
            { "Totales.ITBIS2", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 0}, {47, 0}
            }},
            { "Totales.TotalITBIS", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 0}, {47, 0}
            }},
            { "Totales.TotalITBIS1", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 0}, {47, 0}
            }},
            { "Totales.TotalITBIS2", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 2}, {43, 0}, {44, 0}, {45, 2}, {46, 0}, {47, 0}
            }},
            { "Totales.MontoImpuestoAdicional", new Dictionary<int, int> {
                {31, 2}, {32, 2}, {33, 2}, {34, 2}, {41, 0}, {43, 0}, {44, 2}, {45, 2}, {46, 0}, {47, 0}
            }}
        };

        public static int ObtenerNivel(string campo, int tipoECF)
        {
            if (Matriz.ContainsKey(campo) && Matriz[campo].ContainsKey(tipoECF))
                return Matriz[campo][tipoECF];
            return 3;
        }

        public static List<string> ObtenerCamposObligatorios(int tipoECF)
        {
            var obligatorios = new List<string>();
            foreach (var kvp in Matriz)
            {
                if (kvp.Value.ContainsKey(tipoECF) && kvp.Value[tipoECF] == 1)
                    obligatorios.Add(kvp.Key);
            }
            return obligatorios;
        }

        public static List<string> ObtenerCamposNoCorresponden(int tipoECF)
        {
            var noCorresponden = new List<string>();
            foreach (var kvp in Matriz)
            {
                if (kvp.Value.ContainsKey(tipoECF) && kvp.Value[tipoECF] == 0)
                    noCorresponden.Add(kvp.Key);
            }
            return noCorresponden;
        }
    }
}