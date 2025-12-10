using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace SnakePoison;

class Program
{
    static void Main()
    {
        var game = new Game();
        game.Run();
    }
}

class Game
{
    private const int WINDOW_WIDTH = 800;
    private const int WINDOW_HEIGHT = 700;
    private const int GRID_SIZE = 25;
    private const int GRID_WIDTH = 28;
    private const int GRID_HEIGHT = 22;
    private const int GRID_OFFSET_X = 50;
    private const int GRID_OFFSET_Y = 80;

    private GameState _state = GameState.Menu;
    private readonly World _world;
    private readonly Snake _snake;
    private float _moveTimer;
    private float _moveInterval = 0.12f;

    private float _screenShake;
    private readonly List<Particle> _particles = new();
    private string _statusMessage = "";
    private float _statusMessageTimer;

    // Sound effects
    private Sound _eatSound;
    private Sound _poisonSound;
    private Sound _deathSound;
    private Sound _evolveSound;

    public Game()
    {
        _world = new World(GRID_WIDTH, GRID_HEIGHT);
        _snake = new Snake(GRID_WIDTH / 2, GRID_HEIGHT / 2);
    }

    public void Run()
    {
        Raylib.InitWindow(WINDOW_WIDTH, WINDOW_HEIGHT, "Snake-Poison");
        Raylib.SetTargetFPS(60);
        
        // Initialize audio
        Raylib.InitAudioDevice();
        _eatSound = GenerateAndLoadSound(800, 0.1f, SoundType.Eat);
        _poisonSound = GenerateAndLoadSound(150, 0.25f, SoundType.Poison);
        _deathSound = GenerateAndLoadSound(200, 0.4f, SoundType.Death);
        _evolveSound = GenerateAndLoadSound(500, 0.35f, SoundType.Evolve);

        while (!Raylib.WindowShouldClose())
        {
            Update(Raylib.GetFrameTime());
            Draw();
        }

        // Cleanup audio
        Raylib.UnloadSound(_eatSound);
        Raylib.UnloadSound(_poisonSound);
        Raylib.UnloadSound(_deathSound);
        Raylib.UnloadSound(_evolveSound);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }

    private static Sound GenerateAndLoadSound(float freq, float duration, SoundType type)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        var audioData = new float[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float sample = 0;
            
            switch (type)
            {
                case SoundType.Eat:
                    float f1 = freq * (1 - t * 0.5f);
                    sample = (float)Math.Sin(2 * Math.PI * f1 * i / sampleRate) * (1 - t);
                    break;
                case SoundType.Poison:
                    sample = (float)(Math.Sin(2 * Math.PI * freq * i / sampleRate) *
                                    Math.Sin(2 * Math.PI * 30 * i / sampleRate)) * (1 - t);
                    break;
                case SoundType.Death:
                    float f2 = freq * (1 - t * 0.7f);
                    sample = (float)(Math.Sin(2 * Math.PI * f2 * i / sampleRate) +
                                    (Random.Shared.NextDouble() - 0.5) * 0.3) * (float)Math.Exp(-t * 4);
                    break;
                case SoundType.Evolve:
                    float f3 = freq * (1 + t);
                    sample = (float)Math.Sin(2 * Math.PI * f3 * i / sampleRate) * 
                            (float)Math.Sin(t * Math.PI);
                    break;
            }
            audioData[i] = sample * 0.4f;
        }
        
        // Write to temp WAV file and load
        string tempFile = Path.Combine(Path.GetTempPath(), $"snake_snd_{Guid.NewGuid()}.wav");
        WriteWavFile(tempFile, audioData, sampleRate);
        var sound = Raylib.LoadSound(tempFile);
        try { File.Delete(tempFile); } catch { }
        return sound;
    }
    
    private static void WriteWavFile(string path, float[] samples, int sampleRate)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        
        int byteRate = sampleRate * 2; // 16-bit mono
        int dataSize = samples.Length * 2;
        
        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);
        
        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write((short)1); // mono
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)2); // block align
        bw.Write((short)16); // bits per sample
        
        // data chunk
        bw.Write("data"u8);
        bw.Write(dataSize);
        
        foreach (var s in samples)
        {
            short val = (short)(Math.Clamp(s, -1f, 1f) * 32767);
            bw.Write(val);
        }
    }
    
    enum SoundType { Eat, Poison, Death, Evolve }

    private void Update(float dt)
    {
        UpdateParticles(dt);
        _screenShake *= 0.9f;
        if (_statusMessageTimer > 0) _statusMessageTimer -= dt;

        switch (_state)
        {
            case GameState.Menu: UpdateMenu(); break;
            case GameState.Playing: UpdatePlaying(dt); break;
            case GameState.Paused: UpdatePaused(); break;
            case GameState.GameOver: UpdateGameOver(); break;
        }
    }

    private void UpdateMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            StartGame();
    }

    private void StartGame()
    {
        _snake.Reset(GRID_WIDTH / 2, GRID_HEIGHT / 2);
        _world.Reset();
        _world.SpawnFood(_snake);
        _state = GameState.Playing;
        _moveInterval = 0.12f;
    }

    private void UpdatePlaying(float dt)
    {
        HandleInput();
        ProcessPoisonEffects(dt);

        _moveTimer += dt;
        if (_moveTimer >= _moveInterval)
        {
            _moveTimer = 0;
            _snake.Move();

            if (_snake.CollidesWithWall(_world))
            {
                _snake.Die("Hit the wall");
                _state = GameState.GameOver;
                _screenShake = 15f;
                SpawnDeathParticles();
                Raylib.PlaySound(_deathSound);
                return;
            }

            if (_snake.CollidesWithSelf())
            {
                _snake.Die("Bit yourself");
                _state = GameState.GameOver;
                _screenShake = 15f;
                SpawnDeathParticles();
                Raylib.PlaySound(_deathSound);
                return;
            }

            var food = _world.GetFoodAt(_snake.HeadX, _snake.HeadY);
            if (food != null)
            {
                _snake.Grow(food.GrowthValue);
                SpawnEatParticles(food);
                Raylib.PlaySound(_eatSound);

                if (food.IsPoisoned && food.Poison != null)
                {
                    _snake.ApplyPoison(food.Poison);
                    ShowStatus($"Poisoned: {food.Poison.Name}");
                    _screenShake = 5f;
                    Raylib.PlaySound(_poisonSound);
                }

                _world.RemoveFood(food);
                _world.SpawnFood(_snake);
            }
        }
    }

    private void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
            _snake.SetDirection(0, -1);
        else if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
            _snake.SetDirection(0, 1);
        else if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A))
            _snake.SetDirection(-1, 0);
        else if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D))
            _snake.SetDirection(1, 0);

        if (Raylib.IsKeyPressed(KeyboardKey.Space)) _state = GameState.Paused;
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) _state = GameState.Menu;
    }

    private void ProcessPoisonEffects(float dt)
    {
        for (int i = _snake.ActivePoisons.Count - 1; i >= 0; i--)
        {
            var poison = _snake.ActivePoisons[i];

            switch (poison.Type)
            {
                case PoisonType.Impulsive:
                    if (Random.Shared.NextDouble() < 0.1 * poison.Intensity)
                        _moveInterval = 0.06f + (float)Random.Shared.NextDouble() * 0.1f;
                    break;

                case PoisonType.Evolving:
                    poison.EvolutionTimer += dt;
                    if (poison.EvolutionTimer >= 3f)
                    {
                        poison.EvolutionTimer = 0;
                        poison.EvolutionStage++;
                        ShowStatus($"Evolution Stage {poison.EvolutionStage}/3");

                        if (poison.EvolutionStage >= 3)
                        {
                            var ability = _snake.UnlockAbility();
                            ShowStatus($"Awakened: {ability}");
                            _snake.RemovePoison(poison);
                            SpawnAbilityParticles();
                            Raylib.PlaySound(_evolveSound);
                            continue;
                        }
                    }
                    break;
            }

            poison.RemainingTime -= dt;
            if (poison.RemainingTime <= 0)
            {
                _snake.RemovePoison(poison);
                _moveInterval = 0.12f;
            }
        }
    }

    private void UpdatePaused()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space)) _state = GameState.Playing;
    }

    private void UpdateGameOver()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            StartGame();
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) _state = GameState.Menu;
    }

    private void Draw()
    {
        Raylib.BeginDrawing();

        int shakeX = (int)(Random.Shared.NextDouble() * _screenShake - _screenShake / 2);
        int shakeY = (int)(Random.Shared.NextDouble() * _screenShake - _screenShake / 2);

        Raylib.ClearBackground(GetBackgroundColor());

        switch (_state)
        {
            case GameState.Menu: DrawMenu(); break;
            case GameState.Playing:
            case GameState.Paused:
                DrawGame(shakeX, shakeY);
                if (_state == GameState.Paused) DrawPauseOverlay();
                break;
            case GameState.GameOver:
                DrawGame(shakeX, shakeY);
                DrawGameOver();
                break;
        }

        DrawParticles();
        Raylib.EndDrawing();
    }

    private Color GetBackgroundColor()
    {
        if (_snake.HasPoison(PoisonType.Perception))
        {
            float t = (float)Math.Sin(Raylib.GetTime() * 2) * 0.5f + 0.5f;
            return new Color((byte)(250 - t * 20), (byte)(245 - t * 30), (byte)(245 + t * 10), (byte)255);
        }
        return new Color((byte)250, (byte)250, (byte)245, (byte)255);
    }

    private void DrawMenu()
    {
        DrawCenteredText("SNAKE-POISON", 120, 60, Color.DarkGray);
        DrawCenteredText("Life is a snake. Choices are poison.", 190, 20, Color.Gray);

        int y = 280;
        DrawCenteredText("[O] Normal Food     +1 Growth", y, 20, Color.DarkGray); y += 30;
        DrawCenteredText("[<>] Suspicious     +2 Growth  May be poisoned", y, 20, new Color((byte)180, (byte)100, (byte)50, (byte)255)); y += 30;
        DrawCenteredText("[*] Valuable        +3 Growth  High risk", y, 20, new Color((byte)200, (byte)150, (byte)50, (byte)255)); y += 45;

        DrawCenteredText("POISON TYPES:", y, 18, Color.DarkGray); y += 25;
        DrawCenteredText("Perception - Distorted vision", y, 16, Color.Gray); y += 22;
        DrawCenteredText("Impulsive - Speed changes", y, 16, Color.Gray); y += 22;
        DrawCenteredText("Memory - Blurred trail", y, 16, Color.Gray); y += 22;
        DrawCenteredText("Evolving - 3-stage mutation -> Awakening", y, 16, Color.Gray); y += 40;

        DrawCenteredText("Arrow Keys / WASD to move", y, 18, Color.Gray); y += 22;
        DrawCenteredText("SPACE to pause    ESC for menu", y, 18, Color.Gray); y += 45;

        float alpha = (float)(Math.Sin(Raylib.GetTime() * 3) * 0.3 + 0.7);
        DrawCenteredText("Press SPACE or ENTER to start", y, 24, 
            new Color((byte)80, (byte)80, (byte)80, (byte)(alpha * 255)));
    }

    private void DrawGame(int shakeX, int shakeY)
    {
        int offsetX = GRID_OFFSET_X + shakeX;
        int offsetY = GRID_OFFSET_Y + shakeY;

        Raylib.DrawRectangle(offsetX - 5, offsetY - 5,
            GRID_WIDTH * GRID_SIZE + 10, GRID_HEIGHT * GRID_SIZE + 10,
            new Color((byte)245, (byte)245, (byte)240, (byte)255));
        Raylib.DrawRectangleLines(offsetX - 5, offsetY - 5,
            GRID_WIDTH * GRID_SIZE + 10, GRID_HEIGHT * GRID_SIZE + 10, Color.DarkGray);

        foreach (var food in _world.Foods)
            DrawFood(food, offsetX, offsetY);

        DrawSnake(offsetX, offsetY);
        DrawUI();
    }

    private void DrawFood(FoodItem food, int offsetX, int offsetY)
    {
        int x = offsetX + food.X * GRID_SIZE + GRID_SIZE / 2;
        int y = offsetY + food.Y * GRID_SIZE + GRID_SIZE / 2;

        if (_snake.HasPoison(PoisonType.Perception))
        {
            if (Random.Shared.NextDouble() < 0.2) return;
            x += Random.Shared.Next(-3, 4);
            y += Random.Shared.Next(-3, 4);
        }

        float pulse = (float)Math.Sin(Raylib.GetTime() * 4) * 2;
        int radius = (int)(GRID_SIZE / 2 - 4 + pulse);

        switch (food.Type)
        {
            case FoodType.Normal:
                Raylib.DrawCircle(x, y, radius, new Color((byte)80, (byte)80, (byte)80, (byte)255));
                break;
            case FoodType.Suspicious:
                DrawDiamond(x, y, radius, new Color((byte)180, (byte)100, (byte)50, (byte)255));
                break;
            case FoodType.Valuable:
                DrawStar(x, y, radius, new Color((byte)200, (byte)170, (byte)50, (byte)255));
                break;
        }
    }

    private void DrawSnake(int offsetX, int offsetY)
    {
        bool hasMemory = _snake.HasPoison(PoisonType.Memory);
        bool hasPerception = _snake.HasPoison(PoisonType.Perception);

        for (int i = _snake.Body.Count - 1; i >= 0; i--)
        {
            var seg = _snake.Body[i];
            int x = offsetX + seg.X * GRID_SIZE + GRID_SIZE / 2;
            int y = offsetY + seg.Y * GRID_SIZE + GRID_SIZE / 2;

            float t = (float)i / _snake.Body.Count;
            int radius = (int)(GRID_SIZE / 2 - 2 - t * 3);

            byte alpha = 255;
            if (hasMemory && i > _snake.Body.Count / 2)
                alpha = (byte)Math.Max(50, 150 - (i - _snake.Body.Count / 2) * 20);

            var color = i == 0
                ? new Color((byte)40, (byte)40, (byte)40, alpha)
                : new Color((byte)80, (byte)80, (byte)80, alpha);

            if (hasPerception && i > 0)
            {
                x += Random.Shared.Next(-2, 3);
                y += Random.Shared.Next(-2, 3);
            }

            if (i == 0)
            {
                Raylib.DrawCircle(x, y, radius + 2, color);
                int eyeOff = 4;
                int ex1 = x - eyeOff * (_snake.DirectionY != 0 ? 1 : 0) + _snake.DirectionX * 3;
                int ey1 = y - eyeOff * (_snake.DirectionX != 0 ? 1 : 0) + _snake.DirectionY * 3;
                int ex2 = x + eyeOff * (_snake.DirectionY != 0 ? 1 : 0) + _snake.DirectionX * 3;
                int ey2 = y + eyeOff * (_snake.DirectionX != 0 ? 1 : 0) + _snake.DirectionY * 3;
                Raylib.DrawCircle(ex1, ey1, 3, Color.White);
                Raylib.DrawCircle(ex2, ey2, 3, Color.White);
                Raylib.DrawCircle(ex1, ey1, 1, Color.Black);
                Raylib.DrawCircle(ex2, ey2, 1, Color.Black);
            }
            else
            {
                Raylib.DrawCircle(x, y, radius, color);
            }
        }
    }

    private void DrawUI()
    {
        Raylib.DrawText($"Length: {_snake.Length}", 50, 30, 22, Color.DarkGray);
        Raylib.DrawText($"Moves: {_snake.Trajectory.TotalMoves}", 200, 30, 22, Color.DarkGray);
        Raylib.DrawText($"Poison: {_snake.Trajectory.PoisonsTaken}", 380, 30, 22, Color.DarkGray);
        Raylib.DrawText($"Time: {_snake.Trajectory.LifeSpan.TotalSeconds:F1}s", WINDOW_WIDTH - 140, 30, 22, Color.DarkGray);

        int py = GRID_OFFSET_Y + GRID_HEIGHT * GRID_SIZE + 20;
        if (_snake.ActivePoisons.Count > 0)
        {
            Raylib.DrawText("Status:", 50, py, 18, Color.DarkGray);
            int x = 130;
            foreach (var p in _snake.ActivePoisons)
            {
                var (name, col) = p.Type switch
                {
                    PoisonType.Perception => ("Perception", new Color((byte)100, (byte)50, (byte)150, (byte)255)),
                    PoisonType.Impulsive => ("Impulsive", new Color((byte)200, (byte)50, (byte)50, (byte)255)),
                    PoisonType.Memory => ("Memory", new Color((byte)50, (byte)100, (byte)150, (byte)255)),
                    PoisonType.Evolving => ($"Evolving {p.EvolutionStage}/3", new Color((byte)50, (byte)150, (byte)50, (byte)255)),
                    _ => ("Unknown", Color.Gray)
                };
                Raylib.DrawText($"[{name}]", x, py, 18, col);
                x += Raylib.MeasureText($"[{name}]", 18) + 12;
            }
        }

        if (_snake.Abilities.Count > 0)
            Raylib.DrawText($"Awakened: {string.Join(", ", _snake.Abilities)}", 50, py + 25, 18,
                new Color((byte)150, (byte)120, (byte)50, (byte)255));

        if (_statusMessageTimer > 0)
        {
            byte a = (byte)Math.Min(255, _statusMessageTimer * 255);
            DrawCenteredText(_statusMessage, GRID_OFFSET_Y + GRID_HEIGHT * GRID_SIZE / 2, 26,
                new Color((byte)80, (byte)80, (byte)80, a));
        }
    }

    private void DrawPauseOverlay()
    {
        Raylib.DrawRectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT, new Color((byte)250, (byte)250, (byte)245, (byte)200));
        DrawCenteredText("PAUSED", WINDOW_HEIGHT / 2 - 30, 48, Color.DarkGray);
        DrawCenteredText("Press SPACE to continue", WINDOW_HEIGHT / 2 + 30, 22, Color.Gray);
    }

    private void DrawGameOver()
    {
        Raylib.DrawRectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT, new Color((byte)250, (byte)250, (byte)245, (byte)220));
        DrawCenteredText("GAME OVER", WINDOW_HEIGHT / 2 - 100, 48, Color.DarkGray);
        DrawCenteredText(_snake.Trajectory.DeathCause ?? "", WINDOW_HEIGHT / 2 - 45, 22, Color.Gray);
        DrawCenteredText($"Survived: {_snake.Trajectory.LifeSpan.TotalSeconds:F1}s   " +
                        $"Max Length: {_snake.Trajectory.MaxLength}   " +
                        $"Poisons: {_snake.Trajectory.PoisonsTaken}",
                        WINDOW_HEIGHT / 2 + 10, 18, Color.DarkGray);
        DrawCenteredText("\"You didn't lose. You just made a choice.\"", WINDOW_HEIGHT / 2 + 55, 18, Color.Gray);

        float alpha = (float)(Math.Sin(Raylib.GetTime() * 3) * 0.3 + 0.7);
        DrawCenteredText("Press SPACE to restart", WINDOW_HEIGHT / 2 + 110, 22,
            new Color((byte)80, (byte)80, (byte)80, (byte)(alpha * 255)));
    }

    private void DrawCenteredText(string text, int y, int fontSize, Color color)
    {
        int w = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, (WINDOW_WIDTH - w) / 2, y, fontSize, color);
    }

    private void DrawDiamond(int cx, int cy, int size, Color color)
    {
        Raylib.DrawTriangle(new Vector2(cx, cy - size), new Vector2(cx - size, cy), new Vector2(cx, cy + size), color);
        Raylib.DrawTriangle(new Vector2(cx, cy - size), new Vector2(cx, cy + size), new Vector2(cx + size, cy), color);
    }

    private void DrawStar(int cx, int cy, int size, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            float a1 = (float)(i * Math.PI * 2 / 5 - Math.PI / 2);
            float a2 = (float)((i + 2) * Math.PI * 2 / 5 - Math.PI / 2);
            Raylib.DrawLine(cx + (int)(Math.Cos(a1) * size), cy + (int)(Math.Sin(a1) * size),
                           cx + (int)(Math.Cos(a2) * size), cy + (int)(Math.Sin(a2) * size), color);
        }
        Raylib.DrawCircle(cx, cy, size / 3, color);
    }

    private void ShowStatus(string msg) { _statusMessage = msg; _statusMessageTimer = 2f; }

    private void SpawnEatParticles(FoodItem food)
    {
        int x = GRID_OFFSET_X + food.X * GRID_SIZE + GRID_SIZE / 2;
        int y = GRID_OFFSET_Y + food.Y * GRID_SIZE + GRID_SIZE / 2;
        var col = food.Type switch {
            FoodType.Suspicious => new Color((byte)180, (byte)100, (byte)50, (byte)255),
            FoodType.Valuable => new Color((byte)200, (byte)170, (byte)50, (byte)255),
            _ => Color.DarkGray
        };
        for (int i = 0; i < 8; i++)
            _particles.Add(new Particle { X = x, Y = y,
                VX = (float)(Random.Shared.NextDouble() - 0.5) * 100,
                VY = (float)(Random.Shared.NextDouble() - 0.5) * 100,
                Life = 0.5f, Color = col, Size = 4 });
    }

    private void SpawnDeathParticles()
    {
        foreach (var s in _snake.Body)
        {
            int x = GRID_OFFSET_X + s.X * GRID_SIZE + GRID_SIZE / 2;
            int y = GRID_OFFSET_Y + s.Y * GRID_SIZE + GRID_SIZE / 2;
            for (int i = 0; i < 3; i++)
                _particles.Add(new Particle { X = x, Y = y,
                    VX = (float)(Random.Shared.NextDouble() - 0.5) * 150,
                    VY = (float)(Random.Shared.NextDouble() - 0.5) * 150,
                    Life = 1f, Color = Color.DarkGray, Size = 5 });
        }
    }

    private void SpawnAbilityParticles()
    {
        int x = GRID_OFFSET_X + _snake.HeadX * GRID_SIZE + GRID_SIZE / 2;
        int y = GRID_OFFSET_Y + _snake.HeadY * GRID_SIZE + GRID_SIZE / 2;
        for (int i = 0; i < 20; i++)
        {
            float a = (float)(i * Math.PI * 2 / 20);
            _particles.Add(new Particle { X = x, Y = y,
                VX = (float)Math.Cos(a) * 120, VY = (float)Math.Sin(a) * 120,
                Life = 1f, Color = new Color((byte)200, (byte)170, (byte)50, (byte)255), Size = 6 });
        }
    }

    private void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.X += p.VX * dt; p.Y += p.VY * dt; p.VY += 200 * dt; p.Life -= dt;
            if (p.Life <= 0) _particles.RemoveAt(i);
        }
    }

    private void DrawParticles()
    {
        foreach (var p in _particles)
        {
            byte a = (byte)(p.Life * 255);
            Raylib.DrawCircle((int)p.X, (int)p.Y, p.Size * p.Life, new Color(p.Color.R, p.Color.G, p.Color.B, a));
        }
    }
}

class Particle { public float X, Y, VX, VY, Life, Size; public Color Color; }
enum GameState { Menu, Playing, Paused, GameOver }

class Snake
{
    public List<(int X, int Y)> Body { get; } = new();
    public int DirectionX { get; private set; } = 1;
    public int DirectionY { get; private set; }
    public List<ActivePoison> ActivePoisons { get; } = new();
    public SnakeTrajectory Trajectory { get; private set; } = new();
    public List<string> Abilities { get; } = new();
    public int HeadX => Body[0].X;
    public int HeadY => Body[0].Y;
    public int Length => Body.Count;

    public Snake(int x, int y) => Reset(x, y);

    public void Reset(int x, int y)
    {
        Body.Clear();
        Body.Add((x, y)); Body.Add((x - 1, y)); Body.Add((x - 2, y));
        DirectionX = 1; DirectionY = 0;
        ActivePoisons.Clear(); Abilities.Clear();
        Trajectory = new SnakeTrajectory();
    }

    public void SetDirection(int dx, int dy) { if (dx + DirectionX != 0 || dy + DirectionY != 0) { DirectionX = dx; DirectionY = dy; } }
    public void Move() { Body.Insert(0, (HeadX + DirectionX, HeadY + DirectionY)); Body.RemoveAt(Body.Count - 1); Trajectory.RecordMove(); }
    public void Grow(int n) { for (int i = 0; i < n; i++) Body.Add(Body[^1]); Trajectory.RecordGrowth(Length); }
    public bool CollidesWithWall(World w) => HeadX < 0 || HeadX >= w.Width || HeadY < 0 || HeadY >= w.Height;
    public bool CollidesWithSelf() { for (int i = 1; i < Body.Count; i++) if (Body[i] == Body[0]) return true; return false; }
    public void ApplyPoison(PoisonEffect p) { ActivePoisons.Add(new ActivePoison { Type = p.Type, Intensity = p.Intensity, RemainingTime = p.Duration }); Trajectory.RecordPoison(); }
    public void RemovePoison(ActivePoison p) => ActivePoisons.Remove(p);
    public void Die(string cause) => Trajectory.RecordDeath(cause);
    public string UnlockAbility() { var a = new[] { "TrueSight", "Dash", "Rebirth", "Phase" }[Random.Shared.Next(4)]; if (!Abilities.Contains(a)) Abilities.Add(a); return a; }
    public bool HasPoison(PoisonType t) => ActivePoisons.Any(p => p.Type == t);
}

class SnakeTrajectory
{
    public DateTime BirthTime { get; } = DateTime.Now;
    public DateTime? DeathTime { get; private set; }
    public string? DeathCause { get; private set; }
    public int TotalMoves { get; private set; }
    public int MaxLength { get; private set; } = 3;
    public int PoisonsTaken { get; private set; }
    public TimeSpan LifeSpan => (DeathTime ?? DateTime.Now) - BirthTime;
    public void RecordMove() => TotalMoves++;
    public void RecordGrowth(int len) { if (len > MaxLength) MaxLength = len; }
    public void RecordPoison() => PoisonsTaken++;
    public void RecordDeath(string c) { DeathTime = DateTime.Now; DeathCause = c; }
}

class World
{
    public int Width { get; }
    public int Height { get; }
    public List<FoodItem> Foods { get; } = new();
    public World(int w, int h) { Width = w; Height = h; }
    public void Reset() => Foods.Clear();
    public FoodItem? GetFoodAt(int x, int y) => Foods.FirstOrDefault(f => f.X == x && f.Y == y);
    public void RemoveFood(FoodItem f) => Foods.Remove(f);
    public void SpawnFood(Snake snake)
    {
        var empty = new List<(int, int)>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (!snake.Body.Any(b => b.X == x && b.Y == y) && !Foods.Any(f => f.X == x && f.Y == y))
                    empty.Add((x, y));
        if (empty.Count == 0) return;
        var pos = empty[Random.Shared.Next(empty.Count)];
        var roll = Random.Shared.NextDouble();
        FoodItem food;
        if (roll < 0.55) food = new FoodItem(pos.Item1, pos.Item2, FoodType.Normal, 1);
        else if (roll < 0.80) food = new FoodItem(pos.Item1, pos.Item2, FoodType.Suspicious, 2) { IsPoisoned = true, Poison = PoisonFactory.CreateRandom() };
        else food = new FoodItem(pos.Item1, pos.Item2, FoodType.Valuable, 3) { IsPoisoned = Random.Shared.NextDouble() > 0.5, Poison = Random.Shared.NextDouble() > 0.5 ? PoisonFactory.CreateEvolving() : null };
        Foods.Add(food);
    }
}

class FoodItem
{
    public int X { get; }
    public int Y { get; }
    public FoodType Type { get; }
    public int GrowthValue { get; }
    public bool IsPoisoned { get; set; }
    public PoisonEffect? Poison { get; set; }
    public FoodItem(int x, int y, FoodType t, int g) { X = x; Y = y; Type = t; GrowthValue = g; }
}

enum FoodType { Normal, Suspicious, Valuable }
enum PoisonType { Perception, Impulsive, Memory, Evolving }

class PoisonEffect
{
    public PoisonType Type { get; init; }
    public string Name { get; init; } = "";
    public float Duration { get; init; }
    public float Intensity { get; init; }
}

class ActivePoison
{
    public PoisonType Type { get; set; }
    public float Intensity { get; set; }
    public float RemainingTime { get; set; }
    public int EvolutionStage { get; set; }
    public float EvolutionTimer { get; set; }
}

static class PoisonFactory
{
    public static PoisonEffect CreateRandom()
    {
        var t = (PoisonType)Random.Shared.Next(4);
        return t switch
        {
            PoisonType.Perception => new PoisonEffect { Type = t, Name = "Perception", Duration = 10, Intensity = 0.5f },
            PoisonType.Impulsive => new PoisonEffect { Type = t, Name = "Impulsive", Duration = 8, Intensity = 0.5f },
            PoisonType.Memory => new PoisonEffect { Type = t, Name = "Memory", Duration = 15, Intensity = 0.5f },
            _ => CreateEvolving()
        };
    }
    public static PoisonEffect CreateEvolving() => new() { Type = PoisonType.Evolving, Name = "Evolving", Duration = 30, Intensity = 0.3f };
}
