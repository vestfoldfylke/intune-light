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

public enum OffboardingRoutine
{
    None,
    Privatization,
    Deletion
}

public enum OffboardingMethod
{
    None,
    RemoveAutopilot,
    WithWipe,
    ForceDelete
}

public sealed record OffboardingSelection(OffboardingRoutine Routine, OffboardingMethod Method);