using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Admin.Command.CreateCurrency;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Domain.Preferences;
using NSubstitute;

namespace Expensify.Modules.Users.UnitTests.Application.Admin.Command;

[TestFixture]
internal sealed class CreateCurrencyCommandHandlerTests
{
    private ICurrencyRepository _currencyRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateCurrencyCommandHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _currencyRepository = Substitute.For<ICurrencyRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new CreateCurrencyCommandHandler(_currencyRepository, _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenCreatingNewDefault_ShouldClearExistingDefault()
    {
        var command = new CreateCurrencyCommand("USD", "US Dollar", "$", 2, true, true, 1);
        var existingDefault = Currency.Create("GBP", "British Pound", "GBP", 2, true, true, 0);

        _currencyRepository.GetByIdAsync("USD", Arg.Any<CancellationToken>()).Returns((Currency?)null);
        _currencyRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(existingDefault);

        Result<Expensify.Modules.Users.Application.Abstractions.CurrencyResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(existingDefault.IsDefault, Is.False);
        }

        _currencyRepository.Received(1).Update(existingDefault);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDefaultCurrencyWouldBeInactive_ShouldReturnFailure()
    {
        var command = new CreateCurrencyCommand("USD", "US Dollar", "$", 2, false, true, 1);

        Result<Expensify.Modules.Users.Application.Abstractions.CurrencyResponse> result = await _sut.Handle(command, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error, Is.EqualTo(PreferenceCatalogErrors.CurrencyMustRemainActiveWhenDefault()));
        }
    }
}
