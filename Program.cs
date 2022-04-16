using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BarTime
{
    internal static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static void Main()
        {
            var font = new Font("Tahoma", 0.35F, FontStyle.Bold);

            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = null;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new ContextMenu(new[] {new MenuItem("Exit", (_, _) => Application.Exit())});

            var bitmap = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
            var g = PrepareGraphics(bitmap);
            var timer = new System.Threading.Timer(_ =>
            {
                DrawClock(bitmap, g, font, Color.White, DateTime.Now.ToLocalTime());
                notifyIcon.Text = GenerateText(DateTime.Now.ToLocalTime());
                notifyIcon.Icon?.Dispose();
                notifyIcon.Icon = ConvertToIcon(bitmap);
            }, null, 0, 10000);

            Application.ApplicationExit += (_, _) =>
            {
                g.Dispose();
                timer.Dispose();
                bitmap.Dispose();
                notifyIcon.Dispose();
            };

            Application.Run();
        }

        private static string GenerateText(DateTime now) => $"{now.ToShortTimeString()} - {now:dd MMM yyyy}";

        private static void DrawClock(Image img, Graphics g, Font font, Color color, DateTime localTime)
        {
            var penSize = 24f / (img.Width + 0.5f);
            using var solid = new Pen(color, penSize);
            using var faded = new Pen(Color.FromArgb(128, color), penSize);
            using var brush = new SolidBrush(Color.White);

            g.Clear(Color.Transparent);
            DrawArc(g, faded, 360f);
            DrawArc(g, solid, localTime.Minute / 60f * 360f);
            DrawString(g, font, brush, localTime.ToString("HH"));
        }

        private static void DrawArc(Graphics g, Pen pen, float degrees)
        {
            var pos = pen.Width / 2f - 0.5f;
            var size = 1f - pen.Width;
            var rect = new RectangleF(pos, pos, size, size);
            g.DrawArc(pen, rect, -90f, degrees);
        }

        private static void DrawString(Graphics g, Font font, Brush brush, string str)
        {
            var size = g.MeasureString(str, font);
            g.DrawString(str, font, brush, -size.Width / 2f, -size.Height / 2f);
        }

        private static Graphics PrepareGraphics(Image bitmap)
        {
            var g = HighGraphicsQuality(Graphics.FromImage(bitmap));
            g.ResetTransform();
            g.ScaleTransform(bitmap.Width, bitmap.Height);
            g.TranslateTransform(0.5f, 0.5f);
            return g;
        }

        private static Icon ConvertToIcon(Image bitmap)
        {
            using var resized = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            var iconHandle = ResizeImage(resized, bitmap).GetHicon();
            var tempManagedRes = Icon.FromHandle(iconHandle);
            var icon = (Icon) tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
            return icon;
        }

        private static Bitmap ResizeImage(Bitmap resized, Image bitmap)
        {
            using var g = HighGraphicsQuality(Graphics.FromImage(resized));
            g.DrawImage(bitmap, new Rectangle(Point.Empty, resized.Size));
            return resized;
        }

        private static Graphics HighGraphicsQuality(Graphics g)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            return g;
        }
    }
}
