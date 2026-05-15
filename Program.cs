using Raylib_cs;
using System.Numerics;

class Program
{
    static void Main()
    {
        const int screenWidth = 1280;
        const int screenHeight = 720;

        Raylib.InitWindow(screenWidth, screenHeight, "My Raylib Game");

        Raylib.SetTargetFPS(60);

        Vector2 playerPos = new Vector2(400, 300);

        while (!Raylib.WindowShouldClose())
        {
            // Movement
            if (Raylib.IsKeyDown(KeyboardKey.Right))
                playerPos.X += 5;

            if (Raylib.IsKeyDown(KeyboardKey.Left))
                playerPos.X -= 5;

            if (Raylib.IsKeyDown(KeyboardKey.Up))
                playerPos.Y -= 5;

            if (Raylib.IsKeyDown(KeyboardKey.Down))
                playerPos.Y += 5;

            // Drawing
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.Black);

            Raylib.DrawText("Raylib + C#", 20, 20, 30, Color.White);

            Raylib.DrawCircleV(playerPos, 30, Color.Red);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}