using System;
using Microsoft.Extensions.Configuration;

namespace SummaCore.Configuration
{
    public class DgiiEnvironmentConfig
    {
        private readonly IConfiguration _configuration;
        private readonly string _currentEnvironment;

        public DgiiEnvironmentConfig(IConfiguration configuration)
        {
            _configuration = configuration;
            _currentEnvironment = _configuration["Dgii:Environment"] ?? "Certification";
        }

        public string GetUrl(string serviceKey)
        {
            var url = _configuration[$"Dgii:Environments:{_currentEnvironment}:{serviceKey}"];
            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException($"URL no configurada para {serviceKey} en ambiente {_currentEnvironment}");
            return url;
        }

        public string AuthenticationUrl => GetUrl("Authentication");
        public string ReceptionUrl => GetUrl("Reception");
        public string ReceptionFCUrl => GetUrl("ReceptionFC");
        public string ConsultaResultadoUrl => GetUrl("ConsultaResultado");
        public string ConsultaEstadoUrl => GetUrl("ConsultaEstado");
        public string AprobacionComercialUrl => GetUrl("AprobacionComercial");
        public string AnulacionRangosUrl => GetUrl("AnulacionRangos");
        public string ConsultaDirectorioUrl => GetUrl("ConsultaDirectorio");
        public string StatusServiceUrl => GetUrl("StatusService");

        public string CurrentEnvironment => _currentEnvironment;
        public string Rnc => _configuration["Dgii:Rnc"] ?? "131487272";
        public string RazonSocial => _configuration["Dgii:RazonSocial"] ?? "EMPRESA PRUEBA SRL";
    }
}