using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Admin.Command.UpdateCurrency;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Preferences;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Command;

[TestFixture]
internal sealed class UpdateCurrencyCommandHandlerTests
{
    private ICurrencyRepository _currencyRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private UpdateCurrencyCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _currencyRepository = Substitute.For<ICurrencyRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new UpdateCurrencyCommandHandler(_currencyRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenReplacingDefault_ShouldDemotePreviousDefault()
    {
        var command = new UpdateCurrencyCommand("USD", "US Dollar", "$", 2, true, true, 1);
        var currentDefault = Currency.Create("GBP", "British Pound", "GBP", 2, true, true, 0);
        var usd = Currency.Create("USD", "US Dollar", "$", 2, true, false, 1);

        _currencyRepository.GetByIdAsync("USD", Arg.Any<CancellationToken>()).Returns(usd);
        _currencyRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.CurrencyResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(currentDefault.IsDefault, Is.False);
            Assert.That(usd.IsDefault, Is.True);
        }
    }

    [Test]
    public async Task Handle_WhenDeactivatingCurrentDefault_ShouldReturnFailure()
    {
        var command = new UpdateCurrencyCommand("GBP", "British Pound", "GBP", 2, false, false, 0);
        var currentDefault = Currency.Create("GBP", "British Pound", "GBP", 2, true, true, 0);

        _currencyRepository.GetByIdAsync("GBP", Arg.Any<CancellationToken>()).Returns(currentDefault);
        _currencyRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.CurrencyResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(PreferenceCatalogErrors.DefaultCurrencyRequired()));
        }
    }
}
