using CommandLine;

internal class CommandLineOptions
{
    
    [Option('p', "port", Required = false, Default = 5200, HelpText = "Port to proxy")]
    public int Port { get; set; }

    [Option('h', "host", Required = false, Default = "localhost", HelpText = "Hostname or IP to proxy")]
    public string Host { get; set; }

    [Option('s', "https", Required = false, Default = false, HelpText = "Is https?")]
    public bool Https { get; set; }

    [Option('l', "listen", Required = false, Default = 5200, HelpText = "Port to listen.")]
    public int Listen { get; set; }

}

