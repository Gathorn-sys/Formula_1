using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Formula
{
    public class Game : IDisposable
    {
        public Bitmap TrackImage { get; private set; }
        public Car PlayerCar { get; private set; }
        public List<BotCar> Bots { get; private set; }

        public enum RaceState { Countdown, Racing, RaceOver }
        public RaceState CurrentRaceState { get; private set; }
        public const int TotalLaps = 3;

        public string CountdownMessage { get; private set; }
        public string RaceOverMessage { get; private set; }

        private int countdownValue;
        private float countdownTimerAccumulator;
        private const float CountdownInterval = 1.0f;

        private List<PointF> playerCheckpoints;
        private int currentPlayerCheckpointIndex;
        private const float CheckpointRadius = 30f;
        private Pen checkpointPen = new Pen(Color.FromArgb(150, Color.Yellow), 3);
        private Brush checkpointBrush = new SolidBrush(Color.FromArgb(100, Color.Yellow));
        private Font checkpointFont = new Font("Arial", 10, FontStyle.Bold);
        private Brush checkpointTextBrush = Brushes.Black;

        private string trackImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "race.png");
        private string playerCarImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "car.png");
        private string botCarImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "botcar.png");

        private List<PointF> suzukaPathPoints = new List<PointF>
        {
            

            new PointF(1221.67f, 66.67f),
            new PointF(1458.33f, 75.00f),
            new PointF(1525.00f, 158.33f),
            new PointF(1375.00f, 195.83f),
            new PointF(1275.00f, 254.17f),
            new PointF(1178.33f, 225.00f),
            new PointF(1066.67f, 283.33f),
            new PointF(958.33f, 194.17f),
            new PointF(858.33f, 237.50f),
            new PointF(841.67f, 483.33f),
            new PointF(758.33f, 575.00f),
            new PointF(616.67f, 491.67f),
            new PointF(483.33f, 350.00f),
            new PointF(458.33f, 533.33f),
            new PointF(333.33f, 666.67f),
            new PointF(125.00f, 666.67f),
            new PointF(66.67f, 750.00f),
            new PointF(145.83f, 808.33f),
            new PointF(391.67f, 750.00f),
            new PointF(629.17f, 591.67f),
            new PointF(725.00f, 475.00f),
            new PointF(733.33f, 221.67f),
            new PointF(819.17f, 101.67f),
            new PointF(958.33f, 65.00f),
        };

        public Game()
        {
            Bots = new List<BotCar>();
            LoadResources();
            InitializeEntities();
            RestartRace();
        }

        private void LoadResources()
        {
            try
            {
                TrackImage = new Bitmap(trackImagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження трека: {trackImagePath}\n{ex.Message}", "Критична помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                TrackImage = new Bitmap(800, 600);
                using (Graphics g = Graphics.FromImage(TrackImage))
                {
                    g.FillRectangle(Brushes.DarkGray, 0, 0, 800, 600);
                    g.DrawString("Трек не завантажений", new Font("Arial", 16), Brushes.White, 10, 10);
                }
            }
        }

        private void InitializeEntities()
        {
            if (!suzukaPathPoints.Any())
            {
                suzukaPathPoints.Add(new PointF(100, 100));
                suzukaPathPoints.Add(new PointF(200, 100));
            }

            PointF playerStartPos = suzukaPathPoints[0];
            float playerStartAngle = CalculateInitialAngle(suzukaPathPoints[0], suzukaPathPoints.Count > 1 ? suzukaPathPoints[1] : new PointF(playerStartPos.X + 10, playerStartPos.Y));
            PlayerCar = new Car(playerCarImagePath, playerStartPos, playerStartAngle);

            Bots.Add(new BotCar(botCarImagePath, suzukaPathPoints, 4.5f));

            playerCheckpoints = new List<PointF>(suzukaPathPoints);
        }

        private float CalculateInitialAngle(PointF p1, PointF p2)
        {
            return (float)(Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180.0 / Math.PI);
        }

        public void RestartRace()
        {
            CurrentRaceState = RaceState.Countdown;
            countdownValue = 3;
            CountdownMessage = countdownValue.ToString();
            countdownTimerAccumulator = 0f;
            RaceOverMessage = "";

            PlayerCar.Reset();
            foreach (var bot in Bots)
            {
                bot.Reset();
            }
            currentPlayerCheckpointIndex = 0;
        }

        public void Update(Keys[] pressedKeys, float deltaTime)
        {
            bool movementEnabled = CurrentRaceState == RaceState.Racing;

            if (CurrentRaceState == RaceState.Countdown)
            {
                countdownTimerAccumulator += deltaTime;
                if (countdownTimerAccumulator >= CountdownInterval)
                {
                    countdownTimerAccumulator -= CountdownInterval;
                    countdownValue--;
                    if (countdownValue > 0)
                    {
                        CountdownMessage = countdownValue.ToString();
                    }
                    else if (countdownValue == 0)
                    {
                        CountdownMessage = "GO!";
                    }
                    else
                    {
                        CurrentRaceState = RaceState.Racing;
                        CountdownMessage = "";
                    }
                }
            }

            PlayerCar.Update(pressedKeys, TrackImage, movementEnabled);
            foreach (var bot in Bots)
            {
                bot.Update(movementEnabled);
            }

            if (CurrentRaceState == RaceState.Racing)
            {
                CheckPlayerCheckpoints();
                CheckLapCompletionAndWinCondition();
            }
        }

        private void CheckPlayerCheckpoints()
        {
            if (PlayerCar.CurrentLap >= TotalLaps) return;

            if (currentPlayerCheckpointIndex < playerCheckpoints.Count)
            {
                PointF currentCheckpoint = playerCheckpoints[currentPlayerCheckpointIndex];
                float dx = PlayerCar.Position.X - currentCheckpoint.X;
                float dy = PlayerCar.Position.Y - currentCheckpoint.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                float carRadius = Math.Max(PlayerCar.CarImage.Width, PlayerCar.CarImage.Height) / 3.0f;

                if (distance < CheckpointRadius + carRadius)
                {
                    currentPlayerCheckpointIndex++;
                    if (currentPlayerCheckpointIndex >= playerCheckpoints.Count)
                    {
                        PlayerCar.CurrentLap++;
                        currentPlayerCheckpointIndex = 0;
                    }
                }
            }
        }

        private void CheckLapCompletionAndWinCondition()
        {
            if (PlayerCar.CurrentLap >= TotalLaps)
            {
                CurrentRaceState = RaceState.RaceOver;
                RaceOverMessage = "Player WINS!";
                return;
            }

            foreach (var bot in Bots)
            {
                if (bot.CurrentLap >= TotalLaps)
                {
                    CurrentRaceState = RaceState.RaceOver;
                    RaceOverMessage = "Bot WINS!";
                    return;
                }
            }
        }

        public void Draw(Graphics g)
        {
            if (TrackImage != null)
            {
                g.DrawImage(TrackImage, 0, 0, TrackImage.Width, TrackImage.Height);
            }

            if (CurrentRaceState == RaceState.Racing &&
                PlayerCar.CurrentLap < TotalLaps &&
                currentPlayerCheckpointIndex < playerCheckpoints.Count)
            {
                PointF cp = playerCheckpoints[currentPlayerCheckpointIndex];
                RectangleF rect = new RectangleF(cp.X - CheckpointRadius, cp.Y - CheckpointRadius, CheckpointRadius * 2, CheckpointRadius * 2);
                g.FillEllipse(checkpointBrush, rect);
                g.DrawEllipse(checkpointPen, rect);
                string cpNum = (currentPlayerCheckpointIndex + 1).ToString();
                SizeF textSize = g.MeasureString(cpNum, checkpointFont);
                g.DrawString(cpNum, checkpointFont, checkpointTextBrush, cp.X - textSize.Width / 2, cp.Y - textSize.Height / 2);
            }

            foreach (var bot in Bots)
            {
                bot.Draw(g);
            }
            PlayerCar.Draw(g);
        }

        public void Dispose()
        {
            TrackImage?.Dispose();
            PlayerCar?.CarImage?.Dispose();
            foreach (var bot in Bots)
            {
                bot?.CarImage?.Dispose();
            }
            checkpointPen?.Dispose();
            checkpointBrush?.Dispose();
            checkpointFont?.Dispose();
            checkpointTextBrush?.Dispose();
        }
    }
}
