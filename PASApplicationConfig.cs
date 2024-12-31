namespace rmqfiletransfer;

public class PasApplicationConfig
{
    public static void ReadLocalParameterFile(DateTime start, ref PasApplicationConfig pCfg)
    {
        var fileName = "settings.txt";
        var changed = 0;

        if (File.Exists(fileName))
        {
            var allLines = File.ReadAllLines(fileName);
            for (var index = 0; index < allLines.Length; index++)
            {
                var singleLine = allLines[index].Trim();
                if (singleLine.StartsWith('/'))
                {
                    singleLine = "";
                }
                switch (index)
                {
                    // mqserver
                    case 0:
                        pCfg.MqHost = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqHost;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }

                        break;
                    // mquser
                    case 1:
                        pCfg.MqLogin = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqLogin;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }
                        break;
                    // mqpassword
                    case 2:
                        pCfg.MqPassword = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqPassword;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }
                        break;
                    // pqqueue
                    case 3:
                        pCfg.MqQueueName = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqQueueName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }

                        break;
                    // mqexchange
                    case 4:
                        pCfg.MqExchangeName = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqExchangeName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }
                        break;
                    // mqroutingkey
                    case 5:
                        pCfg.MqRoutingKey = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqRoutingKey;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }
                        break;
                    // mqvhost
                    case 6:
                        pCfg.MqvHostName = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() :  pCfg.MqvHostName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pCfg.Verbose)
                        {
                        }
                       
                        break;
                    default:
                        break;
                }
            }

        }
        else
        {
            
        }
    }
        
    public string MqLogin { get; set; } = "dummy";

    public string MqPassword { get; set; } = "password";

    public string MqHost { get; set; } = "127.0.0.1";

    public string MqQueueName { get; set; } = "filetransfer";

    public ushort MqQueueSize { get; set; } = 100;
    
    public int MqMinForwardLevel { get; set; } = 9999999;

    public string MqForwardRoutingKey { get; set; } = "paslogalarm";

    public string MqvHostName { get; set; } = "/";

    public string MqExchangeName { get; set; } = "amq.topic";

    public string MqRoutingKey { get; set; } = "evfiletransfer";
    
    public bool MsgDebug { get; set; } = false;

    public bool Verbose { get; set; } = false;
    
    
    public bool MsgLog { get; set; } = false;
    
    /// <summary>
    /// Size of each package sent over rabbitmq
    /// </summary>
    public int FilePackageSize { get; set; } = 100000;

    /// <summary>
    /// Informationen über die Übertragungen
    /// </summary>
    public Dictionary<string,long> Transmissions  { get; set; } = new Dictionary<string, long>();

    
    // In diesem Feld wird der Start des Sendens oder des Empfanges festgehalten
    public Dictionary<string,DateTime> TransmissionStarts = new Dictionary<string, DateTime>();
}