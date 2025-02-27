// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

// Derived from https://github.com/dotnet/BenchmarkDotNet
// Licensed under the MIT License

using System;
using System.Linq;

namespace Nethermind.Init.Cpu;

/// <summary>
/// CPU information from output of the `cat /proc/info` command.
/// Linux only.
/// </summary>
internal static class ProcCpuInfoProvider
{
    internal static readonly Lazy<CpuInfo?> ProcCpuInfo = new Lazy<CpuInfo?>(Load);

    private static CpuInfo? Load()
    {
        if (RuntimeInformation.IsLinux())
        {
            string? content = ProcessHelper.RunAndReadOutput("cat", "/proc/cpuinfo");
            string output = GetCpuSpeed();
            content = content + output;
            return ProcCpuInfoParser.ParseOutput(content);
        }
        return null;
    }

    private static string GetCpuSpeed()
    {
        var output = ProcessHelper.RunAndReadOutput("/bin/bash", "-c \"lscpu | grep MHz\"")?
                                  .Split('\n')
                                  .SelectMany(x => x.Split(':'))
                                  .ToArray() ?? Array.Empty<string>();

        return ParseCpuFrequencies(output) ?? "";
    }

    private static string? ParseCpuFrequencies(string[] input)
    {
        // Example of output we trying to parse:
        //
        // CPU MHz: 949.154
        // CPU max MHz: 3200,0000
        // CPU min MHz: 800,0000
        //
        // And we don't need "CPU MHz" line
        if (input == null || input.Length < 6)
            return null;

        Frequency.TryParseMHz(input[3].Trim().Replace(',', '.'), out var minFrequency);
        Frequency.TryParseMHz(input[5].Trim().Replace(',', '.'), out var maxFrequency);

        return $"\n{ProcCpuInfoKeyNames.MinFrequency}\t:{minFrequency.ToMHz()}\n{ProcCpuInfoKeyNames.MaxFrequency}\t:{maxFrequency.ToMHz()}\n";
    }
}
