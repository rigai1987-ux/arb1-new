namespace SpreadAggregator.Domain.Services;

/// <summary>
/// Filters trading pairs based on their 24-hour trading volume.
/// </summary>
public class VolumeFilter
{
    private readonly decimal _minVolume;
    private readonly decimal _maxVolume;

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeFilter"/> class.
    /// </summary>
    /// <param name="minVolume">The minimum allowed 24-hour volume in USD.</param>
    /// <param name="maxVolume">The maximum allowed 24-hour volume in USD.</param>
    public VolumeFilter(decimal minVolume, decimal maxVolume)
    {
        _minVolume = minVolume;
        _maxVolume = maxVolume;
    }

    /// <summary>
    /// Checks if the provided volume is within the configured range.
    /// </summary>
    /// <param name="volume">The 24-hour volume of a trading pair.</param>
    /// <returns><c>true</c> if the volume is sufficient; otherwise, <c>false</c>.</returns>
    public bool IsVolumeSufficient(decimal volume)
    {
        return volume >= _minVolume && volume <= _maxVolume;
    }
}