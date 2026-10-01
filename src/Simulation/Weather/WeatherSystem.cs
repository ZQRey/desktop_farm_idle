using System;
using DesktopFarmIdle.Core.Config;
using DesktopFarmIdle.Core.Events;

namespace DesktopFarmIdle.Simulation.Weather;

public class WeatherSystem
{
    private readonly IEventBus _eventBus;
    private readonly Random _random = new();

    public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
    public float CurrentIntensity { get; private set; } = 0.5f;
    public float WeatherDurationRemaining { get; private set; } = 180.0f; // 3 minutes

    public WeatherSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Update(float deltaSimulationTime)
    {
        WeatherDurationRemaining -= deltaSimulationTime;
        if (WeatherDurationRemaining <= 0f)
        {
            TransitionWeather();
        }
    }

    public void ForceWeather(WeatherType type, float durationSeconds = 180.0f)
    {
        CurrentWeather = type;
        CurrentIntensity = 0.5f + (float)_random.NextDouble() * 0.5f;
        WeatherDurationRemaining = durationSeconds;
        _eventBus.Publish(new WeatherChangedEvent((int)CurrentWeather, CurrentWeather.ToString(), CurrentIntensity, DateTime.Now));
    }

    private void TransitionWeather()
    {
        // Markov transitions
        WeatherType next = CurrentWeather switch
        {
            WeatherType.Clear => _random.Next(10) switch
            {
                < 6 => WeatherType.Cloudy,
                < 8 => WeatherType.Wind,
                _ => WeatherType.Hot
            },
            WeatherType.Cloudy => _random.Next(10) switch
            {
                < 4 => WeatherType.LightRain,
                < 6 => WeatherType.Clear,
                < 8 => WeatherType.Fog,
                _ => WeatherType.Rain
            },
            WeatherType.LightRain => _random.Next(10) switch
            {
                < 5 => WeatherType.Rain,
                < 8 => WeatherType.Cloudy,
                _ => WeatherType.HeavyRain
            },
            WeatherType.Rain => _random.Next(10) switch
            {
                < 4 => WeatherType.HeavyRain,
                < 7 => WeatherType.LightRain,
                < 9 => WeatherType.Storm,
                _ => WeatherType.Cloudy
            },
            WeatherType.HeavyRain or WeatherType.Storm => _random.Next(10) switch
            {
                < 6 => WeatherType.Rain,
                < 8 => WeatherType.Cloudy,
                _ => WeatherType.Hail
            },
            WeatherType.Hail => WeatherType.Cloudy,
            WeatherType.Hot => _random.Next(10) switch
            {
                < 3 => WeatherType.HeatWave,
                _ => WeatherType.Clear
            },
            WeatherType.HeatWave => WeatherType.Cloudy,
            WeatherType.Wind => _random.Next(10) switch
            {
                < 3 => WeatherType.StrongWind,
                _ => WeatherType.Clear
            },
            WeatherType.StrongWind => WeatherType.Cloudy,
            WeatherType.Fog => WeatherType.Clear,
            WeatherType.Snow => WeatherType.Cloudy,
            _ => WeatherType.Clear
        };

        CurrentWeather = next;
        CurrentIntensity = 0.4f + (float)_random.NextDouble() * 0.6f;
        WeatherDurationRemaining = 120.0f + (float)_random.NextDouble() * 180.0f; // 2 to 5 minutes

        _eventBus.Publish(new WeatherChangedEvent((int)CurrentWeather, CurrentWeather.ToString(), CurrentIntensity, DateTime.Now));
    }

    public float GetMoistureEffect() => CurrentWeather switch
    {
        WeatherType.LightRain => 0.5f * CurrentIntensity,
        WeatherType.Rain => 1.2f * CurrentIntensity,
        WeatherType.HeavyRain => 2.0f * CurrentIntensity,
        WeatherType.Storm => 2.5f * CurrentIntensity,
        WeatherType.Hot => -0.8f,
        WeatherType.HeatWave => -1.8f,
        _ => 0.0f
    };

    public float GetTemperatureGrowthModifier() => CurrentWeather switch
    {
        WeatherType.Hot => 1.25f,
        WeatherType.HeatWave => 0.7f, // Too hot, stresses crops
        WeatherType.Snow => 0.2f,     // Too cold, freezes growth
        WeatherType.Hail => 0.5f,
        _ => 1.0f
    };

    public float GetVehicleFrictionModifier() => CurrentWeather switch
    {
        WeatherType.HeavyRain => 0.75f, // Muddy soil
        WeatherType.Storm => 0.65f,
        WeatherType.Snow => 0.70f,
        _ => 1.0f
    };
}
