# T2.2-A — Noyau d’autorité des identités et sessions

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche éprouve les règles d’autorité nécessaires avant d’introduire le stockage des mots de passe et le commissioning des comptes.

Elle ne constitue pas encore une authentification produit complète. La méthode d’émission de session est volontairement une API de composition de confiance appelée **après authentification** ; aucun transport ou client HMI ne doit pouvoir l’utiliser directement.

## Implémentation plateforme

Nouveau composant : `Platform.Poc.Identity.Runtime`.

Le composant :

- implémente `IClientSessionAuthority` ;
- enregistre des identités humaines nominatives sans mot de passe dans cette première micro-tranche ;
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
- ne persiste pas les sessions : la recréation de l’autorité Core exige donc une nouvelle authentification, conformément à la décision T2.

`ClientSessionReference` refuse désormais les valeurs vides.

## Limites volontaires

T2.2-A ne choisit pas encore :

- la bibliothèque Argon2id ;
- les paramètres de coût de dérivation ;
- le schéma durable des comptes et rôles ;
- le secret de commissioning du premier administrateur ;
- la réinitialisation par secret temporaire ou autorisation de récupération signée.

Ces éléments appartiennent à T2.2-B et doivent être qualifiés sans cryptographie maison.

## Recette locale

Entrée :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

La recette rejoue d’abord T0/T1 + T2.1, puis vérifie notamment :

- indépendance du runtime d’identité vis-à-vis de SQLite et gRPC ;
- deux utilisateurs nominatifs distincts ;
- unicité normalisée du nom utilisateur ;
- interdiction de `*` pour un humain ;
- permissions issues de l’autorité ;
- refus de rebinding client/cible ;
- différence d’expiration local/distant ;
- absence de prolongation par simple résolution automatique ;
- prolongation par activité humaine explicite ;
- retrait de permission effectif sur la session déjà émise ;
- révocation explicite ;
- désactivation du compte ;
- durée absolue de session ;
- ancienne session introuvable après recréation de l’autorité Core.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.2-A n’est déclaré tant que le journal `Test-T22.ps1` n’a pas été exécuté avec succès sur le poste Windows du pilote.