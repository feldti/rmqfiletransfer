using System.CommandLine;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using pas_services_logger;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace rmqfiletransfer;



public partial class Program
{
    private static int DoStorageDirectoryTransfer(DateTime startTS, IModel channel, string mqExchangeName, string mqRoutingKey, string directoryPath, PasApplicationConfig pApplicationConfig)
    {
        if (Directory.Exists(directoryPath))
        {
            // Alle Dateien im Verzeichnis abrufen
            string[] files = Directory.GetFiles(directoryPath);

            // Schleife über alle Dateien
            foreach (string file in files)
            {
                SendFile(startTS, channel, mqExchangeName, mqRoutingKey, file, pApplicationConfig);
            }
        }
        
        return 0;
    }


}
