using FluentAssertions;
using MiniPdm.Domain.Exceptions;
using MiniPdm.Domain.ValueObjects;

namespace MiniPdm.Tests.Domain;

public class DesignationTests
{
    [Theory]
    [InlineData("РДЦЛ.304112.300")]
    [InlineData("АБВГ.123456.789")]
    [InlineData("ЯЯЯЯ.000000.000")]
    public void Create_WithValidEskdFormat_ShouldCreateDesignation(string validCode)
    {
        // Act
        var designation = Designation.Create(validCode);

        // Assert
        designation.Value.Should().Be(validCode);
        designation.ToString().Should().Be(validCode);
        string str = designation;
        str.Should().Be(validCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("ABC1.123456.789")] // Латиница
    [InlineData("АБВ.123456.789")]  // 3 буквы вместо 4
    [InlineData("АБВГД.123456.789")] // 5 букв
    [InlineData("АБВГ.12345.789")]  // 5 цифр вместо 6
    [InlineData("АБВГ.123456.78")]  // 2 цифры в конце
    [InlineData("абвг.123456.789")]  // Строчные буквы
    [InlineData("АБВГ-123456-789")]  // Дефисы вместо точек
    public void Create_WithInvalidFormat_ShouldThrowDomainValidationException(string? invalidCode)
    {
        // Act
        var act = () => Designation.Create(invalidCode!);

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void TryCreate_WithValidCode_ShouldReturnTrueAndPopulateObject()
    {
        // Act
        var success = Designation.TryCreate("РДЦЛ.304112.300", out var designation, out var error);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        designation.Value.Should().Be("РДЦЛ.304112.300");
    }

    [Fact]
    public void TryCreate_WithInvalidCode_ShouldReturnFalseAndErrorMessage()
    {
        // Act
        var success = Designation.TryCreate("INVALID", out var designation, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }
}
