# T2.3-A — Contrat et vérification de licence hors ligne signée

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.

## Objectif

Cette micro-tranche qualifie le format et la vérification cryptographique d’une licence utilisable hors ligne, sans clé privée d’émission dans le runtime machine et sans dépendance Internet.

Elle ne traite pas encore la persistance de la licence installée, le rollback de renouvellement, le temps de confiance ni le raccordement à l’admission des commandes ; ces points appartiennent à T2.3-B/C.

## Architecture

Trois composants plateforme sont séparés :

- `Platform.Poc.Licensing.Contracts` : payload de licence, enveloppe signée, clé publique approuvée, interfaces de vérification et statuts ;
- `Platform.Poc.Licensing.Runtime` : représentation canonique, codec de fichier, registre de clés approuvées et règles sémantiques de vérification ;
- `Platform.Poc.Licensing.Cryptography` : adaptateur cryptographique concret de vérification.

Les contrats et le runtime ne dépendent pas du projet cryptographique concret. L’interface de signature du runtime expose uniquement la **vérification** ; aucune opération de signature par clé privée n’est fournie au Core ou à la HMI.

## Format signé V1

Le payload canonique contient :

- version du format ;
- `LicenseId` ;
- version de renouvellement ;
- identifiant de l’émetteur ;
- identifiant de la clé de signature ;
- produit ;
- identité d’installation ;
- capacités licenciées ;
- début de validité UTC ;
- expiration UTC optionnelle.

Les capacités sont triées ordinalement avant sérialisation et les doublons sont refusés. Le payload JSON est produit dans un ordre de propriétés fixe et avec un format UTC déterministe. Après vérification de signature, le runtime réencode le payload et refuse une représentation signée qui n’est pas canonique.

L’enveloppe de fichier contient la version d’enveloppe, l’algorithme, l’identifiant de clé, le payload canonique encodé en Base64 et la signature encodée en Base64.

## Choix cryptographique de qualification

Candidat T2.3-A : **ECDSA P-256 avec SHA-256**, signature au format IEEE P1363, via `System.Security.Cryptography` de .NET 10.

Identifiant d’algorithme :

`ecdsa-p256-sha256-p1363`

La clé publique approuvée est importée au format X.509 `SubjectPublicKeyInfo` DER. Le registre associe explicitement :

- identifiant de clé ;
- identifiant d’émetteur ;
- algorithme ;
- format de clé publique ;
- matériau public.

Cela empêche une clé approuvée de se réattribuer silencieusement à un autre émetteur dans le payload signé.

Le format reste agile : l’algorithme fait partie de l’enveloppe et du registre de clés, afin qu’un futur algorithme puisse être ajouté sans modifier les contrats métier.

## Clé privée

Aucune clé privée d’émission n’est ajoutée aux projets `src` de la plateforme ou au pilote.

La recette de qualification génère uniquement une clé privée ECDSA **éphémère dans le projet de test**, afin de produire des fixtures signées. Une architecture d’outil d’émission WM séparé sera traitée ultérieurement ; la clé privée de production ne doit jamais être distribuée avec le produit machine.

## Recette T2.3-A

Commande globale :

```powershell
.\eng\Test-T23.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

La recette rejoue d’abord T0/T1, T2.1 et T2.2, puis vérifie notamment :

- contrats de licence indépendants de l’adaptateur cryptographique concret ;
- runtime indépendant de cet adaptateur ;
- contrat de signature limité à la vérification ;
- canonicalité déterministe quelle que soit l’ordre d’entrée des capacités ;
- aller-retour du fichier de licence hors ligne ;
- licence correctement signée acceptée avec une clé publique approuvée ;
- capacités provenant uniquement du payload signé ;
- altération du contenu refusée ;
- altération de signature refusée ;
- clé inconnue refusée ;
- algorithme inconnu refusé ;
- impossibilité de rebinder un payload signé vers un autre identifiant de clé ;
- cohérence émetteur/clé ;
- mauvais produit refusé ;
- mauvaise installation refusée ;
- début de validité et expiration respectés ;
- payload signé mais non canonique refusé ;
- incompatibilité algorithme/clé refusée ;
- capacités dupliquées refusées avant signature.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.3-A n’est déclaré avant réception du journal `Test-T23.ps1`. Cette tranche qualifie le contrat et la vérification cryptographique, pas encore le temps de confiance ni l’autorité durable de licence.
