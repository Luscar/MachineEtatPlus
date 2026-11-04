# Catalogue des TypeProcessus — V3 (version simplifiée)

Le catalogue est une table `(CategorieProcessus, critère?) → TypeProcessus`, où le critère
est **une seule valeur d'enum** (`TypeFormulaire`, `TypeDossier`, …) ou rien.

```csharp
catalogue.Obtenir(CategorieProcessus.Relance);                                   // Relance
catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Renouvellement);  // ReceptionRenouvellement
catalogue.Obtenir(CategorieProcessus.PlanificationDossier, TypeDossier.Entreprise); // PlanificationEntreprise
catalogue.Obtenir(CategorieProcessus.Reception);                                 // null : aucun processus
```

`Obtenir` retourne `null` quand aucun processus ne correspond (critère manquant, superflu,
d'un autre enum, ou valeur non déclarée).

## Déclarer la table

Tout est dans `Configuration/CatalogueProcessusModule.cs` (`Definir`) : une ligne par correspondance.

```csharp
.Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance)
.Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)
```

`CatalogueProcessusBuilder` refuse à la construction :
- un doublon `(catégorie, critère)`;
- une catégorie qui mélange plusieurs genres de critère (ex. `TypeFormulaire` et `TypeDossier`),
  ou avec et sans critère.

## Autofac

```csharp
builder.RegisterModule<CatalogueProcessusModule>(); // ICatalogueProcessus, singleton
```

## Tests

```
dotnet test CatalogueProcessus.sln
```
