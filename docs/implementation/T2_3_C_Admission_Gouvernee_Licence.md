# T2.3-C — Admission gouvernée par licence

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette micro-tranche raccorde l’autorité de licence durable T2.3-A/B au point d’admission des nouvelles opérations machine, sans transformer la licence en mécanisme d’arrêt rétroactif.

Les invariants visés sont :

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

`Platform.Poc.Machine.Runtime` consomme uniquement ce contrat Foundation via `LicensedToolHandlingOperations`. Il ne référence ni :

- `Platform.Poc.Licensing.Runtime` ;
- `Platform.Poc.Licensing.Persistence.Sqlite` ;
- `Platform.Poc.Licensing.Cryptography`.

Le mapping `EntitlementId -> LicenseCapabilityId` est composé côté Core et ne provient pas d’un paramètre client.

## Ordre des autorités

La composition qualifiée sépare quatre responsabilités :

1. contexte de requête : sujet et client enregistrés ;
2. licence : entitlement produit évalué à chaque **nouvelle** admission ;
3. permission humaine : `tool.prepare` / `tool.load` évaluées par l’autorité d’identité ;
4. runtime machine : préconditions et effet technologique.

Une licence valide ne donne donc jamais une permission humaine. Inversement, posséder `tool.prepare` ou `tool.load` ne contourne pas une licence absente, expirée ou temporellement incohérente.

## États explicables

La décision d’admission distingue notamment :

- `MissingLicense` ;
- entitlement absent de la licence ;
- `LicenseExpired` ;
- `LicenseNotYetValid` ;
- `LicenseTimeIncoherent` ;
- licence invalide ou autorité indisponible.

T2.3-C rend également `Expired` et `NotYetValid` explicites dans l’évaluation durable, sans modifier les garanties cryptographiques T2.3-A/B.

## Administration de licence

L’import d’un artefact déjà signé passe par `GovernedLicenseAdministrationService` et exige explicitement la permission :

`license.install`

Cette permission n’est pas une capacité de signature. Aucune clé privée d’émission n’est ajoutée à la machine ; elle autorise uniquement l’import d’un fichier qui doit ensuite satisfaire les contrôles cryptographiques et durables T2.3-A/B.

## Opérations déjà admises

`LicensedToolHandlingOperations.StartAsync(...)` évalue la licence avant de déléguer au reste de l’admission.

En revanche :

- `EvaluateCompletionAsync(...)` ;
- `ReconcileCompletionAsync(...)`

ne réévaluent pas la licence. La licence est une autorité d’**admission d’un nouvel effet**, pas une autorité de coupure d’une opération physique déjà admise. La fin d’une opération continue d’être établie à partir des preuves machine fraîches du runtime d’opération.

## Lectures

Les runtimes de requête ne dépendent pas de `IEntitlementAdmissionAuthority`. L’expiration d’une licence de mutation ne coupe donc pas implicitement la consultation. Une éventuelle capacité de lecture explicitement licenciée devra être modélisée comme telle, et non déduite de ce verrou d’opération.

## Recette T2.3-C

Commande globale :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

La section C vérifie notamment :

- refus d’une nouvelle commande lorsqu’aucune licence n’est installée ;
- aucun appel au runtime machine sous-jacent en cas de refus licence ;
- refus d’installation par un sujet sans `license.install` ;
- installation par un sujet possédant `license.install` ;
- une licence valide ne donne pas `tool.prepare` à un utilisateur qui ne le possède pas ;
- utilisateur autorisé + licence/capacité valide : admission jusqu’au runtime machine ;
- expiration après admission : l’opération en cours peut toujours se compléter ;
- la complétion n’effectue aucune nouvelle évaluation de licence ;
- une nouvelle commande après expiration est refusée avant effet physique ;
- un recul d’horloge produit le même refus d’admission cohérent ;
- HMI, API et OPC UA reçoivent la même décision lorsque leurs requêtes atteignent cette autorité commune ;
- `OperationRequestContext` ne contient aucune capacité ou autorité de licence déclarée par le client ;
- le runtime de lecture reste hors du verrou de licence des mutations ;
- `Machine.Runtime` reste indépendant des projets concrets de licence, SQLite et cryptographie.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.3-C ni clôture T2.3 n’est déclaré avant réception du journal `Test-T23.ps1` contenant cette nouvelle section avec les régressions précédentes toujours vertes.
