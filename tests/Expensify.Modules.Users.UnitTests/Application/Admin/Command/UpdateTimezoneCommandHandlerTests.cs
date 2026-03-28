using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Admin.Command.UpdateTimezone;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Domain.Preferences;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Command;

[TestFixture]
internal sealed class UpdateTimezoneCommandHandlerTests
{
    private ITimezoneRepository _timezoneRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private UpdateTimezoneCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _timezoneRepository = Substitute.For<ITimezoneRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateTimezoneCommandHandler(_timezoneRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenReplacingDefault_ShouldDemotePreviousDefault()
    {
        var command = new UpdateTimezoneCommand("Europe/London", "Europe/London", true, true, 1);
        var currentDefault = Timezone.Create("UTC", "UTC", true, true, 0);
        var london = Timezone.Create("Europe/London", "Europe/London", true, false, 1);

        _timezoneRepository.GetByIdAsync("Europe/London", Arg.Any<CancellationToken>()).Returns(london);
        _timezoneRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.TimezoneResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(currentDefault.IsDefault, Is.False);
            Assert.That(london.IsDefault, Is.True);
        }
    }

    [Test]
    public async Task Handle_WhenDeactivatingCurrentDefault_ShouldReturnFailure()
    {
        var command = new UpdateTimezoneCommand("UTC", "UTC", false, false, 0);
        var currentDefault = Timezone.Create("UTC", "UTC", true, true, 0);

        _timezoneRepository.GetByIdAsync("UTC", Arg.Any<CancellationToken>()).Returns(currentDefault);
        _timezoneRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.TimezoneResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(PreferenceCatalogErrors.DefaultTimezoneRequired()));
        }
    }
}
