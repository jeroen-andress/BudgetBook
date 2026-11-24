using FluentAssertions;
using Haushaltsbuch.Core.Domain;
using Haushaltsbuch.Core.Services;
using Haushaltsbuch.Tests.Support;
using Xunit;

namespace Haushaltsbuch.Tests.Core;

public class CategoryServiceTests
{
    [Fact]
    public async Task Add_NewName_IsTrimmedAndSaved()
    {
        var service = new CategoryService(new InMemoryCategoryRepository(["Miete"]));

        var created = await service.AddAsync("  Reisen ");

        created.Name.Should().Be("Reisen");
        (await service.GetAllAsync()).Select(c => c.Name).Should().Contain("Reisen");
    }

    [Theory]
    [InlineData("miete")]
    [InlineData("MIETE ")]
    public async Task Add_DuplicateIgnoringCase_IsRejected(string name)
    {
        var service = new CategoryService(new InMemoryCategoryRepository(["Miete"]));

        var act = () => service.AddAsync(name);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*existiert bereits*");
    }

    [Fact]
    public async Task Add_BlankName_IsRejected()
    {
        var service = new CategoryService(new InMemoryCategoryRepository([]));

        await service.Invoking(s => s.AddAsync("  ")).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Delete_CategoryInUse_IsRejected()
    {
        var repo = new InMemoryCategoryRepository(["Miete"], isInUse: _ => true);
        var service = new CategoryService(repo);

        await service.Invoking(s => s.DeleteAsync(1)).Should().ThrowAsync<ValidationException>();
        (await service.GetAllAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_UnusedCategory_IsRemoved()
    {
        var service = new CategoryService(new InMemoryCategoryRepository(["Miete", "Freizeit"]));

        await service.DeleteAsync(2);

        (await service.GetAllAsync()).Select(c => c.Name).Should().Equal("Miete");
    }

    [Fact]
    public async Task GetAll_IsSortedByName()
    {
        var service = new CategoryService(new InMemoryCategoryRepository(["Miete", "Freizeit", "Gehalt"]));

        (await service.GetAllAsync()).Select(c => c.Name).Should().Equal("Freizeit", "Gehalt", "Miete");
    }
}
