using Autofac;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Configuration;

/// <summary>
/// Enregistrement des résolveurs (keyed par <see cref="CategorieProcessus"/>, un seul par catégorie).
/// Autofac conserve silencieusement le dernier service keyed : les doublons sont donc bloqués
/// dès l'enregistrement via <see cref="ContainerBuilder.Properties"/>.
/// </summary>
public static class CatalogueAutofacExtensions
{
    private const string CleCategories = "CatalogueProcessus.Categories";

    public static ContainerBuilder AddResolveurUnique(
        this ContainerBuilder b, CategorieProcessus categorie, TypeProcessus typeProcessus)
        => b.AddInstance(categorie, new ResolveurUnique(typeProcessus));

    public static ContainerBuilder AddResolveurParTypeFormulaire(
        this ContainerBuilder b, CategorieProcessus categorie,
        IReadOnlyDictionary<TypeFormulaire, TypeProcessus> correspondances)
        => b.AddInstance(categorie, new ResolveurParCle<TypeFormulaire>(correspondances));

    public static ContainerBuilder AddResolveurParTypeDossier(
        this ContainerBuilder b, CategorieProcessus categorie,
        IReadOnlyDictionary<TypeDossier, TypeProcessus> correspondances)
        => b.AddInstance(categorie, new ResolveurParCle<TypeDossier>(correspondances));

    public static ContainerBuilder AddResolveur<TResolveur>(
        this ContainerBuilder b, CategorieProcessus categorie)
        where TResolveur : IResolveurTypeProcessus
    {
        VerifierUnicite(b, categorie);
        b.RegisterType<TResolveur>().Keyed<IResolveurTypeProcessus>(categorie).SingleInstance();
        return b;
    }

    private static ContainerBuilder AddInstance(
        this ContainerBuilder b, CategorieProcessus categorie, IResolveurTypeProcessus resolveur)
    {
        VerifierUnicite(b, categorie);
        b.RegisterInstance(resolveur).Keyed<IResolveurTypeProcessus>(categorie);
        return b;
    }

    private static void VerifierUnicite(ContainerBuilder b, CategorieProcessus categorie)
    {
        ArgumentNullException.ThrowIfNull(b);

        if (!b.Properties.TryGetValue(CleCategories, out var o) || o is not HashSet<CategorieProcessus> categories)
            b.Properties[CleCategories] = categories = new HashSet<CategorieProcessus>();

        if (!categories.Add(categorie))
            throw new InvalidOperationException($"Un résolveur est déjà enregistré pour '{categorie}'.");
    }
}
