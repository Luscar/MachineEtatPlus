# SelectionProcessus

Détermine le **type de processus** à utiliser selon une **catégorie** et des **critères propres à cette catégorie**.

```csharp
var selecteur = new SelecteurProcessus(
[
    new DeterminateurReceptionDeclaration(),
    new DeterminateurReceptionCorrection(),
]);

selecteur.Determiner(CategorieProcessus.ReceptionDeclaration,
    new CriteresReceptionDeclaration(TypeFormulaire.FormulaireLot));
// => TypeProcessus.DeclarationOriginaleMassive
```

## Principe

| Élément | Rôle |
|---|---|
| `CategorieProcessus` | La catégorie passée en paramètre (ex. `ReceptionDeclaration`). |
| `ICriteresProcessus` | Les informations complémentaires ; **un record par catégorie** (ex. `CriteresReceptionDeclaration(TypeFormulaire)`). |
| `DeterminateurParRegles<TCriteres>` | Une classe par catégorie ; déclare des règles ordonnées `Si(condition, type)`. La première règle vraie l'emporte. |
| `SelecteurProcessus` | Point d'entrée : choisit le déterminateur selon la catégorie et lui délègue. |

Les erreurs sont explicites :
- `CategorieNonSupporteeException` : aucun déterminateur pour la catégorie ;
- `ArgumentException` : les critères ne sont pas ceux attendus par la catégorie ;
- `ProcessusIntrouvableException` : aucune règle ne correspond (`EssayerDeterminer` retourne `false` à la place).

## Ajouter une catégorie

1. Ajouter la valeur à `CategorieProcessus` et les types à `TypeProcessus`.
2. Créer le record de critères : `public sealed record CriteresXyz(...) : ICriteresProcessus;`
3. Créer le déterminateur :

```csharp
public sealed class DeterminateurXyz : DeterminateurParRegles<CriteresXyz>
{
    public override CategorieProcessus Categorie => CategorieProcessus.Xyz;

    protected override void DefinirRegles(ConstructeurRegles regles) => regles
        .Si(c => c.UneCondition, TypeProcessus.ProcessusA)
        .SinonParDefaut(TypeProcessus.ProcessusB);
}
```

4. L'enregistrer (avec Autofac : `builder.RegisterAssemblyTypes(asm).AssignableTo<IDeterminateurProcessus>().As<IDeterminateurProcessus>();`
   puis `builder.RegisterType<SelecteurProcessus>().As<ISelecteurProcessus>().SingleInstance();`).

Une catégorie sans critère peut utiliser `AucunCritere.Instance`.

## Tests

```bash
dotnet test
```
