using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Formula
{
    public class BotCar
    {
        public Image CarImage { get; private set; }
        public PointF Position { get; private set; }
        public float Angle { get; private set; }
        public float Speed { get; private set; }
        private float initialSpeed;

        public int CurrentLap { get; private set; } = 0;
        private List<PointF> initialPath;
        private PointF initialPosition;

        private List<PointF> path;
        private int currentPathIndex;
        private SizeF carSize;
        private const float TurnSpeedFactor = 0.1f;
        private const float TargetReachedThresholdMultiplier = 1.5f;

        public BotCar(string imagePath, List<PointF> trackPath, float speed)
        {
            try
            {
                CarImage = Image.FromFile(imagePath);
                carSize = new SizeF(CarImage.Width, CarImage.Height);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження бота: {imagePath}\n{ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Bitmap bmp = new Bitmap(30, 15);
                using (Graphics g = Graphics.FromImage(bmp)) { g.FillRectangle(Brushes.Blue, 0, 0, 30, 15); }
                CarImage = bmp;
                carSize = new SizeF(30, 15);
            }

            this.initialPath = new List<PointF>(trackPath);
            this.initialSpeed = speed;
            if (this.initialPath != null && this.initialPath.Any())
            {
                this.initialPosition = this.initialPath[0];
            }
            else
            {
                this.initialPosition = new PointF(100, 100);
                this.initialPath = new List<PointF> { this.initialPosition };
            }
            Reset();
        }

        public void Reset()
        {
            path = new List<PointF>(initialPath);
            Speed = 0;
            currentPathIndex = 0;
            if (path != null && path.Any())
            {
                Position = path[0];
                if (path.Count > 1)
                {
                    float dx = path[1].X - Position.X;
                    float dy = path[1].Y - Position.Y;
                    Angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);
                }
                else
                {
                    Angle = 0;
                }
            }
            else
            {
                Position = initialPosition;
                Angle = 0;
            }
            CurrentLap = 0;
        }

        public void Update(bool movementEnabled)
        {
            if (path == null || !path.Any()) return;

            if (!movementEnabled)
            {
                Speed = 0;
                return;
            }

            if (Speed < initialSpeed)
            {
                Speed += 0.1f;
                if (Speed > initialSpeed) Speed = initialSpeed;
            }


            PointF targetPoint = path[currentPathIndex];
            float dx = targetPoint.X - Position.X;
            float dy = targetPoint.Y - Position.Y;
            float distanceToTarget = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distanceToTarget < Speed * TargetReachedThresholdMultiplier + carSize.Height)
            {
                currentPathIndex++;
                if (currentPathIndex >= path.Count)
                {
                    currentPathIndex = 0;
                    CurrentLap++;
                }
            }

            float targetAngleRad = (float)Math.Atan2(dy, dx);
            float targetAngleDeg = (float)(targetAngleRad * 180.0 / Math.PI);
            float angleDiff = targetAngleDeg - Angle;
            while (angleDiff <= -180) angleDiff += 360;
            while (angleDiff > 180) angleDiff -= 360;

            Angle += angleDiff * TurnSpeedFactor * (Speed / initialSpeed);

            if (Angle > 360) Angle -= 360;
            if (Angle < 0) Angle += 360;

            float actualAngleRad = (float)(Angle * Math.PI / 180.0);
            Position = new PointF(
                Position.X + (float)(Speed * Math.Cos(actualAngleRad)),
                Position.Y + (float)(Speed * Math.Sin(actualAngleRad))
            );
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
