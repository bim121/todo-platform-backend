using TodoPlatform.Application.Admin.Commands.SwitchTenantTrack;

namespace TodoPlatform.Application.Tests.Admin;

public sealed class AdminStubHandlerTests
{
    [Fact]
    public async Task SwitchTenantTrack_ReturnsStubTenantWithRequestedTrack()
    {
        var handler = new SwitchTenantTrackHandler();
        var result = await handler.Handle(
            new SwitchTenantTrackCommand("tenant-1", "green"),
            CancellationToken.None);

        Assert.Equal("tenant-1", result.Id);
        Assert.Equal("green", result.DeploymentTrack);
        Assert.Equal("active", result.Status);
    }

    [Fact]
    public void SwitchTenantTrackValidator_RejectsInvalidTrack()
    {
        var validator = new SwitchTenantTrackCommandValidator();
        var result = validator.Validate(new SwitchTenantTrackCommand("tenant-1", "purple"));

        Assert.False(result.IsValid);
    }
}
