/*
 * Copyright (c) 2025 Stephen Denne
 * https://github.com/datacute/LightweightTracing
 */

using System.Diagnostics;
using System.Text;

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
        Events[_index] = (Stopwatch.ElapsedTicks, eventId);
        _index = (_index + 1) % Capacity;
    }

    public static void GetTrace(StringBuilder stringBuilder, Dictionary<int, string> eventNameMap)
    {
        var index = _index;
        for (var i = 0; i < Capacity; i++)
        {
            var (timestamp, eventId) = Events[index];
            if (timestamp > 0)
            {
                stringBuilder.AppendFormat("{0:o} [{1:000}] {2}",
                        StartTime.AddTicks(timestamp),
                        eventId,
                        eventNameMap.TryGetValue(eventId, out var name) ? name : string.Empty)
                    .AppendLine();
            }

            index = (index + 1) % Capacity;
        }
    }
}