# MachineEtatPlus

Moteur de processus (state machine) distribue sous forme de paquet NuGet, concu pour etre
pilote par l'application cliente : **c'est elle qui possede la transaction**, les tables et la
gestion des taches.

Une application decrit un processus compose de cinq familles de noeuds :

| Noeud | Role |
|---|---|
| **Metier** | Execute une commande de l'application cliente |
| **Decision** | Execute une query de l'application cliente et aiguille selon son resultat |
| **Attente** | Suspend l'instance jusqu'a une date/heure, un signal, ou le premier des deux |
| **Interactif** | Suspend l'instance jusqu'a une action humaine (avec destinataire et echeance) |
| **Sous-processus** | Demarre un processus enfant, attendu ou detache |

Le moteur **avance tant qu'il peut dans une seule transaction** : un appel unique peut
traverser une decision, plusieurs noeuds metier et un sous-processus complet. Il s'arrete
uniquement sur un noeud bloquant, une fin de processus, une erreur, ou l'epuisement du budget
de pas configure.

## Structure du depot

```
src/MachineEtatPlus/          le paquet NuGet
  Abstractions/               ce que l'application cliente implemente
  Definition/                 modele de processus et constructeur fluide
  Execution/                  moteur, contexte d'execution, dispatchers
  Model/                      instances, attentes, variables, historique
  DependencyInjection/        integration Microsoft.Extensions.DependencyInjection

tests/MachineEtatPlus.Tests/  61 tests, dont une implementation client de reference
  Infrastructure/             stockage SQLite + ordonnanceur outbox, schema de tables complet
```

`tests/MachineEtatPlus.Tests/Infrastructure/` contient une implementation cliente complete et
fonctionnelle (`SqliteProcessStore`, `SqliteJobScheduler`, `SqliteSchema`) qui sert autant de
test que d'exemple de portage vers SQL Server, PostgreSQL ou Oracle.

## Les quatre abstractions a implementer

```csharp
// 1. La connexion et la transaction, injectees par le client. Le moteur ne commit jamais.
public interface IDbSession { DbConnection Connection { get; } DbTransaction? Transaction { get; } }
public interface IDbSessionAccessor { IDbSession? Current { get; } }

// 2. La persistance. Le schema des tables est libre et defini par le client.
public interface IProcessStore { /* instances, attentes, historique */ }

// 3. La gestion de taches, externe au moteur.
public interface IProcessJobScheduler { Task ScheduleAsync(ProcessJob job, CancellationToken ct); ... }

// 4. Les traitements metier.
public interface ICommandHandler<in TCommand> { Task<string?> HandleAsync(...); }
public interface IDecisionHandler<in TQuery> { Task<string> EvaluateAsync(...); }
```

Une implementation d'`IProcessJobScheduler` qui ecrit dans une table de la meme base obtient
gratuitement la semantique **outbox** : les reveils programmes sont valides ou annules avec la
transaction metier, jamais separement.

## Exemple complet

Voir [`src/MachineEtatPlus/README.md`](src/MachineEtatPlus/README.md) — le README embarque dans
le paquet — pour la definition d'un processus, l'enregistrement dans le conteneur et l'appel
dans la transaction du client.

## Developpement

```bash
dotnet build      # netstandard2.0 + net8.0, sans avertissement
dotnet test       # 61 tests sur une base SQLite en memoire
dotnet pack src/MachineEtatPlus/MachineEtatPlus.csproj -c Release -o artifacts
```

## Licence

MIT
