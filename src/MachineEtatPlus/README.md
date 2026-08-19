# MachineEtatPlus

Moteur de processus embarquable pour applications .NET.

Une application cliente decrit un processus compose de noeuds **metier** (commande), **decision**
(query), **attente** (date/heure ou signal), **interactif** et **sous-processus**. Le moteur fait
avancer l'instance aussi loin qu'il le peut **dans la transaction fournie par l'application** ;
il ne valide ni n'annule jamais cette transaction.

Tout ce qui touche a l'infrastructure reste une abstraction implementee cote client :

| Abstraction | Role |
|---|---|
| `IDbSession` / `IDbSessionAccessor` | Connexion et transaction injectees par le client |
| `IProcessStore` | Persistance ; le schema des tables est libre et defini par le client |
| `IProcessJobScheduler` | Gestion de taches externe (echeances, reprises) |
| `ICommandHandler<T>` / `IDecisionHandler<T>` | Traitements metier et requetes de decision |

## Installation

```
dotnet add package MachineEtatPlus
```

## Definir un processus

```csharp
ProcessDefinition commande = ProcessDefinition.Create("commande", version: 1)
    .Named("Traitement d'une commande")
    .Start("debut", to: "solvabilite")

    .Decision("solvabilite", d => d
        .Query(ctx => new ClientSolvableQuery(ctx.GetVariable<int>("clientId")))
        .When("oui", "reserver")
        .When("non", "refuser"))

    .Business("reserver", b => b
        .Command(ctx => new ReserverStockCommand(ctx.GetVariable<int>("commandeId")))
        .To("paiement"))

    .Wait("paiement", w => w
        .ForSignal("paiement-recu")
        .After(TimeSpan.FromDays(2))
        .StorePayloadIn("paiement")
        .OnSignal("expedition")
        .OnTimer("refuser"))

    .SubProcess("expedition", "expedition", s => s
        .Input((ctx, vars) => vars.Set("commandeId", ctx.GetVariable<int>("commandeId")))
        .Output((ctx, childVars) => ctx.SetVariable("suivi", childVars.Get<string>("numeroSuivi")))
        .To("fin"))

    .End("fin", outcome: "livree")
    .End("refuser", outcome: "refusee")
    .Build();
```

## Enregistrer le moteur

```csharp
services.AddMachineEtatPlus(o => o.MaxStepsPerTransaction = 100)
    .AddDefinition(commande)
    .UseStore<SqlProcessStore>()          // vos tables
    .UseJobScheduler<OutboxJobScheduler>() // votre gestion de taches
    .AddDecisionHandler<ClientSolvableQuery, ClientSolvableHandler>()
    .AddCommandHandler<ReserverStockCommand, ReserverStockHandler>();
```

## Avancer dans la transaction du client

```csharp
await using DbConnection connection = new SqlConnection(connectionString);
await connection.OpenAsync(ct);
using DbTransaction transaction = connection.BeginTransaction();

using (sessions.BeginSession(new DbSession(connection, transaction)))
{
    ProcessExecutionResult result = await engine.StartAsync(
        new ProcessStartRequest("commande").With("clientId", 42).With("commandeId", 1001), ct);

    // result.Status : Waiting, Completed, Faulted...
    // result.CurrentNodeId : noeud sur lequel l'instance s'est arretee
}

transaction.Commit();   // c'est le client qui valide
```

Reprise sur signal, interaction ou echeance :

```csharp
await engine.SignalAsync(new SignalRequest("paiement-recu")
{
    CorrelationKey = "CMD-1001",
    Payload = new PaiementRecu(montant: 249.90m),
}, ct);

await engine.CompleteInteractionAsync(
    new InteractionCompletionRequest(instanceId, outcome: "approuve") { Payload = formulaire }, ct);

await engine.FireDueTimersAsync(maxCount: 50, ct);   // depuis votre planificateur
```

## Regles d'avancement

- Le moteur enchaine les noeuds tant qu'aucun ne bloque : un seul appel peut traverser une
  decision, plusieurs noeuds metier et un sous-processus complet.
- Il s'arrete sur un noeud d'attente, un noeud interactif, un sous-processus non termine, un
  noeud de fin, une erreur, ou l'epuisement de `MaxStepsPerTransaction` — auquel cas une tache
  de poursuite est confiee a `IProcessJobScheduler` et `ProcessExecutionResult.Deferred` vaut `true`.
- Les appels a l'ordonnanceur ont lieu dans la transaction du client : une implementation qui
  ecrit dans une table de la meme base obtient la semantique *outbox* sans effort.
- Par defaut, une exception levee par un traitement client est propagee (`PropagateHandlerExceptions`),
  car la transaction est alors vraisemblablement compromise.

## Licence

MIT
