# T2.1-A — Validation locale des contrats d'autorités durables

Date : 15 septembre 2026  
Statut : **PASS LOCAL**  
Périmètre : contrats d'autorité et non-régression T0/T1. Ce résultat ne valide pas encore la persistance SQLite T2.1-B, les comptes utilisateurs, les licences produit ni un raccordement machine réel.

## Commande exécutée

```powershell
.\eng\Test-T21.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Environnement observé : Windows 10.0.22631 x64, .NET SDK 10.0.400, MSBuild 18.9.6.

## Non-régression T0/T1

La frontière de processus T0/T1 reste validée : deux clients partagent la même session Machine, l'autorité survit au cycle de vie des clients, la sélection reste par client, les contrats restent indépendants des fournisseurs, les refus de version/cible/requête sont conservés et le redémarrage du Core change son incarnation.

La composition des paquets du pilote est également PASS LOCAL. Les projets `MagasinOutil.Core`, `MagasinOutil.ReadClient`, `MagasinOutil.Platform` et `MagasinOutil.CoreHost` compilent dans la recette reçue.

Les scénarios de régression plateforme WS-AT04, WS-AT11 et P6.2-D restent verts. En particulier : complétion sémantique sur observation fraîche, contrôle d'autorisation avant effet technologique et indépendance du cycle de vie des renderers.

## Résultats T2.1-A

Les contrôles suivants sont PASS LOCAL :

1. les contrats d'admission durable ne dépendent pas de SQLite ;
2. les contrats d'admission durable ne dépendent pas de gRPC ;
3. une session résolue n'est valide qu'avec un état produit par l'autorité ;
4. une référence de session inconnue ne constitue pas une autorité ;
5. une session ne peut pas être rattachée à un autre client par des paramètres de requête ;
6. une session ne peut pas être rattachée à une autre cible par des paramètres de requête ;
7. l'admission durable conserve une représentation canonique versionnée et pas seulement une empreinte ;
8. l'incertitude de commit est explicite et permet une réconciliation sans rejeu aveugle ;
9. les conflits de propriété d'une intention sont distingués des conflits de contenu.

Résultat final : `T2.1-A durable authority contracts: PASS` puis `T2.1-A : validation locale terminee avec succes.`

## Ce que cette preuve autorise

T2.1-A permet de poursuivre avec un adaptateur de stockage concret derrière les contrats désormais vérifiés. SQLite reste un choix d'adaptateur ; aucune dépendance SQLite ne doit remonter dans `Platform.Poc.Application.Contracts`.

La micro-tranche suivante est T2.1-B : schéma/migrations SQLite, implémentation de `IDurableAdmissionStore`, atomicité admission + audit, déduplication/concurrence, persistance après recréation et sauvegarde/restauration cohérente. Les comptes locaux et sessions persistées restent hors de cette micro-tranche.
