using F1App.Api.Models;

namespace F1App.Api.Services;

// Ergast has no fields for these — see Story 5.1's scope-decision note.
// Covers the same circuitId roster as RaceScheduleService.CircuitTimezones
// (the current season's calendar); anything outside this list yields a
// null CircuitStats rather than a fabricated number.
public static class CircuitStaticFacts
{
    public static readonly Dictionary<string, CircuitStats> ByCircuitId =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bahrain"] = new(5.412, 15, 3),
            ["jeddah"] = new(6.174, 27, 3),
            ["albert_park"] = new(5.278, 14, 4),
            ["suzuka"] = new(5.807, 18, 1),
            ["shanghai"] = new(5.451, 16, 2),
            ["miami"] = new(5.412, 19, 3),
            ["imola"] = new(4.909, 19, 2),
            ["monaco"] = new(3.337, 19, 1),
            ["villeneuve"] = new(4.361, 14, 2),
            ["catalunya"] = new(4.657, 16, 2),
            ["red_bull_ring"] = new(4.318, 10, 3),
            ["silverstone"] = new(5.891, 18, 2),
            ["hungaroring"] = new(4.381, 14, 1),
            ["spa"] = new(7.004, 19, 2),
            ["zandvoort"] = new(4.259, 14, 2),
            ["monza"] = new(5.793, 11, 2),
            ["baku"] = new(6.003, 20, 2),
            ["marina_bay"] = new(4.940, 19, 3),
            ["americas"] = new(5.513, 20, 2),
            ["rodriguez"] = new(4.304, 17, 2),
            ["interlagos"] = new(4.309, 15, 2),
            ["las_vegas"] = new(6.201, 17, 2),
            ["losail"] = new(5.419, 16, 1),
            ["yas_marina"] = new(5.281, 16, 2),
        };
}
