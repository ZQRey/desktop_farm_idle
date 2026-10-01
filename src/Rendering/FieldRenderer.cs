using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Field;

namespace DesktopFarmIdle.Rendering;

public partial class FieldRenderer : Node2D
{
    private FieldModel? _field;
    private float _windTimer = 0f;

    // Palette
    private static readonly Color ColorSoilStubble = new(0.38f, 0.28f, 0.18f);
    private static readonly Color ColorStubbleStraw = new(0.78f, 0.65f, 0.32f);
    private static readonly Color ColorSoilPlowedDark = new(0.20f, 0.14f, 0.08f);
    private static readonly Color ColorSoilPlowedRidge = new(0.28f, 0.19f, 0.11f);
    private static readonly Color ColorSoilCultivated = new(0.32f, 0.22f, 0.13f);
    private static readonly Color ColorSoilSeeded = new(0.30f, 0.21f, 0.12f);
    private static readonly Color ColorSproutGreen = new(0.45f, 0.75f, 0.22f);
    private static readonly Color ColorVegGreen = new(0.30f, 0.65f, 0.18f);
    private static readonly Color ColorRipeningWheat = new(0.75f, 0.70f, 0.25f);
    private static readonly Color ColorGoldenWheat = new(0.92f, 0.76f, 0.24f);
    private static readonly Color ColorWheatAmber = new(0.82f, 0.60f, 0.15f);

    public void BindField(FieldModel field)
    {
        _field = field;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _windTimer += (float)delta * 2.5f;
        // Redraw for swaying crops and progressive work
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_field == null) return;

        float startX = 60.0f;
        float groundY = 150.0f;
        float cellWidth = GameBalanceConfig.FieldCellWidthWorldUnits;
        float cellHeight = 28.0f;

        // Draw sub-layer ground bed
        float totalWidth = _field.CellCount * cellWidth;
        DrawRect(new Rect2(startX - 20, groundY + 8, totalWidth + 40, 25), new Color(0.18f, 0.12f, 0.07f));

        for (int i = 0; i < _field.CellCount; i++)
        {
            var cell = _field.Cells[i];
            float cx = startX + i * cellWidth;
            Rect2 cellRect = new(cx, groundY, cellWidth + 0.5f, cellHeight);

            DrawCellSoil(cell, cellRect);
            DrawCellCrop(cell, cx, groundY);
        }
    }

    private void DrawCellSoil(FieldCell cell, Rect2 rect)
    {
        switch (cell.State)
        {
            case SoilState.Stubble or SoilState.Harvested:
                DrawRect(rect, ColorSoilStubble);
                // Stubble straws
                for (int s = 0; s < 4; s++)
                {
                    float sx = rect.Position.X + 4 + s * 7;
                    DrawLine(new Vector2(sx, rect.Position.Y + 2), new Vector2(sx + 2, rect.Position.Y - 5), ColorStubbleStraw, 1.5f);
                }
                break;

            case SoilState.Plowed:
                DrawRect(rect, ColorSoilPlowedDark);
                // Deep curved furrows
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 4), new Vector2(rect.End.X, rect.Position.Y + 8), ColorSoilPlowedRidge, 3.0f);
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 14), new Vector2(rect.End.X, rect.Position.Y + 18), ColorSoilPlowedRidge, 2.5f);
                break;

            case SoilState.Cultivated:
                DrawRect(rect, ColorSoilCultivated);
                // Fine smooth tilled texture
                for (int d = 0; d < 3; d++)
                {
                    float dx = rect.Position.X + 6 + d * 9;
                    DrawCircle(new Vector2(dx, rect.Position.Y + 6), 1.5f, ColorSoilPlowedRidge);
                }
                break;

            case SoilState.Seeded:
                DrawRect(rect, ColorSoilSeeded);
                // Tiny seed marks
                for (int s = 0; s < 3; s++)
                {
                    float sx = rect.Position.X + 5 + s * 10;
                    DrawCircle(new Vector2(sx, rect.Position.Y + 4), 1.2f, ColorGoldenWheat);
                }
                break;

            default: // Germinating and growing stages
                DrawRect(rect, ColorSoilSeeded);
                break;
        }
    }

    private void DrawCellCrop(FieldCell cell, float cx, float groundY)
    {
        if (cell.State < SoilState.Germinating || cell.State > SoilState.ReadyToHarvest)
            return;

        float windSway = MathF.Sin(_windTimer + cx * 0.05f);

        switch (cell.State)
        {
            case SoilState.Germinating:
                for (int i = 0; i < 3; i++)
                {
                    float bx = cx + 5 + i * 9;
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 1.0f, groundY - 6), ColorSproutGreen, 1.5f);
                }
                break;

            case SoilState.GrowthStage1:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 4 + i * 7;
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 2.0f, groundY - 14), ColorVegGreen, 1.8f);
                }
                break;

            case SoilState.GrowthStage2:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 4 + i * 7;
                    float topX = bx + windSway * 3.5f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 24), ColorVegGreen, 2.0f);
                    DrawCircle(new Vector2(topX, groundY - 24), 2.2f, ColorRipeningWheat);
                }
                break;

            case SoilState.GrowthStage3:
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 4.5f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 32), ColorRipeningWheat, 2.2f);
                    DrawLine(new Vector2(topX, groundY - 32), new Vector2(topX + windSway * 2f, groundY - 38), ColorGoldenWheat, 3.0f);
                }
                break;

            case SoilState.ReadyToHarvest:
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 5.5f;
                    // Golden wheat stalks
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 36), ColorWheatAmber, 2.4f);
                    // Plump golden wheat head with awns
                    DrawLine(new Vector2(topX, groundY - 36), new Vector2(topX + windSway * 3f, groundY - 44), ColorGoldenWheat, 3.8f);
                    DrawCircle(new Vector2(topX + windSway * 3f, groundY - 44), 2.5f, ColorGoldenWheat);
                }
                break;
        }
    }
}
