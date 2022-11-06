using System;
using System.Threading.Tasks;
using EdmxRuler.Extensions;

namespace EdmxRuler.Tests.Util;

public static class PerformanceTestExtensions {
    public static uint RunTimedTask(this Action a, uint times) {
        var start = DateTimeExtensions.GetTime();
        for (int i = 0; i < times; i++) {
            a();
        }

        var elapsed = DateTimeExtensions.GetTime() - start;
        return (uint)(elapsed);
    }

    public static uint RunTimedTask(this Action a) {
        var start = DateTimeExtensions.GetTime();
        a();
        var elapsed = DateTimeExtensions.GetTime() - start;
        return elapsed;
    }

    public static async Task<uint> RunTimedTask(this Func<Task> a, uint times) {
        var start = DateTimeExtensions.GetTime();
        for (int i = 0; i < times; i++) {
            await a();
        }

        var elapsed = DateTimeExtensions.GetTime() - start;
        return (uint)(elapsed);
    }

    public static async Task<uint> RunTimedTask(this Func<Task> a) {
        var start = DateTimeExtensions.GetTime();
        await a();
        var elapsed = DateTimeExtensions.GetTime() - start;
        return elapsed;
    }
}