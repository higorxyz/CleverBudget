using CleverBudget.Application.Validators;
using CleverBudget.Core.DTOs;
using FluentValidation.TestHelper;
using Xunit;

namespace CleverBudget.Tests.Validators;

public class UpdateProfileDtoValidatorTests
{
    private readonly UpdateProfileDtoValidator _validator;

    public UpdateProfileDtoValidatorTests()
    {
        _validator = new UpdateProfileDtoValidator();
    }

    [Fact]
    public void FirstName_WhenEmpty_ShouldHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "", LastName = "Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WhenTooShort_ShouldHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "J", LastName = "Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WhenTooLong_ShouldHaveError()
    {
        var dto = new UpdateProfileDto 
        { 
            FirstName = new string('A', 101), 
            LastName = "Doe" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_WithValidLength_ShouldNotHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "John", LastName = "Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void LastName_WhenEmpty_ShouldHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "John", LastName = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void LastName_WhenTooShort_ShouldHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "John", LastName = "D" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void LastName_WhenTooLong_ShouldHaveError()
    {
        var dto = new UpdateProfileDto 
        { 
            FirstName = "John", 
            LastName = new string('A', 101) 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void LastName_WithValidLength_ShouldNotHaveError()
    {
        var dto = new UpdateProfileDto { FirstName = "John", LastName = "Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void ValidDto_ShouldNotHaveAnyErrors()
    {
        var dto = new UpdateProfileDto 
        { 
            FirstName = "Jane", 
            LastName = "Smith" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class ChangePasswordDtoValidatorTests
{
    private readonly ChangePasswordDtoValidator _validator;

    public ChangePasswordDtoValidatorTests()
    {
        _validator = new ChangePasswordDtoValidator();
    }

    [Fact]
    public void CurrentPassword_WhenEmpty_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "", 
            NewPassword = "NewPass123", 
            ConfirmPassword = "NewPass123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void NewPassword_WhenEmpty_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "", 
            ConfirmPassword = "" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WhenTooShort_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "Abc1", 
            ConfirmPassword = "Abc1" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WithoutUppercase_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "newpass123", 
            ConfirmPassword = "newpass123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WithoutLowercase_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NEWPASS123", 
            ConfirmPassword = "NEWPASS123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WithoutNumber_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPassword", 
            ConfirmPassword = "NewPassword" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WithValidFormat_ShouldNotHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPass123", 
            ConfirmPassword = "NewPass123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ConfirmPassword_WhenEmpty_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPass123", 
            ConfirmPassword = "" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void ConfirmPassword_WhenDoesNotMatch_ShouldHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPass123", 
            ConfirmPassword = "DifferentPass123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void ConfirmPassword_WhenMatches_ShouldNotHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPass123", 
            ConfirmPassword = "NewPass123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void ValidDto_ShouldNotHaveAnyErrors()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "CurrentPass123", 
            NewPassword = "BrandNewPass456", 
            ConfirmPassword = "BrandNewPass456" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NewPassword_WithMinimumRequirements_ShouldNotHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "Abc123", 
            ConfirmPassword = "Abc123" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void NewPassword_WithSpecialCharacters_ShouldNotHaveError()
    {
        var dto = new ChangePasswordDto 
        { 
            CurrentPassword = "OldPass123", 
            NewPassword = "NewPass123!@#", 
            ConfirmPassword = "NewPass123!@#" 
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }
}
