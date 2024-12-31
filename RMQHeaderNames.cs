namespace rmqfiletransfer;

public static class RMQHeaderNames
{
    /// <summary>
    /// First some constants for commmands
    /// </summary>
    public const string FileTransferCommandHeader = "pas_ft_command";
    public const string FileTransferCommand = "pas_file_transfer";

    /// <summary>
    /// You may define a topic, so that the receivers know, that they should be interested into this package ... routing key
    /// </summary>
    public const string FileTransferTopicHeader = "pas_ft_topic";
    public const string FileTransferUUIDHeader = "pas_ft_uuid";
    public const string FileTransferOriginalFilename = "pas_ft_originalfilename";
    public const string FileTransferTransferFilename = "pas_ft_transferfilename";
    /// <summary>
    /// The current index of this file transmission (zero based)
    /// </summary>
    public const string FileTransferCurrentPackageIndexHeader = "pas_ft_pkg_current_index";
    /// <summary>
    /// Defines the total size of the file to be produced (before optional uncompressing)
    /// </summary>
    public const string FileTransferFinalSizeHeader = "pas_ft_file_final_size";
    /// <summary>
    /// Defines the default size of each package ... must be used together with the index of the current package to get the location within the target file
    /// </summary>
    public const string FileTransferPackageSizeHeader = "pas_ft_pkg_size";
    /// <summary>
    /// Defines the index of the last file transfer for this file
    /// </summary>
    public const string FileTransferMaxPackageIndexHeader = "pas_ft_pkg_max_index";
    /// <summary>
    /// Uncompress the file after receiving. The following extensions should be supported: zip, tgz, gz
    /// </summary>
    public const string FileTransferUncompressHeader = "pas_ft_uncompress";
    /// <summary>
    /// Zeitstempel, wann die Datei erzeugt wurde
    /// </summary>
    public const string FileTransferFileCreationHeader = "pas_ft_creationTS";
    /// <summary>
    /// Zeitstempel, wann die Datei verändert wurde
    /// </summary>
    public const string FileTransferFileModificationHeader = "pas_ft_modifyTS";
}