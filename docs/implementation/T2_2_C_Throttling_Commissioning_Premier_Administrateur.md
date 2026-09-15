# T2.2-C — Limitation des tentatives et commissioning du premier administrateur

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche complète le chemin initial des identités locales avec deux protections distinctes :

1. limitation durable des tentatives d’authentification répétées ;
2. commissioning à usage unique du premier administrateur, sans compte universel ni mot de passe par défaut.

Elle ne couvre pas encore le secret temporaire de réinitialisation de mot de passe ni la récupération signée du dernier administrateur.

## C1 — limitation des tentatives

Nouveaux contrats fournisseur-indépendants :

- `ProgressiveAuthenticationDelayPolicy` ;
- `AuthenticationThrottleRecord` ;
- `IAuthenticationThrottleStore` ;
- `ProtectedLocalAuthenticationResult`.

Le runtime `RateLimitedLocalAuthenticator` enveloppe l’authentification locale T2.2-B. Les règles sont :

- compteur rattaché au nom utilisateur normalisé, et non à une adresse IP ;
- état de limitation persistant au redémarrage du store ;
- seuil pilote acquis : le délai progressif commence au cinquième échec consécutif ;
- authentification réussie : remise à zéro de l’état d’échec ;
- un mot de passe correct ne contourne pas un délai déjà actif ;
- utilisateur inconnu et compte connu suivent le même mécanisme de limitation et n’exposent pas de différence de statut avant/après seuil ;
- aucun verrouillage permanent automatique n’est introduit dans cette tranche.

La **courbe exacte de temporisation produit reste configurable et à valider**. La recette utilise uniquement comme fixture de qualification `1 s → 2 s → 4 s → 8 s` afin de prouver la mécanique sans transformer ces valeurs en décision produit.

Le store SQLite `SqliteIdentitySecurityStore` persiste `consecutive_failures`, `retry_not_before_utc` et une révision par identifiant normalisé.

## C2 — commissioning du premier administrateur

Le commissioning repose sur `IFirstAdministratorCommissioningStore` et `FirstAdministratorCommissioningService`.

Règles :

- le secret d’activation est spécifique à une installation ;
- il est injecté par un chemin de provisioning de confiance distinct de l’interface applicative ordinaire ;
- seul son dérivé Argon2id est persisté ; le secret en clair n’est jamais enregistré dans SQLite ;
- le provisioning est refusé si un compte existe déjà ;
- une autorisation d’activation ne peut pas être reprovisionnée silencieusement ;
- un identifiant d’installation différent ne peut pas consommer l’autorisation ;
- un mauvais secret ne crée aucun compte partiel ;
- création du premier compte et consommation de l’autorisation sont effectuées dans une même transaction SQLite ;
- deux tentatives concurrentes doivent converger vers exactement un premier administrateur ;
- le premier administrateur reçoit un ensemble explicite de permissions d’administration côté serveur, sans `*` et sans commande machine implicite `tool.prepare` / `tool.load` ;
- l’autorisation consommée ne peut pas créer un deuxième administrateur.

Le sujet du premier administrateur est généré côté Core. Le client de commissioning ne fournit pas ses permissions.

## Recette locale

Commande :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Après les régressions T0/T1, T2.1, T2.2-A et T2.2-B, la section T2.2-C vérifie notamment :

- quatre échecs sans délai, puis délai au cinquième ;
- impossibilité de contourner le délai avec le bon mot de passe ;
- conservation du délai après recréation du store ;
- remise à zéro après authentification réussie ;
- même comportement de limitation pour un nom utilisateur inconnu ;
- absence d’administrateur avant commissioning ;
- provisioning d’un secret d’activation spécifique à l’installation ;
- absence de ce secret en clair dans les artefacts SQLite ;
- refus d’une autre installation et d’un mauvais secret ;
- création atomique du premier administrateur ;
- permissions administratives explicites et absence de commande machine implicite ;
- consommation durable à usage unique ;
- authentification hors ligne de l’administrateur après recréation du store ;
- absence du mot de passe administrateur en clair ;
- refus d’un nouveau provisioning après existence d’un compte ;
- exactement un gagnant lors de deux commissionings concurrents.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.2-C n’est déclaré avant réception du journal local. Les durées `1/2/4/8 s` sont uniquement une fixture de test ; elles ne constituent pas la politique produit validée.
