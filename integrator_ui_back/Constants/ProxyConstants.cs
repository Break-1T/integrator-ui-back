namespace integrator_ui_back.Constants;

public class ProxyConstants
{
    public const string ProxyPath = "/proxy";

    /// <summary>
    /// The yarp integrator host
    /// </summary>
    /// <remarks>
    /// This variable defines the hostname for the YARP reverse proxy to access the local integration services. 
    /// Set it to integrator.local to ensure proper routing and forwarding of requests within the proxy configuration.
    /// </remarks>
    public const string YARP_INTEGRATOR_HOST = "YARP_INTEGRATOR_HOST";
}
