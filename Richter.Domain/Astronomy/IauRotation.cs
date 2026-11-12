namespace Richter.Domain.Astronomy;

/// <summary>
/// Converts the IAU's rotation elements, the published form of a body's spin, to a
/// <see cref="Rotation"/>.
/// </summary>
/// <remarks>
/// The IAU gives a body's pole as a right ascension and declination against Earth's J2000
/// equator, and its prime meridian W as an angle measured anticlockwise about that pole from
/// where the body's equator rises through Earth's equator, 90° on from the pole's right
/// ascension. The pole is turned into the system's frame, and W is measured again from the
/// system's reference direction projected onto the body's equator. A negative rate means the body
/// spins backwards, so the pole is flipped to follow the right-hand rule, which makes latitudes
/// and longitudes on that body the negatives of the IAU's.
/// </remarks>
public static class IauRotation
{
    /// <param name="poleRightAscension"> α₀, in degrees. </param>
    /// <param name="poleDeclination"> δ₀, in degrees. </param>
    /// <param name="primeMeridian"> W at <paramref name="epoch"/>, in degrees. </param>
    /// <param name="degreesPerDay"> W's rate; negative for a body that spins backwards. </param>
    /// <param name="epoch"> The instant the elements describe, normally J2000. </param>
    /// <param name="referencePlaneTilt">
    /// How far, in degrees, the system's reference plane is tilted from Earth's J2000 equator
    /// about their shared x axis, the March equinox. For the ecliptic, the obliquity.
    /// </param>
    public static Rotation ToRotation(
        Double poleRightAscension,
        Double poleDeclination,
        Double primeMeridian,
        Double degreesPerDay,
        DateTimeOffset epoch,
        Double referencePlaneTilt)
    {
        if (!Double.IsFinite(degreesPerDay) || degreesPerDay == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degreesPerDay), degreesPerDay, $"The spin rate must be a finite number other than 0, but is {degreesPerDay}.");
        }

        Double tilt = -Double.DegreesToRadians(referencePlaneTilt);
        Vector3D iauPole = Vector3D.Direction(poleRightAscension, poleDeclination);
        Vector3D iauNode = Vector3D.Direction(poleRightAscension + 90, 0);
        Double w = Double.DegreesToRadians(primeMeridian);
        Vector3D towardsPrimeMeridian = (Math.Cos(w) * iauNode + Math.Sin(w) * Vector3D.Cross(iauPole, iauNode)).RotateX(tilt);
        Vector3D pole = degreesPerDay < 0 ? -1 * iauPole.RotateX(tilt) : iauPole.RotateX(tilt);

        Rotation unmeasured = new Rotation
        {
            PoleLongitude = Double.RadiansToDegrees(Math.Atan2(pole.Y, pole.X)),
            PoleLatitude = Double.RadiansToDegrees(Math.Asin(Math.Clamp(pole.Z, -1, 1))),
            PrimeMeridianAtEpoch = 0,
            SiderealPeriod = TimeSpan.FromDays(360 / Math.Abs(degreesPerDay)),
            Epoch = epoch,
        };
        (Vector3D xAxis, Vector3D yAxis, _) = BodyFrames.EquatorAxes(unmeasured);

        return unmeasured with
        {
            PrimeMeridianAtEpoch = Double.RadiansToDegrees(Math.Atan2(Vector3D.Dot(towardsPrimeMeridian, yAxis), Vector3D.Dot(towardsPrimeMeridian, xAxis))),
        };
    }
}
