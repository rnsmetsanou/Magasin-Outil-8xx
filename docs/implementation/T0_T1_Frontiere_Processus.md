# T0/T1 — Contrats et frontière de processus

14 septembre 2026. Implémentation de développement sur `pilot/t0-t1-contracts-process-boundary`. Compilation et qualification Windows en attente au moment de ce premier commit ; ce document sera actualisé avec les résultats.

## Ce qui change

Le pilote consomme des paquets privés `Platform.Poc.*`, version `0.1.0-pilot.t01.1`. Aucun code source privé de la plateforme n'est recopié dans ce dépôt. SQLite n'est pas introduit dans T0/T1. Le contrat de persistance reste indépendant d'un moteur ; son implémentation et sa qualification appartiennent à T2.

- `MagasinOutil.Platform` compose le simulateur de connectivité et le coordinateur de sessions de la plateforme, puis projette le magasin 8xx dans le contrat commun de lecture.
- `MagasinOutil.CoreHost` possède cette composition et expose uniquement la lecture sur un tube nommé Windows.
- `MagasinOutil.ReadClient` consomme le HMI Runtime et le transport communs. C'est un client de diagnostic, sans runtime Machine/Application concret, sans PLC et sans état magasin faisant autorité.
- `MagasinOutil.Pilot.slnx` prépare cette composition indépendamment de la solution Avalonia existante.

Le simulateur 8xx reste propriétaire de son inventaire statique : les 140 adresses, les trois exclusions et les huit racks restent spécifiques au pilote. Les positions d'outils viennent de cet inventaire simulé, pas d'une observation physique Beckhoff ou du moteur d'opérations outil de la plateforme. Le raccordement des opérations est prévu en T3.

## Garanties et limites

Le cœur ouvre sa session Machine une fois au démarrage. Une lecture ou une recréation de client ne la crée ni ne la ferme. L'identité retournée contient cible, incarnation du runtime, identifiant de session et génération. La génération peut repartir à 1 après redémarrage ; l'incarnation distingue alors les deux autorités.

Le contrat distingue outil affecté et présence à la place. Les mesures sont typées par grandeur et unité de référence ; longueur et usure sont exprimées en millimètres. Le snapshot indique la provenance simulée, la qualité, la fraîcheur et l'heure de lecture. Il ne fabrique pas d'horodatage source. Sa cohérence atomique est celle du snapshot en mémoire du simulateur, jamais une promesse d'atomicité PLC.

Le transport gRPC utilise des messages JSON UTF-8 explicitement sérialisés dans l'adaptateur. Il ne s'agit ni de gRPC-Web ni d'un schéma Protobuf. Le contrat métier reste sans dépendance gRPC. Le serveur accepte la version exacte `1.0`, refuse une autre cible et n'expose aucune méthode d'écriture. Chaque lecture distante a une échéance de cinq secondes. Une lecture échouée invalide le snapshot courant du HMI Runtime.

Le tube est limité au même compte Windows (`CurrentUserOnly`). Cela permet de qualifier la séparation des processus, **pas** les comptes nominatifs de l'application. Le nom du client sert au diagnostic et ne donne aucun droit. Les identités de services distinctes et leurs règles d'accès attendent T2 et la configuration de déploiement. Aucun point TCP n'est ouvert par cet hôte.

L'interface Avalonia reste le prototype autonome existant ; elle n'est pas encore raccordée au nouveau cœur. La HMI web, l'OPC UA et Fleet suivent les lots T4/T5. Aucune commande distante, licence produit, persistance durable ou conformité CRA n'est revendiquée ici.

## Préparer et lancer sous Windows

Prérequis : SDK .NET 10, accès au dépôt privé de la plateforme et aux paquets NuGet publics pendant la construction. À partir de la branche dédiée de la plateforme :

```powershell
./eng/Pack-Pilot.ps1 -Output D:/Projets/PaquetsPlateformePilote
```

Dans ce dépôt, restaurer les paquets, compiler et exécuter la vérification de composition :

```powershell
./eng/Test-PlatformBoundary.ps1 -PackageDirectory D:/Projets/PaquetsPlateformePilote
```

Le script crée une configuration NuGet et un cache isolés dans `.artifacts/t01`. Les paquets `Platform.Poc.*` proviennent exclusivement du dossier fourni. Le dépôt Avalonia conserve sa configuration NuGet indépendante.

Ensuite, laisser le cœur tourner dans un premier terminal :

```powershell
dotnet run --project src/MagasinOutil.CoreHost -c Release --no-build -- --simulation
```

Dans deux autres terminaux, exécuter des lectures indépendantes :

```powershell
dotnet run --project src/MagasinOutil.ReadClient -c Release --no-build -- wm.magasin8xx.t01 client-a
dotnet run --project src/MagasinOutil.ReadClient -c Release --no-build -- wm.magasin8xx.t01 client-b
```

Les deux réponses doivent porter la même identité de runtime/session et les mêmes places. Le client termine après une lecture ; le cœur reste actif jusqu'à Ctrl+C. Aucun téléchargement n'est nécessaire à l'exécution des binaires déjà construits.

## Validation attendue

Le workflow privé `Pilot T0 T1` compile les composants plateforme et les consommateurs existants, exécute les scénarios HMI/Application ciblés, teste deux clients simultanés, tue un processus client, le recrée, vérifie les refus de version/cible, puis redémarre le cœur. Il construit les paquets et lance la vérification de ce dépôt sans référence de projet vers le code plateforme.

La qualification produit des rôles, de la licence, de l'audit et des opérations n'appartient pas à cette preuve en lecture seule. Le contrat préparatoire d'admission durable doit être complété et testé avant toute utilisation pour un effet machine.
