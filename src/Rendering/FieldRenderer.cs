using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Field;

namespace DesktopFarmIdle.Rendering;

public partial class FieldRenderer : Node2D
{
    private FieldModel? _field;
    private float _windTimer = 0f;

    // Organic diorama agricultural palette
    private static readonly Color ColorSubSoil = new(0.13f, 0.08f, 0.04f, 0.95f);
    private static readonly Color ColorSoilStubble = new(0.32f, 0.22f, 0.13f);
    private static readonly Color ColorStubbleStraw = new(0.68f, 0.54f, 0.24f);
    private static readonly Color ColorSoilPlowedDark = new(0.18f, 0.11f, 0.06f);
    private static readonly Color ColorSoilPlowedRidge = new(0.24f, 0.15f, 0.09f);
    private static readonly Color ColorSoilCultivated = new(0.28f, 0.18f, 0.10f);
    private static readonly Color ColorSoilSeeded = new(0.25f, 0.16f, 0.09f);
    private static readonly Color ColorSproutGreen = new(0.44f, 0.74f, 0.18f);
    private static readonly Color ColorVegGreen = new(0.30f, 0.62f, 0.14f);
    private static readonly Color ColorRipeningWheat = new(0.78f, 0.68f, 0.20f);
    private static readonly Color ColorGoldenWheat = new(0.92f, 0.76f, 0.20f);
    private static readonly Color ColorWheatAmber = new(0.82f, 0.58f, 0.12f);

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

        float startX = 20.0f;
        float groundY = 85.0f; // Organic soil surface line for 120px overlay
        float cellWidth = GameBalanceConfig.FieldCellWidthWorldUnits;
        float cellHeight = 35.0f; // Thin diorama soil band (Y: 85 to 120)
        float totalWidth = _field.CellCount * cellWidth;

        // Subtle sub-soil base at the very bottom (Y: 110 to 120)
        DrawRect(new Rect2(0, groundY + 25.0f, totalWidth + 100, 10), ColorSubSoil);

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
                // Stubble straw tufts (fine, natural)
                for (int s = 0; s < 3; s++)
                {
                    float sx = rect.Position.X + 4 + s * 8;
                    DrawLine(new Vector2(sx, rect.Position.Y + 1), new Vector2(sx + 1, rect.Position.Y - 4), ColorStubbleStraw, 1.2f);
                }
                break;

            case SoilState.Plowed:
                DrawRect(rect, ColorSoilPlowedDark);
                // Deep organic furrow ridges
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 6), new Vector2(rect.End.X, rect.Position.Y + 10), ColorSoilPlowedRidge, 2.5f);
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 18), new Vector2(rect.End.X, rect.Position.Y + 22), ColorSoilPlowedRidge, 2.0f);
                break;

            case SoilState.Cultivated:
                DrawRect(rect, ColorSoilCultivated);
                // Fine smooth seedbed texture
                for (int d = 0; d < 3; d++)
                {
                    float dx = rect.Position.X + 5 + d * 8;
                    DrawCircle(new Vector2(dx, rect.Position.Y + 8), 1.4f, ColorSoilPlowedRidge);
                    DrawCircle(new Vector2(dx + 3, rect.Position.Y + 18), 1.2f, ColorSoilPlowedRidge);
                }
                break;

            case SoilState.Seeded:
                DrawRect(rect, ColorSoilSeeded);
                // Natural dark seed furrows (NO yellow line!)
                DrawLine(new Vector2(rect.Position.X, rect.Position.Y + 6), new Vector2(rect.End.X, rect.Position.Y + 8), ColorSoilPlowedRidge, 1.5f);
                break;

            default: // Germinating and growing stages
                DrawRect(rect, ColorSoilSeeded);
                break;
        }

        // Soft organic soil boundary edge on top of each cell
        DrawLine(new Vector2(rect.Position.X, rect.Position.Y), new Vector2(rect.End.X, rect.Position.Y), ColorSoilPlowedRidge, 1.0f);
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
                    float bx = cx + 4 + i * 8;
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 0.8f, groundY - 5), ColorSproutGreen, 1.2f);
                }
                break;

            case SoilState.GrowthStage1:
                for (int i = 0; i < 3; i++)
                {
                    float bx = cx + 4 + i * 8;
                    DrawLine(new Vector2(bx, groundY), new Vector2(bx + windSway * 1.4f, groundY - 12), ColorVegGreen, 1.5f);
                }
                break;

            case SoilState.GrowthStage2:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 2.2f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 19), ColorVegGreen, 1.8f);
                    DrawCircle(new Vector2(topX, groundY - 19), 1.8f, ColorRipeningWheat);
                }
                break;

            case SoilState.GrowthStage3:
                for (int i = 0; i < 4; i++)
                {
                    float bx = cx + 3 + i * 6;
                    float topX = bx + windSway * 2.8f;
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 26), ColorRipeningWheat, 2.0f);
                    DrawLine(new Vector2(topX, groundY - 26), new Vector2(topX + windSway * 1.5f, groundY - 30), ColorGoldenWheat, 2.4f);
                }
                break;

            case SoilState.ReadyToHarvest:
                for (int i = 0; i < 5; i++)
                {
                    float bx = cx + 2 + i * 5;
                    float topX = bx + windSway * 3.2f;
                    // Golden wheat stalks bending with grain heads
                    DrawLine(new Vector2(bx, groundY), new Vector2(topX, groundY - 28), ColorGoldenWheat, 2.2f);
                    // Ripe golden ear
                    DrawLine(new Vector2(topX, groundY - 28), new Vector2(topX + windSway * 1.8f + 2, groundY - 33), ColorWheatAmber, 3.0f);
                }
                break;
        }
    }
}
