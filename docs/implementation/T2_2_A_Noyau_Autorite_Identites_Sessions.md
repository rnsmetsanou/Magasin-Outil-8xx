# T2.2-A — Noyau d’autorité des identités et sessions

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche éprouve les règles d’autorité nécessaires avant d’introduire le stockage durable des mots de passe et le commissioning des comptes.

La méthode d’émission de session reste volontairement une API de composition de confiance appelée **après authentification** ; aucun transport ou client HMI ne doit pouvoir l’utiliser directement.

## Implémentation plateforme

Composant : `Platform.Poc.Identity.Runtime`.

Le composant :

- implémente `IClientSessionAuthority` ;
- gère des identités humaines nominatives ;
- interdit la permission générique `*` pour les identités humaines ;
- impose l’unicité du nom utilisateur après normalisation ;
- émet une référence de session opaque aléatoire ;
- lie la session au sujet, au client, à la cible et au type d’accès ;
- applique 30 minutes d’inactivité pour l’interactif local, 10 minutes pour l’interactif distant et 8 heures de durée absolue selon la politique T2 validée ;
- ne considère pas une résolution/actualisation automatique comme activité humaine ;
- permet de déclarer explicitement une activité humaine ;
- permet la révocation d’une session ;
- invalide l’autorité lorsque l’identité est désactivée ;
- reconstruit le `SubjectContext` à chaque résolution à partir des permissions courantes afin qu’un retrait de droit soit effectif avant la prochaine admission ;
- ne persiste pas les sessions : la recréation de l’autorité Core exige une nouvelle authentification.

`ClientSessionReference` refuse les valeurs vides.

## Validation locale reçue

Commande exécutée :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Environnement observé : Windows 10.0.22631, `win-x64`, .NET SDK 10.0.400.

Le journal du 15 septembre 2026 confirme :

- régression T0/T1 verte ;
- T2.1-A, T2.1-B et T2.1-C toujours PASS LOCAL ;
- indépendance du runtime d’identité vis-à-vis de SQLite et gRPC ;
- deux utilisateurs nominatifs distincts ;
- unicité normalisée du nom utilisateur ;
- interdiction de `*` pour un humain ;
- permissions issues de l’autorité et non du client ;
- refus de rebinding client/cible ;
- différence d’expiration local/distant ;
- absence de prolongation par simple résolution automatique ;
- prolongation par activité humaine explicite ;
- retrait de permission effectif sur une session déjà émise ;
- révocation explicite et non réversible ;
- désactivation invalidant l’autorité existante ;
- activité humaine toutes les 20 minutes restant valide jusqu’à 7 h 40 ;
- expiration absolue à 8 heures malgré cette activité régulière ;
- ancienne session introuvable après recréation de l’autorité Core.

## État

**PASS LOCAL.**

Cette preuve reste une qualification en simulation Windows. Elle ne qualifie ni stockage durable des comptes, ni dérivation de mot de passe, ni commissioning, ni récupération signée, ni machine Beckhoff réelle.

## Suite

T2.2-B introduit des contrats de comptes locaux durables, un adaptateur de dérivation Argon2id et un adaptateur SQLite séparé, sans coupler le runtime d’identité aux fournisseurs. Le délai progressif après échecs d’authentification reste un sous-jalon ultérieur : le seuil de cinq échecs est acquis, mais la courbe/durée de temporisation n’est pas encore figée.
