using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Classe de base pour les résolveurs dédiés paramétrés par <see cref="TypeFormulaire"/>.</summary>
public abstract class ResolveurParTypeFormulaire : IResolveurTypeProcessus<TypeFormulaire>
{
    public Type? TypeParametre => typeof(TypeFormulaire);

    public abstract TypeProcessus Resoudre(TypeFormulaire typeFormulaire);
}
