using System;
using Godot;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Simulation.Weather;

namespace DesktopFarmIdle.Rendering;

public partial class WeatherView : Node2D
{
    private WeatherSystem? _weatherSystem;
    private CpuParticles2D? _rainParticles;
    private CpuParticles2D? _snowParticles;
    private CanvasModulate? _dayNightModulate;
    private ColorRect? _lightningFlash;
    private float _flashTimer = 0f;
    private Random _random = new();

    public void BindWeather(WeatherSystem weatherSystem)
    {
        _weatherSystem = weatherSystem;
    }

    public override void _Ready()
    {
        // Rain particles
        _rainParticles = new CpuParticles2D
        {
            Position = new Vector2(960, -20),
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(1000, 10),
            Amount = 120,
            Lifetime = 0.7f,
            Direction = new Vector2(0.2f, 1.0f),
            Spread = 5f,
            Gravity = new Vector2(0, 450f),
            InitialVelocityMin = 280f,
            InitialVelocityMax = 380f,
            ScaleAmountMin = 1.0f,
            ScaleAmountMax = 2.0f,
            Color = new Color(0.6f, 0.75f, 0.95f, 0.5f),
            Emitting = false
        };
        AddChild(_rainParticles);

        // Snow particles
        _snowParticles = new CpuParticles2D
        {
            Position = new Vector2(960, -20),
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(1000, 10),
            Amount = 70,
            Lifetime = 2.5f,
            Direction = new Vector2(0.3f, 1.0f),
            Spread = 20f,
            Gravity = new Vector2(0, 40f),
            InitialVelocityMin = 30f,
            InitialVelocityMax = 60f,
            ScaleAmountMin = 2.0f,
            ScaleAmountMax = 4.0f,
            Color = new Color(0.95f, 0.95f, 1.0f, 0.75f),
            Emitting = false
        };
        AddChild(_snowParticles);

        // Lightning flash overlay
        _lightningFlash = new ColorRect
        {
            Size = new Vector2(1920, 200),
            Color = new Color(1, 1, 1, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_lightningFlash);
    }

    public override void _Process(double delta)
    {
        if (_weatherSystem == null) return;

        float dt = (float)delta;
        var weather = _weatherSystem.CurrentWeather;

        // Rain activation
        bool isRaining = weather is WeatherType.LightRain or WeatherType.Rain or WeatherType.HeavyRain or WeatherType.Storm;
        if (_rainParticles != null)
        {
            _rainParticles.Emitting = isRaining;
            if (isRaining)
            {
                _rainParticles.Amount = weather switch
                {
                    WeatherType.LightRain => 60,
                    WeatherType.Rain => 140,
                    WeatherType.HeavyRain => 240,
                    WeatherType.Storm => 320,
                    _ => 100
                };
            }
        }

        // Snow activation
        bool isSnowing = weather == WeatherType.Snow;
        if (_snowParticles != null)
        {
            _snowParticles.Emitting = isSnowing;
        }

        // Storm lightning flashes
        if (weather == WeatherType.Storm)
        {
            _flashTimer -= dt;
            if (_flashTimer <= 0f)
            {
                if (_random.NextDouble() < 0.05)
                {
                    if (_lightningFlash != null) _lightningFlash.Color = new Color(1, 1, 1, 0.5f);
                    _flashTimer = 0.15f;
                }
            }
            else if (_lightningFlash != null && _lightningFlash.Color.A > 0f)
            {
                _lightningFlash.Color = new Color(1, 1, 1, Math.Max(0f, _lightningFlash.Color.A - dt * 3.0f));
            }
        }
    }
}
