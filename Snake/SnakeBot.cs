using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Snake
{
    public class SnakeBot
    {
        private class Node
        {
            // Change this depending on what the desired size is for each element in the grid
            public Node Parent;
            public readonly Point Position;
            public float DistanceToTarget;
            public float Cost;
            public float F
            {
                get
                {
                    if (Math.Abs(DistanceToTarget - (-1)) > 0.00001 && Math.Abs(Cost - (-1)) > 0.000001)
                    {
                        return DistanceToTarget + Cost;
                    }
                    return -1;
                }
            }
            public bool Walkable;

            public Node(Point pos, bool walkable)
            {
                Parent = null;
                Position = pos;
                DistanceToTarget = -1;
                Cost = 1;
                Walkable = walkable;
            }
            public Node(int x, int y, bool walkable) : this(new Point(x, y), walkable)
            {
            }
        }

        private readonly int _width;
        private readonly int _height;
        private readonly Node[,] _grid;


        public SnakeBot(int width, int height)
        {
            _width = width;
            _height = height;
            _grid = new Node[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _grid[x, y] = new Node(x, y, true);
                }
            }
        }
        public List<Program.Direction> GetNextSteps(Program.BlockType[,] gameGrid, List<Point> snakeBody,
            Point snakeHead)
        {
            Point appleLocation = Point.Empty;
            for (int x = 0; x < _width; x++)
            {
                bool shouldBreak = false;
                for (int y = 0; y < _height; y++)
                {
                    if (gameGrid[x, y] == Program.BlockType.Apple)
                    {
                        appleLocation = new Point(x, y);
                        shouldBreak = true;
                        break;
                    }
                }

                if (shouldBreak)
                {
                    break;
                }
            }

            return GetNextSteps(appleLocation, snakeBody, snakeHead);
        }
        public List<Program.Direction> GetNextSteps(Point apple, List<Point> snakeBody,
            Point snakeHead)
        {
            bool doVerticalFirst = false;
            if (snakeBody.Count > 0)
            {
                Point lastPart = snakeBody.Last();
                if (lastPart.X != snakeHead.X)
                {
                    doVerticalFirst = true;
                }
            }
            if (ArePointsClose(apple, snakeHead))
            {
                return GetDirections(snakeHead, apple, doVerticalFirst);
            }
            // Use the A* algorithm
            // target is apple location, location is snake head, "walls" are the body of the snake
            foreach (Point point in snakeBody)
            {
                _grid[point.X, point.Y].Walkable = false;
            }
            Stack<Node> nodes = FindPath(snakeHead, apple);
            if (nodes == null)
            {
                // TODO: if gets stuck, move next to yourself until a path is available or you lost
                // TODO: start a fill strategy, with the side that the head is on,
                return null;
            }
            List<Program.Direction> directions = new List<Program.Direction>();
            Point currentPoint = snakeHead;


            foreach (Node node in nodes)
            {
                directions.AddRange(GetDirections(currentPoint, node.Position, doVerticalFirst));
                currentPoint = node.Position;
            }


            foreach (Point point in snakeBody)
            {
                _grid[point.X, point.Y].Walkable = true;
            }
            return directions;
        }

        private static bool ArePointsClose(Point point1, Point point2)
        {
            int xDiffrence = Math.Abs(point2.X - point1.X);
            int yDiffrence = Math.Abs(point2.Y - point1.Y);
            return xDiffrence < 2 && yDiffrence < 2;
        }

        private List<Program.Direction> GetDirections(Point original, Point target, bool doVerticalFirst)
        {
            int deltaX = target.X - original.X;
            int deltaY = target.Y - original.Y;
            List<Program.Direction> directions = new List<Program.Direction>(2);

            void AddHorizontal()
            {
                if (deltaX > 0)
                {
                    for (int i = 0; i < deltaX; i++)
                    {
                        directions.Add(Program.Direction.Right);
                    }
                }
                else if (deltaX < 0)
                {
                    for (int i = 0; i > deltaX; i--)
                    {
                        directions.Add(Program.Direction.Left);
                    }
                }

            }
            void AddVertical()
            {
                if (deltaY > 0)
                {
                    for (int i = 0; i < deltaY; i++)
                    {
                        directions.Add(Program.Direction.Down);
                    }
                }
                else if (deltaY < 0)
                {
                    for (int i = 0; i > deltaY; i--)
                    {
                        directions.Add(Program.Direction.Up);
                    }
                }
            }

            if (doVerticalFirst)
            {
                AddVertical();
                AddHorizontal();
            }
            else
            {
                AddHorizontal();
                AddVertical();
            }


            return directions;
        }

        private Stack<Node> FindPath(Point startPoint, Point endPoint)
        {
            Node start = new Node(new Point(startPoint.X, startPoint.Y), true);
            Node end = new Node(new Point(endPoint.X, endPoint.Y), true);

            Stack<Node> path = new Stack<Node>();
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            Node current = start;

            // add start node to Open List
            openList.Add(start);

            while (openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position))
            {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                IEnumerable<Node> adjacencies = GetAdjacentNodes(current);
                foreach (Node n in adjacencies)
                {
                    if (closedList.Contains(n) || !n.Walkable)
                    {
                        continue;
                    }
                    if (openList.Contains(n))
                    {
                        continue;
                    }

                    n.Parent = current;
                    n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) + Math.Abs(n.Position.Y - end.Position.Y);
                    n.Cost = 1 + n.Parent.Cost;
                    openList.Add(n);
                    openList = openList.OrderBy(node => node.F).ToList<Node>();
                }
            }

            // construct path, if end was not closed return null
            if (!closedList.Exists(x => x.Position == end.Position))
            {
                return null;
            }

            // if all good, return path
            Node temp = closedList[closedList.IndexOf(current)];
            if (temp == null)
            {
                return null;
            }

            while (temp.Parent != start && temp != null)
            {
                path.Push(temp);
                temp = temp.Parent;
            }
            return path;
        }

        private IEnumerable<Node> GetAdjacentNodes(Node n)
        {
            List<Node> temp = new List<Node>();

            int row = n.Position.Y;
            int col = n.Position.X;

            if (row + 1 < _height)
            {
                temp.Add(_grid[col, row + 1]);
            }
            if (row - 1 >= 0)
            {
                temp.Add(_grid[col, row - 1]);
            }
            if (col - 1 >= 0)
            {
                temp.Add(_grid[col - 1, row]);
            }
            if (col + 1 < _width)
            {
                temp.Add(_grid[col + 1, row]);
            }

            return temp;
        }
    }
}
