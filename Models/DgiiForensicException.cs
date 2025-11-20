using System;

namespace SummaCore.Models
{
    public class DgiiForensicException : Exception
    {
        public string ReporteForense { get; }

        public DgiiForensicException(string mensaje, string reporte) : base(mensaje)
        {
            ReporteForense = reporte;
        }
    }
}