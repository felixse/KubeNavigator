using System;
using k8s.Models;

namespace KubeNavigator.Models;

public readonly record struct CpuQuantity(long Nanocores) : IComparable<CpuQuantity>
{
    public static CpuQuantity Zero => new(0);

    public static CpuQuantity FromResourceQuantity(ResourceQuantity quantity)
    {
        var nanocores = (long)(quantity.ToDecimal() * 1_000_000_000m);
        return new CpuQuantity(nanocores);
    }

    public static CpuQuantity operator +(CpuQuantity left, CpuQuantity right) =>
        new(left.Nanocores + right.Nanocores);

    public static CpuQuantity operator -(CpuQuantity left, CpuQuantity right) =>
        new(left.Nanocores - right.Nanocores);

    public int CompareTo(CpuQuantity other) => Nanocores.CompareTo(other.Nanocores);

    public string Format()
    {
        var millicores = Nanocores / 1_000_000;
        if (millicores >= 1000)
            return $"{millicores / 1000d:0.##}";
        return $"{millicores}m";
    }

    public override string ToString() => Format();
}

public readonly record struct MemoryQuantity(long Bytes) : IComparable<MemoryQuantity>
{
    public static MemoryQuantity Zero => new(0);

    public static MemoryQuantity FromResourceQuantity(ResourceQuantity quantity)
    {
        var bytes = (long)quantity.ToDecimal();
        return new MemoryQuantity(bytes);
    }

    public static MemoryQuantity operator +(MemoryQuantity left, MemoryQuantity right) =>
        new(left.Bytes + right.Bytes);

    public static MemoryQuantity operator -(MemoryQuantity left, MemoryQuantity right) =>
        new(left.Bytes - right.Bytes);

    public int CompareTo(MemoryQuantity other) => Bytes.CompareTo(other.Bytes);

    public string Format()
    {
        if (Bytes >= 1024L * 1024 * 1024)
            return $"{Bytes / (1024.0 * 1024 * 1024):0.#}Gi";
        if (Bytes >= 1024L * 1024)
            return $"{Bytes / (1024.0 * 1024):0.#}Mi";
        if (Bytes >= 1024)
            return $"{Bytes / 1024.0:0.#}Ki";
        return $"{Bytes}B";
    }

    public double ToGigabytes() => Bytes / (1024.0 * 1024 * 1024);

    public override string ToString() => Format();
}

public record struct ResourceUsage(CpuQuantity Cpu, MemoryQuantity Memory);
