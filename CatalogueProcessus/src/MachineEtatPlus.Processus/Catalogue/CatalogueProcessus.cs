using Autofac.Features.Indexed;
using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Catalogue;

public sealed class CatalogueProcessus(IIndex<CategorieProcessus, IResolveurTypeProcessus> resolveurs)
    : ICatalogueProcessus
{
    public TypeProcessus Resolve(CategorieProcessus categorie) =>
        Obtenir(categorie) switch
        {
            IResolveurSansParametre r => r.Resoudre(),
            var r => throw MauvaisParametre(categorie, r, "aucun")
        };

    public TypeProcessus Resolve<TParam>(CategorieProcessus categorie, TParam parametre) =>
        Obtenir(categorie) switch
        {
            IResolveurTypeProcessus<TParam> r => r.Resoudre(parametre),
            var r => throw MauvaisParametre(categorie, r, typeof(TParam).Name)
        };

    private IResolveurTypeProcessus Obtenir(CategorieProcessus categorie) =>
        resolveurs.TryGetValue(categorie, out var r)
            ? r
            : throw new InvalidOperationException($"Aucun résolveur pour la catégorie '{categorie}'.");

    private static InvalidOperationException MauvaisParametre(
        CategorieProcessus categorie, IResolveurTypeProcessus r, string recu) =>
        new($"La catégorie '{categorie}' attend le paramètre " +
            $"'{r.TypeParametre?.Name ?? "aucun"}', reçu '{recu}'.");
}
