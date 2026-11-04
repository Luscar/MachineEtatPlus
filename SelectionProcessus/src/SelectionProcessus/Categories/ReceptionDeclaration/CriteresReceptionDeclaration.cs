namespace SelectionProcessus.Categories.ReceptionDeclaration;

/// <summary>Informations requises pour déterminer le processus de réception d'une déclaration.</summary>
public sealed record CriteresReceptionDeclaration(TypeFormulaire TypeFormulaire) : ICriteresProcessus;
