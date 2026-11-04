using MachineEtatPlus.Processus.Catalogue;
using MachineEtatPlus.Processus.Enums;

namespace MachineEtatPlus.Processus.Resolveurs;

/// <summary>Reception : un renouvellement reçu après la date limite est tardif.</summary>
public sealed class ResolveurReception(TimeProvider horloge, IParametresProcessus parametres)
    : IResolveurTypeProcessus
{
    public TypeProcessus Resoudre(ContexteProcessus contexte)
    {
        var typeFormulaire = contexte.ExigerTypeFormulaire();
        var aujourdhui = DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);

        return typeFormulaire switch
        {
            TypeFormulaire.Initial => TypeProcessus.ReceptionInitiale,
            TypeFormulaire.Renouvellement when aujourdhui > parametres.DateLimiteRenouvellement
                => TypeProcessus.ReceptionRenouvellementTardif,
            TypeFormulaire.Renouvellement => TypeProcessus.ReceptionRenouvellement,
            _ => throw new ArgumentOutOfRangeException(nameof(contexte), typeFormulaire,
                     "TypeFormulaire non supporté pour la catégorie Reception.")
        };
    }
}
