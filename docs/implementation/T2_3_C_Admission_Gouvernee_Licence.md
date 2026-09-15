# T2.3-C — Admission gouvernée par licence

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette micro-tranche raccorde l’autorité de licence durable T2.3-A/B au point d’admission des nouvelles opérations machine, sans transformer la licence en mécanisme d’arrêt rétroactif.

Les invariants qualifiés sont :

- une nouvelle mutation/commande licenciée est refusée si la licence ou la capacité requise n’est pas autoritative ;
- une opération déjà admise continue jusqu’à sa complétion observée même si la licence expire ensuite ;
- les lectures restent hors du verrou de licence des mutations ;
- les capacités de licence ne sont jamais déclarées par le client ;
- permission humaine et licence produit restent deux autorités indépendantes.

## Architecture

Un contrat générique asynchrone est ajouté dans `Platform.Poc.Foundation.Licensing` :

- `IEntitlementAdmissionAuthority` ;
- `EntitlementAdmissionDecision` ;
- `EntitlementAdmissionStatus`.

`Platform.Poc.Licensing.Runtime` implémente ce contrat avec `DurableLicenseEntitlementAuthority`, qui traduit l’état de la licence durable et du temps de confiance en décision d’admission.

`Platform.Poc.Machine.Runtime` consomme uniquement ce contrat Foundation via `LicensedToolHandlingOperations`. Il ne référence ni `Platform.Poc.Licensing.Runtime`, ni `Platform.Poc.Licensing.Persistence.Sqlite`, ni `Platform.Poc.Licensing.Cryptography`.

Le mapping `EntitlementId -> LicenseCapabilityId` est composé côté Core et ne provient pas d’un paramètre client.

## Ordre des autorités

La composition qualifiée sépare quatre responsabilités :

1. contexte de requête : sujet et client enregistrés ;
2. licence : entitlement produit évalué à chaque nouvelle admission ;
3. permission humaine : `tool.prepare` / `tool.load` évaluées par l’autorité d’identité ;
4. runtime machine : préconditions et effet technologique.

Une licence valide ne donne donc jamais une permission humaine. Inversement, posséder `tool.prepare` ou `tool.load` ne contourne pas une licence absente, expirée ou temporellement incohérente.

## États explicables

La décision d’admission distingue notamment `MissingLicense`, entitlement absent, `LicenseExpired`, `LicenseNotYetValid`, `LicenseTimeIncoherent`, licence invalide et autorité indisponible.

## Administration de licence

L’import d’un artefact déjà signé passe par `GovernedLicenseAdministrationService` et exige explicitement `license.install`. Cette permission n’est pas une capacité de signature : aucune clé privée d’émission n’est ajoutée à la machine.

## Opérations déjà admises

`LicensedToolHandlingOperations.StartAsync(...)` évalue la licence avant de déléguer au reste de l’admission. En revanche `EvaluateCompletionAsync(...)` et `ReconcileCompletionAsync(...)` ne réévaluent pas la licence.

La licence est donc une autorité d’**admission d’un nouvel effet**, pas une autorité de coupure d’une opération physique déjà admise. La fin d’une opération continue d’être établie à partir des preuves machine du runtime d’opération.

## Lectures

Les runtimes de requête ne dépendent pas de `IEntitlementAdmissionAuthority`. L’expiration d’une licence de mutation ne coupe donc pas implicitement la consultation.

## Validation locale reçue

Commande globale :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Le journal reçu le 15 septembre 2026 confirme :

- absence de licence : nouvelle commande refusée avant le runtime machine ;
- `license.install` obligatoire pour importer/renouveler une licence ;
- licence valide sans permission humaine : commande refusée ;
- permission humaine + licence/capacité valide : admission jusqu’au runtime machine ;
- opération admise avant expiration : complétion encore permise après expiration ;
- aucune nouvelle évaluation de licence lors de la complétion ;
- nouvelle commande après expiration : refus explicite `LicenseExpired` avant deuxième soumission physique ;
- recul d’horloge : refus des nouvelles admissions sans révocation rétroactive ;
- HMI, API et OPC UA : même décision autoritative lorsqu’ils atteignent le même point d’admission ;
- aucun entitlement/capacité de licence déclaré dans `OperationRequestContext` ;
- runtime de lecture hors du verrou de mutation licenciée ;
- `Machine.Runtime` indépendant du runtime de licence, de SQLite et de l’adaptateur cryptographique concret.

La même exécution conserve T0/T1, T2.1, T2.2, T2.3-A et la révision courante de T2.3-B verts.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

T2.3-C qualifie le raccordement de l’autorité de licence au point d’admission. La composition réelle des composants de licence dans `MagasinOutil.CoreHost` est traitée séparément par T2.3-D avant clôture complète de T2.3.
