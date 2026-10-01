using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Vehicles;

namespace DesktopFarmIdle.Rendering;

public partial class VehicleView : Node2D
{
    private VehicleModel? _vehicle;

    private Sprite2D? _bodySprite;
    private Sprite2D? _rearWheelSprite;
    private Sprite2D? _frontWheelSprite;
    private Sprite2D? _plowSprite;
    private CpuParticles2D? _exhaustParticles;
    private CpuParticles2D? _furrowParticles;

    private float _rearWheelRotation = 0f;
    private float _frontWheelRotation = 0f;
    private float _suspensionTimer = 0f;
    private float _lastPositionX = 0f;

    // Physical wheel radii in logical pixels
    private const float RearRadius = 21.0f;
    private const float FrontRadius = 14.0f;

    public void BindVehicle(VehicleModel vehicle)
    {
        _vehicle = vehicle;
        _lastPositionX = vehicle.PositionX;
        BuildVehicleLayers();
    }

    private void BuildVehicleLayers()
    {
        // Load clean alpha textures (0 checkerboard, true RGBA)
        var bodyTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/body.png");
        var rearWheelTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/wheel_rear.png");
        var frontWheelTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/wheel_front.png");
        var plowTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/plow.png");

        // 1. Attached Equipment (Plow) - behind tractor
        _plowSprite = new Sprite2D
        {
            Texture = plowTex,
            Scale = new Vector2(0.045f, 0.045f),
            Position = new Vector2(-44, -12),
            Visible = true
        };
        AddChild(_plowSprite);

        // 2. Rear Wheel (Large drive wheel, rests on ground line Y=0)
        _rearWheelSprite = new Sprite2D
        {
            Texture = rearWheelTex,
            Scale = new Vector2(0.046f, 0.046f),
            Position = new Vector2(-20, -RearRadius)
        };
        AddChild(_rearWheelSprite);

        // 3. Front Wheel (Steering wheel, rests on ground line Y=0)
        _frontWheelSprite = new Sprite2D
        {
            Texture = frontWheelTex,
            Scale = new Vector2(0.033f, 0.033f),
            Position = new Vector2(26, -FrontRadius)
        };
        AddChild(_frontWheelSprite);

        // 4. Chassis Body (Cabin, hood, engine)
        _bodySprite = new Sprite2D
        {
            Texture = bodyTex,
            Scale = new Vector2(0.070f, 0.070f),
            Position = new Vector2(0, -28)
        };
        AddChild(_bodySprite);

        // 5. Exhaust Smoke Particles (Scaled to tiny diorama proportions)
        _exhaustParticles = new CpuParticles2D
        {
            Position = new Vector2(14, -48),
            Amount = 10,
            Lifetime = 0.6f,
            Direction = new Vector2(0.3f, -1.0f),
            Spread = 20f,
            Gravity = new Vector2(0, -15f),
            InitialVelocityMin = 15f,
            InitialVelocityMax = 30f,
            ScaleAmountMin = 0.8f,
            ScaleAmountMax = 2.2f,
            Color = new Color(0.35f, 0.35f, 0.35f, 0.45f),
            Emitting = true
        };
        AddChild(_exhaustParticles);

        // 6. Furrow Soil Clods Particles (Ejected from plow when tilling)
        _furrowParticles = new CpuParticles2D
        {
            Position = new Vector2(-46, -2),
            Amount = 12,
            Lifetime = 0.35f,
            Direction = new Vector2(-1.0f, -0.6f),
            Spread = 25f,
            Gravity = new Vector2(0, 140f),
            InitialVelocityMin = 20f,
            InitialVelocityMax = 45f,
            ScaleAmountMin = 0.8f,
            ScaleAmountMax = 1.6f,
            Color = new Color(0.24f, 0.15f, 0.09f, 0.85f),
            Emitting = false
        };
        AddChild(_furrowParticles);
    }

    public override void _Process(double delta)
    {
        if (_vehicle == null) return;

        float dt = (float)delta;
        float currentX = _vehicle.PositionX;
        float dx = currentX - _lastPositionX;
        _lastPositionX = currentX;

        // Position on field: Soil surface is at Y=85.0f, vehicle base touches ground at Y=85
        Position = new Vector2(currentX, 85.0f);

        // Flip orientation based on movement direction
        Scale = new Vector2(_vehicle.MoveDirection > 0 ? 1.0f : -1.0f, 1.0f);

        // Suspension bounce and wheel rotation when moving
        bool isMoving = MathF.Abs(dx) > 0.001f && !_vehicle.IsBroken;
        if (isMoving)
        {
            _suspensionTimer += dt * 14.0f;
            float bounceY = MathF.Sin(_suspensionTimer) * 0.8f;
            if (_bodySprite != null)
                _bodySprite.Position = new Vector2(0, -28 + bounceY);

            // Wheel rotation physics based on traveled distance and wheel radius
            _rearWheelRotation += (dx / RearRadius) * _vehicle.MoveDirection;
            _frontWheelRotation += (dx / FrontRadius) * _vehicle.MoveDirection;

            if (_rearWheelSprite != null) _rearWheelSprite.Rotation = _rearWheelRotation;
            if (_frontWheelSprite != null) _frontWheelSprite.Rotation = _frontWheelRotation;
        }

        // Equipment positioning: Lower when actively working, lift when traveling/turning
        if (_plowSprite != null)
        {
            bool isPlowing = _vehicle.IsWorking && (_vehicle.CurrentTask is VehicleTaskType.Plowing or VehicleTaskType.Cultivating);
            float targetY = isPlowing ? -10.0f : -16.0f;
            _plowSprite.Position = new Vector2(-44, Mathf.Lerp(_plowSprite.Position.Y, targetY, dt * 6.0f));

            if (_furrowParticles != null)
            {
                _furrowParticles.Emitting = isPlowing && isMoving;
            }
        }

        // Exhaust smoke: Puffs faster when working under engine load
        if (_exhaustParticles != null)
        {
            bool isWorking = _vehicle.IsWorking;
            _exhaustParticles.Amount = isWorking ? 14 : 8;
            _exhaustParticles.InitialVelocityMax = isWorking ? 36f : 22f;
        }
    }
}
