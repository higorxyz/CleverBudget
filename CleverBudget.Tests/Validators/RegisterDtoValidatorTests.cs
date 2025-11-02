using CleverBudget.Application.Validators;
using CleverBudget.Core.DTOs;
using FluentValidation.TestHelper;
using Xunit;

namespace CleverBudget.Tests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator;

    public RegisterDtoValidatorTests()
    {
        _validator = new RegisterDtoValidator();
    }

    [Fact]
    public void FirstName_WhenEmpty_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { FirstName = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WhenTooShort_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { FirstName = "a" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WhenTooLong_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { FirstName = new string('a', 51) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new RegisterDto { FirstName = "John" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastName_WhenEmpty_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { LastName = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void LastName_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new RegisterDto { LastName = "Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Email_WhenEmpty_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Email = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenInvalidFormat_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Email = "invalid-email" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new RegisterDto { Email = "test@example.com" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_WhenTooShort_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Password = "Ab1" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenNoUppercase_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Password = "abcdef1" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenNoLowercase_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Password = "ABCDEF1" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenNoNumber_ShouldHaveValidationError()
    {
        var dto = new RegisterDto { Password = "Abcdefg" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_WhenValid_ShouldNotHaveValidationError()
    {
        var dto = new RegisterDto { Password = "Password123" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ConfirmPassword_WhenDoesNotMatch_ShouldHaveValidationError()
    {
        var dto = new RegisterDto
        {
            Password = "Password123",
            ConfirmPassword = "DifferentPassword123"
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void ConfirmPassword_WhenMatches_ShouldNotHaveValidationError()
    {
        var dto = new RegisterDto
        {
            Password = "Password123",
            ConfirmPassword = "Password123"
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}
