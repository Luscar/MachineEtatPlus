# Catalogue de résolution des TypeProcessus (V2)

```csharp
catalogue.Resolve(CategorieProcessus.Relance);
catalogue.Resolve(CategorieProcessus.Reception, TypeFormulaire.Initial);
catalogue.Resolve(CategorieProcessus.PlanificationDossier, TypeDossier.Entreprise);
catalogue.Resolve(categorieLueEnBase, new ContexteProcessus(dossier.TypeFormulaire, dossier.TypeDossier));
```

## Principes
- **Contexte plutôt que paramètre** : `ICatalogueProcessus` n'a qu'une méthode,
  `Resolve(categorie, ContexteProcessus)`. Chaque résolveur lit ce dont il a besoin ;
  l'appelant n'a pas à savoir quel paramètre attend quelle catégorie. Les raccourcis
  typés n'acceptent que `TypeFormulaire` ou `TypeDossier`, donc un type de paramètre
  inconnu ne compile pas.
- **Table explicite, validée au démarrage** : le module déclare la table une fois
  (`DefinitionCatalogueProcessus`). Les doublons sont rejetés à la déclaration ; une catégorie
  manquante ou une dépendance absente d'un résolveur dédié fait échouer `Build()` (le catalogue
  est auto-activé).
- **Catalogue indépendant du conteneur** : `CatalogueProcessus` reçoit un dictionnaire et se
  teste sans Autofac. Autofac ne sert qu'à construire les résolveurs dédiés.

## Ajouter une catégorie
Une ligne dans `CatalogueProcessusModule` : `.Unique(...)`, `.ParTypeFormulaire(...)`,
`.ParTypeDossier(...)`, ou `.Dedie<MonResolveur>(...)` pour une logique avec dépendances.
Une catégorie volontairement non couverte se déclare avec `.Exclure(...)`.
Si un résolveur dédié dépend d'un service scoped : `.DureeDeVieParScope()`.

## Hôte
Enregistrer `IParametresProcessus` puis `builder.RegisterModule<CatalogueProcessusModule>()`.
