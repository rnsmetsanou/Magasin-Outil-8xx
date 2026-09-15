# T2.2-C — Limitation des tentatives et commissioning du premier administrateur

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche complète le chemin initial des identités locales avec deux protections distinctes :

1. limitation durable des tentatives d’authentification répétées ;
2. commissioning à usage unique du premier administrateur, sans compte universel ni mot de passe par défaut.

## C1 — limitation des tentatives

Le runtime `RateLimitedLocalAuthenticator` enveloppe l’authentification locale T2.2-B. Les règles qualifiées sont :

- compteur rattaché au nom utilisateur normalisé et non à une adresse IP ;
- état de limitation persistant au redémarrage du store ;
- début du délai progressif au cinquième échec consécutif ;
- remise à zéro après authentification réussie ;
- impossibilité de contourner un délai actif avec le bon mot de passe ;
- même comportement externe pour identités connues et inconnues ;
- aucun verrouillage permanent automatique.

La courbe exacte reste configurable. La recette utilise `1 s → 2 s → 4 s → 8 s` uniquement comme fixture de qualification et non comme décision produit.

## C2 — commissioning du premier administrateur

Le commissioning repose sur `IFirstAdministratorCommissioningStore` et `FirstAdministratorCommissioningService`.

Règles qualifiées :

- secret d’activation spécifique à l’installation ;
- seul son dérivé Argon2id est persisté ;
- aucun administrateur universel ou mot de passe par défaut ;
- provisioning refusé si un compte existe déjà ;
- activation non reprovisionnable silencieusement ;
- autre identifiant d’installation ou mauvais secret refusé ;
- aucun compte partiel après échec ;
- création du premier compte et consommation du secret dans une même transaction SQLite ;
- autorisation consommée à usage unique ;
- deux tentatives concurrentes convergent vers exactement un premier administrateur ;
- permissions administratives explicites sans `*`, `tool.prepare` ni `tool.load` ;
- authentification hors ligne du compte commissionné après recréation du store ;
- secret d’activation et mot de passe administrateur absents en clair des artefacts SQLite.

## Validation locale

Commande :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Le journal reçu le 15 septembre 2026 confirme tous les scénarios C1/C2 ainsi que la régression T0/T1, T2.1, T2.2-A et la révision durcie de T2.2-B.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

La robustesse de persistance face à un flood de noms d’utilisateur inexistants est traitée séparément dans T2.2-D. Une exposition réseau devra en outre composer le throttling par compte avec une limitation indépendante par source ou contexte de transport.
