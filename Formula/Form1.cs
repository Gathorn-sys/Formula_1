using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Formula
{
    public partial class Form1 : Form
    {

        private Game game;
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();

        private Label lblPlayerLaps;
        private Label lblBotLaps;
        private Label lblPlayerSpeed;
        private Label lblRaceInfo;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            pictureBoxTrack.Paint += PictureBoxTrack_Paint;

            InitializeUI();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            game = new Game();

            if (game.TrackImage != null)
            {
                pictureBoxTrack.Width = game.TrackImage.Width;
                pictureBoxTrack.Height = game.TrackImage.Height;
                this.ClientSize = new Size(game.TrackImage.Width, game.TrackImage.Height);
            }
            else
            {
                pictureBoxTrack.Width = 800;
                pictureBoxTrack.Height = 600;
                this.ClientSize = new Size(800, 600);
            }

            this.WindowState = FormWindowState.Maximized;
            CenterRaceInfoLabel();


            gameTimer.Interval = 20;
            gameTimer.Start();
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            game.Update(pressedKeys.ToArray(), (float)gameTimer.Interval / 1000.0f);
            UpdateUI();
            pictureBoxTrack.Invalidate();
        }

        private void PictureBoxTrack_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (game != null)
            {
                game.Draw(e.Graphics);
            }
        }

        private void UpdateUI()
        {
            if (game == null) return;

            lblPlayerLaps.Text = $"Player Lap: {game.PlayerCar.CurrentLap}/{Game.TotalLaps}";
            if (game.Bots.Any())
            {
                lblBotLaps.Text = $"Bot Lap: {game.Bots[0].CurrentLap}/{Game.TotalLaps}";
            }
            lblPlayerSpeed.Text = $"Speed: {Math.Abs(game.PlayerCar.Speed * 45):F0} km/h";

            switch (game.CurrentRaceState)
            {
                case Game.RaceState.Countdown:
                    lblRaceInfo.Text = game.CountdownMessage;
                    lblRaceInfo.Visible = true;
                    CenterRaceInfoLabel();
                    break;
                case Game.RaceState.Racing:
                    lblRaceInfo.Visible = false;
                    break;
                case Game.RaceState.RaceOver:
                    lblRaceInfo.Text = game.RaceOverMessage + "\n(Click to Restart)";
                    lblRaceInfo.Visible = true;
                    CenterRaceInfoLabel();
                    break;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);

            if (e.KeyCode == Keys.Enter && game != null && game.CurrentRaceState == Game.RaceState.RaceOver)
            {
                game.RestartRace();
                if (!gameTimer.Enabled) gameTimer.Start();
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            gameTimer.Stop();
            game?.Dispose();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            
        }

        private void CenterRaceInfoLabel()
        {
            if (lblRaceInfo != null)
            {
                lblRaceInfo.Left = (this.ClientSize.Width - lblRaceInfo.Width) / 2;
                lblRaceInfo.Top = (this.ClientSize.Height - lblRaceInfo.Height) / 2;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (game != null && game.CurrentRaceState == Game.RaceState.RaceOver)
            {
                game.RestartRace();
                if (!gameTimer.Enabled)
                {
                    gameTimer.Start();
                }
            }
        }

        private void InitializeUI()
        {
            lblPlayerLaps = new Label
            {
                Name = "lblPlayerLaps",
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 0, 0),
                Font = new Font(label1.Font.FontFamily, 15),
                Margin = new Padding(0,0,0,20)
            };
            lblBotLaps = new Label
            {
                Name = "lblBotLaps",
                Location = new Point(10, 30),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 0, 0),
                Font = new Font(label1.Font.FontFamily, 15),
                Margin = new Padding(0, 0, 0, 20)
            };
            lblPlayerSpeed = new Label
            {
                Name = "lblPlayerSpeed",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 0, 0),
                Font = new Font(label1.Font.FontFamily, 15),
                Margin = new Padding(0, 0, 0, 20)
            };
            lblRaceInfo = new Label
            {
                Name = "lblRaceInfo",
                AutoSize = true,
                Font = new Font("Arial", 36, FontStyle.Bold),
                ForeColor = Color.Yellow,
                BackColor = Color.FromArgb(180, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            this.Controls.Add(lblPlayerLaps);
            this.Controls.Add(lblBotLaps);
            this.Controls.Add(lblPlayerSpeed);
            this.Controls.Add(lblRaceInfo);

            lblPlayerLaps.BringToFront();
            lblBotLaps.BringToFront();
            lblPlayerSpeed.BringToFront();
            lblRaceInfo.BringToFront();

            lblRaceInfo.Click += LblRaceInfo_Click;
        }

        private void LblRaceInfo_Click(object sender, EventArgs e)
        {
            if (game != null && game.CurrentRaceState == Game.RaceState.RaceOver)
            {
                game.RestartRace();
                if (!gameTimer.Enabled)
                {
                    gameTimer.Start();
                }
            }
        }
    }
}
