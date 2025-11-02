using CleverBudget.Application.Validators;
using CleverBudget.Core.DTOs;
using FluentValidation.TestHelper;
using Xunit;

namespace CleverBudget.Tests.Validators;

public class CreateBudgetDtoValidatorTests
{
    private readonly CreateBudgetDtoValidator _validator;

    public CreateBudgetDtoValidatorTests()
    {
        _validator = new CreateBudgetDtoValidator();
    }

    [Fact]
    public void CategoryId_WhenZero_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { CategoryId = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CategoryId_WhenNegative_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { CategoryId = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CategoryId_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateBudgetDto { CategoryId = 1 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Amount_WhenZero_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Amount = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenNegative_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Amount = -10 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenTooLarge_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Amount = 1000001 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateBudgetDto { Amount = 1000 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Month_WhenZero_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Month = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Month_WhenGreaterThan12_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Month = 13 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Month_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateBudgetDto { Month = 6 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Year_WhenTooOld_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Year = 2019 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Year_WhenTooFarInFuture_ShouldHaveValidationError()
    {
        var dto = new CreateBudgetDto { Year = DateTime.UtcNow.Year + 10 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Year_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateBudgetDto { Year = DateTime.UtcNow.Year };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }
}

public class UpdateBudgetDtoValidatorTests
{
    private readonly UpdateBudgetDtoValidator _validator;

    public UpdateBudgetDtoValidatorTests()
    {
        _validator = new UpdateBudgetDtoValidator();
    }

    [Fact]
    public void Amount_WhenNull_ShouldNotHaveValidationError()
    {
        var dto = new UpdateBudgetDto { Amount = null };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenZero_ShouldHaveValidationError()
    {
        var dto = new UpdateBudgetDto { Amount = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Amount.Value");
    }

    [Fact]
    public void Amount_WhenTooLarge_ShouldHaveValidationError()
    {
        var dto = new UpdateBudgetDto { Amount = 1000001 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Amount.Value");
    }

    [Fact]
    public void Amount_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new UpdateBudgetDto { Amount = 1000 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }
}
