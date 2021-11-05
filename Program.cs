﻿using System;
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
            var font = new Font("Poppins", 0.4F, FontStyle.Bold);

            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = null;
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = new ContextMenu(new[] {new MenuItem("Exit", (_, _) => Application.Exit())});

            var bitmap = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            var graphics = PrepareGraphics(bitmap);

            var timer = new System.Threading.Timer(_ =>
            {
                DrawClock(bitmap, graphics, font, Color.White, DateTime.Now.ToLocalTime());

                notifyIcon.Text = DateTime.Now.ToLocalTime().ToShortTimeString();
                notifyIcon.Icon?.Dispose();
                notifyIcon.Icon = ConvertToIcon(bitmap);
            }, null, 0, 1000);

            Application.ApplicationExit += (_, _) =>
            {
                graphics.Dispose();
                timer.Dispose();
                bitmap.Dispose();
                notifyIcon.Dispose();
            };

            Application.Run();
        }

        private static void DrawClock(Image img, Graphics graphics, Font font, Color color, DateTime localTime)
        {
            var penSize = 12f / (img.Width + 0.5f);
            using var solid = new Pen(color, penSize);
            using var faded = new Pen(Color.FromArgb(128, color), penSize);
            using var brush = new SolidBrush(Color.White);

            graphics.Clear(Color.Transparent);

            DrawArc(graphics, faded, 360f);
            DrawArc(graphics, solid, localTime.Minute / 60f * 360f);
            DrawString(graphics, font, brush, localTime.ToString("HH"));
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
            var width = g.MeasureString(str, font).Width;
            var height = g.MeasureString(str, font, 0, new StringFormat(StringFormatFlags.DirectionVertical)).Height;
            g.DrawString(str, font, brush, -width / 2f, -height / 2f);
        }

        private static Graphics PrepareGraphics(Image bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            graphics.ResetTransform();
            graphics.ScaleTransform(bitmap.Width, bitmap.Height);
            graphics.TranslateTransform(0.5f, 0.5f);
            return graphics;
        }

        private static Icon ConvertToIcon(Bitmap bitmap)
        {
            var iconHandle = bitmap.GetHicon();
            var tempManagedRes = Icon.FromHandle(iconHandle);
            var icon = (Icon) tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
            return icon;
        }
    }
}