using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace TiPPGK6
{
    public partial class Form1 : Form
    {
        private readonly Graphics graphics;
        private Bitmap bmp = null;

        private int mapWidth = 60;
        private int mapHeight = 60;

        private int tileSize = 10;
        private int shortSide = 4;
        private int longSide = 7;

        private int[,] map;
        private bool[,] roads;

        private Point player;
        private Point destination;

        Heap heap;
        List<Node> tree;

        private Timer timer;

        public Form1()
        {
            InitializeComponent();

            bmp = new Bitmap(600, 600);
            graphics = Graphics.FromImage(bmp);
            pictureBox1.Image = bmp;

            map = new int[mapWidth, mapHeight];
            roads = new bool[mapWidth, mapHeight];

            player.X = -100;
            player.Y = -100;

            destination.X = -100;
            destination.Y = -100;

            heap = new Heap();
            tree = new List<Node>();

            label1.Text = "Smooth: " + trackBar1.Value.ToString();

            timer = new Timer();
            timer.Interval = 33;
            timer.Tick += OnTick;
            timer.Start();

            Invalidate();
        }

        private void OnTick(object sender, EventArgs e)
        {
            graphics.Clear(Color.White);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                DrawMap(g);
                DrawRoads(g);

                g.FillEllipse(Brushes.Aqua, player.X, player.Y, tileSize, tileSize);
                g.FillEllipse(Brushes.Goldenrod, destination.X, destination.Y, tileSize - 1, tileSize - 1);

                if (tree.Count() > 0)
                {
                    Node current = tree[tree.Count() - 1];
                    while (current.GetParent() != null)
                    {
                        current = current.GetParent();

                        if (current.GetParent() != null)
                            g.FillEllipse(Brushes.Black, current.GetCoords().Item1 * tileSize + 3, current.GetCoords().Item2 * tileSize + 3, 4, 4);
                    }
                }

                Refresh();
                Invalidate();
            }
        }

        void GenerateMap()
        {
            Random random = new Random();

            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                    map[x, y] = random.Next(0, 200);

            SmoothTerrain(trackBar1.Value);
        }

        void GenerateRoad()
        {
            Random random = new Random();

            int[,] r = new int[mapWidth, mapHeight];
            roads = new bool[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                    r[x, y] = random.Next(6);

            SmoothRoads(1, r);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (r[x, y] == 0 || (map[x, y] >= 50 && map[x, y] < 75))
                    {
                        roads[x, y] = false;
                    }
                    else if (r[x, y] == 1 && !(map[x, y] > 50 && map[x, y] < 75))
                    {
                        if (((x - 1) > 0 && r[x - 1, y] == 0) && ((x + 1) < mapWidth && r[x + 1, y] == 0) && ((y - 1) > 0 && r[x, y - 1] == 0) && ((y + 1) < mapHeight && r[x, y + 1] == 0))
                            roads[x, y] = false;
                        else
                            roads[x, y] = true;
                    }
                }
            }
        }

        void SmoothTerrain(int passes)
        {
            int[,] tmpMap;

            while (passes > 0)
            {
                passes--;

                tmpMap = new int[mapWidth, mapHeight];

                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        int adjacentSections = 0;
                        int sectionsTotal = 0;

                        if ((x - 1) > 0)
                        {
                            sectionsTotal += map[x - 1, y];
                            adjacentSections++;

                            if ((y - 1) > 0)
                            {
                                sectionsTotal += map[x - 1, y - 1];
                                adjacentSections++;
                            }

                            if ((y + 1) < mapHeight)
                            {
                                sectionsTotal += map[x - 1, y + 1];
                                adjacentSections++;
                            }
                        }

                        if ((x + 1) < mapWidth)
                        {
                            sectionsTotal += map[x + 1, y];
                            adjacentSections++;

                            if ((y - 1) > 0)
                            {
                                sectionsTotal += map[x + 1, y - 1];
                                adjacentSections++;
                            }

                            if ((y + 1) < mapHeight)
                            {
                                sectionsTotal += map[x + 1, y + 1];
                                adjacentSections++;
                            }
                        }

                        if ((y - 1) > 0)
                        {
                            sectionsTotal += map[x, y - 1];
                            adjacentSections++;
                        }

                        if ((y + 1) < mapHeight)
                        {
                            sectionsTotal += map[x, y + 1];
                            adjacentSections++;
                        }

                        tmpMap[x, y] = (map[x, y] + (sectionsTotal / adjacentSections)) / 2;
                    }
                }

                for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                        map[x, y] = tmpMap[x, y];
            }
        }

        void SmoothRoads(int passes, int[,] r)
        {
            int[,] tmpRoads;

            while (passes > 0)
            {
                passes--;

                tmpRoads = new int[mapWidth, mapHeight];

                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        int adjacentSections = 0;
                        int sectionsTotal = 0;

                        if ((x - 1) > 0)
                        {
                            sectionsTotal += r[x - 1, y];
                            adjacentSections++;

                            if ((y - 1) > 0)
                            {
                                sectionsTotal += r[x - 1, y - 1];
                                adjacentSections++;
                            }

                            if ((y + 1) < mapHeight)
                            {
                                sectionsTotal += r[x - 1, y + 1];
                                adjacentSections++;
                            }
                        }

                        if ((x + 1) < mapWidth)
                        {
                            sectionsTotal += r[x + 1, y];
                            adjacentSections++;

                            if ((y - 1) > 0)
                            {
                                sectionsTotal += r[x + 1, y - 1];
                                adjacentSections++;
                            }

                            if ((y + 1) < mapHeight)
                            {
                                sectionsTotal += r[x + 1, y + 1];
                                adjacentSections++;
                            }
                        }

                        if ((y - 1) > 0)
                        {
                            sectionsTotal += r[x, y - 1];
                            adjacentSections++;
                        }

                        if ((y + 1) < mapHeight)
                        {
                            sectionsTotal += r[x, y + 1];
                            adjacentSections++;
                        }

                        tmpRoads[x, y] = (r[x, y] + (sectionsTotal / adjacentSections)) / 4;
                    }
                }

                for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                        r[x, y] = tmpRoads[x, y];
            }
        }

        void DrawMap(Graphics g)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    //Swamp
                    if (map[x, y] < 25)
                        g.FillRectangle(Brushes.DarkOliveGreen, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Rough
                    else if (map[x, y] >= 25 && map[x, y] < 50)
                        g.FillRectangle(Brushes.BurlyWood, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Water
                    else if (map[x, y] >= 50 && map[x, y] < 75)
                        g.FillRectangle(Brushes.Blue, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Grass
                    else if (map[x, y] >= 75 && map[x, y] < 100)
                        g.FillRectangle(Brushes.Green, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Dirt
                    else if (map[x, y] >= 100 && map[x, y] < 125)
                        g.FillRectangle(Brushes.SaddleBrown, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Sand
                    else if (map[x, y] >= 125 && map[x, y] < 150)
                        g.FillRectangle(Brushes.Yellow, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Lava
                    else if (map[x, y] >= 150 && map[x, y] < 175)
                        g.FillRectangle(Brushes.Red, x * tileSize, y * tileSize, tileSize, tileSize);

                    //Snow
                    else if (map[x, y] >= 175 && map[x, y] < 200)
                        g.FillRectangle(Brushes.WhiteSmoke, x * tileSize, y * tileSize, tileSize, tileSize);
                }
            }
        }

        void DrawRoads(Graphics g)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (roads[x, y])
                    {
                        if ((x - 1) >= 0 && roads[x - 1, y])
                            g.FillRectangle(Brushes.Gray, x * tileSize, y * tileSize + (longSide - shortSide), longSide, shortSide);
                        if ((x + 1) < mapWidth && roads[x + 1, y])
                            g.FillRectangle(Brushes.Gray, x * tileSize + (longSide - shortSide), y * tileSize + (longSide - shortSide), longSide, shortSide);
                        if ((y - 1) >= 0 && roads[x, y - 1])
                            g.FillRectangle(Brushes.Gray, x * tileSize + (longSide - shortSide), y * tileSize, shortSide, longSide);
                        if ((y + 1) < mapHeight && roads[x, y + 1])
                            g.FillRectangle(Brushes.Gray, x * tileSize + (longSide - shortSide), y * tileSize + (longSide - shortSide), shortSide, longSide);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GenerateMap();
            GenerateRoad();

            Random random = new Random();
            int x;
            int y;

            do
            {
                x = random.Next(60);
                y = random.Next(60);
            }
            while ((map[x, y] >= 50 && map[x, y] < 75) || !roads[x, y]);

            player.X = x * tileSize;
            player.Y = y * tileSize;

            Debug.WriteLine("PLAYER: " + x + " " + y);

            destination.X = -100;
            destination.Y = -100;

            tree.Clear();
            heap.Clear();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            label1.Text = "Smooth: " + trackBar1.Value.ToString();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if ((player.X != -100 && player.Y != -100) && e.Button == MouseButtons.Left)
            {
                int x = e.X / 10;
                int y = e.Y / 10;

                if (!(map[x, y] >= 50 && map[x, y] < 75))
                {
                    destination.X = x * tileSize;
                    destination.Y = y * tileSize;

                    heap.Clear();
                    tree.Clear();

                    FindPath();
                }
            }
        }
       
        float GetTileValue(int x, int y)
        {
            float value = -1;

            //Swamp
            if (map[x, y] < 25)
            {
                value = 175;

                if (roads[x, y])
                    value *= 0.75f;
            }

            //Rough
            else if (map[x, y] >= 25 && map[x, y] < 50)
            {
                value = 125;

                if (roads[x, y])
                    value *= 0.75f;
            }

            //Water
            else if (map[x, y] >= 50 && map[x, y] < 75)
                value = int.MaxValue;

            //Grass
            else if (map[x, y] >= 75 && map[x, y] < 100)
            {
                value = 100;

                if (roads[x, y])
                    value *= 0.65f;
            }

            //Dirt
            else if (map[x, y] >= 100 && map[x, y] < 125)
            {
                value = 100;

                if (roads[x, y])
                    value *= 0.65f;
            }

            //Sand
            else if (map[x, y] >= 125 && map[x, y] < 150)
            {
                value = 150;

                if (roads[x, y])
                    value *= 0.75f;
            }

            //Lava
            else if (map[x, y] >= 150 && map[x, y] < 175)
            {
                value = 175;

                if (roads[x, y])
                    value *= 0.75f;
            }

            //Snow
            else if (map[x, y] >= 175 && map[x, y] < 200)
            {
                value = 150;

                if (roads[x, y])
                    value *= 0.65f;
            }

            //Debug.WriteLine("value: " + value + "\n");

            return value;
        }

        void FindPath()
        {
            bool[,] visited = new bool[mapWidth, mapHeight];
            tree = new List<Node>();

            Node root = new Node();
            root.SetCoords(player.X / tileSize, player.Y / tileSize);

            heap.Insert(root);

            while (!heap.Empty())
            {
                tree.Add(heap.GetMinNode());
                heap.ExtractMin();
                visited[tree[tree.Count() - 1].GetCoords().Item1, tree[tree.Count() - 1].GetCoords().Item2] = true;

                if (tree[tree.Count() - 1].GetCoords().Item1 == destination.X / 10 && tree[tree.Count() - 1].GetCoords().Item2 == destination.Y / 10)
                    break;

                Node up = new Node();
                Node right = new Node();
                Node down = new Node();
                Node left = new Node();

                Node upLeft = new Node();
                Node upRight = new Node();
                Node downLeft = new Node();
                Node downRight = new Node();

                if (tree[tree.Count() - 1].GetCoords().Item1 - 1 >= 0 && !visited[tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2] && !(map[tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2] >= 50 && map[tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2] < 75))
                {
                    left = new Node(1 + GetCurrentPathValue(tree[tree.Count() - 1]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2) + GetTileValue(tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2), tree[tree.Count - 1]);
                    left.SetCoords(tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2);
                    heap.Insert(left);
                    visited[tree[tree.Count() - 1].GetCoords().Item1 - 1, tree[tree.Count() - 1].GetCoords().Item2] = true;
                }

                if (left.GetValue() == 0)
                    left.SetValue(int.MaxValue);
                
                tree.Add(left);

                if (tree[tree.Count() - 2].GetCoords().Item1 + 1 < mapWidth && !visited[tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2] && !(map[tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2] >= 50 && map[tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2] < 75))
                {
                    right = new Node(1 + GetCurrentPathValue(tree[tree.Count() - 2]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2) + GetTileValue(tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2), tree[tree.Count - 2]);
                    right.SetCoords(tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2);
                    heap.Insert(right);
                    visited[tree[tree.Count() - 2].GetCoords().Item1 + 1, tree[tree.Count() - 2].GetCoords().Item2] = true;
                }

                if (right.GetValue() == 0)
                    right.SetValue(int.MaxValue);

                tree.Add(right);

                if (tree[tree.Count() - 3].GetCoords().Item2 - 1 >= 0 && !visited[tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1] && !(map[tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1] >= 50 && map[tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1] < 75))
                {
                    up = new Node(1 + GetCurrentPathValue(tree[tree.Count() - 3]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1) + GetTileValue(tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1), tree[tree.Count - 3]);
                    up.SetCoords(tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1);
                    heap.Insert(up);
                    visited[tree[tree.Count() - 3].GetCoords().Item1, tree[tree.Count() - 3].GetCoords().Item2 - 1] = true;
                }

                if (up.GetValue() == 0)
                    up.SetValue(int.MaxValue);

                tree.Add(up);

                if (tree[tree.Count() - 4].GetCoords().Item2 + 1 < mapHeight && !visited[tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1] && !(map[tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1] >= 50 && map[tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1] < 75))
                {
                    down = new Node(1 + GetCurrentPathValue(tree[tree.Count() - 4]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1) + GetTileValue(tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1), tree[tree.Count - 4]);
                    down.SetCoords(tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1);
                    heap.Insert(down);
                    visited[tree[tree.Count() - 4].GetCoords().Item1, tree[tree.Count() - 4].GetCoords().Item2 + 1] = true;
                }

                if (down.GetValue() == 0)
                    down.SetValue(int.MaxValue);

                tree.Add(down);

                if ((tree[tree.Count() - 5].GetCoords().Item1 - 1 >= 0 && tree[tree.Count() - 5].GetCoords().Item2 - 1 >= 0) && !visited[tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1] && !(map[tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1] >= 50 && map[tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1] < 75))
                {
                    upLeft = new Node((float)Math.Sqrt(2) + GetCurrentPathValue(tree[tree.Count() - 5]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1) + GetTileValue(tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1), tree[tree.Count - 5]);
                    upLeft.SetCoords(tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1);
                    heap.Insert(upLeft);
                    visited[tree[tree.Count() - 5].GetCoords().Item1 - 1, tree[tree.Count() - 5].GetCoords().Item2 - 1] = true;
                }

                if (upLeft.GetValue() == 0)
                    upLeft.SetValue(int.MaxValue);

                tree.Add(upLeft);

                if ((tree[tree.Count() - 6].GetCoords().Item1 + 1 < mapWidth && tree[tree.Count() - 6].GetCoords().Item2 - 1 >= 0) && !visited[tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1] && !(map[tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1] >= 50 && map[tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1] < 75))
                {
                    upRight = new Node((float)Math.Sqrt(2) + GetCurrentPathValue(tree[tree.Count() - 6]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1) + GetTileValue(tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1), tree[tree.Count - 6]);
                    upRight.SetCoords(tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1);
                    heap.Insert(upRight);
                    visited[tree[tree.Count() - 6].GetCoords().Item1 + 1, tree[tree.Count() - 6].GetCoords().Item2 - 1] = true;
                }

                if (upRight.GetValue() == 0)
                    upRight.SetValue(int.MaxValue);

                tree.Add(upRight);

                if ((tree[tree.Count() - 7].GetCoords().Item1 - 1 >= 0 && tree[tree.Count() - 7].GetCoords().Item2 + 1 < mapHeight) && !visited[tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1] && !(map[tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1] >= 50 && map[tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1] < 75))
                {
                    downLeft = new Node((float)Math.Sqrt(2) + GetCurrentPathValue(tree[tree.Count() - 7]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1) + GetTileValue(tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1), tree[tree.Count - 7]);
                    downLeft.SetCoords(tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1);
                    heap.Insert(downLeft);
                    visited[tree[tree.Count() - 7].GetCoords().Item1 - 1, tree[tree.Count() - 7].GetCoords().Item2 + 1] = true;
                }

                if (downLeft.GetValue() == 0)
                    downLeft.SetValue(int.MaxValue);

                tree.Add(downLeft);

                if ((tree[tree.Count() - 8].GetCoords().Item1 + 1 < mapWidth && tree[tree.Count() - 8].GetCoords().Item2 + 1 < mapHeight) && !visited[tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1] && !(map[tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1] >= 50 && map[tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1] < 75))
                {
                    downRight = new Node((float)Math.Sqrt(2) + GetCurrentPathValue(tree[tree.Count() - 8]) + GetFuturePathValue(destination.X / 10, destination.Y / 10, tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1) + GetTileValue(tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1), tree[tree.Count - 8]);
                    downRight.SetCoords(tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1);
                    heap.Insert(downRight);
                    visited[tree[tree.Count() - 8].GetCoords().Item1 + 1, tree[tree.Count() - 8].GetCoords().Item2 + 1] = true;
                }

                if (downRight.GetValue() == 0)
                    downRight.SetValue(int.MaxValue);

                tree.Add(downRight);
            }

            /*Node current = tree[tree.Count() - 1];
            int i = 0;
            while (current.GetParent() != null)
            {
                Debug.WriteLine(i + "\tx: " + current.GetCoords().Item1 + "\ty: " + current.GetCoords().Item2);
                current = current.GetParent();
                i++;
            }*/
        }

        float GetCurrentPathValue(Node node)
        {
            float steps = 0;

            while (node != null)
            {
                steps += node.GetValue();
                steps++;
                node = node.GetParent();
            }

            //Debug.WriteLine(steps);

            return steps;
        }

        float GetFuturePathValue(int x1, int y1, int x2, int y2)
        {
            double value = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            //Debug.WriteLine("(" + x1 + "," + y1 + ") -> (" + x2 + "," + y2 + ") = " + value);
            return (float)value;
        }

    }

    class Node
    {
        float value;
        int x = -1, y = -1;
        Node parent;
        //Node first;
        //Node second;
        //Node third;
        //Node fourth;

        public Node(float value = 0, Node parent = null)
        {
            this.value = value;
            this.parent = parent;
            //this.first = first;
            //this.second = second;
            //this.third = third;
            //this.fourth = fourth;
        }

        public void SetValue(float value) { this.value = value; }
        public float GetValue() { return value; }
        //public void SetFirst(Node first) { this.first = first; }
        //public Node GetFirst() { return first; }
        //public void SetSecond(Node second) { this.second = second; }
        //public Node GetSecond() { return second; }
        //public void SetThird(Node third) { this.third = third; }
        //public Node GetThird() { return third; }
        //public void SetFourth(Node fourth) { this.fourth = fourth; }
        //public Node GetFourth() { return fourth; }
        public void SetParent(Node parent) { this.parent = parent; }
        public Node GetParent() { return parent; }
        public void SetCoords(int x, int y) { this.x = x; this.y = y; }
        public Tuple<int, int> GetCoords() { return Tuple.Create(x, y); }
    }

    class Heap
    {
        private List<Node> nodes;

        public Heap()
        {
            nodes = new List<Node>();
        }

        ~Heap()
        {
            nodes.Clear(); ;
        }

        public void Insert(Node node)
        {
            nodes.Add(node);
            Update(nodes.IndexOf(nodes.Last()));
        }

        public Node GetMinNode()
        {
            if (!Empty())
                return nodes.ElementAt(0);
            return null;
            //throw new System.ArgumentOutOfRangeException("Empty");
        }

        public void ExtractMin()
        {
            if (Empty())
                return;

            nodes[0] = nodes.Last();
            nodes.Remove(nodes.Last());

            if (!Empty())
                Update(0);
        }

        void Update(int index)
        {
            if (index < nodes.Count() && index >= 0)
            {
                bool l = false, r = false;

                if (index * 2 + 1 < nodes.Count())
                    if (nodes[index].GetValue() > nodes[index * 2 + 1].GetValue())
                        l = true;

                if (index * 2 + 2 < nodes.Count())
                    if (nodes[index].GetValue() > nodes[index * 2 + 2].GetValue())
                        r = true;

                if (l && r)
                {
                    if (nodes[index * 2 + 1].GetValue() < nodes[index * 2 + 2].GetValue())
                    {
                        Swap(nodes, index, index * 2 + 1);
                        Update(index * 2 + 1);
                    }
                    else
                    {
                        Swap(nodes, index, index * 2 + 2);
                        Update(index * 2 + 2);
                    }
                }
                else if (l)
                {
                    Swap(nodes, index, index * 2 + 1);
                    Update(index * 2 + 1);
                }
                else if (r)
                {
                    Swap(nodes, index, index * 2 + 2);
                    Update(index * 2 + 2);
                }

                if ((index - 1) / 2 >= 0 && index != 0)
                {
                    if (nodes[index].GetValue() < nodes[(index - 1) / 2].GetValue())
                    {
                        Swap(nodes, index, (index - 1) / 2);
                        Update((index - 1) / 2);
                    }
                }
            }
            else
                Debug.WriteLine(index + " Out of Range");
        }

        public bool Empty()
        {
            if (nodes.Count == 0)
                return true;
            return false;
        }

        public void Print()
        {
            foreach (Node node in nodes)
            {
                Debug.WriteLine(node.GetValue());
            }
        }
        static void Swap(IList<Node> list, int indexA, int indexB)
        {
            Node tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        public void Clear()
        {
            nodes.Clear();
        }
    }
}
