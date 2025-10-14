using SpreadAggregator.Domain.Services;
using Xunit;

namespace SpreadAggregator.Tests.Domain.Services;

public class VolumeFilterTests
{
    private readonly VolumeFilter _volumeFilter;
    private const decimal MinVolume = 10_000_000;
    private const decimal MaxVolume = 100_000_000;

    public VolumeFilterTests()
    {
        _volumeFilter = new VolumeFilter(MinVolume, MaxVolume);
    }

    [Theory]
    [InlineData(15_000_000)]      // Inside the range
    [InlineData(10_000_000)]      // Exactly at the lower bound
    [InlineData(100_000_000)]     // Exactly at the upper bound
    public void IsVolumeSufficient_WhenVolumeIsInRange_ShouldReturnTrue(decimal volume)
    {
        // Act
        var result = _volumeFilter.IsVolumeSufficient(volume);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(9_999_999.99)] // Just below the range
    [InlineData(100_000_000.01)] // Just above the range
    [InlineData(0)]              // Zero volume
    public void IsVolumeSufficient_WhenVolumeIsOutOfRange_ShouldReturnFalse(decimal volume)
    {
        // Act
        var result = _volumeFilter.IsVolumeSufficient(volume);

        // Assert
        Assert.False(result);
    }
}