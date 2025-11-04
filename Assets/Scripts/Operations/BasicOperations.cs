using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;


namespace Fixor {
    /*
    public readonly struct Not : IOperation {
        public uint Operate(uint input) => ~input;
    }
    
    public readonly struct And : IOperation {
        public uint Operate(uint input) => (input & 0b11) >> 1;
    }

    public readonly struct Or : IOperation {
        public uint Operate(uint input) => (input | (input >> 1)) & 1;
    }

    public readonly struct Xor : IOperation {
        public uint Operate(uint input) => (input ^ (input >> 1)) & 1;
    }

    public readonly struct Nand : IOperation {
        public uint Operate(uint input) => (input & 0b00) >> 1;
    }
    */
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class Operations {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint NOT(uint input)  => ~input;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint AND(uint input)  => (input >> 1) & input & 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint OR(uint input)   => (input | (input >> 1)) & 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint XOR(uint input)  => (input ^ (input >> 1)) & 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint NAND(uint input) => ~AND(input);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint NOR(uint input)  => ~OR(input);
        // NAND is technically the "true" gate but it's simpler to build it from NOT & AND.
    }
}