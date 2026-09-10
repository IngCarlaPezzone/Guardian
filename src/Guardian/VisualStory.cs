using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Guardian
{
    public static class VisualStoryDefinition
    {
        public const string SkillKey = "visual_story.location.home";
        public const string CategoryId = "visual_story";
        public const string LevelId = "location";
        public const string SkillId = "home";
        public const string VariantId = "where_you_live_plottier";
        public const string Title = "Dónde vivís";
        public const double DurationSeconds = 13.6;

        public static Mission CreateMission()
        {
            return new Mission
            {
                Id = Guid.NewGuid().ToString(),
                CategoryId = CategoryId,
                LevelId = LevelId,
                SkillId = SkillId,
                VariantId = VariantId,
                Prompt = Title,
                AcceptedAnswers = new List<string>(),
                HelpSteps = new List<MissionHelpStep>()
            };
        }

        public static Dictionary<string, object> Telemetry(Mission mission)
        {
            return new Dictionary<string, object>
            {
                { "mission_id", mission.Id },
                { "missionId", mission.Id },
                { "category_id", mission.CategoryId },
                { "level_id", mission.LevelId },
                { "skill_id", mission.SkillId },
                { "variant_id", mission.VariantId },
                { "duration_seconds", DurationSeconds }
            };
        }
    }

    public sealed class VisualStoryNarrator : IDisposable
    {
        private SoundPlayer _player;
        private Stream _stream;

        public void Start()
        {
            Stop();
            try
            {
                _stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Guardian.Assets.Audio.donde-vivis.wav");
                if (_stream == null) return;
                _player = new SoundPlayer(_stream);
                _player.Load();
                _player.Play();
            }
            catch
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (_player != null) try { _player.Stop(); }
            catch { }
            if (_player != null) try { _player.Dispose(); }
            catch { }
            if (_stream != null) try { _stream.Dispose(); }
            catch { }
            _player = null;
            _stream = null;
        }

        public void Dispose() { Stop(); }
    }

    public sealed class VisualStorySurface : FrameworkElement
    {
        private double _position;
        private static readonly CultureInfo Culture = new CultureInfo("es-AR");
        private static readonly Typeface DisplayTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Typeface LabelTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public double PositionSeconds
        {
            get { return _position; }
            set { _position = Math.Max(0, Math.Min(VisualStoryDefinition.DurationSeconds, value)); InvalidateVisual(); }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = Math.Max(1, ActualWidth);
            var h = Math.Max(1, ActualHeight);
            DrawBackdrop(dc, w, h);
            DrawStars(dc, w, h, _position);

            DrawEarth(dc, w, h, _position, SceneAlpha(_position, 0.0, 3.35, 0.0, 0.75));
            DrawSouthAmerica(dc, w, h, _position, SceneAlpha(_position, 2.45, 5.95, 0.75, 0.75));
            DrawArgentina(dc, w, h, _position, SceneAlpha(_position, 5.05, 8.65, 0.75, 0.75));
            DrawNeuquen(dc, w, h, _position, SceneAlpha(_position, 7.75, 11.05, 0.7, 0.8));
            DrawPlottier(dc, w, h, _position, SceneAlpha(_position, 9.95, 13.6, 0.75, 0.0));

            DrawChrome(dc, w, h, _position);
        }

        private static void DrawBackdrop(DrawingContext dc, double w, double h)
        {
            var brush = new LinearGradientBrush(Color.FromRgb(3, 10, 22), Color.FromRgb(7, 24, 39), new Point(0, 0), new Point(1, 1));
            dc.DrawRectangle(brush, null, new Rect(0, 0, w, h));
            var glow = new RadialGradientBrush(Color.FromArgb(100, 16, 104, 128), Color.FromArgb(0, 2, 8, 18));
            glow.Center = new Point(0.56, 0.48); glow.GradientOrigin = glow.Center; glow.RadiusX = 0.7; glow.RadiusY = 0.8;
            dc.DrawRectangle(glow, null, new Rect(0, 0, w, h));
        }

        private static void DrawStars(DrawingContext dc, double w, double h, double t)
        {
            for (var i = 0; i < 86; i++)
            {
                var x = Fraction(Math.Sin(i * 91.17) * 431.7) * w;
                var y = Fraction(Math.Sin(i * 37.31 + 2.2) * 271.9) * h;
                var pulse = 0.35 + 0.35 * Math.Sin(t * 1.1 + i);
                var radius = i % 13 == 0 ? 1.7 : i % 5 == 0 ? 1.1 : 0.65;
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(95 + 90 * pulse), 213, 238, 255)), null, new Point(x, y), radius, radius);
            }
        }

        private static void DrawEarth(DrawingContext dc, double w, double h, double t, double alpha)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var p = Ease(Cap(t / 3.35));
            var r = h * (0.30 + 0.16 * p);
            var center = new Point(w * (0.53 - 0.02 * p), h * (0.51 + 0.01 * p));
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(75, 54, 214, 255)), h * 0.032), center, r * 1.04, r * 1.04);
            var ocean = new RadialGradientBrush(Color.FromRgb(22, 111, 139), Color.FromRgb(3, 24, 47));
            ocean.GradientOrigin = new Point(0.32, 0.27); ocean.Center = new Point(0.43, 0.42); ocean.RadiusX = 0.72; ocean.RadiusY = 0.72;
            dc.DrawEllipse(ocean, new Pen(new SolidColorBrush(Color.FromArgb(170, 79, 222, 255)), 2), center, r, r);
            dc.PushClip(new EllipseGeometry(center, r - 2, r - 2));
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 181, 235, 245)), 1.2);
            for (var i = -2; i <= 2; i++) dc.DrawEllipse(null, gridPen, center, r * (0.3 + Math.Abs(i) * 0.18), r);
            for (var j = -2; j <= 2; j++) dc.DrawEllipse(null, gridPen, center, r, r * (0.3 + Math.Abs(j) * 0.18));

            var land = new SolidColorBrush(Color.FromRgb(50, 132, 115));
            var coast = new Pen(new SolidColorBrush(Color.FromArgb(150, 143, 238, 213)), 1.5);
            DrawPolygon(dc, center.X - r * 0.72, center.Y - r * 0.66, r * 0.78, r * 0.68, land, coast, new[] {
                P(.02,.22),P(.18,.05),P(.55,.08),P(.78,.22),P(.92,.47),P(.75,.68),P(.55,.66),P(.35,.83),P(.12,.70)
            });
            DrawPolygon(dc, center.X + r * 0.05, center.Y - r * 0.08, r * 0.68, r * 1.18, land, coast, SouthAmericaPoints());
            var southGlow = new SolidColorBrush(Color.FromRgb(248, 181, 73));
            DrawPolygon(dc, center.X + r * 0.05, center.Y - r * 0.08, r * 0.68, r * 1.18, southGlow, new Pen(new SolidColorBrush(Color.FromRgb(255, 225, 154)), 2.2), SouthAmericaPoints());
            dc.Pop();
            DrawTitle(dc, w, h, "AMÉRICA DEL SUR", "DESDE EL ESPACIO", AlphaBetween(t, 0.35, 2.85, .45), -12 + 12 * Ease(Cap(t / 1.0)));
            dc.Pop();
        }

        private static void DrawSouthAmerica(DrawingContext dc, double w, double h, double t, double alpha)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var local = Cap((t - 2.45) / 3.5);
            var scale = 0.80 + 0.22 * Ease(local);
            var mapH = h * 0.90 * scale;
            var mapW = mapH * 0.66;
            var left = w * 0.5 - mapW * 0.5;
            var top = h * 0.51 - mapH * 0.5;
            var shadow = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            DrawPolygon(dc, left + 18, top + 22, mapW, mapH, shadow, null, SouthAmericaPoints());
            var continent = new LinearGradientBrush(Color.FromRgb(31, 101, 99), Color.FromRgb(19, 62, 75), 45);
            DrawPolygon(dc, left, top, mapW, mapH, continent, new Pen(new SolidColorBrush(Color.FromArgb(190, 126, 232, 216)), 2.2), SouthAmericaPoints());
            DrawMapLines(dc, left, top, mapW, mapH, new SolidColorBrush(Color.FromArgb(40, 209, 250, 239)));
            DrawPolygon(dc, left + mapW * .385, top + mapH * .51, mapW * .27, mapH * .47,
                new SolidColorBrush(Color.FromRgb(246, 176, 68)), new Pen(new SolidColorBrush(Color.FromRgb(255, 231, 170)), 2.5), ArgentinaPoints());
            DrawTitle(dc, w, h, "ARGENTINA", "TU PAÍS", AlphaBetween(t, 3.05, 5.55, .45), -10 + 10 * Ease(local));
            dc.Pop();
        }

        private static void DrawArgentina(DrawingContext dc, double w, double h, double t, double alpha)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var local = Cap((t - 5.05) / 3.6);
            var mapH = h * (0.94 + 0.18 * Ease(local));
            var mapW = mapH * .34;
            var left = w * .53 - mapW * .5;
            var top = h * .51 - mapH * .5;
            DrawPolygon(dc, left + 15, top + 18, mapW, mapH, new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), null, ArgentinaPoints());
            DrawPolygon(dc, left, top, mapW, mapH, new LinearGradientBrush(Color.FromRgb(35, 117, 124), Color.FromRgb(16, 67, 83), 55),
                new Pen(new SolidColorBrush(Color.FromRgb(137, 238, 225)), 2.5), ArgentinaPoints());
            for (var i = 1; i < 7; i++)
            {
                var y = top + mapH * (.16 + i * .105);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(40, 187, 242, 229)), 1.1), new Point(left + mapW * .22, y), new Point(left + mapW * (.8 - i * .035), y + 4));
            }
            var nx = left + mapW * .12; var ny = top + mapH * .37; var nw = mapW * .43; var nh = mapH * .13;
            DrawPolygon(dc, nx, ny, nw, nh, new SolidColorBrush(Color.FromRgb(247, 178, 70)), new Pen(new SolidColorBrush(Color.FromRgb(255, 235, 180)), 2.5), NeuquenPoints());
            var halo = new RadialGradientBrush(Color.FromArgb(100, 255, 191, 73), Color.FromArgb(0, 255, 191, 73));
            dc.DrawEllipse(halo, null, new Point(nx + nw * .5, ny + nh * .52), nw * .9, nh * 1.7);
            DrawTitle(dc, w, h, "NEUQUÉN", "TU PROVINCIA", AlphaBetween(t, 5.65, 8.25, .45), -10 + 10 * Ease(local));
            dc.Pop();
        }

        private static void DrawNeuquen(DrawingContext dc, double w, double h, double t, double alpha)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var local = Cap((t - 7.75) / 3.3);
            var mapW = w * (.52 + .12 * Ease(local));
            var mapH = h * (.59 + .08 * Ease(local));
            var left = w * .52 - mapW * .5; var top = h * .52 - mapH * .5;
            DrawPolygon(dc, left + 20, top + 23, mapW, mapH, new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)), null, NeuquenPoints());
            DrawPolygon(dc, left, top, mapW, mapH, new LinearGradientBrush(Color.FromRgb(89, 93, 68), Color.FromRgb(31, 82, 82), 35),
                new Pen(new SolidColorBrush(Color.FromRgb(252, 202, 104)), 3), NeuquenPoints());
            var contour = new Pen(new SolidColorBrush(Color.FromArgb(48, 255, 225, 166)), 1.2);
            for (var i = 0; i < 8; i++)
            {
                var inset = 25 + i * 17;
                dc.DrawEllipse(null, contour, new Point(left + mapW * (.37 + i * .018), top + mapH * (.45 + i * .01)), Math.Max(20, mapW * .34 - inset), Math.Max(14, mapH * .25 - inset * .45));
            }
            var destination = new Point(left + mapW * .78, top + mapH * .72);
            var homeIntro = Ease(Cap((t - 8.85) / .65));
            var destinationGlow = new RadialGradientBrush(Color.FromArgb((byte)(95 * homeIntro), 255, 195, 73), Color.FromArgb(0, 255, 195, 73));
            dc.DrawEllipse(destinationGlow, null, destination, 50 * homeIntro, 50 * homeIntro);
            dc.PushTransform(new ScaleTransform(homeIntro, homeIntro, destination.X, destination.Y));
            DrawHouse(dc, destination, h * .055);
            dc.Pop();
            DrawTitle(dc, w, h, "PLOTTIER", "TU CIUDAD", AlphaBetween(t, 8.35, 10.65, .42), -8 + 8 * Ease(local));
            dc.Pop();
        }

        private static void DrawPlottier(DrawingContext dc, double w, double h, double t, double alpha)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var local = Cap((t - 9.95) / 3.65);
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(75, 120, 220, 207)), 1.3);
            dc.PushTransform(new RotateTransform(-12, w * .5, h * .5));
            for (var i = -8; i <= 16; i++)
            {
                var x = w * .12 + i * w * .055;
                dc.DrawLine(gridPen, new Point(x, h * .17), new Point(x, h * .86));
            }
            for (var j = -5; j <= 14; j++)
            {
                var y = h * .12 + j * h * .055;
                dc.DrawLine(gridPen, new Point(w * .13, y), new Point(w * .87, y));
            }
            dc.Pop();
            var river = new StreamGeometry();
            using (var c = river.Open())
            {
                c.BeginFigure(new Point(-40, h * .72), false, false);
                c.BezierTo(new Point(w * .18, h * .58), new Point(w * .42, h * .82), new Point(w * .67, h * .68), true, false);
                c.BezierTo(new Point(w * .82, h * .59), new Point(w * .92, h * .7), new Point(w + 40, h * .56), true, false);
            }
            river.Freeze();
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(150, 35, 170, 205)), h * .055), river);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(155, 92, 224, 241)), 2), river);

            for (var k = 0; k < 46; k++)
            {
                var x = Fraction(Math.Sin(k * 21.4) * 161.7) * w;
                var y = (.22 + Fraction(Math.Sin(k * 12.7 + 1.8) * 97.1) * .55) * h;
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(100, 255, 201, 93)), null, new Point(x, y), 1.6, 1.6);
            }

            var housePoint = new Point(w * .57, h * .48);
            var houseIntro = Ease(Cap((t - 10.65) / .75));
            var pulse = 1 + .035 * Math.Sin(Math.Max(0, t - 11.2) * 3.2);
            var halo = new RadialGradientBrush(Color.FromArgb((byte)(120 * houseIntro), 255, 193, 72), Color.FromArgb(0, 255, 193, 72));
            dc.DrawEllipse(halo, null, housePoint, h * .18 * houseIntro, h * .18 * houseIntro);
            dc.PushTransform(new ScaleTransform(houseIntro * pulse, houseIntro * pulse, housePoint.X, housePoint.Y));
            DrawHouse(dc, housePoint, h * .115);
            dc.Pop();
            DrawHomeTitle(dc, w, h, t, houseIntro);
            dc.Pop();
        }

        private static void DrawHouse(DrawingContext dc, Point center, double size)
        {
            var body = new Rect(center.X - size * .46, center.Y - size * .05, size * .92, size * .62);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(246, 239, 220)), new Pen(new SolidColorBrush(Color.FromRgb(255, 210, 111)), 2), body, size * .07, size * .07);
            var roof = new StreamGeometry();
            using (var c = roof.Open())
            {
                c.BeginFigure(new Point(center.X - size * .58, center.Y + size * .02), true, true);
                c.LineTo(new Point(center.X, center.Y - size * .51), true, false);
                c.LineTo(new Point(center.X + size * .58, center.Y + size * .02), true, false);
            }
            roof.Freeze();
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(241, 151, 73)), new Pen(new SolidColorBrush(Color.FromRgb(255, 222, 151)), 2), roof);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(32, 84, 91)), null, new Rect(center.X - size * .11, center.Y + size * .19, size * .22, size * .38), size * .03, size * .03);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 210, 101)), null, new Point(center.X + size * .055, center.Y + size * .37), 2.2, 2.2);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(111, 202, 207)), null, new Rect(center.X - size * .34, center.Y + size * .17, size * .16, size * .16));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(130, 255, 232, 158)), null, new Rect(center.X - size * .32, center.Y + size * .19, size * .12, size * .12));
        }

        private static void DrawChrome(DrawingContext dc, double w, double h, double t)
        {
            var top = new LinearGradientBrush(Color.FromArgb(175, 2, 8, 18), Color.FromArgb(0, 2, 8, 18), new Point(.5, 0), new Point(.5, 1));
            dc.DrawRectangle(top, null, new Rect(0, 0, w, h * .23));
            var bottom = new LinearGradientBrush(Color.FromArgb(0, 2, 8, 18), Color.FromArgb(220, 2, 8, 18), new Point(.5, 0), new Point(.5, 1));
            dc.DrawRectangle(bottom, null, new Rect(0, h * .62, w, h * .38));
            DrawText(dc, "DÓNDE VIVÍS", w * .065, h * .07, Math.Max(12, h * .021), new SolidColorBrush(Color.FromArgb(195, 183, 235, 229)), LabelTypeface, 1.8);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 113, 226, 211)), null, new Rect(w * .065, h * .115, w * .11, 3), 1.5, 1.5);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)), null, new Rect(w * .065, h * .94, w * .87, 3), 1.5, 1.5);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(246, 183, 72)), null, new Rect(w * .065, h * .94, w * .87 * Cap(t / VisualStoryDefinition.DurationSeconds), 3), 1.5, 1.5);
        }

        private static void DrawTitle(DrawingContext dc, double w, double h, string title, string eyebrow, double alpha, double yOffset)
        {
            if (alpha <= 0) return;
            dc.PushOpacity(alpha);
            var x = w * .065; var y = h * .70 + yOffset;
            DrawText(dc, eyebrow, x, y, Math.Max(13, h * .021), new SolidColorBrush(Color.FromRgb(246, 183, 72)), LabelTypeface, 2.0);
            DrawText(dc, title, x, y + h * .042, Math.Max(34, h * .070), Brushes.White, DisplayTypeface, .1);
            dc.Pop();
        }

        private static void DrawHomeTitle(DrawingContext dc, double w, double h, double t, double alpha)
        {
            var a = alpha * AlphaBetween(t, 10.7, 13.6, .45);
            if (a <= 0) return;
            dc.PushOpacity(a);
            var y = h * .70;
            DrawText(dc, "PLOTTIER", w * .065, y, Math.Max(13, h * .021), new SolidColorBrush(Color.FromRgb(246, 183, 72)), LabelTypeface, 2.0);
            DrawText(dc, "ACÁ VIVÍS", w * .065, y + h * .042, Math.Max(36, h * .072), Brushes.White, DisplayTypeface, .1);
            dc.Pop();
        }

        private static void DrawMapLines(DrawingContext dc, double x, double y, double w, double h, Brush brush)
        {
            var pen = new Pen(brush, 1.1);
            for (var i = 1; i < 7; i++)
            {
                var yy = y + h * i / 8.0;
                dc.DrawLine(pen, new Point(x + w * (.20 + .02 * i), yy), new Point(x + w * (.77 - .018 * i), yy + 5));
            }
        }

        private static void DrawPolygon(DrawingContext dc, double x, double y, double w, double h, Brush fill, Pen pen, Point[] points)
        {
            if (points == null || points.Length == 0) return;
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(x + points[0].X * w, y + points[0].Y * h), fill != null, true);
                for (var i = 1; i < points.Length; i++) context.LineTo(new Point(x + points[i].X * w, y + points[i].Y * h), true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(fill, pen, geometry);
        }

        private static void DrawText(DrawingContext dc, string text, double x, double y, double size, Brush brush, Typeface typeface, double letterSpacing)
        {
            var formatted = new FormattedText(text, Culture, FlowDirection.LeftToRight, typeface, size, brush, 1.0);
            dc.DrawText(formatted, new Point(x, y));
        }

        private static Point[] SouthAmericaPoints()
        {
            return new[] { P(.27,.02),P(.48,.05),P(.69,.13),P(.84,.26),P(.76,.37),P(.80,.46),P(.68,.55),P(.62,.67),P(.54,.78),P(.45,.96),P(.36,.86),P(.33,.72),P(.24,.59),P(.17,.43),P(.22,.31),P(.12,.20) };
        }

        private static Point[] ArgentinaPoints()
        {
            return new[] { P(.31,.02),P(.60,.07),P(.78,.18),P(.70,.31),P(.78,.43),P(.67,.54),P(.63,.67),P(.54,.82),P(.46,.98),P(.34,.87),P(.36,.71),P(.25,.56),P(.30,.41),P(.19,.29) };
        }

        private static Point[] NeuquenPoints()
        {
            return new[] { P(.13,.18),P(.42,.04),P(.76,.12),P(.91,.36),P(.82,.72),P(.57,.94),P(.24,.86),P(.05,.55) };
        }

        private static Point P(double x, double y) { return new Point(x, y); }
        private static double Fraction(double value) { return value - Math.Floor(value); }
        private static double Cap(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static double Ease(double value) { value = Cap(value); return value * value * (3 - 2 * value); }
        private static double SceneAlpha(double t, double start, double end, double fadeIn, double fadeOut)
        {
            var result = 1.0;
            if (fadeIn > 0 && t < start + fadeIn) result *= Ease((t - start) / fadeIn);
            if (fadeOut > 0 && t > end - fadeOut) result *= Ease((end - t) / fadeOut);
            return t < start || t > end ? 0 : Cap(result);
        }
        private static double AlphaBetween(double t, double start, double end, double fade)
        {
            return SceneAlpha(t, start, end, fade, fade);
        }
    }

    public sealed class VisualStoryWindow : Window
    {
        private readonly Mission _mission;
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly VisualStorySurface _surface;
        private readonly Button _exitButton;
        private readonly Grid _adminOverlay;
        private readonly TextBox _adminUserBox;
        private readonly PasswordBox _adminPasswordBox;
        private readonly TextBlock _adminFeedback;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly VisualStoryNarrator _narrator = new VisualStoryNarrator();
        private bool _completed;
        private bool _unlockRequested;

        public event Action UnlockRequested;
        public event Action AdminShutdownRequested;
        public event Action StoryCompleted;

        public VisualStoryWindow(Mission mission, GuardianConfig config, EventLogger logger)
        {
            _mission = mission; _config = config; _logger = logger;
            Title = "Guardian — " + VisualStoryDefinition.Title;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized;
            Topmost = true; ShowInTaskbar = true; Background = Brushes.Black; Foreground = Brushes.White;
            var root = new Grid();
            _surface = new VisualStorySurface(); root.Children.Add(_surface);

            _exitButton = MakeButton("SALIR", 108, 42);
            _exitButton.HorizontalAlignment = HorizontalAlignment.Right; _exitButton.VerticalAlignment = VerticalAlignment.Top;
            _exitButton.Margin = new Thickness(0, 24, 28, 0); _exitButton.Visibility = Visibility.Hidden;
            _exitButton.Click += delegate { RequestUnlock(); }; root.Children.Add(_exitButton);

            var adminButton = MakeButton("ADMIN", 88, 36);
            adminButton.HorizontalAlignment = HorizontalAlignment.Right; adminButton.VerticalAlignment = VerticalAlignment.Bottom;
            adminButton.Margin = new Thickness(0, 0, 24, 24); adminButton.Opacity = .62;
            adminButton.Click += delegate { ShowAdminOverlay(); }; root.Children.Add(adminButton);

            _adminOverlay = new Grid { Visibility = Visibility.Hidden, Background = new SolidColorBrush(Color.FromArgb(220, 2, 8, 18)) };
            var card = new Border { Width = 380, Padding = new Thickness(26), CornerRadius = new CornerRadius(16), Background = new SolidColorBrush(Color.FromRgb(242, 247, 246)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = "Salida admin", Foreground = new SolidColorBrush(Color.FromRgb(10, 28, 37)), FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 16) });
            panel.Children.Add(new TextBlock { Text = "Usuario", Foreground = Brushes.Black });
            _adminUserBox = new TextBox { FontSize = 16, Margin = new Thickness(0, 5, 0, 12) }; panel.Children.Add(_adminUserBox);
            panel.Children.Add(new TextBlock { Text = "Contraseña", Foreground = Brushes.Black });
            _adminPasswordBox = new PasswordBox { FontSize = 16, Margin = new Thickness(0, 5, 0, 16) }; panel.Children.Add(_adminPasswordBox);
            _adminPasswordBox.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) TryAdminExit(); };
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = MakeButton("CANCELAR", 106, 38); cancel.Margin = new Thickness(0, 0, 8, 0); cancel.Click += delegate { _adminOverlay.Visibility = Visibility.Hidden; }; buttons.Children.Add(cancel);
            var close = MakeButton("CERRAR GUARDIAN", 150, 38); close.Click += delegate { TryAdminExit(); }; buttons.Children.Add(close); panel.Children.Add(buttons);
            _adminFeedback = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(169, 32, 32)), FontSize = 13, MinHeight = 22, Margin = new Thickness(0, 12, 0, 0) }; panel.Children.Add(_adminFeedback);
            card.Child = panel; _adminOverlay.Children.Add(card); root.Children.Add(_adminOverlay);
            Content = root;

            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += delegate { Advance(); };
            Loaded += delegate { _clock.Restart(); _timer.Start(); _narrator.Start(); Activate(); };
            Closed += delegate { _timer.Stop(); _narrator.Dispose(); };
        }

        public string QuestionText { get { return VisualStoryDefinition.Title; } }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_unlockRequested) { e.Cancel = true; Topmost = true; Activate(); }
            base.OnClosing(e);
        }

        public void AllowProgrammaticClose() { _unlockRequested = true; }

        private void Advance()
        {
            _surface.PositionSeconds = _clock.Elapsed.TotalSeconds;
            if (!_completed && _clock.Elapsed.TotalSeconds >= VisualStoryDefinition.DurationSeconds)
            {
                _completed = true; _timer.Stop(); _narrator.Stop(); _exitButton.Visibility = Visibility.Visible;
                if (StoryCompleted != null) StoryCompleted();
            }
        }

        private static Button MakeButton(string text, double width, double height)
        {
            return new Button { Content = text, Width = width, Height = height, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromArgb(190, 9, 39, 53)), BorderBrush = new SolidColorBrush(Color.FromArgb(180, 122, 225, 211)), BorderThickness = new Thickness(1), Padding = new Thickness(10, 5, 10, 5) };
        }

        private void RequestUnlock()
        {
            if (!_completed) return;
            _unlockRequested = true;
            _logger.Log("ExitClicked", VisualStoryDefinition.Telemetry(_mission));
            if (UnlockRequested != null) UnlockRequested();
            Close();
        }

        private void ShowAdminOverlay()
        {
            _adminFeedback.Text = ""; _adminUserBox.Text = _config.AdminUsername; _adminPasswordBox.Password = "";
            _adminOverlay.Visibility = Visibility.Visible; _adminPasswordBox.Focus(); Keyboard.Focus(_adminPasswordBox);
            _logger.Log("AdminExitPromptOpened", VisualStoryDefinition.Telemetry(_mission));
        }

        private void TryAdminExit()
        {
            if (AdminAuth.Verify(_config, _adminUserBox.Text, _adminPasswordBox.Password))
            {
                _unlockRequested = true;
                var payload = VisualStoryDefinition.Telemetry(_mission); payload["username"] = _adminUserBox.Text;
                _logger.Log("AdminExitSucceeded", payload);
                if (AdminShutdownRequested != null) AdminShutdownRequested();
                Close(); return;
            }
            _adminFeedback.Text = "Usuario o contraseña incorrectos."; _adminPasswordBox.SelectAll(); _adminPasswordBox.Focus();
            _logger.Log("AdminExitFailed", VisualStoryDefinition.Telemetry(_mission));
        }
    }

    public static class VisualStoryPreview
    {
        public static int Run(CommandLine command)
        {
            var captureDirectory = command.Value("--visual-story-capture");
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                var window = new Window { Width = 1280, Height = 720, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Left = -2000, Top = 0 };
                var surface = new VisualStorySurface(); window.Content = surface;
                window.Loaded += delegate
                {
                    try { CaptureFrames(surface, captureDirectory); app.Shutdown(0); }
                    catch { app.Shutdown(1); }
                };
                app.MainWindow = window; window.Show(); return app.Run();
            }

            var preview = new VisualStoryPreviewWindow(); app.MainWindow = preview; preview.Show(); return app.Run();
        }

        private static void CaptureFrames(VisualStorySurface surface, string directory)
        {
            Directory.CreateDirectory(directory);
            var frames = new Dictionary<string, double>
            {
                { "01-tierra-america-del-sur.png", 1.65 },
                { "02-argentina.png", 4.25 },
                { "03-neuquen.png", 7.05 },
                { "04-llegada-plottier.png", 9.55 },
                { "05-hogar.png", 12.25 }
            };
            foreach (var frame in frames)
            {
                surface.PositionSeconds = frame.Value; surface.Measure(new Size(1280, 720)); surface.Arrange(new Rect(0, 0, 1280, 720)); surface.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1280, 720, 96, 96, PixelFormats.Pbgra32); bitmap.Render(surface);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var output = File.Create(Path.Combine(directory, frame.Key))) encoder.Save(output);
            }
        }
    }

    public sealed class VisualStoryPreviewWindow : Window
    {
        private readonly VisualStorySurface _surface = new VisualStorySurface();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Slider _slider;
        private readonly TextBlock _time;
        private readonly VisualStoryNarrator _narrator = new VisualStoryNarrator();
        private bool _playing;
        private bool _updatingSlider;

        public VisualStoryPreviewWindow()
        {
            Title = "Guardian — Preview Visual Story"; Width = 1280; Height = 780; MinWidth = 960; MinHeight = 600;
            Background = new SolidColorBrush(Color.FromRgb(3, 10, 22)); WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var root = new Grid(); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_surface, 0); root.Children.Add(_surface);
            var controls = new Grid { Height = 64, Background = new SolidColorBrush(Color.FromRgb(8, 27, 39)), Margin = new Thickness(0) };
            controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var restart = new Button { Content = "REINICIAR", Width = 112, Height = 38, Margin = new Thickness(16, 0, 12, 0) }; restart.Click += delegate { Restart(); }; controls.Children.Add(restart);
            _slider = new Slider { Minimum = 0, Maximum = VisualStoryDefinition.DurationSeconds, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            _slider.ValueChanged += delegate { if (!_updatingSlider) { Pause(); _surface.PositionSeconds = _slider.Value; UpdateTime(); } }; Grid.SetColumn(_slider, 1); controls.Children.Add(_slider);
            _time = new TextBlock { Width = 88, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_time, 2); controls.Children.Add(_time); Grid.SetRow(controls, 1); root.Children.Add(controls); Content = root;
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) }; _timer.Tick += delegate { Advance(); };
            Loaded += delegate { Restart(); }; Closed += delegate { _timer.Stop(); _narrator.Dispose(); };
        }

        private void Restart()
        {
            _surface.PositionSeconds = 0; _clock.Restart(); _playing = true; _timer.Start(); _narrator.Start(); UpdateTime();
        }
        private void Pause() { _playing = false; _timer.Stop(); _narrator.Stop(); }
        private void Advance()
        {
            if (!_playing) return;
            var position = _clock.Elapsed.TotalSeconds;
            if (position >= VisualStoryDefinition.DurationSeconds) { position = VisualStoryDefinition.DurationSeconds; Pause(); }
            _surface.PositionSeconds = position; _updatingSlider = true; _slider.Value = position; _updatingSlider = false; UpdateTime();
        }
        private void UpdateTime() { _time.Text = _surface.PositionSeconds.ToString("00.0", CultureInfo.InvariantCulture) + " / 13.6"; }
    }
}
