
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using RabbitMQ.Client;
using rmqfiletransfer;

namespace pas_services_logger;

public class PASLoggingServices
{
    public static PASPayloadLog CreateLogMessage(string pMsgText, int pMsgLevel)
    {
        var newLogMessage = new PASPayloadLog();
        newLogMessage.MsgClientTs = DateTime.UtcNow;
        newLogMessage.MsgLevel = pMsgLevel;
        newLogMessage.MsgText = pMsgText;
        
        return newLogMessage;
    }
    
    public static void ReadLocalLogSettingsParameterFile(DateTime start, ref PASLogConfig lCfg)
    {
        var fileName = "logs-setting.txt";
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
                        lCfg.MqHost = !String.IsNullOrEmpty(singleLine) ? singleLine : lCfg.MqHost;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mquser
                    case 1:
                        lCfg.MqLogin = !String.IsNullOrEmpty(singleLine) ? singleLine : lCfg.MqLogin;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqpassword
                    case 2:
                        lCfg.MqPassword = !String.IsNullOrEmpty(singleLine) ? singleLine : lCfg.MqPassword;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqexchange
                    case 3:
                        lCfg.MqExchangeName = !String.IsNullOrEmpty(singleLine) ? singleLine : lCfg.MqExchangeName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqroutingkey
                    case 4:
                        lCfg.MqRoutingKey = !String.IsNullOrEmpty(singleLine) ? singleLine : lCfg.MqRoutingKey;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqvhost
                    case 5:
                        lCfg.MqvHostName = !String.IsNullOrEmpty(singleLine) ? singleLine :  lCfg.MqvHostName;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqvhost
                    case 6:
                        lCfg.MqNoLog = !String.IsNullOrEmpty(singleLine) ? singleLine.ToLower().Trim() == "true" : lCfg.MqNoLog ;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                    // mqlMinLogLevel
                    case 7:
                        lCfg.MqMinLogLevel = !String.IsNullOrEmpty(singleLine) ? int.Parse(singleLine) : lCfg.MqMinLogLevel ;
                        changed += String.IsNullOrEmpty(singleLine) ? 0 : 1;
                        break;
                }
            }

        }
        PASLoggingServices.ConsoleMessage(DateTime.Now, "Log Settings found and read. # of attributes changed: " + changed);
    }
        

    
    public static void ConsoleMessage(DateTime start, string message)
    {
        var now = DateTime.Now;
        var duration = now - start;
        Console.WriteLine(now.ToString("HH:mm:ss.fff ") + $"{Math.Truncate(duration.TotalMilliseconds),6}" + ": " + message);
    }
    
    public static void SendLogMessages(IModel channel, string mqExchangeName, string mqRoutingKey, List<PASPayloadLog> listOfMessages)
    {
        foreach (var eachMessage in listOfMessages)
        {
            SendSingleLogMessage(channel, mqExchangeName, mqRoutingKey, eachMessage);
        }
    }

    public static void SendSingleLogMessage(IModel channel, string mqExchangeName, string mqRoutingKey, PASPayloadLog eachMessage)
    {
        var messageCarrier = new PASMessageCarrier
        {
            Name = PASMessageCarrier.EVLOG,
            PayloadType = "paspayloadlog",
            Payload = JsonConvert.SerializeObject(eachMessage)
        };
        var json = JsonConvert.SerializeObject(messageCarrier);
        channel.BasicPublish(mqExchangeName, mqRoutingKey, true, null, System.Text.Encoding.UTF8.GetBytes(json));
        channel.WaitForConfirmsOrDie();
    }
}