using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
#pragma warning disable CA1822

namespace UStealth.Benchmarks
{
    // For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
    [CPUUsageDiagnoser]
    [SimpleJob(RuntimeMoniker.NativeAot10_0)]
    [SimpleJob(RuntimeMoniker.Net10_0)]
    [SimpleJob(RuntimeMoniker.Net90)]
    [SimpleJob(RuntimeMoniker.NativeAot90)]
    public class UStealthCLI
    {
        private readonly Consumer _consumer = new Consumer();
        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public void ListDrives() => _consumer.Consume(DriveHelper.Program.GetDrives());
    }
}
