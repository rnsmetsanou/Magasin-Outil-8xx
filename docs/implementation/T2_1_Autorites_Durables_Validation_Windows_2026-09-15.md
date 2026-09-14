# T2.1 — Autorités durables : validation Windows

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-1-durable-authorities-storage`.
Branche plateforme : `pilot/t2-1-durable-authorities-storage`.

## 1. Portée

Ce dossier consigne les preuves locales du lot T2.1 : contrats d'autorité de session et d'admission durable, adaptateur SQLite remplaçable, puis composition réelle de cet adaptateur par le CoreHost du pilote.

Aucune connexion Beckhoff réelle, aucun compte utilisateur produit, aucune licence produit et aucune commande machine ne sont qualifiés par ces résultats.

## 2. Environnement observé

Journal utilisateur du 15 septembre 2026 :

- Windows 10.0.22631, `win-x64` ;
- .NET SDK 10.0.400 ;
- hôte .NET 10.0.12 ;
- validation exécutée depuis `D:\Projets\Plateforme\Repos\PlateformeWM-Demo` avec le dépôt pilote `D:/Projets/Magasin-Outil-8xx`.

## 3. T0/T1 — non-régression

La validation T2.1 rejoue le socle T0/T1. Le journal confirme notamment : frontière de processus PASS, deux clients sur la même session Machine, aucune reconnexion Machine liée à la recréation client, dépendances HMI conformes, compilation plateforme, checkpoints WS-AT04, WS-AT11 et P6.2-D, puis composition des paquets pilote.

État : **PASS LOCAL**.

## 4. T2.1-A — contrats des autorités durables

Le journal confirme :

- contrats d'admission durable indépendants de SQLite et de gRPC ;
- une session n'est valide qu'après résolution par l'autorité ;
- une référence inconnue ne confère aucune autorité ;
- une session ne peut pas être rebondie vers un autre client ou une autre cible par paramètres de requête ;
- l'admission durable conserve une représentation canonique versionnée en plus de son empreinte ;
- l'incertitude de commit est explicite et impose réconciliation sans rejeu aveugle ;
- les conflits de propriétaire sont distingués des conflits de contenu.

État : **PASS LOCAL**.

## 5. T2.1-B — persistance SQLite

Le journal confirme :

- migration de schéma V1 appliquée ;
- contrat applicatif toujours indépendant de SQLite ;
- premier commit atomique de la corrélation, requête canonique et audit ;
- déduplication de la même intention et du même contenu ;
- refus d'un contenu différent sans divulgation du record ;
- impossibilité de réattribuer une intention à un autre propriétaire ;
- lookup sans divulgation inter-propriétaire ;
- échec d'écriture de l'audit empêchant tout état `Committed` et provoquant le rollback de l'admission ;
- huit appels concurrents convergeant sur une seule opération autoritative ;
- persistance après recréation du store ;
- sauvegarde SQLite cohérente, conservant migration et admission restaurable.

État : **PASS LOCAL**.

## 6. T2.1-C — composition par le pilote

Préparé après le PASS T2.1-B :

- les paquets pilote passent à `0.1.0-pilot.t21.1` ;
- `Platform.Poc.Persistence.Sqlite` est distribué comme paquet plateforme distinct ;
- `MagasinOutil.Platform` reste indépendant de SQLite ;
- `MagasinOutil.CoreHost`, composition root du pilote, référence l'adaptateur SQLite et initialise `durable-authority.db` avant d'annoncer `READY` ;
- la recette vérifie la création effective de la base, la présence du fournisseur uniquement côté CoreHost et l'absence du fournisseur dans le client de lecture et le module `MagasinOutil.Platform` ;
- le comportement T0/T1 reste contrôlé avec deux clients sur la même autorité Machine.

État : **PRÊT À QUALIFIER LOCALEMENT**. Aucun PASS n'est déclaré avant réception du prochain journal d'exécution.

## 7. Critère de clôture T2.1

T2.1 pourra être déclaré terminé en simulation lorsque T2.1-C sera PASS LOCAL avec les régressions T0/T1, T2.1-A et T2.1-B toujours vertes. Le lot suivant sera T2.2 : identités locales, sessions révocables et résolution des permissions, sans intégrer prématurément les opérations métier T3.
