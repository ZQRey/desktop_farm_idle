using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Field;

namespace DesktopFarmIdle.Rendering;

public partial class FieldRenderer : Node2D
{
    private FieldModel? _field;
    private float _windTimer = 0f;

    // Rich rural palette
    private static readonly Color ColorSubSoil = new(0.14f, 0.09f, 0.05f);
    private static readonly Color ColorSoilStubble = new(0.36f, 0.26f, 0.16f);
    private static readonly Color ColorStubbleStraw = new(0.82f, 0.68f, 0.30f);
    private static readonly Color ColorSoilPlowedDark = new(0.18f, 0.12f, 0.07f);
    private static readonly Color ColorSoilPlowedRidge = new(0.26f, 0.18f, 0.10f);
    private static readonly Color ColorSoilCultivated = new(0.30f, 0.20f, 0.12f);
    private static readonly Color ColorSoilSeeded = new(0.28f, 0.19f, 0.11f);
    private static readonly Color ColorSproutGreen = new(0.48f, 0.78f, 0.20f);
    private static readonly Color ColorVegGreen = new(0.32f, 0.68f, 0.16f);
    private static readonly Color ColorRipeningWheat = new(0.80f, 0.72f, 0.22f);
    private static readonly Color ColorGoldenWheat = new(0.96f, 0.80f, 0.22f);
    private static readonly Color ColorWheatAmber = new(0.85f, 0.62f, 0.14f);

    public void BindField(FieldModel field)
    {
        _field = field;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _windTimer += (float)delta * 2.5f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_field == null) return;

        float startX = 40.0f;
        float groundY = 160.0f; // Top surface of the soil
        float cellWidth = GameBalanceConfig.FieldCellWidthWorldUnits;
        float cellHeight = 55.0f; // Deep visible soil bed down to Y=215
        float totalWidth = _field.CellCount * cellWidth;

        // Draw sub-layer base loamy ground bed (Y: 215 to 240)
        DrawRect(new Rect2(0, groundY + cellHeight, totalWidth + 200, 25), ColorSubSoil);

        // Draw individual field cells
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
                // Stubble straw tufts
                for (int s = 0; s < 4; s++)
                {
                    float sx = rect.Position.X + 4 + s * 7;
                    DrawLine(new Vector2(sx, rect.Position.Y + 2), new Vector2(sx + 2, rect.Position.Y - 6), ColorStubbleStraw, 1.8f);
                }
                break;

            case SoilState.Plowed:
                DrawRect(rect, ColorSoilPlowedDark);
                // Deep curved furrows across the cell
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 8), new Vector2(rect.End.X, rect.Position.Y + 14), ColorSoilPlowedRidge, 4.0f);
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 24), new Vector2(rect.End.X, rect.Position.Y + 30), ColorSoilPlowedRidge, 3.5f);
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 40), new Vector2(rect.End.X, rect.Position.Y + 45), ColorSoilPlowedRidge, 3.0f);
                break;

            case SoilState.Cultivated:
                DrawRect(rect, ColorSoilCultivated);
                // Fine smooth tilled seedbed
                for (int d = 0; d < 4; d++)
                {
                    float dx = rect.Position.X + 5 + d * 7;
                    DrawCircle(new Vector2(dx, rect.Position.Y + 12), 2.0f, ColorSoilPlowedRidge);
                    DrawCircle(new Vector2(dx + 3, rect.Position.Y + 28), 1.8f, ColorSoilPlowedRidge);
                }
                break;

            case SoilState.Seeded:
                DrawRect(rect, ColorSoilSeeded);
                // Seed drill rows
                for (int s = 0; s < 3; s++)
                {
                    float sx = rect.Position.X + 5 + s * 10;
                    DrawCircle(new Vector2(sx, rect.Position.Y + 8), 1.5f, ColorGoldenWheat);
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
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 1.2f, groundY - 8), ColorSproutGreen, 1.8f);
                }
                break;

            case SoilState.GrowthStage1:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 4 + i * 7;
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 2.2f, groundY - 18), ColorVegGreen, 2.0f);
                }
                break;

            case SoilState.GrowthStage2:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 4 + i * 7;
                    float topX = bx + windSway * 3.8f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 30), ColorVegGreen, 2.2f);
                    DrawCircle(new Vector2(topX, groundY - 30), 2.5f, ColorRipeningWheat);
                }
                break;

            case SoilState.GrowthStage3:
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 4.8f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 40), ColorRipeningWheat, 2.4f);
                    DrawLine(new Vector2(topX, groundY - 40), new Vector2(topX + windSway * 2.2f, groundY - 46), ColorGoldenWheat, 3.2f);
                }
                break;

            case SoilState.ReadyToHarvest:
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 6.0f;
                    // Golden wheat stalks
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 44), ColorWheatAmber, 2.6f);
                    // Ripe golden ear of wheat with awns
                    DrawLine(new Vector2(topX, groundY - 44), new Vector2(topX + windSway * 3.5f, groundY - 52), ColorGoldenWheat, 4.0f);
                    DrawCircle(new Vector2(topX + windSway * 3.5f, groundY - 52), 3.0f, ColorGoldenWheat);
                }
                break;
        }
    }
}
