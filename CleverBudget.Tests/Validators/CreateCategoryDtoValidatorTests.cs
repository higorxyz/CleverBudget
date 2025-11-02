using CleverBudget.Application.Validators;
using CleverBudget.Core.DTOs;
using FluentValidation.TestHelper;
using Xunit;

namespace CleverBudget.Tests.Validators;

public class CreateCategoryDtoValidatorTests
{
    private readonly CreateCategoryDtoValidator _validator;

    public CreateCategoryDtoValidatorTests()
    {
        _validator = new CreateCategoryDtoValidator();
    }

    [Fact]
    public void Name_WhenEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenTooShort_ShouldHaveValidationError()
    {
        var dto = new CreateCategoryDto { Name = "a" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenTooLong_ShouldHaveValidationError()
    {
        var dto = new CreateCategoryDto { Name = new string('a', 101) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateCategoryDto { Name = "Food" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Icon_WhenTooLong_ShouldHaveValidationError()
    {
        var dto = new CreateCategoryDto { Icon = new string('a', 51) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Icon);
    }

    [Fact]
    public void Icon_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new CreateCategoryDto { Icon = "icon-food" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Icon);
    }

    [Fact]
    public void Color_WhenInvalidFormat_ShouldHaveValidationError()
    {
        var dto = new CreateCategoryDto { Color = "red" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Color_WhenValidHex_ShouldNotHaveValidationError()
    {
        var dto = new CreateCategoryDto { Color = "#FF5733" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Color_WhenEmpty_ShouldNotHaveValidationError()
    {
        var dto = new CreateCategoryDto { Color = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }
}
