namespace IntuneLight.Models.Offboarding;

public enum OffboardingStepStatus
{
    Pending,
    Running,
    Success,
    Skipped,
    Failed
}

public sealed record OffboardingStepResult(
    string StepName,
    OffboardingStepStatus Status,
    string? Message = null);

public enum OffboardingType 
{ 
    None, 
    RemoveAutopilot, 
    WithWipe 
}