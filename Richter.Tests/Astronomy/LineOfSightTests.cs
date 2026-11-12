using Richter.Domain.Astronomy;

namespace Richter.Tests.Astronomy;

/// <summary>
/// Checks which straight lines a sphere blocks, including lines that start on its surface, as a
/// ground station's do.
/// </summary>
public class LineOfSightTests
{
    [Theory]
    [InlineData(-2.0, 0.0, 2.0, 0.0, true)]
    [InlineData(-2.0, 0.999, 2.0, 0.999, true)]
    [InlineData(-2.0, 1.5, 2.0, 1.5, false)]
    [InlineData(1.0, 0.0, 3.0, 0.0, false)]
    [InlineData(1.0, 0.0, -3.0, 0.0, true)]
    [InlineData(-3.0, 0.0, -2.0, 0.0, false)]
    public void BlocksOnlyALineThatPassesInsideTheSphere(Double startX, Double startY, Double endX, Double endY, Boolean blocked)
    {
        // A unit sphere at the origin. The fourth and fifth cases start on its surface, as a
        // ground station does.
        Boolean result = LineOfSight.IsBlockedBySphere(new Vector3D(startX, startY, 0), new Vector3D(endX, endY, 0), Vector3D.Zero, 1);

        Assert.Equal(blocked, result);
    }
}
