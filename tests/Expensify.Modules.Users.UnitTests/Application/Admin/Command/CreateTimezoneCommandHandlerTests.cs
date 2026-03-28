using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Admin.Command.CreateTimezone;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Preferences;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Command;

[TestFixture]
internal sealed class CreateTimezoneCommandHandlerTests
{
    private ITimezoneRepository _timezoneRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateTimezoneCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _timezoneRepository = Substitute.For<ITimezoneRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateTimezoneCommandHandler(_timezoneRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenCreatingNewDefault_ShouldClearExistingDefault()
    {
        var command = new CreateTimezoneCommand("Europe/London", "Europe/London", true, true, 1);
        var currentDefault = Timezone.Create("UTC", "UTC", true, true, 0);

        _timezoneRepository.GetByIdAsync("Europe/London", Arg.Any<CancellationToken>()).Returns((Timezone?)null);
        _timezoneRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.TimezoneResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(currentDefault.IsDefault, Is.False);
        }
    }

    [Test]
    public async Task Handle_WhenDefaultTimezoneWouldBeInactive_ShouldReturnFailure()
    {
        var command = new CreateTimezoneCommand("Europe/London", "Europe/London", false, true, 1);

        Result<Expensify.Modules.Users.Application.Abstractions.TimezoneResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(PreferenceCatalogErrors.TimezoneMustRemainActiveWhenDefault()));
        }
    }
}
