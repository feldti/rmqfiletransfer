namespace pas_services_logger;

public class PASMessageCarrier
{
    public  static string EVLOG = "evlog";
    public static string EVLOGALARM = "evlogalarm";
    public  static string EVSTDAT = "evstatdat";
    private string name = "";
    private string payload = "";
    private string payloadType = "";

    public string PayloadType
    {
        get => payloadType;
        set => payloadType = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Payload
    {
        get => payload;
        set => payload = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Name
    {
        get => name;
        set => name = value ?? throw new ArgumentNullException(nameof(value));
    }
}