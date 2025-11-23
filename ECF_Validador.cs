using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SummaCore.Models;

namespace SummaCore.Validation
{
    public class ECFValidator
    {
        public List<string> Validar(ECF ecf)
        {
            var errores = new List<string>();
            int tipoECF = ecf.Encabezado.IdDoc.TipoeCF;

            errores.AddRange(ValidarAnotaciones(ecf));
            errores.AddRange(ValidarReglasObligatorias(ecf, tipoECF));
            errores.AddRange(ValidarCalculos(ecf));

            return errores;
        }

        public void SanitizarParaEsquema(ECF ecf)
        {
            int tipo = ecf.Encabezado.IdDoc.TipoeCF;

            // Limpiar campos no permitidos según tipo
            if (!new[] { 31, 33, 41, 43, 44, 45, 46, 47 }.Contains(tipo))
                ecf.Encabezado.IdDoc.FechaVencimientoSecuencia = null;

            if (!new[] { 32, 33, 34 }.Contains(tipo))
                ecf.InformacionReferencia = null;
        }

        private IEnumerable<string> ValidarAnotaciones(ECF ecf)
        {
            var context = new ValidationContext(ecf);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(ecf, context, results, true);
            return results.Select(r => $"[Estructura] {r.ErrorMessage}");
        }

        private IEnumerable<string> ValidarReglasObligatorias(ECF ecf, int tipo)
        {
            var errores = new List<string>();

            // RNC Comprador obligatorio para ciertos tipos
            if (new[] { 31, 41, 43, 44, 45, 46, 47 }.Contains(tipo))
            {
                if (string.IsNullOrWhiteSpace(ecf.Encabezado.Comprador.RNCComprador))
                    errores.Add("Campo obligatorio: Comprador.RNCComprador");
            }

            // NC/ND requieren NCF modificado
            if (tipo == 33 || tipo == 34)
            {
                if (ecf.InformacionReferencia == null || string.IsNullOrWhiteSpace(ecf.InformacionReferencia.NCFModificado))
                    errores.Add("e-CF tipo 33/34 requiere NCF modificado.");
            }

            return errores;
        }

        private IEnumerable<string> ValidarCalculos(ECF ecf)
        {
            var errores = new List<string>();
            var t = ecf.Encabezado.Totales;

            decimal? calculado = (t.MontoGravadoTotal ?? 0) + (t.MontoExento ?? 0) + (t.TotalITBIS ?? 0);
            if (t.MontoTotal.HasValue && Math.Abs(t.MontoTotal.Value - calculado.Value) > 0.01m)
                errores.Add($"MontoTotal ({t.MontoTotal:F2}) != Suma Componentes ({calculado:F2})");

            return errores;
        }
    }
}