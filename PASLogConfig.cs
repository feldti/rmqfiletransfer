using rmqfiletransfer;

namespace pas_services_logger;

public class PASLogConfig
{
    public static void ReadLocalParameterFile(DateTime start, ref PASLogConfig pCfg, ref PasApplicationConfig pACfg)
    {
        var fileName = "log-settings.txt";
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
                        if (pACfg.Verbose)
                        {
                        }

                        break;
                    // mquser
                    case 1:
                        pCfg.MqLogin = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqLogin;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pACfg.Verbose)
                        {
                        }
                        break;
                    // mqpassword
                    case 2:
                        pCfg.MqPassword = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqPassword;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pACfg.Verbose)
                        {
                        }
                        break;
                    // mqexchange
                    case 4:
                        pCfg.MqExchangeName = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqExchangeName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pACfg.Verbose)
                        {
                        }
                        break;
                    // mqroutingkey
                    case 5:
                        pCfg.MqRoutingKey = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() : pCfg.MqRoutingKey;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pACfg.Verbose)
                        {
                        }
                        break;
                    // mqvhost
                    case 6:
                        pCfg.MqvHostName = !String.IsNullOrEmpty(singleLine) ? singleLine.Trim() :  pCfg.MqvHostName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        if (pACfg.Verbose)
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

    public string MqHost { get; set; } = "host";
    
    public string MqvHostName { get; set; } = "/";

    public string MqExchangeName { get; set; } = "amq.topic";

    public string MqRoutingKey { get; set; } = "evlog"; 
    
    public bool MqNoLog { get; set; } = true; 
    
    public int MqMinLogLevel { get; set; } = 30; 
}