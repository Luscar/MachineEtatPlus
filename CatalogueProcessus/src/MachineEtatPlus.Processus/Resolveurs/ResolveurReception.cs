using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>
/// Résolveur dédié à la catégorie <see cref="CategorieProcessus.Reception"/> :
/// un renouvellement reçu après la date limite est un renouvellement tardif.
/// </summary>
public sealed class ResolveurReception(
    TimeProvider horloge,
    IParametresProcessus parametres) : ResolveurParTypeFormulaire
{
    public override TypeProcessus Resoudre(TypeFormulaire typeFormulaire)
    {
        var aujourdhui = DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);

        return typeFormulaire switch
        {
            TypeFormulaire.Initial => TypeProcessus.ReceptionInitiale,
            TypeFormulaire.Renouvellement when aujourdhui > parametres.DateLimiteRenouvellement
                => TypeProcessus.ReceptionRenouvellementTardif,
            TypeFormulaire.Renouvellement => TypeProcessus.ReceptionRenouvellement,
            _ => throw new ArgumentOutOfRangeException(nameof(typeFormulaire), typeFormulaire,
                     "TypeFormulaire non supporté pour la catégorie Reception.")
        };
    }
}
