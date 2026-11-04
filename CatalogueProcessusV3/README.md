# Catalogue des TypeProcessus — V3 (version simplifiée)

Le catalogue est une table `(CategorieProcessus, critère?) → TypeProcessus`, où le critère
est **une seule valeur d'enum** (`TypeFormulaire`, `TypeDossier`, …) ou rien.

```csharp
catalogue.Obtenir(CategorieProcessus.Relance);                                   // Relance
catalogue.Obtenir(CategorieProcessus.Reception, TypeFormulaire.Renouvellement);  // ReceptionRenouvellement
catalogue.Obtenir(CategorieProcessus.PlanificationDossier, TypeDossier.Entreprise); // PlanificationEntreprise
catalogue.Obtenir(CategorieProcessus.Reception);                                 // null : aucun processus
```

Recherche : d'abord `(catégorie, critère)`, sinon le type par défaut de la catégorie.
`Obtenir` retourne `null` quand rien ne correspond : critère absent ou non listé sans type par défaut,
ou critère d'un autre enum que celui de la catégorie (ex. `TypeDossier` pour `Reception`).

## Déclarer la table

Tout est dans `Configuration/CatalogueProcessusModule.cs` (`Definir`) : une ligne par correspondance.

```csharp
// Catégorie à processus unique
.Ajouter(CategorieProcessus.Relance, TypeProcessus.Relance)

// Catégorie aiguillée : un bloc critère → type
.Ajouter(CategorieProcessus.Reception,
    (TypeFormulaire.Initial,        TypeProcessus.ReceptionInitiale),
    (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))

// Un type par défaut + seulement les critères atypiques (ex. 15 formulaires, 2 atypiques)
.Ajouter(CategorieProcessus.Reception, parDefaut: TypeProcessus.ReceptionInitiale,
    (TypeFormulaire.Renouvellement, TypeProcessus.ReceptionRenouvellement))

// Une correspondance à la fois
.Ajouter(CategorieProcessus.Reception, TypeFormulaire.Initial, TypeProcessus.ReceptionInitiale)
```

`CatalogueProcessusBuilder` refuse à la construction :
- un doublon `(catégorie, critère)`, ou deux types par défaut pour une catégorie;
- une catégorie aiguillée par plusieurs enums (ex. `TypeFormulaire` et `TypeDossier`).

## Autofac

```csharp
builder.RegisterModule<CatalogueProcessusModule>(); // ICatalogueProcessus, singleton
```

## Tests

```
dotnet test CatalogueProcessus.sln
```
