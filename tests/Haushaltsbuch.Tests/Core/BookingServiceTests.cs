using FluentAssertions;
using Haushaltsbuch.Core.Abstractions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Tests.Support;
using NSubstitute;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class BookingServiceTests
{
    private readonly IBookingRepository _repo = Substitute.For<IBookingRepository>();
    private readonly BookingService _service;

    public BookingServiceTests() => _service = new BookingService(_repo);

    [Fact]
    public async Task Add_ValidBooking_TrimsDescriptionAndSaves()
    {
        _repo.AddAsync(Arg.Any<Booking>()).Returns(ci => ci.Arg<Booking>() with { Id = 7 });

        var saved = await _service.AddAsync(TestData.Expense(10m, description: "  Brot  "));

        saved.Id.Should().Be(7);
        await _repo.Received(1).AddAsync(Arg.Is<Booking>(b => b.Description == "Brot"));
    }

    [Fact]
    public async Task Add_InvalidBooking_ThrowsValidationExceptionAndDoesNotSave()
    {
        var act = () => _service.AddAsync(TestData.Expense(-1m));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().NotBeEmpty();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Update_InvalidBooking_ThrowsAndDoesNotSave()
    {
        var act = () => _service.UpdateAsync(TestData.Expense(10m, description: " ", id: 3));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task Update_ValidBooking_IsForwarded()
    {
        await _service.UpdateAsync(TestData.Expense(10m, id: 3));

        await _repo.Received(1).UpdateAsync(Arg.Is<Booking>(b => b.Id == 3));
    }

    [Fact]
    public async Task Delete_IsForwarded()
    {
        await _service.DeleteAsync(5);

        await _repo.Received(1).DeleteAsync(5);
    }

    [Fact]
    public async Task GetAll_ReturnsOldestFirst()
    {
        _repo.GetAllAsync().Returns([
            TestData.Expense(1m, date: "2026-01-01", id: 1),
            TestData.Expense(1m, date: "2026-03-01", id: 2),
            TestData.Expense(1m, date: "2026-03-01", id: 3)
        ]);

        var result = await _service.GetAllAsync();

        result.Select(b => b.Id).Should().Equal(1, 2, 3);
    }
}
