# T2.3-B — Identité d’installation, renouvellement et temps de confiance

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette micro-tranche complète T2.3-A avec une autorité de licence durable utilisable hors ligne : identité d’installation persistante, licence signée installée, version de renouvellement monotone, temps de confiance durable, détection de recul d’horloge et récupération temporelle signée.

Elle ne raccorde pas encore la décision de licence à l’admission métier ; ce point appartient à T2.3-C.

## Architecture

Les contrats fournisseur-indépendants de `Platform.Poc.Licensing.Contracts` couvrent `IInstallationIdentityStore`, `IInstalledLicenseStore`, `ITrustedTimeStore`, les enregistrements durables et les statuts d’installation/évaluation/récupération. Le runtime contient `InstallationIdentityService`, `DurableLicenseAuthority`, `OfflineTrustedTimeAuthority` et `TrustedTimeRecoveryService`. SQLite reste dans `Platform.Poc.Licensing.Persistence.Sqlite`.

## Identité d’installation V1

L’identité est générée avec **256 bits d’aléa cryptographiquement sûr** et encodée `inst1_...`. Elle ne dépend ni d’une adresse MAC, ni d’un disque, ni du nom Windows, ni du réseau. Elle est créée une seule fois et reste stable après recréation du store.

Cette identité logicielle n’est pas déclarée matériellement non clonable. Un futur fournisseur adossé à un matériel de confiance peut remplacer l’implémentation sans modifier les contrats. Un remplacement d’iPC reste un événement de migration/réémission gouvernée.

## Licence installée et anti-rollback

Le store conserve la plus haute `RenewalVersion`, l’enveloppe signée originale, son empreinte SHA-256, l’instant d’installation et une révision optimiste.

Règles qualifiées : première installation valide ; idempotence de la même enveloppe ; conflit si une même version porte un autre contenu ; refus d’une version plus basse ; acceptation d’une version supérieure correctement signée. La borne de renouvellement appartient au produit/installation et ne se réinitialise pas avec un nouvel identifiant de licence.

## Temps de confiance hors ligne

Le mécanisme distingue :

1. le temps UTC mural, utilisé pour les périodes de licence et persisté comme borne haute ;
2. le temps monotone de `TimeProvider`, utilisé pendant le processus pour empêcher un recul silencieux.

Un saut vers l’avant progresse la borne haute. Un retour arrière ultérieur ne restitue pas la validité et produit un état d’incohérence durable. Une récupération explicite signée et liée à l’installation peut réinitialiser la borne avec une séquence strictement croissante.

Un renouvellement plus récent peut être installé pendant cet état de récupération, mais il ne répare pas à lui seul l’incohérence temporelle.

## Validation locale reçue

Commande :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Le journal reçu le 15 septembre 2026 confirme :

- contrats et runtime durables indépendants de SQLite ;
- identité d’installation stable et distincte entre installations ;
- clés d’émission de licence et de récupération temporelle distinctes dans la composition de qualification ;
- installation durable V1 et évaluation d’une capacité signée ;
- refus d’une capacité absente ;
- réinstallation idempotente ;
- persistance après recréation du store ;
- renouvellement vers une version supérieure et prise d’autorité des nouvelles capacités ;
- conflit sur même version/contenu différent ;
- refus du rollback vers une ancienne version signée ;
- expiration après progression du temps ;
- impossibilité de retrouver la validité par recul de l’horloge ;
- persistance de la détection après recréation ;
- installation d’un renouvellement plus récent sans effacer l’incohérence temporelle ;
- récupération temporelle signée, liée à l’installation et non rejouable ;
- refus d’une récupération destinée à une autre installation ou dont la signature est modifiée ;
- conservation de la plus haute version de renouvellement après récupération ;
- persistance finale de l’identité, de V3 et du temps récupéré ;
- absence de clé privée d’émission ou de récupération dans les artefacts SQLite.

La même exécution conserve **T0/T1, T2.1, T2.2 et T2.3-A verts**.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

Ce PASS ne qualifie pas encore un TPM, un outil d’émission de production, les règles commerciales de transfert, ni le raccordement des licences aux admissions machine. Ces points restent respectivement des sujets produit/matériel ou T2.3-C.
