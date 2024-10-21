namespace integrator_ui_back.Classes;

public class DeploymentInformation
{
    public string Id { get; set; }
    public string DeploymentName { get; set; }
    public string WorkerUrl { get; set; }
    public string Namespace { get; set; }
    public List<ContainerInformation> ContainerInformationList { get; set; }
    public List<PodInformation> PodInformationList { get; set; }
}
