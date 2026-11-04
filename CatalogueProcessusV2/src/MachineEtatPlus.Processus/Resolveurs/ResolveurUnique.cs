using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

public sealed class ResolveurUnique(TypeProcessus typeProcessus) : IResolveurTypeProcessus
{
    public TypeProcessus Resoudre(ContexteProcessus contexte) => typeProcessus;
}
