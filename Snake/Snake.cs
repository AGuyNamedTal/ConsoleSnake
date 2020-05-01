using System;
using System.Collections.Generic;
using System.Drawing;

namespace Snake
{
    public class Snake
    {
        public Point Head;
        public List<Point> Body = new List<Point>();
        private static readonly Func<Point, Point>[] DirectionTransofrmation = new Func<Point, Point>[4];
        public bool Grow;
        public Random Random;


        static Snake()
        {
            DirectionTransofrmation[(int)Program.Direction.Down] = point => new Point(point.X, point.Y + 1);
            DirectionTransofrmation[(int)Program.Direction.Up] = point => new Point(point.X, point.Y - 1);
            DirectionTransofrmation[(int)Program.Direction.Left] = point => new Point(point.X - 1, point.Y);
            DirectionTransofrmation[(int)Program.Direction.Right] = point => new Point(point.X + 1, point.Y);
        }


        public Snake(Random random)
        {
            Random = random;
        }


        public Program.GameStatus UpdateSnake(Program.Direction? currentDirection, ref Program.Direction? previousDirection, Program.BlockType[,] gameGrid, int gameWidth, int gameHeight)
        {
            if (currentDirection == null)
            {
                return Program.GameStatus.Playing;
            }
            Point snakeHeadOldPosition = Head;
            Head = DirectionTransofrmation[(int)currentDirection](Head);
            if (previousDirection != null && Body.Count > 0)
            {
                Program.Direction oppistieDirection = (Program.Direction)(3 - currentDirection);
                if (previousDirection == oppistieDirection && gameGrid[snakeHeadOldPosition.X, snakeHeadOldPosition.Y] == Program.BlockType.Snake)
                {
                    currentDirection = previousDirection;
                    Head = DirectionTransofrmation[(int)previousDirection](snakeHeadOldPosition);
                }
            }

            // Border check
            if (Head.X == -1 || Head.X == gameWidth || Head.Y == -1 ||
                Head.Y == gameHeight)
            {
                return Program.GameStatus.Lost;
            }
            // Check if the snake ate itself
            if (gameGrid[Head.X, Head.Y] == Program.BlockType.Snake)
            {
                return Program.GameStatus.Lost;
            }
            if (Grow)
            {
                Body.Insert(0, Body.Count > 0 ? Body[0] : snakeHeadOldPosition);
                gameGrid[Body[0].X, Body[0].Y] = Program.BlockType.Snake;
                for (int i = 1; i < Body.Count; i++)
                {
                    Body[i] = i + 1 == Body.Count ? snakeHeadOldPosition : Body[i + 1];
                    gameGrid[Body[i].X, Body[i].Y] = Program.BlockType.Snake;
                }
                Grow = false;
            }
            else if (Body.Count > 0)
            {
                gameGrid[Body[0].X, Body[0].Y] = Program.BlockType.Empty;
                for (int i = 0; i < Body.Count; i++)
                {
                    Body[i] = i + 1 == Body.Count ? snakeHeadOldPosition : Body[i + 1];
                    gameGrid[Body[i].X, Body[i].Y] = Program.BlockType.Snake;
                }
            }
            else
            {
                gameGrid[snakeHeadOldPosition.X, snakeHeadOldPosition.Y] = Program.BlockType.Empty;
            }

            if (gameGrid[Head.X, Head.Y] == Program.BlockType.Apple)
            {
                Grow = true;
                if (PlaceRandomApple(gameGrid, gameWidth, gameHeight))
                {
                    gameGrid[Head.X, Head.Y] = Program.BlockType.Snake;
                    return Program.GameStatus.Won;
                }
            }


            gameGrid[Head.X, Head.Y] = Program.BlockType.Snake;
            previousDirection = (Program.Direction)currentDirection;
            return Program.GameStatus.Playing;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if there are no more places for the apple to be places, false otherwise</returns>
        public bool PlaceRandomApple(Program.BlockType[,] gameGrid, int gameWidth, int gameHeight)
        {
            List<int> availableY = new List<int>(gameHeight);
            for (int y = 0; y < gameHeight; y++)
            {
                bool fullLine = true;
                for (int x = 0; x < gameWidth; x++)
                {
                    if (gameGrid[x, y] != Program.BlockType.Snake)
                    {
                        fullLine = false;
                        break;
                    }
                }
                if (!fullLine)
                {
                    availableY.Add(y);
                }
            }
            if (availableY.Count == 0)
            {
                return true;
            }
            int appleY = availableY[Random.Next(0, availableY.Count)];
            List<int> availableX = new List<int>(gameWidth);
            for (int x = 0; x < gameWidth; x++)
            {
                if (gameGrid[x, appleY] != Program.BlockType.Snake)
                {
                    availableX.Add(x);
                }
            }
            //availableX.Count will always be > 0
            int appleX = availableX[Random.Next(0, availableX.Count)];
            gameGrid[appleX, appleY] = Program.BlockType.Apple;
            return false;
        }



    }
}
