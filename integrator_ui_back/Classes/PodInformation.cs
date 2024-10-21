namespace integrator_ui_back.Classes;

public class PodInformation
{
    public string Name { get; set; }

    public bool IsRunning { get; set; }

    public string ContainerStatus { get; set; }

    public DateTime? LastStartTime { get; set; }

    public int? RestartCount { get; set; }
}
