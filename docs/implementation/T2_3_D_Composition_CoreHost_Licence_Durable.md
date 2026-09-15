# T2.3-D — Composition réelle de la licence durable dans CoreHost

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette dernière micro-tranche de T2.3 vérifie que le pilote `Magasin-Outil-8xx` consomme réellement les composants communs de licence qualifiés en T2.3-A/B/C dans son processus `MagasinOutil.CoreHost`, au lieu de laisser ces composants uniquement dans des projets de test plateforme.

## Composition pilote

`MagasinOutil.CoreHost` référence les paquets communs suivants :

- `Platform.Poc.Licensing.Contracts` ;
- `Platform.Poc.Licensing.Runtime` ;
- `Platform.Poc.Licensing.Cryptography` ;
- `Platform.Poc.Licensing.Persistence.Sqlite`.

Au démarrage du profil `--simulation`, le CoreHost :

1. initialise le store d’admission durable existant ;
2. initialise `licensing.db` via `SqliteLicensingStateStore` ;
3. crée ou recharge l’identité d’installation via `InstallationIdentityService` ;
4. compose `OfflineTrustedTimeAuthority` avec une tolérance fournie explicitement par la recette ;
5. compose `LicenseVerificationService` et `DurableLicenseAuthority` ;
6. évalue l’état courant de licence avant d’annoncer son état de disponibilité.

## Clés de signature dans la simulation

La composition T2.3-D utilise volontairement **zéro clé publique d’émetteur approuvée**.

Cela signifie :

- aucune clé privée n’est intégrée au pilote ;
- aucune clé publique de production n’est inventée ou embarquée pour la qualification ;
- aucune licence signée ne peut être acceptée par accident dans ce profil de simulation ;
- l’injection des clés publiques approuvées reste une responsabilité de configuration/déploiement ultérieure.

T2.3-D qualifie donc la frontière de composition et la durabilité de l’identité, pas l’outil d’émission de production.

## Politique temporelle

La tolérance au recul d’horloge est passée explicitement au CoreHost dans le profil de simulation.

La recette utilise **120 secondes uniquement comme fixture de qualification**. Cette valeur ne devient pas une politique produit finale.

## Validation locale reçue

Commande globale exécutée :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Le journal reçu le 15 septembre 2026 confirme :

- création réelle et non vide de `licensing.db` par `MagasinOutil.CoreHost` ;
- création d’une identité d’installation cryptographique sans clé privée d’émetteur embarquée ;
- démarrage du profil de simulation avec zéro clé publique de production approuvée ;
- redémarrage du CoreHost sur le même répertoire d’état avec conservation stricte de l’identité d’installation ;
- présence des composants communs de licence, de vérification cryptographique et de persistance SQLite dans la composition du CoreHost ;
- absence de l’autorité de licence concrète, de la cryptographie et du fournisseur SQLite dans `MagasinOutil.ReadClient` ;
- indépendance du module métier/plateforme du pilote vis-à-vis de la composition concrète de licence.

La même exécution conserve **T0/T1, T2.1, T2.2 et T2.3-A/B/C verts**.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

T2.3-D qualifie la composition réelle de l’autorité de licence dans le pilote et clôt le dernier écart entre les preuves plateforme et le processus `MagasinOutil.CoreHost`.

Ce PASS ne qualifie pas :

- une clé publique de production réelle ;
- l’outil d’émission WM de production ;
- les règles commerciales de durée/transfert ;
- un Trusted Platform Module (TPM) ou autre scellement matériel ;
- le PC industriel cible ;
- un raccordement Beckhoff réel.

Avec T2.3-A/B/C également PASS LOCAL, **T2.3 est clôturé en simulation Windows**.
