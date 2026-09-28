using System;
using LastSignal.Persistence;
using System.Collections.Generic;
using LastSignal.Tests;

public class Test
{
    public static void Main()
    {
        var input = SaveFoundationTests.Fixture();
        var validation = new SaveValidation("world.fixture", "content.1", new Dictionary<string, int> { ["ammo.rifle"] = 60, ["medical.bandage"] = 10 }, "rifle.1", 30);
        var codec = new SaveCodec(validation);
        var result = codec.Encode(input, out _);
        Console.WriteLine($"Encode result: {result.Success} - {result.Message}");
    }
}
