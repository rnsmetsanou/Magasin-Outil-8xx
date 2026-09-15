# T2.3-B — Identité d’installation, renouvellement et temps de confiance

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette micro-tranche complète la vérification cryptographique T2.3-A avec une autorité de licence durable utilisable hors ligne :

- identité d’installation créée une seule fois et persistée ;
- licence installée conservée sous sa forme signée ;
- version de renouvellement monotone ;
- temps de confiance persistant entre redémarrages ;
- détection d’un retour arrière de l’horloge ;
- récupération du temps par une autorisation signée explicite.

Elle ne raccorde pas encore la décision de licence à l’admission métier des commandes : ce raccordement appartient à T2.3-C.

## Architecture

Nouveaux contrats fournisseur-indépendants dans `Platform.Poc.Licensing.Contracts` :

- `IInstallationIdentityStore` ;
- `IInstalledLicenseStore` ;
- `ITrustedTimeStore` ;
- enregistrements d’identité, licence installée et état de temps ;
- statuts d’installation, d’évaluation et de récupération signée.

Le runtime contient :

- `InstallationIdentityService` ;
- `DurableLicenseAuthority` ;
- `OfflineTrustedTimeAuthority` ;
- `TrustedTimeRecoveryService`.

L’adaptateur `Platform.Poc.Licensing.Persistence.Sqlite` implémente les trois stores sans introduire SQLite dans les contrats ou le runtime.

## Identité d’installation V1

L’identité d’installation est générée avec **256 bits d’aléa cryptographiquement sûr** et encodée sous la forme `inst1_...`.

Elle ne dépend pas :

- d’une adresse MAC ;
- d’un numéro de disque ;
- du nom Windows ;
- d’une adresse réseau.

Une fois créée, elle est persistée comme identité autoritative de l’installation et réutilisée après redémarrage.

### Limite explicite

T2.3-B ne prétend pas que cette identité logicielle est matériellement non clonable. Le contrat permet de remplacer ultérieurement le fournisseur par un mécanisme adossé à un matériel de confiance, par exemple un fournisseur CNG utilisant un Trusted Platform Module (TPM), après qualification de l’iPC cible.

Un remplacement d’iPC reste un événement de réémission/migration gouvernée de licence ; aucune règle commerciale de transfert n’est déduite de cette implémentation.

## Licence installée et anti-rollback

Le store conserve pour chaque produit :

- la plus haute `RenewalVersion` acceptée ;
- l’enveloppe signée originale ;
- son empreinte SHA-256 ;
- l’instant d’installation ;
- une révision optimiste.

Règles :

- première licence valide : installation ;
- même enveloppe et même version : idempotence ;
- même `RenewalVersion` avec un autre contenu signé : conflit ;
- `RenewalVersion` inférieure : rollback refusé ;
- version supérieure correctement signée : renouvellement accepté.

La borne de renouvellement est rattachée au produit et à l’installation, pas seulement au `LicenseId`, afin qu’un ancien artefact signé ne puisse pas récupérer de l’autorité en changeant d’identifiant de licence.

## Temps de confiance hors ligne

Le mécanisme combine deux notions distinctes :

1. **temps UTC mural** : utilisé comme date civile et persisté sous forme de borne haute entre redémarrages ;
2. **temps monotone du processus** : `TimeProvider.GetTimestamp()` / `GetElapsedTime()`, utilisé pour vérifier que le temps ne recule pas pendant l’exécution.

La politique possède une tolérance explicite `MaximumBackwardSkew`. La recette utilise une valeur de test de deux minutes ; cette valeur n’est pas déclarée comme politique produit finale.

Un saut vers l’avant est accepté comme observation et fait progresser la borne haute. S’il entraîne l’expiration de la licence, remettre ensuite l’horloge Windows en arrière ne restitue pas la validité : l’état devient `ClockRollbackDetected`.

Cet état persiste après recréation du store.

## Récupération signée du temps

Une récupération du temps utilise un payload signé distinct contenant :

- version de format ;
- identifiant de récupération ;
- séquence strictement croissante ;
- émetteur ;
- clé de signature ;
- identité d’installation ;
- UTC attesté.

La recette compose une **clé et un émetteur distincts** de ceux de la licence pour la récupération temporelle.

Une récupération valide peut explicitement abaisser la borne haute si la séquence augmente. La même séquence ne peut pas être rejouée. Une autorisation destinée à une autre installation ou dont la signature est modifiée est refusée.

Un renouvellement de licence peut être installé pendant un état temporel incohérent en utilisant la borne effective non décroissante, mais **il ne répare pas lui-même l’horloge**. La récupération signée reste nécessaire avant de reprendre les nouvelles mutations licenciées.

## Recette T2.3-B

Commande globale :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

La section B doit notamment vérifier :

- indépendance des contrats/runtime vis-à-vis de SQLite ;
- identité d’installation stable après recréation et distincte sur une autre installation ;
- installation durable d’une licence V1 ;
- capacité signée autorisée et capacité absente refusée ;
- installation idempotente de la même enveloppe ;
- persistance après recréation ;
- renouvellement V2 ;
- conflit de deux contenus portant la même version ;
- refus du retour V2 → V1 ;
- expiration après progression du temps ;
- impossibilité de récupérer la validité en reculant l’horloge ;
- persistance de cette détection après redémarrage ;
- installation d’un renouvellement V3 sans effacer l’incohérence temporelle ;
- récupération signée liée à l’installation ;
- refus du replay de récupération ;
- refus d’une récupération pour une autre installation ;
- refus d’une signature de récupération altérée ;
- conservation de la plus haute version de renouvellement après récupération du temps ;
- absence de clé privée d’émission ou de récupération dans les artefacts SQLite.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.3-B n’est déclaré avant réception du journal `Test-T23.ps1` correspondant. T2.3-A est déjà PASS LOCAL.
