namespace BanchoMultiplayerBot.Host.WebApi.Data;

public class Color
{
    public float Red { get; set; }

    public float Green { get; set; }

    public float Blue { get; set; }

    public float Alpha { get; set; }

    public Color(int red, int green, int blue, int alpha)
    {
        this.Red = (float)red / (float)byte.MaxValue;
        this.Green = (float)green / (float)byte.MaxValue;
        this.Blue = (float)blue / (float)byte.MaxValue;
        this.Alpha = (float)alpha / (float)byte.MaxValue;
    }

    public Color(double red, double green, double blue, double alpha)
    {
        this.Red = (float)red;
        this.Green = (float)green;
        this.Blue = (float)blue;
        this.Alpha = (float)alpha;
    }

    public string ToCssString()
    {
        return
            $"rgb({(Red * 255).ToString().Replace(',', '.')}, {(Green * 255).ToString().Replace(',', '.')}, {(Blue * 255).ToString().Replace(',', '.')})";
    }

    // See https://github.com/ppy/osu-framework/blob/master/osu.Framework/Utils/Interpolation.cs#L249
    public static Color Interpolate(
        double time,
        Color startColor,
        Color endColor,
        double startTime,
        double endTime)
    {
        double current = time - startTime;
        double duration = endTime - startTime;

        if (duration == 0 || current == 0)
            return startColor;
        
        double t = Math.Max(0, Math.Min(1, (float)current / duration));

        return new Color(
            startColor.Red + t * (endColor.Red - startColor.Red),
            startColor.Green + t * (endColor.Green - startColor.Green),
            startColor.Blue + t * (endColor.Blue - startColor.Blue),
            startColor.Alpha + t * (endColor.Alpha - startColor.Alpha));
    }

    // See https://github.com/ppy/osu/blob/master/osu.Game/Utils/ColourUtils.cs#L18
    public static Color SampleFromLinearGradient(
        IReadOnlyList<(float position, Color color)> gradient,
        float point)
    {
        if (point < gradient[0].position)
            return gradient[0].color;

        for (int i = 0; i < gradient.Count - 1; i++)
        {
            var startStop = gradient[i];
            var endStop = gradient[i + 1];

            if (point >= endStop.position)
                continue;

            return Interpolate(point, startStop.color, endStop.color, startStop.position, endStop.position);
        }

        return gradient[^1].color;
    }
}