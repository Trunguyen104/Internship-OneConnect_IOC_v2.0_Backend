namespace IOCv2.Domain.Enums;

public enum SprintStatus : short
{
    Planned = 1,    // Sprint created but not started
    Active = 2,     // Sprint is currently running
    Completed = 3   // Sprint has been completed
}
