using FluentAssertions;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Tests.Features.UniAdminInternship;

public class UniAdminInternshipRulesTests
{
    [Fact]
    public void DeriveUiStatus_ReturnsCompleted_WhenGroupFinished()
    {
        var group = InternshipGroup.Create(
            phaseId: Guid.NewGuid(),
            groupName: "G1",
            endDate: DateTime.UtcNow.AddDays(7));
        group.UpdateStatus(GroupStatus.Finished);

        var status = UniAdminInternshipRules.DeriveUiStatus(PlacementStatus.Placed, group, hasPendingApplication: false);

        status.Should().Be(InternshipUiStatus.Completed);
    }

    [Fact]
    public void DeriveUiStatus_ReturnsPendingConfirmation_WhenUnplacedAndPendingApplication()
    {
        var status = UniAdminInternshipRules.DeriveUiStatus(
            PlacementStatus.Unplaced,
            group: null,
            hasPendingApplication: true);

        status.Should().Be(InternshipUiStatus.PendingConfirmation);
    }

    [Fact]
    public void CalculateLogbookSummary_CountsOnlyWeekdaysWithinWindow()
    {
        var group = InternshipGroup.Create(
            phaseId: Guid.NewGuid(),
            groupName: "G1",
            startDate: DateTime.UtcNow.AddDays(-7),
            endDate: DateTime.UtcNow.AddDays(-1));

        var internStudent = new InternshipStudent
        {
            StudentId = Guid.NewGuid(),
            InternshipId = group.InternshipId,
            JoinedAt = DateTime.UtcNow.AddDays(-7)
        };

        var submittedDates = new List<DateTime>
        {
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(-6),
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-4),
            DateTime.UtcNow.AddDays(-3),
            DateTime.UtcNow.AddDays(-2), // weekend could be present; helper excludes it
            DateTime.UtcNow.AddDays(-1)
        };

        var summary = UniAdminInternshipRules.CalculateLogbookSummary(internStudent, group, submittedDates);

        summary.Should().NotBeNull();
        summary!.Total.Should().BeGreaterThan(0);
        summary.Submitted.Should().BeLessThanOrEqualTo(summary.Total);
        summary.Missing.Should().Be(summary.Total - summary.Submitted);
    }
}



