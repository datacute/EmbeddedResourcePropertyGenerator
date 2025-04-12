/*
 * Copyright (c) 2025 Stephen Denne
 * https://github.com/datacute/LightweightTracing
 */

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public static class LightweightTrace
{
    private const int Capacity = 1024;

    private static readonly DateTime StartTime = DateTime.UtcNow;
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    private static readonly (long, int)[] Events = new (long, int)[Capacity];
    private static int _index;

    public static void Add(int eventId)
    {
        var index = Interlocked.Increment(ref _index) % Capacity;
        Events[index] = (Stopwatch.ElapsedTicks, eventId);
    }

    public static void GetTrace(StringBuilder stringBuilder, Dictionary<int, string> eventNameMap)
    {
        var index = _index;
        for (var i = 0; i < Capacity; i++)
        {
            index = (index + 1) % Capacity;
            var (timestamp, eventId) = Events[index];
            if (timestamp > 0)
            {
                string text;
                string item = string.Empty;
                if (eventId > 1000)
                {
                    item = $" ({eventId / 1000})";
                }
                text = eventNameMap.TryGetValue(eventId % 1000, out var name) ? name : string.Empty;
                stringBuilder.AppendFormat("{0:o} [{1:000}] {2} {3}",
                        StartTime.AddTicks(timestamp),
                        eventId % 1000,
                        text,
                        item)
                    .AppendLine();
            }
        }
    }
}

public static class LightweightTraceExtensions
{
    public static IncrementalValuesProvider<T> Trace<T>(this IncrementalValuesProvider<T> source, TrackingNames eventId) =>
        source.Select((input, _) =>
        {
            LightweightTrace.Add((int)eventId);
                
            return input;
        }).WithTrackingName(Enum.GetName(typeof(TrackingNames), eventId) ?? $"({eventId})");

    public static IncrementalValueProvider<T> Trace<T>(this IncrementalValueProvider<T> source, TrackingNames eventId) =>
        source.Select((input, _) =>
        {
            LightweightTrace.Add((int)eventId);
                
            return input;
        }).WithTrackingName(Enum.GetName(typeof(TrackingNames), eventId) ?? $"({eventId})");
}