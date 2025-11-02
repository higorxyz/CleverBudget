using CleverBudget.Application.Validators;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace CleverBudget.Tests.Validators;

public class CreateTransactionDtoValidatorTests
{
    private readonly CreateTransactionDtoValidator _validator;

    public CreateTransactionDtoValidatorTests()
    {
        _validator = new CreateTransactionDtoValidator();
    }

    [Fact]
    public void Amount_WhenZero_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Amount = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenNegative_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Amount = -10 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenTooLarge_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Amount = 1000001 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Amount_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateTransactionDto { Amount = 100 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Description_WhenEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Description = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_WhenTooShort_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Description = "ab" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_WhenTooLong_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Description = new string('a', 501) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateTransactionDto { Description = "Valid description" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CategoryId_WhenZero_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { CategoryId = 0 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CategoryId_WhenNegative_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { CategoryId = -1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void CategoryId_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateTransactionDto { CategoryId = 1 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Date_WhenInFuture_ShouldHaveValidationError()
    {
        var dto = new CreateTransactionDto { Date = DateTime.Now.AddDays(2) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Date_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateTransactionDto { Date = DateTime.Now };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Type_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateTransactionDto { Type = TransactionType.Expense };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }
}
