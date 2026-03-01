namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public enum MoveIncompleteItemsOption
{
    ToBacklog = 1,            // Move to Product Backlog
    ToNextPlannedSprint = 2,  // Move to next Planned sprint (if exists)
    CreateNewSprint = 3       // Create new sprint and move there
}
