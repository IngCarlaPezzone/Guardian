using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Guardian
{
    public sealed class MultiplyBy100StorySurface : FrameworkElement
    {
        private static readonly Dictionary<string, BitmapSource> Assets = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        private double _position;
        private double _duration = 18.0;
        public double PositionSeconds { get { return _position; } set { _position = Math.Max(0, value); InvalidateVisual(); } }
        public double DurationSeconds { get { return _duration; } set { _duration = Math.Max(1, value); InvalidateVisual(); } }

        protected override void OnRender(DrawingContext dc)
        {
            var sx = (ActualWidth > 0 ? ActualWidth : 1280) / 1280.0; var sy = (ActualHeight > 0 ? ActualHeight : 720) / 720.0;
            dc.PushTransform(new ScaleTransform(sx, sy)); var t = Math.Min(1, PositionSeconds / DurationSeconds); Background(dc, t);
            if (t < .18) Opening(dc, t / .18); else if (t < .48) Discover(dc, (t - .18) / .30); else if (t < .60) Example(dc, 1, (t - .48) / .12); else if (t < .72) Example(dc, 2, (t - .60) / .12); else if (t < .84) Example(dc, 3, (t - .72) / .12); else Question(dc, (t - .84) / .16);
            dc.Pop();
        }

        private static void Background(DrawingContext dc, double t)
        {
            dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(20, 17, 75), Color.FromRgb(3, 91, 119), 20), null, new Rect(0, 0, 1280, 720));
            var centers = new[] { new Point(115 + 50 * Math.Sin(t * 5), 110), new Point(1110, 135 + 45 * Math.Cos(t * 4)), new Point(1050, 620), new Point(165, 585) };
            var colors = new[] { Color.FromArgb(76, 184, 79, 255), Color.FromArgb(64, 18, 224, 211), Color.FromArgb(54, 255, 177, 70), Color.FromArgb(52, 250, 87, 167) };
            for (var i = 0; i < centers.Length; i++) dc.DrawEllipse(new RadialGradientBrush(colors[i], Color.FromArgb(0, colors[i].R, colors[i].G, colors[i].B)), null, centers[i], 235, 235);
            for (var i = 0; i < 22; i++) { var x = (i * 109 + t * (25 + i % 4 * 17)) % 1280; var y = 38 + (i * 71) % 642; dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(24 + i % 4 * 12), 255, 255, 255)), null, new Point(x, y), 2 + i % 3, 2 + i % 3); }
        }

        private static void Opening(DrawingContext dc, double t)
        {
            var first = Elastic(Math.Min(1, t * 2.1)); var second = Elastic(Math.Max(0, t * 2.7 - .58)); var third = Elastic(Math.Max(0, t * 3.1 - 1.18));
            Multiplier(dc, 10, 620, 205, 1, first); Multiplier(dc, 100, 580, 360, 1, second); Multiplier(dc, 1000, 540, 515, 1, third);
            Pulse(dc, 650, 205, Color.FromRgb(40, 215, 210), .13 + .09 * Math.Sin(t * 16) * first); Pulse(dc, 660, 360, Color.FromRgb(40, 215, 210), .13 + .09 * Math.Sin(t * 16) * second); Pulse(dc, 670, 515, Color.FromRgb(40, 215, 210), .13 + .09 * Math.Sin(t * 16) * third);
        }

        private static void Discover(DrawingContext dc, double t)
        {
            var rows = new[] { 205.0, 350.0, 505.0 };
            for (var count = 1; count <= 3; count++)
            {
                var y = rows[count - 1]; Multiplier(dc, Pow10(count), 650, y, .68, 1);
                for (var zero = 0; zero < count; zero++) { var delay = (count - 1) * .18 + zero * .11; var p = Math.Max(0, Math.Min(1, (t - delay) / .24)); var x = 650 + (zero + 1) * 82 * .68; Pulse(dc, x, y, Color.FromRgb(40, 215, 210), .16 + .32 * Math.Sin(p * Math.PI)); Asset(dc, "guardian-number-0.png", x, y, 105, 1, 1 + .13 * Math.Sin(p * Math.PI), 0, 1); }
            }
            Asset(dc, "hint.png", 150, 585, 152, Ease(Math.Min(1, t * 3)), 1 + .04 * Math.Sin(t * Math.PI * 4), 0, 1);
            if (t > .62) Number(dc, 23, 315 + Ease((t - .62) / .38) * 45, 350, 116, Ease((t - .62) / .38), 1, 0);
        }

        private static void Example(DrawingContext dc, int zeros, double t)
        {
            var flyStart = .28; var flyEnd = .78; var pre = Ease(Math.Min(1, t / .20)); var y = 365.0;
            var sourceX = zeros == 1 ? 350.0 : zeros == 2 ? 280.0 : 210.0; var multiplyX = sourceX + 150; var oneX = sourceX + 235; var zeroX = sourceX + 310;
            var equalsX = zeroX + (zeros - 1) * 62 + 100; var resultX = equalsX + 100; var upperStart = new Point(sourceX, y); var upperEnd = new Point(resultX, y);
            Number(dc, 23, sourceX, y, 95, pre, 1, 0); Asset(dc, "guardian-sign-multiply.png", multiplyX, y, 76, pre, 1, 0, 1); Asset(dc, "guardian-number-1.png", oneX, y, 92, pre, 1, 0, 1);
            Asset(dc, "guardian-sign-equals.png", equalsX, y, 72, pre, 1, 0, 1);
            for (var i = 0; i < zeros; i++)
            {
                Asset(dc, "guardian-number-0.png", zeroX + i * 62, y, 92, pre, 1 + .06 * Math.Sin(t * Math.PI * 5), 0, 1);
            }
            var upperP = Smooth((t - flyStart) / (flyEnd - flyStart)); var trailOpacity = 1 - Smooth((t - .72) / .20);
            if (t >= flyStart)
            {
                var point = Bezier(upperStart, new Point(sourceX + 100, 165), new Point(resultX - 95, 170), upperEnd, upperP);
                if (trailOpacity > 0) Path(dc, upperStart, new Point(sourceX + 100, 165), new Point(resultX - 95, 170), upperEnd, Color.FromRgb(255, 190, 62), upperP, trailOpacity);
                Pulse(dc, point.X, point.Y, Color.FromRgb(255, 190, 62), .18 * trailOpacity); Number(dc, 23, point.X, point.Y, 95, 1, 1 + .08 * Math.Sin(upperP * Math.PI), 0);
                for (var i = 0; i < zeros; i++)
                {
                    var offset = i * .055; var p = Smooth((t - flyStart - offset) / (flyEnd - flyStart)); var start = new Point(zeroX + i * 62, y); var end = new Point(resultX + 122 + i * 61, y); var lower = Bezier(start, new Point(start.X + 45, 555 + i * 20), new Point(end.X - 80, 560 + i * 16), end, p);
                    if (trailOpacity > 0) Path(dc, start, new Point(start.X + 45, 555 + i * 20), new Point(end.X - 80, 560 + i * 16), end, Color.FromRgb(40, 215, 210), p, trailOpacity);
                    Pulse(dc, lower.X, lower.Y, Color.FromRgb(40, 215, 210), .19 * trailOpacity); Asset(dc, "guardian-number-0.png", lower.X, lower.Y, 95, 1, 1 + .11 * Math.Sin(p * Math.PI), (i % 2 == 0 ? -1 : 1) * 6 * Math.Sin(p * Math.PI), 1);
                }
            }
        }

        private static void Question(DrawingContext dc, double t)
        {
            Pulse(dc, 1070, 340, Color.FromRgb(169, 112, 255), .34 + .12 * Math.Sin(t * Math.PI * 3));
            Pulse(dc, 120, 585, Color.FromRgb(255, 108, 170), .16 + .08 * Math.Sin(t * Math.PI * 3));
            Asset(dc, "think.png", 120, 585, 116, Ease(t * 3), 1 + .05 * Math.Sin(t * Math.PI * 3), 0, 1); Asset(dc, "guardian-sign-question.png", 1100, 340, 176, Ease(t * 2), 1 + .07 * Math.Sin(t * Math.PI * 4), 0, 1);
            Asset(dc, "guardian-sign-multiply.png", 310, 350, 105, Ease(t * 3), Elastic(Math.Min(1, t * 2.2)), 0, 1); Asset(dc, "guardian-number-1.png", 430, 350, 132, Ease(t * 3), 1, 0, 1);
            for (var i = 0; i < 6; i++) Asset(dc, "guardian-number-0.png", 510 + i * 88, 350, 90, Ease(Math.Max(0, t * 3 - i * .16)), Elastic(Math.Max(0, t * 3 - i * .16)), 0, 1);
        }

        private static int Pow10(int zeros) { return zeros == 1 ? 10 : zeros == 2 ? 100 : 1000; }
        private static void Multiplier(DrawingContext dc, int number, double x, double y, double scale, double opacity) { Asset(dc, "guardian-sign-multiply.png", x - 78 * scale, y, 104 * scale, opacity, scale, 0, 1); var text = number.ToString(); for (var i = 0; i < text.Length; i++) Asset(dc, "guardian-number-" + text[i] + ".png", x + i * 82 * scale, y, 124 * scale, opacity, scale, 0, 1); }
        private static void Number(DrawingContext dc, int number, double x, double y, double size, double opacity, double scale, double rotation) { var text = number.ToString(); for (var i = 0; i < text.Length; i++) Asset(dc, "guardian-number-" + text[i] + ".png", x + i * size * .64, y, size, opacity, scale, rotation, 1); }
        private static void Asset(DrawingContext dc, string name, double x, double y, double size, double opacity, double scale, double rotation, double squash) { var image = Load(name); if (image == null) return; dc.PushOpacity(Math.Max(0, Math.Min(1, opacity))); dc.PushTransform(new RotateTransform(rotation, x, y)); dc.PushTransform(new ScaleTransform(scale, scale * squash, x, y)); dc.DrawImage(image, new Rect(x - size / 2, y - size / 2, size, size)); dc.Pop(); dc.Pop(); dc.Pop(); }
        private static BitmapSource Load(string name) { BitmapSource image; if (Assets.TryGetValue(name, out image)) return image; using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Guardian.Assets.Icons." + name)) { if (stream == null) return null; var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad); image = decoder.Frames[0]; image.Freeze(); Assets[name] = image; return image; } }
        private static void Pulse(DrawingContext dc, double x, double y, Color color, double alpha) { dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb((byte)(255 * Math.Max(0, alpha)), color.R, color.G, color.B), Color.FromArgb(0, color.R, color.G, color.B)), null, new Point(x, y), 88, 88); }
        private static void Path(DrawingContext dc, Point a, Point b, Point c, Point d, Color color, double progress, double opacity) { var g = new StreamGeometry(); using (var k = g.Open()) { k.BeginFigure(a, false, false); k.BezierTo(b, c, d, true, false); } g.Freeze(); dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(95 * Math.Max(0, Math.Min(1, opacity))), color.R, color.G, color.B)), 3) { DashStyle = new DashStyle(new[] { 4.0, 8.0 }, progress * 22) }, g); }
        private static Point Bezier(Point a, Point b, Point c, Point d, double t) { t = Math.Max(0, Math.Min(1, t)); var u = 1 - t; return new Point(u * u * u * a.X + 3 * u * u * t * b.X + 3 * u * t * t * c.X + t * t * t * d.X, u * u * u * a.Y + 3 * u * u * t * b.Y + 3 * u * t * t * c.Y + t * t * t * d.Y); }
        private static double Ease(double t) { t = Math.Max(0, Math.Min(1, t)); return 1 - Math.Pow(1 - t, 3); } private static double Smooth(double t) { t = Math.Max(0, Math.Min(1, t)); return t * t * (3 - 2 * t); } private static double Elastic(double t) { if (t <= 0 || t >= 1) return t; return Math.Pow(2, -8 * t) * Math.Sin((t * 8 - .75) * (2 * Math.PI / 3)) + 1; }
    }

    public sealed class VisualStoryMp3Player : IDisposable
    {
        private readonly MediaPlayer _player = new MediaPlayer(); private readonly EventLogger _logger; private readonly Mission _mission; private double _durationSeconds;
        public event Action<double> DurationAvailable; public VisualStoryMp3Player(EventLogger logger, Mission mission) { _logger = logger; _mission = mission; _player.MediaOpened += OnOpened; _player.MediaEnded += delegate { Log("VisualStoryAudioCompleted", null); }; _player.MediaFailed += delegate(object s, ExceptionEventArgs e) { Log("VisualStoryAudioFailed", e.ErrorException.Message); }; }
        public void Start() { var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "multiply-by-10-100-1000.mp3"); Log("VisualStoryAudioLoading", null); _player.Open(new Uri(path)); }
        private void OnOpened(object sender, EventArgs e) { _durationSeconds = _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan.TotalSeconds : 17; if (DurationAvailable != null) DurationAvailable(_durationSeconds); _player.Play(); Log("VisualStoryAudioStarted", null); }
        private void Log(string type, string message) { if (_logger == null || _mission == null) return; var p = VisualStoryDefinition.Telemetry(_mission); p["audio_resource"] = "Assets/Audio/multiply-by-10-100-1000.mp3"; p["audio_output"] = "default_multimedia_endpoint"; p["system_audio_muted"] = CoreAudio.IsDefaultEndpointMuted(); if (_durationSeconds > 0) p["audio_duration_seconds"] = Math.Round(_durationSeconds, 3); if (message != null) p["message"] = message; _logger.Log(type, p); }
        public void Stop() { _player.Stop(); _player.Close(); } public void Dispose() { Stop(); }
    }
}
