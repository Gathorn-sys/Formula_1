using System;
using System.Drawing;
using System.Windows.Forms;

namespace Formula
{
    public class Car
    {
        public Image CarImage { get; private set; }
        public PointF Position { get; set; }
        public float Angle { get; set; }
        public float Speed { get; private set; }

        public int CurrentLap { get; set; } = 0;
        private PointF initialPosition;
        private float initialAngle;

        private const float MaxSpeed = 5f;
        private const float MaxReverseSpeed = -2f;
        private const float Acceleration = 0.3f;
        private const float Deceleration = 0.1f;
        private const float TurnRate = 3.5f;
        private const float OffTrackPenalty = 0.5f;

        private SizeF carSize;

        public Car(string imagePath, PointF startPosition, float startAngle)
        {
            try
            {
                CarImage = Image.FromFile(imagePath);
                carSize = new SizeF(CarImage.Width, CarImage.Height);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження машинки: {imagePath}\n{ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Bitmap bmp = new Bitmap(30, 15);
                using (Graphics g = Graphics.FromImage(bmp)) { g.FillRectangle(Brushes.Red, 0, 0, 30, 15); }
                CarImage = bmp;
                carSize = new SizeF(30, 15);
            }
            this.initialPosition = startPosition;
            this.initialAngle = startAngle;
            Reset();
        }

        public void Reset()
        {
            Position = initialPosition;
            Angle = initialAngle;
            Speed = 0;
            CurrentLap = 0;
        }

        public void Update(Keys[] pressedKeys, Bitmap trackBitmap, bool movementEnabled)
        {
            if (movementEnabled)
            {
                bool accelerating = false;
                bool reversing = false;

                foreach (Keys key in pressedKeys)
                {
                    if (key == Keys.Up || key == Keys.W)
                    {
                        Speed += Acceleration;
                        accelerating = true;
                    }
                    if (key == Keys.Down || key == Keys.S)
                    {
                        Speed -= Acceleration * 0.7f;
                        reversing = true;
                    }
                    if (Speed != 0)
                    {
                        if (key == Keys.Left || key == Keys.A)
                        {
                            Angle -= TurnRate * Math.Sign(Speed);
                        }
                        if (key == Keys.Right || key == Keys.D)
                        {
                            Angle += TurnRate * Math.Sign(Speed);
                        }
                    }
                }

                if (Speed > MaxSpeed) Speed = MaxSpeed;
                if (Speed < MaxReverseSpeed) Speed = MaxReverseSpeed;

                if (!accelerating && !reversing)
                {
                    if (Speed > Deceleration)
                        Speed -= Deceleration;
                    else if (Speed < -Deceleration)
                        Speed += Deceleration;
                    else
                        Speed = 0;
                }
            }
            else
            {
                if (Speed > Deceleration)
                    Speed -= Deceleration * 2;
                else if (Speed < -Deceleration)
                    Speed += Deceleration * 2;
                else
                    Speed = 0;
            }


            float angleRad = (float)(Angle * Math.PI / 180.0);
            Position = new PointF(
                Position.X + (float)(Speed * Math.Cos(angleRad)),
                Position.Y + (float)(Speed * Math.Sin(angleRad))
            );

            CheckOffTrack(trackBitmap);

            if (Angle > 360) Angle -= 360;
            if (Angle < 0) Angle += 360;
        }

        private void CheckOffTrack(Bitmap trackBitmap)
        {
            if (trackBitmap == null || Speed == 0) return;

            float angleRad = (float)(Angle * Math.PI / 180.0);
            float cosA = (float)Math.Cos(angleRad);
            float sinA = (float)Math.Sin(angleRad);
            float halfW = carSize.Width / 2;
            float halfH = carSize.Height / 2;

            PointF[] carCorners = new PointF[4];
            carCorners[0] = new PointF(Position.X + halfW * cosA - halfH * sinA, Position.Y + halfW * sinA + halfH * cosA);
            carCorners[1] = new PointF(Position.X + halfW * cosA + halfH * sinA, Position.Y + halfW * sinA - halfH * cosA);

            Point[] pointsToCheck = new Point[]
            {
                new Point((int)Position.X, (int)Position.Y),
                new Point((int)carCorners[0].X, (int)carCorners[0].Y),
                new Point((int)carCorners[1].X, (int)carCorners[1].Y)
            };

            bool onTrack = false;
            foreach (var p in pointsToCheck)
            {
                if (p.X >= 0 && p.X < trackBitmap.Width && p.Y >= 0 && p.Y < trackBitmap.Height)
                {
                    Color pixelColor = trackBitmap.GetPixel(p.X, p.Y);
                    if (pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0)
                    {
                        onTrack = true;
                        break;
                    }
                }
            }

            if (!onTrack)
            {
                Speed *= OffTrackPenalty;
            }
        }

        public void Draw(Graphics g)
        {
            if (CarImage == null) return;
            var oldState = g.Save();
            g.TranslateTransform(Position.X, Position.Y);
            g.RotateTransform(Angle);
            g.DrawImage(CarImage, -carSize.Width / 2, -carSize.Height / 2, carSize.Width, carSize.Height);
            g.Restore(oldState);
        }
    }
}
