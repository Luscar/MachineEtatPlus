namespace SelectionProcessus;

/// <summary>
/// Classe de base : un déterminateur décrit, pour sa catégorie, une liste ordonnée
/// de règles « condition → type de processus ». La première règle satisfaite l'emporte.
/// </summary>
/// <typeparam name="TCriteres">Les informations complémentaires requises par la catégorie.</typeparam>
public abstract class DeterminateurParRegles<TCriteres> : IDeterminateurProcessus
    where TCriteres : ICriteresProcessus
{
    private readonly IReadOnlyList<(Func<TCriteres, bool> Condition, TypeProcessus Type)> _regles;

    protected DeterminateurParRegles()
    {
        var constructeur = new ConstructeurRegles();
        DefinirRegles(constructeur);
        _regles = constructeur.Regles;
    }

    public abstract CategorieProcessus Categorie { get; }

    public Type TypeCriteres => typeof(TCriteres);

    /// <summary>Déclare les règles de la catégorie, dans l'ordre de priorité.</summary>
    protected abstract void DefinirRegles(ConstructeurRegles regles);

    public TypeProcessus Determiner(ICriteresProcessus criteres)
    {
        ArgumentNullException.ThrowIfNull(criteres);

        if (criteres is not TCriteres criteresTypes)
        {
            throw new ArgumentException(
                $"La catégorie {Categorie} attend des critères de type {typeof(TCriteres).Name}, " +
                $"mais a reçu {criteres.GetType().Name}.",
                nameof(criteres));
        }

        foreach (var (condition, type) in _regles)
        {
            if (condition(criteresTypes))
            {
                return type;
            }
        }

        throw new ProcessusIntrouvableException(Categorie, criteres);
    }

    protected sealed class ConstructeurRegles
    {
        internal List<(Func<TCriteres, bool>, TypeProcessus)> Regles { get; } = [];

        /// <summary>Si <paramref name="condition"/> est vraie, retourner <paramref name="type"/>.</summary>
        public ConstructeurRegles Si(Func<TCriteres, bool> condition, TypeProcessus type)
        {
            ArgumentNullException.ThrowIfNull(condition);
            Regles.Add((condition, type));
            return this;
        }

        /// <summary>Règle par défaut, à déclarer en dernier.</summary>
        public ConstructeurRegles SinonParDefaut(TypeProcessus type) => Si(_ => true, type);
    }
}
