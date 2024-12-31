using System.CommandLine;
using System.Globalization;
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
    
    /// <summary>
    /// Die Methode erhält die notwendigen Informationen, im ein Paket nach RMQ zu senden
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="mqExchangeName"></param>
    /// <param name="mqRoutingKey"></param>
    /// <param name="currentIndex"></param>
    /// <param name="maxIndex"></param>
    /// <param name="maxSize"></param>
    /// <param name="baseFilename"></param>
    /// <param name="extract"></param>
    /// <param name="bytes"></param>
    /// <param name="bytesRead"></param>
    /// <param name="uuid"></param>
    /// <param name="pApplicationConfig"></param>
    static void SendFilePart(DateTime startTS, IModel channel, string? mqExchangeName, string? mqRoutingKey, int currentIndex, int maxIndex, long maxSize, string? baseFilename, Boolean extract, byte[] bytes, int bytesRead, string uuid, DateTime modificationDateTime, DateTime creationDateTime, PasApplicationConfig pApplicationConfig )
    {
        if (String.IsNullOrEmpty(baseFilename))
        {
            PASLoggingServices.ConsoleMessage(startTS,
                "SendFilePart - baseFilename is null or empty" );
            return;
        }
        
        var props = channel.CreateBasicProperties();
        props.ContentType = "application/octet-stream";
        props.Headers = new Dictionary<string, object>();

        // Die Default Größe eines Packages
        props.Headers.Add(RMQHeaderNames.FileTransferPackageSizeHeader, pApplicationConfig.FilePackageSize.ToString());      
        // Aktueller Index der Übertragung: 1 .. n
        props.Headers.Add(RMQHeaderNames.FileTransferCurrentPackageIndexHeader, currentIndex.ToString());
        // Höchster Index der Übertragung: n
        props.Headers.Add(RMQHeaderNames.FileTransferMaxPackageIndexHeader, maxIndex.ToString());
        // Soll die Datei nach dem Transfer nicht mehr komprimiert sein
        props.Headers.Add(RMQHeaderNames.FileTransferUncompressHeader, extract.ToString());
        // Transfer-ID
        props.Headers.Add(RMQHeaderNames.FileTransferUUIDHeader, uuid);
        
        // Originalname der Dateiu
        props.Headers.Add(RMQHeaderNames.FileTransferOriginalFilename, baseFilename.ToString());
        // Name bei der Übertragung
        props.Headers.Add(RMQHeaderNames.FileTransferTransferFilename, "_____" + uuid + ".part");      
        // Max. Größe der zu übertragenden Datei (inkl. Kompression)
        props.Headers.Add(RMQHeaderNames.FileTransferFinalSizeHeader, maxSize.ToString());
        // Wann wurde die Datei erzeugt
        props.Headers.Add(RMQHeaderNames.FileTransferFileCreationHeader, creationDateTime.ToString(CultureInfo.InvariantCulture));
        // Wann wurde die Datei das letzte Mal verändert
        props.Headers.Add(RMQHeaderNames.FileTransferFileModificationHeader, modificationDateTime.ToString(CultureInfo.InvariantCulture));
        byte[] bytesToBeSend;

        if (bytes.Length != bytesRead)
        {
            bytesToBeSend = new byte[bytesRead];
            Array.Copy(bytes, 0, bytesToBeSend, 0, bytesRead);
        }
        else
        {
            bytesToBeSend = bytes;
        }
        
        // Das nächste Package wird verschickt
        channel.BasicPublish(mqExchangeName, mqRoutingKey, true, props, bytesToBeSend);
        channel.WaitForConfirmsOrDie();
        
        // Hier sollte auch eine LOG-Meldung abgeschickt werden werden
        if (pApplicationConfig.MsgLog)
        {
            
        }
    }

    static void SendFile(DateTime startTS, IModel channel, string? mqExchangeName, string? mqRoutingKey, string? filePath, PasApplicationConfig pApplicationConfig)
    {
        if (File.Exists(filePath))
        {
            DateTime modificationDateTime = File.GetLastWriteTime(filePath);
            DateTime creationDateTime = File.GetCreationTime(filePath);
            // Original file name with extensions
            var baseFilename = Path.GetFileName(filePath);
            var fileExtension = Path.GetExtension(filePath);
            List<string> compressedExtensions  = new List<string> { ".zip", ".tgz", ".7z", ".gz", ".mp4", ".jpeg", ".jpg", ".png"};
            var extract = ! compressedExtensions.Contains(fileExtension.ToLower());
            // Identifier for the transmission
            string uuidString = Guid.NewGuid().ToString();
            // Size of the original file
            long originalFilesize = new FileInfo(filePath).Length;
            // Now we do a compression
            var ms = new MemoryStream();
            if (extract)
            {
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    zs.Write(File.ReadAllBytes(filePath));
                }
            }
            else
            {
                ms.Write(File.ReadAllBytes(filePath));
            }
            
            ms.Position = 0;
            // and use the files from that compression
            var bytes = ms.ToArray();
            var transferSize = bytes.Length;
            // how max transmissions do we need for that ... what happens if both values are equal ?
            var maxIndex = (transferSize / pApplicationConfig.FilePackageSize) +1;
            // We need this buffer for transmission
            byte[] buffer = new byte[pApplicationConfig.FilePackageSize];
            for (var currentIndex = 1; currentIndex <= maxIndex; currentIndex++)
            {
                // Prepare a package content
                int bytesRead = ms.Read(buffer, 0, pApplicationConfig.FilePackageSize);
                // Send this package
                SendFilePart( startTS, channel, mqExchangeName, mqRoutingKey, currentIndex, maxIndex, transferSize, baseFilename, extract, buffer, bytesRead, uuidString, modificationDateTime, creationDateTime, pApplicationConfig );
            }
        }
        else
        {
            if (pApplicationConfig.MsgLog)
            {
            
            }
        }
        
       
    }
    
    private static int DoSingleFileTransfer(DateTime startTS, string mqExchangeName, string mqRoutingKey, string filePath, PasApplicationConfig pApplicationConfig)
    {
        if (File.Exists(filePath))
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = pApplicationConfig.MqLogin,
                Password = pApplicationConfig.MqPassword,
                VirtualHost = pApplicationConfig.MqvHostName,
                HostName = pApplicationConfig.MqHost,
                AutomaticRecoveryEnabled = true
            };

            try
            {
                if (pApplicationConfig.Verbose)
                {
                    PASLoggingServices.ConsoleMessage(startTS,
                        "RabbitMQ: Connect to " + pApplicationConfig.MqHost + " at " + pApplicationConfig.MqvHostName + " using " + pApplicationConfig.MqLogin + " and " + pApplicationConfig.MqPassword );     
                }
                IConnection conn = factory.CreateConnection();
                if (pApplicationConfig.Verbose)
                {
                    PASLoggingServices.ConsoleMessage(startTS,
                        "RabbitMQ: Connection created" );     
                }                
                IModel channel = conn.CreateModel();
                channel.ConfirmSelect();
                channel.BasicQos(0, 100, false);
                SendFile(startTS, channel, mqExchangeName, mqRoutingKey, filePath, pApplicationConfig);
                conn.Close();
                if (pApplicationConfig.Verbose)
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Transfer with success: '" + filePath + "'");
                }      
                if (pApplicationConfig.MsgLog)
                {
                    var logMessage = PASLoggingServices.CreateLogMessage("Transfer with success: '" + filePath + "'", 30);
                    PASLoggingServices.SendSingleLogMessage(channel, mqExchangeName, mqRoutingKey,logMessage);
                }
                return 0;
            }
            catch (Exception ex)
            {
                PASLoggingServices.ConsoleMessage(startTS,"No Connection possible to RMQ Server at " + pApplicationConfig.MqHost + " ex: " + ex.Message);
            }

            return 1;

        }
        else
        {
            PASLoggingServices.ConsoleMessage(startTS,"File does not exists: '" + filePath + "'");
            return 1;
        }
    }
}
