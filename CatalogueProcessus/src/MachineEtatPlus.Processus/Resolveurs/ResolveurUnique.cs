using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Résolveur retournant toujours le même <see cref="TypeProcessus"/>.</summary>
public sealed class ResolveurUnique(TypeProcessus typeProcessus) : IResolveurSansParametre
{
    public Type? TypeParametre => null;

    public TypeProcessus Resoudre() => typeProcessus;
}
