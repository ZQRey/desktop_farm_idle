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

    public void BindVehicle(VehicleModel vehicle)
    {
        _vehicle = vehicle;
        _lastPositionX = vehicle.PositionX;
        BuildVehicleLayers();
    }

    private void BuildVehicleLayers()
    {
        // Load textures
        var bodyTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/body.png");
        var rearWheelTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/wheel_rear.png");
        var frontWheelTex = GD.Load<Texture2D>("res://assets/vehicles/tractor_classic/wheel_front.png");
        var plowTex = GD.Load<Texture2D>("res://assets/equipment/plow/plow.png");

        // Attached Equipment (Plow) - behind tractor
        _plowSprite = new Sprite2D
        {
            Texture = plowTex,
            Scale = new Vector2(0.12f, 0.12f),
            Position = new Vector2(-75, 12),
            Visible = true
        };
        AddChild(_plowSprite);

        // Rear Wheel
        _rearWheelSprite = new Sprite2D
        {
            Texture = rearWheelTex,
            Scale = new Vector2(0.14f, 0.14f),
            Position = new Vector2(-36, 18)
        };
        AddChild(_rearWheelSprite);

        // Front Wheel
        _frontWheelSprite = new Sprite2D
        {
            Texture = frontWheelTex,
            Scale = new Vector2(0.11f, 0.11f),
            Position = new Vector2(50, 24)
        };
        AddChild(_frontWheelSprite);

        // Chassis Body
        _bodySprite = new Sprite2D
        {
            Texture = bodyTex,
            Scale = new Vector2(0.14f, 0.14f),
            Position = new Vector2(0, 0)
        };
        AddChild(_bodySprite);

        // Exhaust Particles
        _exhaustParticles = new CpuParticles2D
        {
            Position = new Vector2(28, -48),
            Amount = 14,
            Lifetime = 0.8f,
            Direction = new Vector2(0.3f, -1.0f),
            Spread = 25f,
            Gravity = new Vector2(0, -20f),
            InitialVelocityMin = 25f,
            InitialVelocityMax = 50f,
            ScaleAmountMin = 2.5f,
            ScaleAmountMax = 6.0f,
            Color = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            Emitting = true
        };
        AddChild(_exhaustParticles);

        // Furrow Soil Clods Particles (under plow)
        _furrowParticles = new CpuParticles2D
        {
            Position = new Vector2(-80, 20),
            Amount = 18,
            Lifetime = 0.5f,
            Direction = new Vector2(-1.0f, -0.6f),
            Spread = 35f,
            Gravity = new Vector2(0, 180f),
            InitialVelocityMin = 30f,
            InitialVelocityMax = 65f,
            ScaleAmountMin = 2.0f,
            ScaleAmountMax = 4.5f,
            Color = new Color(0.28f, 0.18f, 0.10f, 0.9f),
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

        // Position on field (ground line is around y=160, wheels touch earth)
        Position = new Vector2(currentX, 138.0f);

        // Flip orientation based on movement direction
        Scale = new Vector2(_vehicle.MoveDirection > 0 ? 1.0f : -1.0f, 1.0f);

        // Suspension bounce when moving
        bool isMoving = MathF.Abs(dx) > 0.001f && !_vehicle.IsBroken;
        if (isMoving)
        {
            _suspensionTimer += dt * 14.0f;
            float bounceY = MathF.Sin(_suspensionTimer) * 1.5f;
            if (_bodySprite != null)
                _bodySprite.Position = new Vector2(0, bounceY);

            // Wheel rotation physics
            float rearRadius = 22.0f;
            float frontRadius = 15.0f;

            _rearWheelRotation += (dx / rearRadius) * _vehicle.MoveDirection;
            _frontWheelRotation += (dx / frontRadius) * _vehicle.MoveDirection;

            if (_rearWheelSprite != null) _rearWheelSprite.Rotation = _rearWheelRotation;
            if (_frontWheelSprite != null) _frontWheelSprite.Rotation = _frontWheelRotation;
        }

        // Equipment positioning: Lower when working, lift when turning or idle
        if (_plowSprite != null)
        {
            _plowSprite.Visible = _vehicle.AttachedEquipment == EquipmentType.Plow;
            float targetPlowY = _vehicle.IsWorking ? 16.0f : 4.0f; // Lowered into ground vs lifted
            _plowSprite.Position = new Vector2(-75, Mathf.Lerp(_plowSprite.Position.Y, targetPlowY, dt * 6.0f));
            _plowSprite.Rotation = Mathf.Lerp(_plowSprite.Rotation, _vehicle.IsWorking ? 0.05f : -0.18f, dt * 6.0f);
        }

        // Soil furrow particles active only when plowing and moving
        if (_furrowParticles != null)
        {
            _furrowParticles.Emitting = _vehicle.IsWorking && _vehicle.CurrentTask == VehicleTaskType.Plowing && isMoving;
        }

        // Exhaust smoke: darker & more frequent under load
        if (_exhaustParticles != null)
        {
            _exhaustParticles.Emitting = !_vehicle.IsBroken;
            if (_vehicle.IsWorking)
            {
                _exhaustParticles.Amount = 24;
                _exhaustParticles.Color = new Color(0.25f, 0.25f, 0.25f, 0.65f);
            }
            else
            {
                _exhaustParticles.Amount = 10;
                _exhaustParticles.Color = new Color(0.55f, 0.55f, 0.55f, 0.35f);
            }
        }
    }
}
