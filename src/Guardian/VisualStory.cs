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
        public const string SkillKey = "visual_story.math.multiply_by_10_100_1000";
        public const string CategoryId = "visual_story";
        public const string LevelId = "math_primer";
        public const string SkillId = "multiply_by_10_100_1000";
        public const string VariantId = "integer_times_powers_of_ten";
        public const string Title = "Multiplicar por 10, 100 y 1000";
        public const double DurationSeconds = 18.0;
        public const string PrimerTargetSkillKey = "math.basic_operations_1.multiply_by_powers_of_ten";

        public static bool IsPrimerEnabled(MissionConfig config)
        {
            if (config == null || config.EnabledSkills == null) return false;
            return config.EnabledSkills.Contains(SkillKey) && config.EnabledSkills.Contains(PrimerTargetSkillKey);
        }

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
        private readonly EventLogger _logger;
        private readonly Mission _mission;
        private bool _started;
        private bool _completed;

        // El WAV actual fue generado previamente con una voz local es-ES. La
        // selección futura es intencionalmente separable: este equipo no tiene
        // voz es-AR ni latinoamericana instalada para regenerarlo.
        public VisualStoryNarrator(EventLogger logger, Mission mission)
        {
            _logger = logger;
            _mission = mission;
        }

        public VisualStoryNarrator() : this(null, null) { }

        public void Start()
        {
            Stop("restart");
            try
            {
                Log("VisualStoryAudioLoading", null);
                _stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Guardian.Assets.Audio.donde-vivis.wav");
                if (_stream == null) throw new InvalidOperationException("embedded narration resource was not found");
                _player = new SoundPlayer(_stream);
                _player.Load();
                _player.Play();
                _started = true;
                _completed = false;
                var payload = new Dictionary<string, object>
                {
                    { "audio_resource", "Guardian.Assets.Audio.donde-vivis.wav" },
                    { "audio_duration_seconds", 13.2 },
                    { "narration_voice_locale", "es-ES" },
                    { "audio_output", "default_multimedia_endpoint" },
                    { "system_audio_muted", CoreAudio.IsDefaultEndpointMuted() }
                };
                Log("VisualStoryAudioStarted", payload);
            }
            catch (Exception ex)
            {
                Log("VisualStoryAudioFailed", new Dictionary<string, object> { { "message", ex.Message } });
                Stop("playback_failure");
            }
        }

        public void MarkCompleted()
        {
            if (!_started || _completed) return;
            _completed = true;
            Log("VisualStoryAudioCompleted", new Dictionary<string, object> { { "audio_duration_seconds", 13.2 } });
        }

        public void Stop(string reason)
        {
            if (_started && !_completed) Log("VisualStoryAudioStopped", new Dictionary<string, object> { { "reason", reason ?? "unknown" } });
            if (_player != null) try { _player.Stop(); }
            catch { }
            if (_player != null) try { _player.Dispose(); }
            catch { }
            if (_stream != null) try { _stream.Dispose(); }
            catch { }
            _player = null;
            _stream = null;
            _started = false;
        }

        private void Log(string eventType, Dictionary<string, object> extra)
        {
            if (_logger == null || _mission == null) return;
            var payload = VisualStoryDefinition.Telemetry(_mission);
            if (extra != null) foreach (var item in extra) payload[item.Key] = item.Value;
            _logger.Log(eventType, payload);
        }

        public void Dispose() { Stop("dispose"); }
    }

    // Conservado sólo como referencia de la primera POC. La experiencia en
    // tiempo de ejecución usa AssetVisualStorySurface, con assets aprobados.
    public sealed class LegacyVisualStorySurface : FrameworkElement
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

    public sealed class AssetVisualStorySurface : FrameworkElement
    {
        private sealed class Scene
        {
            public string ResourceName;
            public double Start;
            public double End;
            public Point FromFocus;
            public Point ToFocus;
            public double FromZoom;
            public double ToZoom;
        }

        private static readonly Scene[] Scenes = {
            new Scene { ResourceName = "Guardian.Assets.VisualStory.earth-south-america.png", Start = 0.0, End = 3.55, FromFocus = new Point(.41, .48), ToFocus = new Point(.46, .50), FromZoom = 1.00, ToZoom = 1.11 },
            new Scene { ResourceName = "Guardian.Assets.VisualStory.argentina.png", Start = 2.85, End = 6.20, FromFocus = new Point(.42, .52), ToFocus = new Point(.47, .56), FromZoom = 1.01, ToZoom = 1.11 },
            new Scene { ResourceName = "Guardian.Assets.VisualStory.neuquen.png", Start = 5.50, End = 9.00, FromFocus = new Point(.42, .48), ToFocus = new Point(.46, .53), FromZoom = 1.01, ToZoom = 1.10 },
            new Scene { ResourceName = "Guardian.Assets.VisualStory.plottier.png", Start = 8.30, End = 11.35, FromFocus = new Point(.42, .47), ToFocus = new Point(.47, .50), FromZoom = 1.02, ToZoom = 1.10 },
            new Scene { ResourceName = "Guardian.Assets.VisualStory.home.png", Start = 10.60, End = VisualStoryDefinition.DurationSeconds, FromFocus = new Point(.42, .47), ToFocus = new Point(.46, .49), FromZoom = 1.01, ToZoom = 1.08 }
        };

        private readonly Dictionary<string, BitmapSource> _images = new Dictionary<string, BitmapSource>();
        private double _position;

        public double PositionSeconds
        {
            get { return _position; }
            set { _position = Math.Max(0, Math.Min(VisualStoryDefinition.DurationSeconds, value)); InvalidateVisual(); }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var width = Math.Max(1, ActualWidth);
            var height = Math.Max(1, ActualHeight);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(1, 8, 18)), null, new Rect(0, 0, width, height));
            foreach (var scene in Scenes)
            {
                var alpha = SceneAlpha(_position, scene.Start, scene.End, .70, .72);
                if (alpha <= 0) continue;
                var image = ImageFor(scene.ResourceName);
                if (image == null) continue;
                dc.PushOpacity(alpha);
                DrawMovingImage(dc, image, scene, width, height);
                dc.Pop();
            }
            DrawVignette(dc, width, height);
        }

        private BitmapSource ImageFor(string resourceName)
        {
            BitmapSource image;
            if (_images.TryGetValue(resourceName, out image)) return image;
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    image = decoder.Frames[0];
                    image.Freeze();
                    _images[resourceName] = image;
                    return image;
                }
            }
            catch { return null; }
        }

        private void DrawMovingImage(DrawingContext dc, BitmapSource image, Scene scene, double width, double height)
        {
            var local = Ease(Cap((_position - scene.Start) / (scene.End - scene.Start)));
            var focus = new Point(Lerp(scene.FromFocus.X, scene.ToFocus.X, local), Lerp(scene.FromFocus.Y, scene.ToFocus.Y, local));
            var zoom = Lerp(scene.FromZoom, scene.ToZoom, local);
            var baseScale = Math.Max(width / image.PixelWidth, height / image.PixelHeight) * zoom;
            var drawWidth = image.PixelWidth * baseScale;
            var drawHeight = image.PixelHeight * baseScale;
            var left = Math.Min(0, Math.Max(width - drawWidth, width * .5 - focus.X * drawWidth));
            var top = Math.Min(0, Math.Max(height - drawHeight, height * .5 - focus.Y * drawHeight));
            dc.DrawImage(image, new Rect(left, top, drawWidth, drawHeight));
        }

        private static void DrawVignette(DrawingContext dc, double width, double height)
        {
            var vignette = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(105, 0, 4, 12));
            vignette.Center = new Point(.5, .48); vignette.GradientOrigin = vignette.Center; vignette.RadiusX = .72; vignette.RadiusY = .76;
            dc.DrawRectangle(vignette, null, new Rect(0, 0, width, height));
        }

        private static double SceneAlpha(double time, double start, double end, double fadeIn, double fadeOut)
        {
            if (time < start || time > end) return 0;
            var alpha = 1.0;
            if (time < start + fadeIn) alpha *= Ease((time - start) / fadeIn);
            if (time > end - fadeOut) alpha *= Ease((end - time) / fadeOut);
            return Cap(alpha);
        }

        private static double Cap(double value) { return Math.Max(0, Math.Min(1, value)); }
        private static double Ease(double value) { value = Cap(value); return value * value * (3 - 2 * value); }
        private static double Lerp(double from, double to, double amount) { return from + (to - from) * amount; }
    }

    public sealed class VisualStoryWindow : Window
    {
        private readonly Mission _mission;
        private readonly GuardianConfig _config;
        private readonly EventLogger _logger;
        private readonly MultiplyBy100StorySurface _surface;
        private readonly Button _exitButton;
        private readonly Grid _adminOverlay;
        private readonly TextBox _adminUserBox;
        private readonly PasswordBox _adminPasswordBox;
        private readonly TextBlock _adminFeedback;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly VisualStoryMp3Player _narrator;
        private readonly bool _continuesToMission;
        private double _storyDuration = VisualStoryDefinition.DurationSeconds;
        private bool _completed;
        private bool _unlockRequested;

        public event Action UnlockRequested;
        public event Action AdminShutdownRequested;
        public event Action StoryCompleted;

        public VisualStoryWindow(Mission mission, GuardianConfig config, EventLogger logger, bool continuesToMission = false)
        {
            _mission = mission; _config = config; _logger = logger;
            _continuesToMission = continuesToMission;
            _narrator = new VisualStoryMp3Player(logger, mission);
            _narrator.DurationAvailable += delegate(double seconds) { _storyDuration = seconds + .85; _surface.DurationSeconds = _storyDuration; if (!_completed) { _clock.Restart(); _timer.Start(); } };
            Title = "Guardian — " + VisualStoryDefinition.Title;
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized;
            Topmost = true; ShowInTaskbar = true; Background = Brushes.Black; Foreground = Brushes.White;
            var root = new Grid();
            _surface = new MultiplyBy100StorySurface(); root.Children.Add(_surface);

            _exitButton = MakeButton(_continuesToMission ? "CONTINUAR" : "SALIR", _continuesToMission ? 132 : 108, 42);
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
            Loaded += delegate { _narrator.Start(); Activate(); };
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
            if (!_completed && _clock.Elapsed.TotalSeconds >= _storyDuration)
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
                var surface = new MultiplyBy100StorySurface { DurationSeconds = VisualStoryDefinition.DurationSeconds }; window.Content = surface;
                window.Loaded += delegate
                {
                    try { CaptureFrames(surface, captureDirectory); app.Shutdown(0); }
                    catch { app.Shutdown(1); }
                };
                app.MainWindow = window; window.Show(); return app.Run();
            }

            var preview = new VisualStoryPreviewWindow(); app.MainWindow = preview; preview.Show(); return app.Run();
        }

        private static void CaptureFrames(MultiplyBy100StorySurface surface, string directory)
        {
            Directory.CreateDirectory(directory);
            var frames = new Dictionary<string, double>
            {
                { "01-apertura-multiplicadores.png", 1.60 },
                { "02-regla-hint-ceros.png", 4.80 },
                { "03-viaje-cero-por-diez.png", 9.40 },
                { "04-resultado-por-cien.png", 12.90 },
                { "05-resultado-por-mil.png", 14.95 },
                { "06-pregunta-think.png", 17.40 }
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

    // Diagnóstico local: reproduce la misma sesión de interrupción que usa
    // Guardian, incluido el mute configurado, sin requerir timer ni servidor.
    public static class VisualStoryInterruptionPreview
    {
        public static int Run()
        {
            var config = GuardianConfig.Default();
            config.MuteSystemAudioDuringMission = true;
            config.PauseMediaOnMission = false;
            var logger = new EventLogger(config);
            var mission = VisualStoryDefinition.CreateMission();
            var media = MediaInterruptionSession.Start(config, logger, mission.Id, MissionTrigger.Manual, true);
            logger.Log("VisualStoryInterruptionPreviewStarted", VisualStoryDefinition.Telemetry(mission));
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var window = new VisualStoryWindow(mission, config, logger);
            window.UnlockRequested += delegate { media.Dispose(); app.Shutdown(0); };
            window.AdminShutdownRequested += delegate { media.Dispose(); app.Shutdown(0); };
            window.Closed += delegate { media.Dispose(); };
            app.MainWindow = window;
            window.Show();
            return app.Run();
        }
    }

    // Preview DEV del flujo completo: el mismo GuardianController selecciona el
    // primer visual y después abre la misión matemática enlazada.
    public static class VisualStoryPrimerPreview
    {
        public static int Run()
        {
            var config = GuardianConfig.Default();
            config.MissionConfig.EnabledSkills = new List<string> { VisualStoryDefinition.SkillKey, VisualStoryDefinition.PrimerTargetSkillKey };
            config.MuteSystemAudioDuringMission = true;
            config.PauseMediaOnMission = false;
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            var controller = new GuardianController(config, new EventLogger(config));
            controller.MissionFlowCompleted += delegate { app.Shutdown(0); };
            controller.TriggerMissionNow();
            return app.Run();
        }
    }

    public sealed class VisualStoryPreviewWindow : Window
    {
        private readonly MultiplyBy100StorySurface _surface = new MultiplyBy100StorySurface();
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Slider _slider;
        private readonly TextBlock _time;
        private readonly VisualStoryMp3Player _narrator = new VisualStoryMp3Player(null, null);
        private double _duration = VisualStoryDefinition.DurationSeconds;
        private bool _playing;
        private bool _updatingSlider;
        private bool _waitingForAudio;

        public VisualStoryPreviewWindow()
        {
            Title = "Guardian — Preview Visual Story"; Width = 1280; Height = 780; MinWidth = 960; MinHeight = 600;
            Background = new SolidColorBrush(Color.FromRgb(3, 10, 22)); WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var root = new Grid(); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(_surface, 0); root.Children.Add(_surface);
            var controls = new Grid { Height = 64, Background = new SolidColorBrush(Color.FromRgb(8, 27, 39)), Margin = new Thickness(0) };
            controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); controls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var restart = new Button { Content = "REINICIAR", Width = 112, Height = 38, Margin = new Thickness(16, 0, 12, 0) }; restart.Click += delegate { Restart(); }; controls.Children.Add(restart);
            _slider = new Slider { Minimum = 0, Maximum = _duration, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
            _slider.ValueChanged += delegate { if (!_updatingSlider) { Pause(); _surface.PositionSeconds = _slider.Value; UpdateTime(); } }; Grid.SetColumn(_slider, 1); controls.Children.Add(_slider);
            _time = new TextBlock { Width = 88, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_time, 2); controls.Children.Add(_time); Grid.SetRow(controls, 1); root.Children.Add(controls); Content = root;
            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) }; _timer.Tick += delegate { Advance(); };
            _narrator.DurationAvailable += delegate(double seconds) { _duration = seconds + .85; _surface.DurationSeconds = _duration; _slider.Maximum = _duration; if (_waitingForAudio) { _waitingForAudio = false; _clock.Restart(); _playing = true; _timer.Start(); } };
            Loaded += delegate { Restart(); }; Closed += delegate { _timer.Stop(); _narrator.Dispose(); };
        }

        private void Restart()
        {
            _timer.Stop(); _narrator.Stop(); _surface.PositionSeconds = 0; _playing = false; _waitingForAudio = true; _narrator.Start(); UpdateTime();
        }
        private void Pause() { _playing = false; _waitingForAudio = false; _timer.Stop(); _narrator.Stop(); }
        private void Advance()
        {
            if (!_playing) return;
            var position = _clock.Elapsed.TotalSeconds;
            if (position >= _duration) { position = _duration; Pause(); }
            _surface.PositionSeconds = position; _updatingSlider = true; _slider.Value = position; _updatingSlider = false; UpdateTime();
        }
        private void UpdateTime() { _time.Text = _surface.PositionSeconds.ToString("00.0", CultureInfo.InvariantCulture) + " / " + _duration.ToString("00.0", CultureInfo.InvariantCulture); }
    }
}
