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
    
    // Method from ChatGPT to write a file with a specific size and fill it with 0
    
    static void WriteZeroFilledFile(string filePath, long fileSize)
    {
        // Überprüfen, ob die angegebene Dateigröße gültig ist
        if (fileSize < 0)
            throw new ArgumentException("Die Dateigröße muss größer oder gleich 0 sein.", nameof(fileSize));

        // Eine Datei mit der gewünschten Größe erstellen und mit Nullen auffüllen
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] buffer = new byte[8192]; // Puffer für 8 KB (angepasst für Performance)
            long remainingBytes = fileSize;

            while (remainingBytes > 0)
            {
                int bytesToWrite = (int)Math.Min(buffer.Length, remainingBytes);
                fileStream.Write(buffer, 0, bytesToWrite);
                remainingBytes -= bytesToWrite;
            }
        }
    }
    
    static void WriteBufferToFile(string filePath, byte[] buffer, long offset, int size)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer), "Der Buffer darf nicht null sein.");

        if (offset < 0)
            throw new ArgumentException("Der Offset muss größer oder gleich 0 sein.", nameof(offset));

        using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        {
            // Falls die Datei kürzer als der Offset ist, erweitern
            if (fileStream.Length < offset)
            {
                fileStream.SetLength(offset);
            }

            fileStream.Seek(offset, SeekOrigin.Begin);
            fileStream.Write(buffer, 0, size);
        }
    }
    
    private static int DoReceiveFiles(DateTime startTS, string mqExchangeName, string mqRoutingKey, string directoryPath, PasApplicationConfig pApplicationConfig)
    {
        pApplicationConfig.Verbose = true;
        if (! Directory.Exists(directoryPath))
        {
            PASLoggingServices.ConsoleMessage(startTS,
                "Directory does not exists: " + directoryPath );
            return 1;
        }
        
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
            channel.BasicQos(0, pApplicationConfig.MqQueueSize, false);
            var privateQueue = channel.QueueDeclare(queue: "", exclusive: true, autoDelete: true);
            // Bind the queue to the exchange with a routing key
            channel.QueueBind(queue: privateQueue.QueueName,
                exchange: mqExchangeName,
                routingKey: mqRoutingKey);
 
            if (pApplicationConfig.Verbose)
            {
                PASLoggingServices.ConsoleMessage(startTS,
                    "RabbitMQ: Create new queue " + privateQueue.QueueName + " bind to " + mqExchangeName + " via routing " + mqRoutingKey );     
            }
            
            // Create a consumer
            var consumer = new EventingBasicConsumer(channel);
            
            
            // Define the callback for received messages
            consumer.Received += (model, ea) =>
            {

                var fileTransferUuid = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferUUIDHeader);
                if (String.IsNullOrEmpty(fileTransferUuid))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the Job UUID");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }

                var transferFilename = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferTransferFilename);
                if (String.IsNullOrEmpty(transferFilename))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain transfer filename");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                string fullPathTransferFilename = Path.Combine(directoryPath, transferFilename);
                
                var transferPackageSizeString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferPackageSizeHeader);               
                if (String.IsNullOrEmpty(transferPackageSizeString))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the default transfer package size");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }

                if (! int.TryParse(transferPackageSizeString, out var transferPackageSize))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message contains wrong transfer package size: " + transferPackageSizeString );
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                
                var transferCurrentPackageIndexString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferCurrentPackageIndexHeader); 
                if (String.IsNullOrEmpty(transferCurrentPackageIndexString))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the current package index");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                if (! int.TryParse(transferCurrentPackageIndexString, out var transferCurrentPackageIndex))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message contains wrong current package index: " + transferCurrentPackageIndexString );
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                
                var transferMaxPackageIndexString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferMaxPackageIndexHeader);                 
                if (String.IsNullOrEmpty(transferMaxPackageIndexString))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the max package index");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                if (! int.TryParse(transferMaxPackageIndexString, out var transferMaxPackageIndex))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message contains wrong max package index: " + transferMaxPackageIndexString );
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }

                var finalSizeString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                    RMQHeaderNames.FileTransferFinalSizeHeader);   
                if (String.IsNullOrEmpty(finalSizeString))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain final size");
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                if (! long.TryParse(finalSizeString, out var finalSize))
                {
                    PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message contains wrong final size: " + finalSizeString );
                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    return ;
                }
                
                // Ist die Übertragung bereits bekannt ?
                if (pApplicationConfig.Transmissions.TryAdd(fileTransferUuid, 1))
                {
                    WriteZeroFilledFile(fullPathTransferFilename, finalSize);
                    pApplicationConfig.TransmissionStarts.TryAdd(fileTransferUuid, DateTime.UtcNow);
                }
                else
                {
                    pApplicationConfig.Transmissions[fileTransferUuid] += transferCurrentPackageIndex;
                }
                
                // Get the message body
                var body = ea.Body.ToArray();
                WriteBufferToFile(fullPathTransferFilename, body, (transferCurrentPackageIndex-1)*transferPackageSize, body.Length);
                PASLoggingServices.ConsoleMessage(startTS,"Transfer JobID: " + fileTransferUuid + ". Part " + (string)transferCurrentPackageIndexString + " of " + (string)transferMaxPackageIndexString + " received");

                
                // Wenn das Ende erreicht wurde:
                // - Datei umbenennen
                // - Transferinformationen löschen
                // - LOG Meldungen lossenden
                if ((transferMaxPackageIndex * (1 + transferMaxPackageIndex) / 2) ==
                    pApplicationConfig.Transmissions[fileTransferUuid])
                {
                    var originalFilenameString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                        RMQHeaderNames.FileTransferOriginalFilename);   
                    if (String.IsNullOrEmpty(originalFilenameString))
                    {
                        PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the original filename");
                        // Acknowledge the message
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        return ;
                    }
                    var orgFilePath = Path.Combine(directoryPath, originalFilenameString);
                    if (File.Exists(orgFilePath))
                    {
                        File.Delete(orgFilePath);
                    }
                    
                    var uncompressString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                        RMQHeaderNames.FileTransferUncompressHeader);   
                    if (String.IsNullOrEmpty(uncompressString))
                    {
                        PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message does not contain the compress information");
                        // Acknowledge the message
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        return ;
                    }
                    if (! bool.TryParse(uncompressString, out var uncompress))
                    {
                        PASLoggingServices.ConsoleMessage(startTS,"Not handled: Message contains wrong compress information: " + uncompressString );
                        // Acknowledge the message
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        return ;
                    }

                    if (uncompress)
                    {
                        string uncompressedFile = orgFilePath + ".uncompressed";
                        using (FileStream compressedFileStream = new FileStream(fullPathTransferFilename, FileMode.Open, FileAccess.Read))
                        using (GZipStream gzipStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
                        using (FileStream outputFileStream = new FileStream(uncompressedFile, FileMode.Create, FileAccess.Write))
                        {
                            gzipStream.CopyTo(outputFileStream);
                        } 
                        File.Delete(fullPathTransferFilename);
                        File.Move(uncompressedFile, orgFilePath);
                        PASLoggingServices.ConsoleMessage(startTS,"Move file from  " + uncompressedFile + " to " + orgFilePath );
                    }
                    else
                    {
                        File.Move(fullPathTransferFilename, orgFilePath);
                        PASLoggingServices.ConsoleMessage(startTS,"Move file from  " + fullPathTransferFilename + " to " + orgFilePath );
                    }
                    
                    var fileCreationString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                        RMQHeaderNames.FileTransferFileCreationHeader);    
                    if (! String.IsNullOrEmpty(fileCreationString))
                    {
                        if (DateTime.TryParse(fileCreationString, CultureInfo.InvariantCulture,
                                out var creationDateTime))
                        {
                            File.SetCreationTime(orgFilePath, creationDateTime);
                        }
                    } 
                    
                    var fileModificationString = GetStringFromMessageHeaderValue(ea.BasicProperties.Headers,
                        RMQHeaderNames.FileTransferFileModificationHeader);                       
                    if (! String.IsNullOrEmpty(fileModificationString))
                    {
                        if (DateTime.TryParse(fileModificationString, CultureInfo.InvariantCulture, out var modificationDateTime))
                        {
                            File.SetLastWriteTime(orgFilePath, modificationDateTime);   
                        }
                       
                    }
                                     
                    var duration = DateTime.UtcNow - pApplicationConfig.TransmissionStarts[fileTransferUuid] ;
                    PASLoggingServices.ConsoleMessage(startTS,"Transfer JobID: " + fileTransferUuid + ", Size: " + finalSizeString + " Bytes, finished in " + duration.TotalSeconds.ToString() + " seconds, Speed: " + (Math.Truncate(finalSize / duration.TotalSeconds)).ToString() + " Bytes/Second");
                    
                    // Transfer has been completed ... so remove it from out statistical data
                    pApplicationConfig.Transmissions.Remove(fileTransferUuid);
                    pApplicationConfig.TransmissionStarts.Remove(fileTransferUuid);
                    
                    PASLoggingServices.ConsoleMessage(startTS,"Number of known Jobs: " +  pApplicationConfig.Transmissions.Count.ToString());
                    
                }
                
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            // Start consuming messages
            channel.BasicConsume(queue:  privateQueue.QueueName,
                autoAck: false, // Disable auto-acknowledge to ensure messages are processed successfully
                consumer: consumer);

            while (true)
            {
                Thread.Sleep(1000);
            }
            return 0;
        }
        catch (Exception ex)
        {
            PASLoggingServices.ConsoleMessage(startTS,"No Connection possible to RMQ Server at " + pApplicationConfig.MqHost + " ex: " + ex.Message);
        }
        return 0;
    }
}
