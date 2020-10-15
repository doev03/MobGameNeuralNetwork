using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MobGame
{
    public partial class Form1 : Form
    {
        Random rnd = new Random();
        PictureBox[] coins = new PictureBox[1];
        Point[] coinPoints;
        int score;
        Image[] imgMainPlayer = new Image[2];
        int playerSpeed;
        int angle;
        Point[] rays = new Point[8];
        int raysDistance;
        int[] raysDistanceFact = new int[8];
        int raysDistanceCalc;
        int raySize;
        int xIntersectCalc;
        int yIntersectCalc;
        int xIntersect;
        int yIntersect;
        int[] xIntersectFact = new int[8];
        int[] yIntersectFact = new int[8];

        double[] L0 = new double[9]; 
        double[] L1 = new double[10];
        double[] L2 = new double[10];
        double[] L3 = new double[3];

        double[,] W01 = new double[9, 9];
        double[,] W12 = new double[9, 10];
        double[,] W23 = new double[3, 10];

        short W01Length0;
        short W01Length1;
        short W12Length0;
        short W12Length1;
        short W23Length0;
        short W23Length1;

        int[] mobScore = new int[80]; //количество мобов в одном поколении должно быть кратно 4-м

        int mobUpIteration;
        int oneGenerationIteration;
        int firstGenerationIteration;
        double mutationRange;
        int generationNum;

        string weightsPath = @"C:\Users\Олег\source\repos\MobGame\Weights";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            imgMainPlayer[0] = Image.FromFile("assets\\Player1.png");
            imgMainPlayer[1] = Image.FromFile("assets\\Player2.png");
            playerSpeed = 5;
            angle = 0;
            Image imgCoin = Image.FromFile("assets\\coin.png");
            score = 0;
            coinPoints = new Point[coins.Length * 4];
            raySize = 2000;

            L0[8] = 1;
            L1[9] = 1;
            L2[9] = 1;
            oneGenerationIteration = 0;
            mutationRange = 0.99;
            generationNum = 1;
            W01Length0 = Convert.ToInt16(W01.GetLength(0));
            W01Length1 = Convert.ToInt16(W01.GetLength(1));
            W12Length0 = Convert.ToInt16(W12.GetLength(0));
            W12Length1 = Convert.ToInt16(W12.GetLength(1));
            W23Length0 = Convert.ToInt16(W23.GetLength(0));
            W23Length1 = Convert.ToInt16(W23.GetLength(1));

            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = new Point(MainPlayer.Location.X + MainPlayer.Width / 2 - (int)(raySize * Math.Sin(i*45 * (Math.PI / 180))), MainPlayer.Location.Y + MainPlayer.Height / 2 + (int)(raySize * Math.Cos(i*45 * (Math.PI / 180))));
                xIntersectFact[i] = 0;
                xIntersectFact[i] = 0;
            }

            for (int i = 0; i < coins.Length; i++)
            {
                coins[i] = new PictureBox
                {
                    Size = new Size(40, 49),
                    BackColor = Color.Transparent,
                    Image = imgCoin,
                    Location = new Point(rnd.Next(50, 750), rnd.Next(60, 530))
                };

                coinPoints[4 * i] = new Point(coins[i].Location.X, coins[i].Location.Y);
                coinPoints[4 * i + 1] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y);
                coinPoints[4 * i + 2] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y + coins[i].Height);
                coinPoints[4 * i + 3] = new Point(coins[i].Location.X, coins[i].Location.Y + coins[i].Height);
                this.Controls.Add(coins[i]);
                coins[i].BringToFront();
            }

            
        }

        private void LeftRotateDef()
        {
            angle -= playerSpeed;
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            RaysRotate();
            AddRayDef();
        }
        private void RightRotateDef()
        {
            angle += playerSpeed;
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            RaysRotate();
            AddRayDef();
        }
        private void UpMoveDef()
        {
            if (true)
            {
                MainPlayer.Left -= (int)Math.Round(playerSpeed * Math.Sin(angle * (Math.PI / 180)));
                MainPlayer.Top += (int)Math.Round(playerSpeed * Math.Cos(angle * (Math.PI / 180)));
                if (!LeftRotate.Enabled && !RightRotate.Enabled)
                {
                    RaysRotate();
                    AddRayDef();
                }
            }
            else
            {
                //GameOver("You lost a game in\nwhich you just need\nto collect coins :|");
            }
        }
        /*private void DownMoveDef()
        {
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            if (true)
            {
                MainPlayer.Left += (int)Math.Round(playerSpeed * Math.Sin(angle * (Math.PI / 180)));
                MainPlayer.Top -= (int)Math.Round(playerSpeed * Math.Cos(angle * (Math.PI / 180)));
                if (!LeftRotate.Enabled && !RightRotate.Enabled)
                {
                    RaysRotate();
                    AddRayDef();
                }
            }
            else
            {
                //GameOver("You lost a game in\nwhich you just need\nto collect coins :|");
            }
        }*/

        private double Sigmoid(double x)
        {
            return 1.0 / (1.0 + Math.Exp(-x));
        }
        
        private void FillW(double[,] W)
        {
            for (int i = 0; i < W.GetLength(0); i++)
            {
                for (int j = 0; j < W.GetLength(1); j++)
                {
                    W[i, j] = 2 * rnd.NextDouble() - 1;
                }
            }
        }

        private void Forward(double[] Li, double[,] W, double[] Lo)
        {
            for (int i = 0; i < W.GetLength(0); i++)
            {
                Lo[i] = 0;
                for (int j = 0; j < W.GetLength(1); j++)
                {
                    Lo[i] += Li[j] * W[i,j];
                }
                Lo[i] = Sigmoid(Lo[i]);
            }
        }

        private void LeftRotate_Tick(object sender, EventArgs e)
        {
            angle -= playerSpeed;
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            RaysRotate();
            Intersect();
            AddRayDef();
        }

        private void RightRotate_Tick(object sender, EventArgs e)
        {
            angle += playerSpeed;
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            RaysRotate();
            Intersect();
            AddRayDef();
        }

        private void UpMove_Tick(object sender, EventArgs e)
        {
            if (MainPlayer.Top > 10 && MainPlayer.Top < 590 && MainPlayer.Left > 10 && MainPlayer.Left < 790)
            {
                MainPlayer.Left -= (int)Math.Round(playerSpeed * Math.Sin(angle * (Math.PI / 180)));
                MainPlayer.Top += (int)Math.Round(playerSpeed * Math.Cos(angle * (Math.PI / 180)));
                if(!LeftRotate.Enabled && !RightRotate.Enabled)
                {
                    RaysRotate();
                    Intersect();
                    AddRayDef();
                }
            }
            else
            {
                GameOver("You lost a game in\nwhich you just need\nto collect coins :|");
            }
        }

        private void DownMove_Tick(object sender, EventArgs e)
        {
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            if (MainPlayer.Top > 10 && MainPlayer.Top < 590 && MainPlayer.Left > 10 && MainPlayer.Left < 790)
            {
                MainPlayer.Left += (int)Math.Round(playerSpeed * Math.Sin(angle * (Math.PI / 180)));
                MainPlayer.Top -= (int)Math.Round(playerSpeed * Math.Cos(angle * (Math.PI / 180)));
                if (!LeftRotate.Enabled && !RightRotate.Enabled)
                {
                    RaysRotate();
                    Intersect();
                    AddRayDef();
                }
            }
            else
            {
                GameOver("You lost a game in\nwhich you just need\nto collect coins :|");
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[1], angle);
            if (e.KeyCode == Keys.Left)
            {
                LeftRotate.Start();
            }
            if (e.KeyCode == Keys.Right)
            {
                RightRotate.Start();
            }
            if (e.KeyCode == Keys.Up)
            {
                UpMove.Start();
            }
            if (e.KeyCode == Keys.Down)
            {
                DownMove.Start();
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                LeftRotate.Stop();
            }
            if (e.KeyCode == Keys.Right)
            {
                RightRotate.Stop();
            }
            if (e.KeyCode == Keys.Up)
            {
                UpMove.Stop();
            }
            if (e.KeyCode == Keys.Down)
            {
                DownMove.Stop();
            }
            if (UpMove.Enabled == false && DownMove.Enabled == false && LeftRotate.Enabled == false & RightRotate.Enabled == false)
            {
                MainPlayer.Image = RotateImage((Bitmap)imgMainPlayer[0], angle);
            }
        }

        private void Intersect()
        {
            for (int j = 0; j < rays.Length; j++)
            {
                raysDistanceFact[j] = 9999;
            }
            for (int i = 0; i < coins.Length; i++)
            {

                if (coins[i].Bounds.IntersectsWith(MainPlayer.Bounds))
                {
                    coins[i].Location = new Point(rnd.Next(50, 750), rnd.Next(60, 530));

                    coinPoints[4 * i] = new Point(coins[i].Location.X, coins[i].Location.Y);
                    coinPoints[4 * i + 1] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y);
                    coinPoints[4 * i + 2] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y + coins[i].Height);
                    coinPoints[4 * i + 3] = new Point(coins[i].Location.X, coins[i].Location.Y + coins[i].Height);

                    score++;
                    Score.Text = score.ToString();
                }
                for (int j = 0; j < rays.Length; j++)
                {
                    raysDistance = IntersectionOfTwoLineSegments(coinPoints[i*4], coinPoints[i*4+1], new Point(MainPlayer.Location.X + MainPlayer.Width / 2, MainPlayer.Location.Y + MainPlayer.Height / 2), rays[j]);
                    
                    xIntersect = xIntersectCalc;
                    yIntersect = yIntersectCalc;

                    raysDistanceCalc = IntersectionOfTwoLineSegments(coinPoints[i * 4 + 1], coinPoints[i * 4 + 2], new Point(MainPlayer.Location.X + MainPlayer.Width / 2, MainPlayer.Location.Y + MainPlayer.Height / 2), rays[j]);
                    if (raysDistanceCalc < raysDistance)
                    {
                        raysDistance = raysDistanceCalc;
                        xIntersect = xIntersectCalc;
                        yIntersect = yIntersectCalc;
                    }

                    raysDistanceCalc = IntersectionOfTwoLineSegments(coinPoints[i * 4 + 2], coinPoints[i * 4 + 3], new Point(MainPlayer.Location.X + MainPlayer.Width / 2, MainPlayer.Location.Y + MainPlayer.Height / 2), rays[j]);
                    if (raysDistanceCalc < raysDistance)
                    {
                        raysDistance = raysDistanceCalc;
                        xIntersect = xIntersectCalc;
                        yIntersect = yIntersectCalc;
                    }
                    raysDistanceCalc = IntersectionOfTwoLineSegments(coinPoints[i * 4 + 3], coinPoints[i * 4], new Point(MainPlayer.Location.X + MainPlayer.Width / 2, MainPlayer.Location.Y + MainPlayer.Height / 2), rays[j]);
                    if (raysDistanceCalc < raysDistance)
                    {
                        raysDistance = raysDistanceCalc;
                        xIntersect = xIntersectCalc;
                        yIntersect = yIntersectCalc;
                    }
                    if (raysDistance < raysDistanceFact[j])
                    {
                        raysDistanceFact[j] = raysDistance;
                        xIntersectFact[j] = xIntersect;
                        yIntersectFact[j] = yIntersect;
                    }

                    label1.Text = "D: " + Convert.ToString(raysDistanceFact[0]);
                    label2.Text = "D: " + Convert.ToString(raysDistanceFact[1]);
                    label3.Text = "D: " + Convert.ToString(raysDistanceFact[2]);
                    label4.Text = "D: " + Convert.ToString(raysDistanceFact[3]);
                    label5.Text = "D: " + Convert.ToString(raysDistanceFact[4]);
                    label6.Text = "D: " + Convert.ToString(raysDistanceFact[5]);
                    label7.Text = "D: " + Convert.ToString(raysDistanceFact[6]);
                    label8.Text = "D: " + Convert.ToString(raysDistanceFact[7]);
                }

                label1.Visible = true;
            }
        }

        private void GameOver(string str)
        {
            label9.Text = str;
            label9.Visible = true;
            label9.BringToFront();
            MainPlayer.Visible = false;
            MainPlayer.Location = new Point(3333, 3333);
            Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.WhiteSmoke);
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;
            label6.Visible = false;
            label7.Visible = false;
            label8.Visible = false;
        }

        private Bitmap RotateImage(Bitmap bmp, int angle)
        {
            Bitmap rotatedImage = new Bitmap(bmp.Width, bmp.Height);
            rotatedImage.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                //Set точку поворота в центр
                g.TranslateTransform(bmp.Width / 2, bmp.Height / 2);
                //Поворот
                g.RotateTransform(angle);
                //Меняем обратно
                g.TranslateTransform(-bmp.Width / 2, -bmp.Height / 2);
                g.DrawImage(bmp, new Point(0, 0));
            }

            return rotatedImage;
        }

        private int IntersectionOfTwoLineSegments(Point p1, Point p2, Point p3, Point p4)
        {
            //p3, p4 - точки луча

            if (p2.X < p1.X)
            {
                Point tmp = p1;
                p1 = p2;
                p2 = tmp;
            }
            if (p4.X < p3.X)
            {
                Point tmp = p3;
                p3 = p4;
                p4 = tmp;
            }

            if (p2.X < p3.X)
            {
                return 9999;
            }

            //Проверка на вертикальность
            if ((p2.X - p1.X == 0) && (p4.X - p3.X == 0))
            {
                if(p1.X == p3.X)
                {
                    if (!((Math.Max(p1.Y, p2.Y) < Math.Min(p3.Y, p4.Y)) ||
                       (Math.Min(p1.Y, p2.Y) > Math.Max(p3.Y, p4.Y))))
                    {
                        return Math.Min(Math.Min(Math.Abs(p1.Y - p3.Y), Math.Abs(p2.Y - p3.Y)), Math.Min(Math.Abs(p1.Y - p4.Y),Math.Abs(p2.Y-p4.Y)));
                    }
                }
                return 9999;
            }

            //Если отрезок объекта вертикален
            if (p1.X == p2.X)
            {
                //угловой коэфф
                double A = (double)(p3.Y - p4.Y) / (double)(p3.X - p4.X);
                //свободный член
                double B = p3.Y - A * p3.X;
                double Y = A * p1.X + B;

                if (p3.X <= p1.X && p4.X >= p1.X && Math.Min(p1.Y, p2.Y) <= Y && Math.Max(p1.Y, p2.Y) >= Y)
                {
                    //точка пересечения
                    xIntersectCalc = p1.X;
                    yIntersectCalc = (int)Y;
                    return (int)Math.Min(Math.Sqrt(Math.Pow(p1.X - p3.X, 2) + Math.Pow(Y - p3.Y, 2)), Math.Sqrt(Math.Pow(p1.X - p4.X, 2) + Math.Pow(Y - p4.Y, 2)));
                }

                return 9999;
            }

            //Если луч вертикален
            if (p3.X == p4.X)
            {
                //угловой коэфф
                double A = (double)(p1.Y - p2.Y) / (double)(p1.X - p2.X);
                //свободный член
                double B = p1.Y - A * p1.X;
                double Y = A * p3.X + B;

                if (p1.X <= p3.X && p2.X >= p3.X && Math.Min(p3.Y, p4.Y) <= Y &&
                Math.Max(p3.Y, p4.Y) >= Y)
                {
                    //точка пересечения
                    xIntersectCalc = p3.X;
                    yIntersectCalc = (int)Y;
                    return (int)Math.Min(Math.Abs(p3.Y - Y), Math.Abs(p4.Y - Y));
                }

                return 9999;
            }

            double A1 = (double)(p1.Y - p2.Y) / (double)(p1.X - p2.X);
            double B1 = p1.Y - A1 * p1.X;
            double A2 = (double)(p3.Y - p4.Y) / (double)(p3.X - p4.X);
            double B2 = p3.Y - A2 * p3.X;

            if (A1 == A2)
            {
                return 9999;
            }

            double Xa = (B2 - B1) / (A1 - A2);
            double Ya = A2 * Xa + B2;
            if ((Xa < Math.Max(p1.X, p3.X)) || (Xa > Math.Min(p2.X, p4.X)))
            {
                return 9999;
            }
            else
            {
                //точка пересечения
                xIntersectCalc = (int)Xa;
                yIntersectCalc = (int)Ya;
                return (int)Math.Min(Math.Sqrt(Math.Pow(Xa - p3.X, 2) + Math.Pow(Ya - p3.Y, 2)), Math.Sqrt(Math.Pow(Xa - p4.X, 2) + Math.Pow(Ya - p4.Y, 2)));
            }
        }

        private void RaysRotate()
        {
            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = new Point(MainPlayer.Location.X + MainPlayer.Width / 2 - (int)(raySize * Math.Sin((i * 45 + angle) * (Math.PI / 180))), MainPlayer.Location.Y + MainPlayer.Height / 2 + (int)(raySize * Math.Cos((i * 45 + angle) * (Math.PI / 180))));
            }
        }

        private void AddRayDef()
        {
            /*Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.WhiteSmoke);
            for (int i = 0; i < rays.Length; i++)
            {
                g.DrawLine(new Pen(Color.Black), new Point(MainPlayer.Location.X + MainPlayer.Width / 2, MainPlayer.Location.Y + MainPlayer.Height / 2), rays[i]);
                if (raysDistanceFact[i] != 9999)
                {
                    g.FillEllipse(Brushes.Red, xIntersectFact[i] - 5, yIntersectFact[i] - 5, 10, 10);
                }
            }*/
        }

        private void GlobalCycle_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < mobScore.Length; i++)
            {
                mobScore[i] = 0;
            }
            oneGenerationIteration = 0;
            mutationRange *= 0.99;
            label10.Text = "Generation num: " + Convert.ToString(++generationNum);
            OneGeneration.Start();
            GlobalCycle.Stop();
        }

        private void FirstGeneration_Tick(object sender, EventArgs e)
        {
            label10.Text = "Generation num: " + Convert.ToString(generationNum);
            if (firstGenerationIteration == mobScore.Length)
            {
                Crossbreeding(BestMob(mobScore), BestMob(mobScore), BestMob(mobScore), BestMob(mobScore));
                Mutation();
                firstGenerationIteration++;
                FirstGeneration.Stop();
                OneGeneration.Start();
                return;
            }

            FillW(W01);
            FillW(W12);
            FillW(W23);

            //WeightsReader(W01, W12, W23, firstGenerationIteration);

            WeightsWriter(W01, W12, W23, firstGenerationIteration);

            mobUpIteration = 0;
            OneMobLifeCycle.Start();
            FirstGeneration.Stop();
        }

        private void OneGeneration_Tick(object sender, EventArgs e)
        {
            if (generationNum == 1)
            {
                label10.Text = "Generation Num: " + Convert.ToString(++generationNum);
                for (int i = 0; i < mobScore.Length; i++)
                {
                    mobScore[i] = 0;
                }
            }
            if (oneGenerationIteration == mobScore.Length)
            {
                Crossbreeding(BestMob(mobScore), BestMob(mobScore), BestMob(mobScore), BestMob(mobScore));
                Mutation();
                oneGenerationIteration++;
                OneGeneration.Stop();
                GlobalCycle.Start();
                return;
            }

            WeightsReader(W01, W12, W23, oneGenerationIteration);

            mobUpIteration = 0;
            OneMobLifeCycle.Start();
            OneGeneration.Stop();
        }

        private void OneMobLifeCycle_Tick(object sender, EventArgs e)
        {
            label11.Text = "Mob Up Iter: " + mobUpIteration;
            Intersect();
            //raysDistanceFact.CopyTo(L0, 0);
            L0[0] = raysDistanceFact[0];
            L0[1] = raysDistanceFact[1];
            L0[2] = raysDistanceFact[2];
            L0[3] = raysDistanceFact[3];
            L0[4] = raysDistanceFact[4];
            L0[5] = raysDistanceFact[5];
            L0[6] = raysDistanceFact[6];
            L0[7] = raysDistanceFact[7];

            Forward(L0, W01, L1);
            Forward(L1, W12, L2);
            Forward(L2, W23, L3);
            double maxRotateVal = Math.Max(L3[1], L3[2]);

            if (L3[0] > 0.5)
            {
                UpMoveDef();
            }

            if(maxRotateVal > 0.5 && Array.IndexOf(L3, maxRotateVal) != 0)
            {
                switch (Array.IndexOf(L3, maxRotateVal))
                {
                    case 1:
                        LeftRotateDef();
                        break;
                    case 2:
                        RightRotateDef();
                        break;
                    default:
                        break;
                }
            }
            mobUpIteration++;

            if (mobUpIteration == 500) //время жизни одного моба
            {

                if (firstGenerationIteration != mobScore.Length+1)
                {
                    mobScore[firstGenerationIteration] = score;
                    firstGenerationIteration++;
                    FirstGeneration.Start();
                }
                else if (oneGenerationIteration != mobScore.Length+1)
                {
                    mobScore[oneGenerationIteration] = score;
                    oneGenerationIteration++;
                    OneGeneration.Start();
                }
                else
                {
                    GlobalCycle.Start();
                }

                MobRestart();
                OneMobLifeCycle.Stop();
            }

        }

        private void MobRestart()
        {

            score = 0;
            Score.Text = "0";
            RaysRotate();
            MainPlayer.Location = new Point(355, 123);

            for (int i = 0; i < raysDistanceFact.Length; i++)
            {
                raysDistanceFact[i] = 0;
            }

            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].Location = new Point(rnd.Next(50, 750), rnd.Next(60, 530));

                coinPoints[4 * i] = new Point(coins[i].Location.X, coins[i].Location.Y);
                coinPoints[4 * i + 1] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y);
                coinPoints[4 * i + 2] = new Point(coins[i].Location.X + coins[i].Width, coins[i].Location.Y + coins[i].Height);
                coinPoints[4 * i + 3] = new Point(coins[i].Location.X, coins[i].Location.Y + coins[i].Height);
            }

        }

        private void WeightsWriter(double[,] W01, double[,] W12, double[,] W23, int iteration)
        {
            using (StreamWriter sw = new StreamWriter(weightsPath + "mob" + Convert.ToString(iteration) + "-w01.txt", false))
            {
                for (int i = 0; i < W01Length0; i++)
                {
                    for (int j = 0; j < W01Length1; j++)
                    {
                        sw.Write(W01[i, j] + " ");
                    }
                    sw.Write("\n");
                }
            }

            using (StreamWriter sw = new StreamWriter(weightsPath + "mob" + Convert.ToString(iteration) + "-w12.txt", false))
            {
                for (int i = 0; i < W12Length0 ; i++)
                {
                    for (int j = 0; j < W12Length1; j++)
                    {
                        sw.Write(W12[i, j] + " ");
                    }
                    sw.Write("\n");
                }
            }

            using (StreamWriter sw = new StreamWriter(weightsPath + "mob" + Convert.ToString(iteration) + "-w23.txt", false))
            {
                for (int i = 0; i < W23Length0; i++)
                {
                    for (int j = 0; j < W23Length1; j++)
                    {
                        sw.Write(W23[i, j] + " ");
                    }
                    sw.Write("\n");
                }
            }
        }

        private void WeightsReader(double[,] W01, double[,] W12, double[,] W23, int iteration)
        {
            using (StreamReader sr = new StreamReader(weightsPath + "mob" + Convert.ToString(iteration) + "-w01.txt", false))
            {
                for (int i = 0; i < W01Length0; i++)
                {
                    string str = sr.ReadLine();
                    str = str.Trim();
                    string[] nums = str.Split(' ');
                    for (int j = 0; j < W01Length1; j++)
                    {
                        W01[i, j] = Convert.ToDouble(nums[j]);
                    }
                }
            }
            
            using (StreamReader sr = new StreamReader(weightsPath + "mob" + Convert.ToString(iteration) + "-w12.txt", false))
            {
                for (int i = 0; i < W12Length0 ; i++)
                {
                    string str = sr.ReadLine();
                    str = str.Trim();
                    string[] nums = str.Split(' ');
                    for (int j = 0; j < W12Length1; j++)
                    {
                        W12[i, j] = Convert.ToDouble(nums[j]);
                    }
                }
            }
            
            using (StreamReader sr = new StreamReader(weightsPath + "mob" + Convert.ToString(iteration) + "-w23.txt", false))
            {
                for (int i = 0; i < W23Length0; i++)
                {
                    string str = sr.ReadLine();
                    str = str.Trim();
                    string[] nums = str.Split(' ');
                    for (int j = 0; j < W23Length1; j++)
                    {
                        W23[i, j] = Convert.ToDouble(nums[j]);
                    }
                }
            }
        }

        private int BestMob(int[] mobScore)
        {
            int index = Array.IndexOf(mobScore, mobScore.Max());
            mobScore[index] = -1;

            return index;
        }

        private void Crossbreeding(int firstMob, int secondMob, int thirdMob, int fourthMob)
        {
            var firstBestW01 = new double[W01Length0, W01Length1];
            var firstBestW12 = new double[W12Length0 , W12Length1];
            var firstBestW23 = new double[W23Length0, W23Length1];
            var secondBestW01 = new double[W01Length0, W01Length1];
            var secondBestW12 = new double[W12Length0 , W12Length1];
            var secondBestW23 = new double[W23Length0, W23Length1];
            var thirdBestW01 = new double[W01Length0, W01Length1];
            var thirdBestW12 = new double[W12Length0 , W12Length1];
            var thirdBestW23 = new double[W23Length0, W23Length1];
            var fourthBestW01 = new double[W01Length0, W01Length1];
            var fourthBestW12 = new double[W12Length0 , W12Length1];
            var fourthBestW23 = new double[W23Length0, W23Length1];

            //Заполнение firstBestW
            WeightsReader(W01, W12, W23, firstMob);
            WeightsWriter(W01, W12, W23, 4);
            for (int i = 0; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    firstBestW01[i, j] = W01[i, j];
                }
            }
            for (int i = 0; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    firstBestW12[i, j] = W12[i, j];
                }
            }
            for (int i = 0; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    firstBestW23[i, j] = W23[i, j];
                }
            }

            //Заполнение secondBestW
            WeightsReader(W01, W12, W23, secondMob);
            WeightsWriter(W01, W12, W23, 5);
            for (int i = 0; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    secondBestW01[i, j] = W01[i, j];
                }
            }
            for (int i = 0; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    secondBestW12[i, j] = W12[i, j];
                }
            }
            for (int i = 0; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    secondBestW23[i, j] = W23[i, j];
                }
            }

            //Точечное скрещивание между первым и вторым
            int point = rnd.Next(1, W01Length0);
            for (int i = point; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    secondBestW01[i, j] = firstBestW01[i, j];
                    firstBestW01[i, j] = W01[i, j];
                }
            }
            point = rnd.Next(1, W12Length0 );
            for (int i = point; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    secondBestW12[i, j] = firstBestW12[i, j];
                    firstBestW12[i, j] = W12[i, j];
                }
            }
            point = rnd.Next(1, W23Length0);
            for (int i = point; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    secondBestW23[i, j] = firstBestW23[i, j];
                    firstBestW23[i, j] = W23[i, j];
                }
            }

            //Заполнение thirdBestW
            WeightsReader(W01, W12, W23, thirdMob);
            WeightsWriter(W01, W12, W23, 6);
            for (int i = 0; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    thirdBestW01[i, j] = W01[i, j];
                }
            }
            for (int i = 0; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    thirdBestW12[i, j] = W12[i, j];
                }
            }
            for (int i = 0; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    thirdBestW23[i, j] = W23[i, j];
                }
            }

            //Заполнение fourthBestW
            WeightsReader(W01, W12, W23, fourthMob);
            WeightsWriter(W01, W12, W23, 7);
            for (int i = 0; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    fourthBestW01[i, j] = W01[i, j];
                }
            }
            for (int i = 0; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    fourthBestW12[i, j] = W12[i, j];
                }
            }
            for (int i = 0; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    fourthBestW23[i, j] = W23[i, j];
                }
            }

            //Точечное скрещивание между третьим и четвертым
            point = rnd.Next(1, W01Length0);
            for (int i = point; i < W01Length0; i++)
            {
                for (int j = 0; j < W01Length1; j++)
                {
                    fourthBestW01[i, j] = thirdBestW01[i, j];
                    thirdBestW01[i, j] = W01[i, j];
                }
            }
            point = rnd.Next(1, W12Length0 );
            for (int i = point; i < W12Length0 ; i++)
            {
                for (int j = 0; j < W12Length1; j++)
                {
                    fourthBestW12[i, j] = thirdBestW12[i, j];
                    thirdBestW12[i, j] = W12[i, j];
                }
            }
            point = rnd.Next(1, W23Length0);
            for (int i = point; i < W23Length0; i++)
            {
                for (int j = 0; j < W23Length1; j++)
                {
                    fourthBestW23[i, j] = thirdBestW23[i, j];
                    thirdBestW23[i, j] = W23[i, j];
                }
            }

            //Лучшие из прошлого поколения
            WeightsReader(W01, W12, W23, firstMob);
            WeightsWriter(W01, W12, W23, 0);
            WeightsReader(W01, W12, W23, secondMob);
            WeightsWriter(W01, W12, W23, 1);
            WeightsReader(W01, W12, W23, thirdMob);
            WeightsWriter(W01, W12, W23, 2);
            WeightsReader(W01, W12, W23, fourthMob);
            WeightsWriter(W01, W12, W23, 3);

            //Создание нового поколения с потомками
            for (int i = 8; i < mobScore.Length - 3 - 4; i+=4)
            {
                WeightsWriter(firstBestW01, firstBestW12, firstBestW23, i);
                WeightsWriter(secondBestW01, secondBestW12, secondBestW23, i+1);
                WeightsWriter(thirdBestW01, thirdBestW12, thirdBestW23, i+2);
                WeightsWriter(fourthBestW01, fourthBestW12, fourthBestW23, i+3);
            }
        }

        private void Mutation()
        {
            for (int l = 4; l < mobScore.Length; l++)
            {
                WeightsReader(W01, W12, W23, l);

                for (int i = 0; i < W01Length0; i++)
                {
                    for (int j = 0; j < W01Length1; j++)
                    {
                        if (rnd.Next() % 2 == 0)
                        {
                            if (rnd.Next() % 2 == 0)
                            {
                                if (W01[i, j] + 0.7 * mutationRange < 4)
                                {
                                    W01[i, j] += 0.7 * mutationRange;
                                }
                            }
                            else
                            {
                                if (W01[i, j] - 0.7 * mutationRange > -4)
                                {
                                    W01[i, j] -= 0.7 * mutationRange;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < W12Length0 ; i++)
                {
                    for (int j = 0; j < W12Length1; j++)
                    {
                        if (rnd.Next() % 2 == 0)
                        {
                            if (rnd.Next() % 2 == 0)
                            {
                                if (W12[i, j] + 0.7 * mutationRange < 4)
                                {
                                    W12[i, j] += 0.7 * mutationRange;
                                }
                            }
                            else
                            {
                                if (W12[i, j] - 0.7 * mutationRange > -4)
                                {
                                    W12[i, j] -= 0.7 * mutationRange;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < W23Length0; i++)
                {
                    for (int j = 0; j < W23Length1; j++)
                    {
                        if (rnd.Next() % 2 == 0)
                        {
                            if (rnd.Next() % 2 == 0)
                            {
                                if (W23[i, j] + 0.7 * mutationRange < 4)
                                {
                                    W23[i, j] += 0.7 * mutationRange;
                                }
                            }
                            else
                            {
                                if (W23[i, j] - 0.7 * mutationRange > -4)
                                {
                                    W23[i, j] -= 0.7 * mutationRange;
                                }
                            }
                        }
                    }
                }

                WeightsWriter(W01, W12, W23, l);
            }
        }

        private void TestRun_Tick(object sender, EventArgs e)
        {
            label11.Text = "Mob Up Iter: " + mobUpIteration;
            Intersect();
            //raysDistanceFact.CopyTo(L0, 0);
            L0[0] = raysDistanceFact[0];
            L0[1] = raysDistanceFact[1];
            L0[2] = raysDistanceFact[2];
            L0[3] = raysDistanceFact[3];
            L0[4] = raysDistanceFact[4];
            L0[5] = raysDistanceFact[5];
            L0[6] = raysDistanceFact[6];
            L0[7] = raysDistanceFact[7];
            AddRayDef();
            WeightsReader(W01, W12, W23, 0);
            Forward(L0, W01, L1);
            Forward(L1, W12, L2);
            Forward(L2, W23, L3);
            //double maxVal = L3.Max();
            if (L3[0] > 0.5)
            {
                UpMoveDef();
            }
            if (L3[1] > 0.5)
            {
                LeftRotateDef();
            }
            if (L3[2] > 0.5)
            {
                RightRotateDef();
            }
        }

        /*private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            coins[0].Location = new Point(MousePosition.X - coins[0].Width/2, MousePosition.Y - coins[0].Height);

            coinPoints[0] = new Point(coins[0].Location.X, coins[0].Location.Y);
            coinPoints[1] = new Point(coins[0].Location.X + coins[0].Width, coins[0].Location.Y);
            coinPoints[2] = new Point(coins[0].Location.X + coins[0].Width, coins[0].Location.Y + coins[0].Height);
            coinPoints[3] = new Point(coins[0].Location.X, coins[0].Location.Y + coins[0].Height);
        }*/
    }
}
