using MachineEtatPlus.Processus.Enums;
using MachineEtatPlus.Processus.Resolveurs;

namespace MachineEtatPlus.Processus.Tests;

public sealed class ResolveurParCleTests
{
    [Fact]
    public void La_table_est_copiee_et_ne_peut_plus_etre_modifiee_apres_creation()
    {
        var table = new Dictionary<TypeDossier, TypeProcessus> { [TypeDossier.Individuel] = TypeProcessus.PlanificationIndividuelle };
        var resolveur = new ResolveurParCle<TypeDossier>(table);

        table[TypeDossier.Entreprise] = TypeProcessus.PlanificationEntreprise;

        Assert.Throws<InvalidOperationException>(() => resolveur.Resoudre(TypeDossier.Entreprise));
        Assert.Equal(typeof(TypeDossier), resolveur.TypeParametre);
    }
}
