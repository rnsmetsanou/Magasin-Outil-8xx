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

Le journal final confirme :

- paquets plateforme `0.1.0-pilot.t21.1` consommés depuis le feed local isolé ;
- `Platform.Poc.Persistence.Sqlite` compilé et distribué comme adaptateur séparé ;
- `MagasinOutil.Platform` reste indépendant du fournisseur SQLite ;
- `MagasinOutil.CoreHost`, composition root du pilote, sélectionne et initialise `durable-authority.db` avant `READY` ;
- le client de lecture n'embarque pas SQLite ni de runtime Machine concret ;
- la base durable est effectivement créée ;
- le comportement T0/T1 reste vert avec deux clients partageant la même autorité Machine.

Résultats explicites du journal :

- `Pilot T0/T1 package behavior regression: PASS` ;
- `Pilot T2.1 durable SQLite composition: PASS` ;
- `T2.1-C : composition pilote qualifiee avec succes.`

État : **PASS LOCAL**.

## 7. Clôture T2.1

Les trois micro-tranches T2.1-A, T2.1-B et T2.1-C sont **PASS LOCAL**, avec la régression T0/T1 toujours verte. T2.1 est donc clôturé en simulation Windows pour le périmètre qualifié.

Ce résultat établit :

- les frontières provider-indépendantes des autorités durables ;
- la persistance atomique et réconciliable de l'admission ;
- la composition réelle de l'adaptateur SQLite par le CoreHost du pilote ;
- l'absence de couplage SQLite dans les contrats et le module métier du pilote.

Il n'établit pas encore :

- l'authentification d'utilisateurs réels ;
- la persistance des comptes et rôles ;
- la révocation de sessions et de droits en situation réelle ;
- la licence produit ;
- le raccordement Beckhoff réel.

Le lot suivant est **T2.2 — identités locales, authentification, sessions révocables et résolution des permissions**. La première micro-tranche T2.2 doit d'abord éprouver le noyau d'autorité avec deux identités nominatives, liaison client/cible, expiration/révocation et réévaluation des permissions avant toute nouvelle admission, sans intégrer prématurément les opérations métier T3.