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

    static PasApplicationConfig applicationConfig = new PasApplicationConfig();
    static PASLogConfig loggingConfig = new PASLogConfig();
    static string  AppLocation = Dns.GetHostName();
  

    
    private static int DoStorageDirectoryReceive(PasApplicationConfig applicationConfig)
    {
        ConnectionFactory factory = new ConnectionFactory();
        factory.UserName = applicationConfig.MqLogin;
        factory.Password = applicationConfig.MqPassword;
        factory.VirtualHost = applicationConfig.MqvHostName;
        factory.HostName = applicationConfig.MqHost;
        IConnection conn = factory.CreateConnection();
        IModel channel = conn.CreateModel();
        channel.ConfirmSelect();

        // Services.CreateLogMessage("Started storage service", EDBHeaderNames.EDBLogTypeInfo, "", ApplicationID, channel);

        channel.BasicQos(0, applicationConfig.MqQueueSize, false);
        channel.CallbackException += (sender, args) =>
        {
            var exception = args as CallbackExceptionEventArgs;
            if (exception != null)
            {
                Console.WriteLine("Exception: " + exception.Exception.ToString()); 
            }
            else 
                Console.WriteLine("Exception: " + args.ToString());
        };
        channel.ModelShutdown += (sender, args) =>
        {
            Console.WriteLine("Model Shutdown !");
        };    
        var consumer = new EventingBasicConsumer(channel);
        consumer.ConsumerCancelled += (sender, args) =>
        {
            Console.WriteLine("Consumer cancelled");
        };
        consumer.Registered += (sender, args) =>
        {
            Console.WriteLine("Consumer registered");
        };
        consumer.Unregistered += (sender, args) =>
        {
            Console.WriteLine("Consumer unregistered");
        };
        consumer.Received += (ch, ea) =>
        {
            
        };
        return 0;
    }
   
    public static string? GetStringFromMessageHeaderValue(IDictionary<string, object>? anDictionary, String key)
    {
        if (anDictionary != null)
            if (anDictionary.TryGetValue(key, out var value))
            {
                var bytes = value as System.Byte[];

                return bytes != null ? System.Text.Encoding.UTF8.GetString(bytes) : null;
            }

        return null;
    }
    
    static async Task<int> Main(string[] args)
    {
        int returnCode = 0;
        var startTS = DateTime.Now;
        
        // Optionale Zugangsdaten aus einer Datei lesen
        PasApplicationConfig.ReadLocalParameterFile(startTS, ref applicationConfig);
        //PASLoggingServices.ReadLocalParameterFile(startTS, ref loggingConfig, ref applicationConfig));
        
        var mqServerOption = new Option<string?>(name: "--mqserver", description: "Name of the RabbitMQ Server",
            getDefaultValue: () => applicationConfig.MqHost);

        var mqUserOption = new Option<string?>(name: "--mquser", description: "Login for RabbitMQ",
            getDefaultValue: () => applicationConfig.MqLogin);

        var mqPasswordOption = new Option<string>(name: "--mqpassword", description: "Password for RabbitMQ user");
        
        // Das System hängt sich an den Excahnge ran, setzt seine Quelle/Ziel als routing key und sollte dann die
        // entsprechenden Daten bekommen
        var mqExchangeOption = new Option<string>(name: "--mqexchange",
            description: "The RabbitMQ exchange where all messages are sent to",
            getDefaultValue: () => applicationConfig.MqExchangeName);
        var mqVHostOption = new Option<string>(name: "--mqvhost",
            description: "Defines the vhost to work with", getDefaultValue: () => applicationConfig.MqvHostName);
        
        var mqRoutingKeyOption = new Option<string>(name: "--mqrkey",
            description: "Defines the routing key when sending/receiving messages");

        var debugOption = new Option<bool>(name: "--debug", description: "Defines debugging state of messages logged",
            getDefaultValue: () => applicationConfig.MsgDebug);
        var verboseOption = new Option<bool>(name: "--verbose", description: "Additional output for debugging",
            getDefaultValue: () => applicationConfig.Verbose);
        var fileOption = new Option<string>(name: "--file", description: "Path to file to betransferred");   
        var directoryOption = new Option<string>(name: "--directory", description: "Path to directory as the target of received files"); 
        var msklogOption = new Option<bool>(name: "--log", description: "Enables logging via PASLOG");              
        
        var rootCommand =
            new RootCommand("Programm, um Dateien über RabbitMQ zu senden oder zu empfangen");
        
        var subCommand = new Command("sendfile", "Send a single file");
        fileOption.IsRequired = true;
        mqRoutingKeyOption.IsRequired = true;   
        subCommand.AddOption(msklogOption);
        subCommand.AddOption(fileOption);
        subCommand.AddOption(mqRoutingKeyOption);
        subCommand.AddOption(verboseOption);
        subCommand.AddOption(debugOption);
        subCommand.SetHandler((context) =>
        {
            string filePath = "";
            string? optionValue = context.ParseResult.GetValueForOption(fileOption);
            if (optionValue != null)
                filePath = optionValue;
            optionValue = context.ParseResult.GetValueForOption(mqRoutingKeyOption);
            if (optionValue != null)
                applicationConfig.MqRoutingKey = optionValue;
            optionValue = context.ParseResult.GetValueForOption(mqExchangeOption);
            if (optionValue != null)
                applicationConfig.MqExchangeName = optionValue;            
            DoSingleFileTransfer(startTS, applicationConfig.MqExchangeName,  "filetransfer." + applicationConfig.MqRoutingKey, filePath, applicationConfig);
        });
        rootCommand.AddCommand(subCommand);
 
        subCommand = new Command("receivefiles", "Receive files service");
        mqRoutingKeyOption.IsRequired = true;  
        directoryOption.IsRequired = true;
        subCommand.AddOption(msklogOption);
        subCommand.AddOption(directoryOption);
        subCommand.AddOption(mqRoutingKeyOption);
        subCommand.AddOption(verboseOption);
        subCommand.AddOption(debugOption);

        
        subCommand.SetHandler((context) =>
        {
            string directoryPath = "";
            string? filePath = null;
            string? optionValue = context.ParseResult.GetValueForOption(fileOption);
            if (optionValue != null)
                filePath = optionValue;
            optionValue = context.ParseResult.GetValueForOption(mqRoutingKeyOption);
            if (optionValue != null)
                applicationConfig.MqRoutingKey = optionValue;
            optionValue = context.ParseResult.GetValueForOption(mqExchangeOption);
            if (optionValue != null)
                applicationConfig.MqExchangeName = optionValue;      
            optionValue = context.ParseResult.GetValueForOption(directoryOption);
            if (optionValue != null)
                directoryPath = optionValue;   
            DoReceiveFiles(startTS, applicationConfig.MqExchangeName,  "filetransfer." + applicationConfig.MqRoutingKey, directoryPath, applicationConfig);
        });
        rootCommand.AddCommand(subCommand);
        
        await rootCommand.InvokeAsync(args);

        if (applicationConfig.Verbose)
        {
            PASLoggingServices.ConsoleMessage(startTS,
                "Exit with code =  " + returnCode.ToString() );     
        }
        return (returnCode);
    }
}
