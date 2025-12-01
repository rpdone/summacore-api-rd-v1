using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SummaCore.Services;

namespace SummaCore.Processor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("SummaCore ECF Processor");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfiguration config = builder.Build();

            string certPath = config["Dgii:CertificatePath"];
            string certPass = config["Dgii:CertificatePassword"];

            if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(certPass))
            {
                Console.WriteLine("Certificate configuration missing in appsettings.json");
                return;
            }

            var signerService = new SignerService(certPath, certPass);
            var dgiiService = new DgiiService(signerService);
            var processor = new ECFProcessor(dgiiService, signerService);

            string excelFile = "131487272-23112025034018.xlsx";
            string outputDir = Directory.GetCurrentDirectory();

            await processor.ProcessExcelFile(excelFile, outputDir);
        }
    }
}
