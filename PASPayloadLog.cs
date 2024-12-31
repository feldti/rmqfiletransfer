namespace pas_services_logger;

public class PASPayloadLog
{
    private string? appLocation = null;
    private string? appName = null;
    private long? appProcessID = null;
    private string? appStackName = null;
    private string? appThreadName = null;
    private long? msgAppNr = null;
    private long? msgClientNr = null;
    private DateTime msgClientTS;
    private string? msgData;
    private DateTime msgImportTS;
    private int msgLevel;
    private string? msgText = null;
    private string? userName = null;
    private string? userConnectionID = null;
    private string? userSessionID = null;
    private string? msgTopic;
    private string? msgSubTopic;
    private long? msgIntVal = null;
    private string? customerID = null;
    private bool msgDebug = false;

    public bool MsgDebug
    {
        get => msgDebug;
        set => msgDebug = value;
    }

    public string? CustomerId
    {
        get => customerID;
        set => customerID = value;
    }

    public string? MsgTopic
    {
        get => msgTopic;
        set => msgTopic = value;
    }

    public string? MsgSubTopic
    {
        get => msgSubTopic;
        set => msgSubTopic = value;
    }

    public long? MsgIntVal
    {
        get => msgIntVal;
        set => msgIntVal = value;
    }
    
    public string? UserSessionId
    {
        get => userSessionID;
        set => userSessionID = value;
    }

    public string? UserConnectionId
    {
        get => userConnectionID;
        set => userConnectionID = value;
    }

    public string? UserName
    {
        get => userName;
        set => userName = value;
    }

    public string? MsgText
    {
        get => msgText;
        set => msgText = value;
    }

    public int MsgLevel
    {
        get => msgLevel;
        set => msgLevel = value;
    }

    public DateTime MsgImportTs
    {
        get => msgImportTS;
        set => msgImportTS = value;
    }
    
    public string? MsgData
    {
        get => msgData;
        set => msgData = value;
    }

    public DateTime MsgClientTs
    {
        get => msgClientTS;
        set => msgClientTS = value;
    }

    public long? MsgClientNr
    {
        get => msgClientNr;
        set => msgClientNr = value;
    }

    public long? MsgAppNr
    {
        get => msgAppNr;
        set => msgAppNr = value;
    }

    public string? AppThreadName
    {
        get => appThreadName;
        set => appThreadName = value;
    }

    public string? AppStackName
    {
        get => appStackName;
        set => appStackName = value;
    }

    public long? AppProcessId
    {
        get => appProcessID;
        set => appProcessID = value;
    }

    public string? AppName
    {
        get => appName;
        set => appName = value;
    }

    public string? AppLocation
    {
        get => appLocation;
        set => appLocation = value;
    }
}