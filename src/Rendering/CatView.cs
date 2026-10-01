using System;
using Godot;

namespace DesktopFarmIdle.Rendering;

public partial class CatView : Node2D
{
    private Sprite2D? _catSprite;
    private float _walkTimer = 0f;
    private float _actionTimer = 5.0f;
    private int _catState = 0; // 0 = walk, 1 = sit/watch, 2 = sleep
    private float _catX = 1400.0f;
    private float _catDir = -1.0f;

    public override void _Ready()
    {
        var catTex = GD.Load<Texture2D>("res://assets/environment/animals/cat.png");
        _catSprite = new Sprite2D
        {
            Texture = catTex,
            Scale = new Vector2(0.08f, 0.08f),
            Position = new Vector2(0, 0)
        };
        AddChild(_catSprite);
        Position = new Vector2(_catX, 156.0f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _actionTimer -= dt;

        if (_actionTimer <= 0f)
        {
            // Switch state
            var rnd = new Random();
            _catState = rnd.Next(3);
            _actionTimer = 4.0f + (float)rnd.NextDouble() * 8.0f;
            if (_catState == 0)
            {
                _catDir = rnd.Next(2) == 0 ? 1.0f : -1.0f;
            }
        }

        if (_catState == 0) // Walk
        {
            _walkTimer += dt * 8.0f;
            _catX += _catDir * 18.0f * dt;
            _catX = Math.Clamp(_catX, 300.0f, 1700.0f);

            Position = new Vector2(_catX, 156.0f + MathF.Sin(_walkTimer) * 1.0f);
            Scale = new Vector2(_catDir > 0 ? -1.0f : 1.0f, 1.0f);
        }
        else if (_catState == 1) // Watch tractor
        {
            Position = new Vector2(_catX, 156.0f);
        }
        else // Resting
        {
            Position = new Vector2(_catX, 157.0f);
        }
    }
}
