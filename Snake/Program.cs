using Snake.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace Snake
{
    public static class Program
    {
        private static int _gameWidth = 20;
        private static int _gameHeight = 20;
        private static float _speed = 7;

        public enum BlockType
        {
            Empty,
            Snake,
            Apple
        }

        public enum GameStatus
        {
            Won,
            Lost,
            Playing
        }

        public enum Direction
        {
            //left and right are opposite 4 bits and so are up and down
            Down = 0x0,
            Up = 0x3 - 0x0,
            Left = 0x1,
            Right = 0x3 - 0x1,
        }

        private static BlockType[,] _gameGrid;
        private static Direction? _previousDirection;
        private static Direction? _currentDirection;

        private static readonly char CornerChar = Resources.CornerChar[0];
        private static readonly char HorizontalChar = Resources.HorizontalChar[0];
        private static readonly char VerticalChar = Resources.VerticalChar[0];

        private static readonly char AppleChar = Resources.AppleChar[0];
        private static readonly char SnakeChar = Resources.SnakeChar[0];
        private static readonly char EmptyBlockChar = Resources.EmptyBlockChar[0];


        /// <summary>
        /// true if you either just lost or won
        /// </summary>
        private static bool _gameOver;

        private static readonly Random Random = new Random();
        private static Thread _gameThread;

        private static SnakeBot _snakeBot;
        private static Snake _snake;
        private static bool _botActive;
        private static bool _gameActive = true;


        private enum PauseActions
        {
            Null,
            Resume,
            ChangeSpeed,
            ChangeSize,
            EnableBot,
            DisableBot
        }

        private static void Main()
        {
            Console.CursorVisible = false;
            GetInputKey("Enter [B] for Bot and[P] for Player:", input =>
            {
                bool playerMode = input.Key == ConsoleKey.P;
                bool botMode = !playerMode && input.Key == ConsoleKey.B;
                if (botMode)
                {
                    Console.WriteLine("Bot Active");
                    _botActive = true;
                }
                else if (playerMode)
                {
                    Console.WriteLine("Bot Disabled");
                    _botActive = false;
                }
                else
                {
                    return "Invalid input" + Environment.NewLine;
                }

                return null;
            });

            GetInput($"Enter speed (default {_speed}):", speedInput =>
            {
                if (speedInput == "")
                {
                    return null;
                }
                bool isSpeedDecimal = float.TryParse(speedInput, out float speed);
                if (!isSpeedDecimal)
                {
                    return "Input must be a number";
                }

                if (speed <= 0)
                {
                    return "Input must be positive";
                }

                if (speed > 1000)
                {
                    speed = 1000;
                }
                _speed = speed;
                Console.WriteLine("Speed is {0}", speed);
                return null;

            });
            GetInput($"Enter game size (default {_gameWidth}x{_gameHeight}):", gameSizeInput =>
            {
                if (gameSizeInput == "")
                {
                    return null;
                }
                gameSizeInput = gameSizeInput.ToUpper();
                string[] parsedGameSizeInput = gameSizeInput.Split('X');
                if (parsedGameSizeInput.Length == 2)
                {
                    string widthInputStr = parsedGameSizeInput[0];
                    string heightInputStr = parsedGameSizeInput[1];
                    bool validWidthInput = Int32.TryParse(widthInputStr, out int widthInput);
                    if (!validWidthInput)
                    {
                        return $"Invalid width input ({widthInput})";
                    }
                    if (widthInput <= 0)
                    {
                        return $"Width ({widthInput}) must be positive";
                    }
                    bool validHeightInput = Int32.TryParse(heightInputStr, out int heightInput);
                    if (!validHeightInput)
                    {
                        return $"Invalid height input ({heightInput})";
                    }
                    if (heightInput <= 0)
                    {
                        return $"Height ({heightInput}) must be positive";
                    }
                    _gameWidth = widthInput;
                    _gameHeight = heightInput;
                    return null;

                }
                else
                {
                    return "Invalid format";
                }
            });

            _snakeBot = new SnakeBot(_gameWidth, _gameHeight);
            _snake = new Snake(Random);
            _gameGrid = new BlockType[_gameWidth, _gameHeight];
            Console.Clear();
            Initialize();
            StartKeyReading();
        }

        private static void StartKeyReading()
        {
            while (true)
            {

                ConsoleKeyInfo keyInput = Console.ReadKey(true);
                if (_gameOver)
                {
                    Thread.Sleep(400);
                    if (keyInput.Key == ConsoleKey.P)
                    {
                        _gameOver = false;
                        _snake.Body.Clear();
                        _currentDirection = null;
                        _previousDirection = null;
                        _snake.Grow = false;
                        for (int x = 0; x < _gameWidth; x++)
                        {
                            for (int y = 0; y < _gameHeight; y++)
                            {
                                _gameGrid[x, y] = BlockType.Empty;
                            }
                        }
                        Console.Clear();
                        Console.SetCursorPosition(0, 0);
                        Initialize();
                    }
                    else
                    {
                        return;
                    }
                }
                switch (keyInput.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        {
                            _currentDirection = Direction.Up;
                            break;
                        }
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        {
                            _currentDirection = Direction.Down;
                            break;
                        }
                    case ConsoleKey.A:
                    case ConsoleKey.LeftArrow:
                        {
                            _currentDirection = Direction.Left;
                            break;
                        }
                    case ConsoleKey.D:
                    case ConsoleKey.RightArrow:
                        {
                            _currentDirection = Direction.Right;
                            break;
                        }
                    case ConsoleKey.P:
                        {
                            _gameActive = false;
                            Console.SetCursorPosition(0, _gameHeight + 2);
                            string currentDisplayedText = Resources.PauseMessage;
                            string messageToDisplay = _botActive
                                ? Resources.PauseOptionsDisableBotMessage
                                : Resources.PauseOptionsEnableBotMessage;
                            messageToDisplay = messageToDisplay.PadRight(currentDisplayedText.Length);
                            switch (GetInputKey<PauseActions>(messageToDisplay, input =>
                            {
                                switch (input.Key)
                                {
                                    case ConsoleKey.R:
                                        _gameActive = true;
                                        return PauseActions.Resume;
                                    case ConsoleKey.S:
                                        return PauseActions.ChangeSpeed;
                                    case ConsoleKey.D:
                                        {
                                            return PauseActions.ChangeSize;
                                        }
                                    default:
                                        if (_botActive && input.Key == ConsoleKey.P)
                                        {
                                            return PauseActions.DisableBot;
                                        }
                                        else if (input.Key == ConsoleKey.B)
                                        {
                                            return PauseActions.EnableBot;
                                        }
                                        break;
                                }

                                return null;
                            }))
                            {
                                case PauseActions.Resume:
                                    {
                                        Console.Clear();
                                        Console.SetCursorPosition(0, 0);
                                        Console.WriteLine();
                                        Console.WriteLine(Resources.PauseMessage.PadRight(messageToDisplay.Length));
                                        _gameActive = true;
                                        break;
                                    }

                            }


                            break;
                        }

                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputRequest"></param>
        /// <param name="responesToInput">returns null to go again, otherwise print response</param>
        private static void GetInput(string inputRequest, Func<string, string> responesToInput)
        {
            Console.WriteLine(inputRequest);
            while (true)
            {
                string input = Console.ReadLine();
                string response = responesToInput(input);
                if (response == null)
                {
                    break;
                }
                Console.WriteLine(response);
            }

        }

        private static void GetInputKey(string inputRequest, Func<ConsoleKeyInfo, string> responesToInput)
        {
            Console.WriteLine(inputRequest);
            while (true)
            {
                ConsoleKeyInfo input = Console.ReadKey(true);
                string response = responesToInput(input);
                if (response == null)
                {
                    break;
                }
                Console.Write(response);
            }
        }
        private static T GetInputKey<T>(string inputRequest, Func<ConsoleKeyInfo, dynamic> responesToInput, T nullReturnValue = default)
        {
            Console.WriteLine(inputRequest);
            while (true)
            {
                ConsoleKeyInfo input = Console.ReadKey(true);
                dynamic response = responesToInput(input);
                if (response == null)
                {
                    return nullReturnValue;
                }

                if (response is string)
                {
                    Console.Write(response);
                }
                else if (response is T)
                {
                    return response;
                }
                Console.Write(response);
            }

        }
        private static void Initialize()
        {
            Console.Write(CornerChar + new string(HorizontalChar, _gameWidth) + CornerChar);
            Console.SetCursorPosition(0, 1 + _gameHeight);
            Console.WriteLine(CornerChar + new string(HorizontalChar, _gameWidth) + CornerChar);
            Console.WriteLine(Resources.PauseMessage);
            _snake.Head = new Point(_gameWidth / 2, _gameHeight / 2);
            _gameGrid[_snake.Head.X, _snake.Head.Y] = BlockType.Snake;
            _snake.PlaceRandomApple(_gameGrid, _gameWidth, _gameHeight);
            _gameThread = new Thread(GameLoop);
            _gameThread.Start();
        }

        private static void GameLoop()
        {
            while (true)
            {
                while (_gameActive)
                {
                    if (_botActive)
                    {
                        List<Direction> nextSteps = _snakeBot.GetNextSteps(_gameGrid, _snake.Body, _snake.Head);
                        if (nextSteps == null)
                        {
                            DoGameOver(GameStatus.Lost);
                            return;
                        }
                        foreach (Direction direction in nextSteps)
                        {
                            if (!_botActive && !_gameActive)
                            {
                                break;
                            }

                            GameStatus gameStatus = _snake.UpdateSnake(direction, ref _previousDirection, _gameGrid, _gameWidth, _gameHeight);
                            if (gameStatus != GameStatus.Playing)
                            {
                                DoGameOver(gameStatus);
                                return;
                            }

                            DrawGameGrid();
                            Thread.Sleep((int)(500 / _speed));
                        }
                    }
                    else
                    {
                        GameStatus gameStatus = _snake.UpdateSnake(_currentDirection, ref _previousDirection, _gameGrid, _gameWidth, _gameHeight);
                        if (gameStatus != GameStatus.Playing)
                        {
                            DoGameOver(gameStatus);
                            return;
                        }
                        DrawGameGrid();
                        Thread.Sleep((int)(500 / _speed));
                    }
                }
                Thread.Sleep(80);
            }
        }

        private static void DoGameOver(GameStatus gameStatus)
        {
            Console.SetCursorPosition(0, _gameHeight + 2);
            int pauseMessageLength = Resources.PauseMessage.Length;
            string messageToDisplay = gameStatus == GameStatus.Won ? Resources.WonMessage : Resources.LostMessage;
            int spacesToWrite = pauseMessageLength - messageToDisplay.Length;
            if (spacesToWrite > 0)
            {
                messageToDisplay += new string(' ', spacesToWrite);
            }
            Console.WriteLine(messageToDisplay);
            Console.WriteLine(Resources.GameOverMessage);
            _gameOver = true;
        }


        private static void DrawGameGrid()
        {
            string output = "";
            for (int y = 0; y < _gameHeight; y++)
            {
                output += VerticalChar;
                for (int x = 0; x < _gameWidth; x++)
                {
                    BlockType block = _gameGrid[x, y];
                    switch (block)
                    {
                        case BlockType.Apple:
                            {
                                output += AppleChar;
                                break;
                            }
                        case BlockType.Snake:
                            {
                                output += SnakeChar;
                                break;
                            }
                        case BlockType.Empty:
                            {
                                output += EmptyBlockChar;
                                break;
                            }
                    }
                }
                output += VerticalChar + Environment.NewLine;
            }

            Console.SetCursorPosition(0, 1);
            Console.Write(output);
        }




    }
}
