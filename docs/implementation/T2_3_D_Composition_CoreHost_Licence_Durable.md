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

La tolérance au recul d’horloge est désormais passée explicitement au CoreHost dans le profil de simulation.

La recette utilise **120 secondes uniquement comme fixture de qualification**. Cette valeur ne devient pas une politique produit finale.

## Recette

`eng/Test-T23D.ps1` :

- restaure le pilote uniquement depuis les paquets plateforme locaux produits par la recette ;
- compile le pilote ;
- démarre le CoreHost avec un nouveau répertoire d’état ;
- vérifie la création non vide de `licensing.db` ;
- extrait l’identité `inst1_...` annoncée par le CoreHost ;
- vérifie l’état initial `license=Missing` ;
- vérifie qu’aucune clé publique d’émetteur n’est embarquée dans la simulation ;
- redémarre le CoreHost sur le même répertoire d’état ;
- vérifie que l’identité d’installation est strictement identique après redémarrage ;
- contrôle que les paquets concrets de licence sont composés uniquement côté CoreHost ;
- contrôle que `MagasinOutil.ReadClient` et `MagasinOutil.Platform` n’embarquent pas l’autorité de licence concrète, l’adaptateur cryptographique ou SQLite.

La recette globale reste :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

T2.3 ne sera déclaré clôturé qu’après réception d’un journal où T2.3-D passe avec A, B, C et toutes les régressions précédentes toujours vertes.
