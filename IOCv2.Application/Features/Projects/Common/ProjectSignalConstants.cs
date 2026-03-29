namespace IOCv2.Application.Features.Projects.Common;

/// <summary>
/// AC-13: Constants cho SignalR real-time signal types và actions.
/// Payload: { "type": "ProjectListChanged", "action": "created|updated|...", "projectId": "..." }
/// </summary>
public static class ProjectSignalConstants
{
    public const string ProjectListChanged = "ProjectListChanged";

    public static class Actions
    {
        public const string Created      = "created";
        public const string Updated      = "updated";
        public const string Deleted      = "deleted";
        public const string Published    = "published";
        public const string Unpublished  = "unpublished";
        public const string Completed    = "completed";
        public const string Archived     = "archived";
        public const string GroupAssigned = "groupAssigned";
        public const string GroupSwapped  = "groupSwapped";
    }
}
