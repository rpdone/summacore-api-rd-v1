using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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
            errores.AddRange(ValidarReglasMatriz(ecf, tipoECF));
            errores.AddRange(ValidarCalculos(ecf));

            return errores;
        }

        private IEnumerable<string> ValidarAnotaciones(ECF ecf)
        {
            var context = new ValidationContext(ecf);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(ecf, context, results, true);
            return results.Select(r => $"[Estructura] {r.ErrorMessage}");
        }

        private IEnumerable<string> ValidarReglasMatriz(ECF ecf, int tipoECF)
        {
            var errores = new List<string>();
            
            foreach (var regla in ECF_Reglas.Matriz)
            {
                string campoPath = regla.Key;
                int nivel = ECF_Reglas.ObtenerNivel(campoPath, tipoECF);

                // Nivel 1 = Obligatorio
                if (nivel == 1)
                {
                    object? valor = ObtenerValorPropiedad(ecf.Encabezado, campoPath);
                    if (valor == null || (valor is string s && string.IsNullOrWhiteSpace(s)))
                    {
                        errores.Add($"[Matriz] Campo obligatorio faltante: {campoPath} para Tipo {tipoECF}");
                    }
                }
                // Nivel 0 = No Corresponde (Optional: warn or ensure it's null, but serialization handles this via ShouldSerialize)
            }

            return errores;
        }

        private object? ObtenerValorPropiedad(object obj, string path)
        {
            if (obj == null) return null;
            
            string[] parts = path.Split('.');
            object? current = obj;

            foreach (var part in parts)
            {
                if (current == null) return null;

                Type type = current.GetType();
                PropertyInfo? prop = type.GetProperty(part);
                
                if (prop == null) return null; // Property not found

                current = prop.GetValue(current);
            }

            return current;
        }

        private IEnumerable<string> ValidarCalculos(ECF ecf)
        {
            var errores = new List<string>();
            var t = ecf.Encabezado.Totales;

            decimal? calculado = (t.MontoGravadoTotal ?? 0) + (t.MontoExento ?? 0) + (t.TotalITBIS ?? 0) + (t.MontoImpuestoAdicional ?? 0);
            
            // Tolerance for floating point arithmetic
            if (Math.Abs(t.MontoTotal - (calculado ?? 0)) > 0.05m)
                errores.Add($"[Calculos] MontoTotal ({t.MontoTotal:F2}) != Suma Componentes ({calculado:F2})");

            return errores;
        }
    }
}