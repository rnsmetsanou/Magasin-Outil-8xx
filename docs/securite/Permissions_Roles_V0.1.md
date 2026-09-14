# Permissions et rôles V0.1 — pilote Magasin 8xx

Date : 14 septembre 2026. Statut : proposition à valider. Aucun rôle, compte, paramètre de sécurité ou code modifié. Les identifiants nouveaux ci-dessous sont candidats ; seul le périmètre maintenance en consultation est déjà validé.

## 1. Base existante vérifiée

PlateformeWM-Demo, branche p6-7-live-commissioning-impl, commit bc0819d0774c67e920481a7e0312d12f55a291a8.

- `ToolHandlingPermissions` définit `tool.prepare` et `tool.load` : à réutiliser sans variantes propres à la HMI ou OPC UA.
- `SubjectKind` distingue Human, Service et System.
- `PermissionAuthorizationService` refuse un sujet non authentifié et vérifie la permission demandée.
- `SubjectContext.HasPermission` accepte aussi `*` ; `TrustedSystem()` crée un contexte système avec cette permission.
- Ces types ne constituent pas un système complet de comptes, de rôles configurables, de sessions révocables ou d’autorisations par machine. Le contexte inspecté contient une collection de permissions sans portée de ressource explicite.

Proposition : compléter les services communs de plateforme ; ne pas coder des rôles propres à Blazor ou OPC UA. Ne pas attribuer `*` aux comptes humains et de service du pilote. Ne pas utiliser `TrustedSystem()` pour transformer une demande cliente en commande autorisée.

## 2. Principes déjà retenus et propositions

Décisions acquises : comptes locaux utilisables hors ligne ; identités nominatives pour actions humaines et administration ; plusieurs clients sous les mêmes contrôles ; licences temporaires ; audit durable ; maintenance en lecture seule en V1.

Propositions à valider : catalogue ci-dessous, rôles de départ, portée machine, identités de service et mécanisme de consultation anonyme limitée. La consultation anonyme ne doit pas être obtenue en contournant le contrôle d’authentification existant : elle exige une surface publique explicitement limitée, avec sa politique propre.

## 3. Catalogue candidat

Chaque permission correspond à une capacité métier commune. Les autorités doivent aussi contrôler la cible et les champs concernés.

| Identifiant | Portée | État |
|---|---|---|
| `magazine.read` | Catalogue, racks, places et états ordinaires des outils | Nouveau proposé |
| `maintenance.read` | Réglages et diagnostics de maintenance sélectionnés | Nouveau proposé ; consultation seule |
| `tool.create` | Création de fiche selon règles d’affectation validées | Nouveau proposé |
| `tool.data.edit` | Modification des paramètres d’outil autorisés | Nouveau proposé |
| `tool.correctors.edit` | Modification des correcteurs applicables | Nouveau proposé |
| `tool.spindle.edit` | Autorisation supplémentaire pour éditer l’outil actuellement en broche | Nouveau proposé |
| `tool.prepare` | Préparer un outil | Existant plateforme |
| `tool.load` | Charger en broche | Existant plateforme |
| `operation.read.own` | Consulter ses opérations autorisées | Nouveau proposé |
| `operation.read.machine` | Consulter les opérations de la machine autorisée, avec données filtrées | Nouveau proposé |
| `audit.read` / `audit.export` | Consultation / export de l’audit | Nouveaux proposés, distincts |
| `identity.manage` / `roles.manage` | Gestion des comptes / attribution des permissions et rôles | Nouveaux proposés, distincts |
| `license.install` | Installation ou remplacement d’une licence signée | Nouveau proposé ; ne permet pas d’émettre une licence |
| `deployment.manage` | Modification des profils et interfaces réseau selon préconditions | Nouveau proposé |
| `audit.policy.manage` | Rétention et budgets selon politique produit | Nouveau proposé ; ne permet pas d’effacer librement l’audit |
| `fleet.publish` | Publication de la projection Fleet par un service identifié | Nouveau proposé ; pas de commande machine |

L’édition d’un correcteur en broche demande `tool.correctors.edit` ET `tool.spindle.edit`. L’édition d’autres données en broche demande `tool.data.edit` ET `tool.spindle.edit`. La position et la révision sont revérifiées à l’admission ; sélectionner un outil au magasin puis attendre son chargement ne doit pas contourner le contrôle.

La permission d’édition ne rend pas tous les champs PLC éditables. Le catalogue de champs, les bornes et la propriété PLC/CNC/application restent une règle métier indépendante.

Aucune permission de teach, de réglage machine, de changement de mode ou de mouvement de maintenance n’est définie pour la V1. Un administrateur ne peut pas rendre une fonctionnalité hors périmètre disponible par simple attribution de droits.

## 4. Rôles de départ proposés

Oui = attribution de départ proposée. Non = absence d’attribution par ce rôle, pas un refus explicite écrasant les autres rôles. Un utilisateur peut cumuler plusieurs rôles explicitement attribués ; aucune hiérarchie implicite.

| Capacité | Consultation | Opérateur | Régleur outils | Administrateur |
|---|---|---|---|---|
| Consulter magasin | Oui | Oui | Oui | Oui |
| Consulter maintenance | Non | Non | Oui | Oui |
| Créer / modifier données outils | Non | Non | Oui | Non |
| Modifier correcteurs | Non | Non | Oui | Non |
| Éditer l’outil en broche | Non | Non | Oui, avec permission d’édition correspondante | Non |
| Préparer / charger | Non | Oui | Oui | Non |
| Suivre ses opérations | Non | Oui | Oui | Oui pour ses opérations administratives |
| Consulter opérations machine | Non | Non | Oui | Oui |
| Lire / exporter audit | Non | Non | Non | Oui |
| Gérer comptes et rôles | Non | Non | Non | Oui |
| Installer licence / configurer déploiement et rétention | Non | Non | Non | Oui |

« Régleur outils » est volontairement limité au domaine outil. Ce nom ne donne aucun accès au teach ou aux réglages de maintenance, qui restent hors exposition V1.

La séparation administration/exploitation évite une attribution automatique de commandes machine. Elle n’est pas une séparation organisationnelle absolue : un administrateur autorisé à attribuer les rôles pourrait accorder un rôle d’exploitation. Toute attribution doit être auditée. L’interdiction d’auto-attribution ou la double validation seraient des politiques supplémentaires, non décidées ici.

La question principale à valider est la frontière Opérateur / Régleur outils, notamment pour les corrections d’usure fréquentes. Aucun choix d’exploitation existant chez WM n’est présumé.

## 5. Clients web, OPC UA et Fleet

- Un utilisateur conserve les mêmes permissions applicables à la même machine quel que soit son terminal. Le poste local ne reçoit aucun privilège automatique.
- Chaque session web conserve son sujet et ses préférences ; aucun utilisateur courant global partagé entre clients.
- Une session OPC UA associe identité authentifiée et identité du client. Une confiance dans le certificat du client ne suffit pas à lui accorder toutes les actions métier.
- Un système automatisé utilise une identité Service distincte et des permissions explicites pour sa mission. Pas de compte opérateur partagé ni de permission `*` par commodité.
- L’exporteur Fleet reçoit la seule capacité de publication nécessaire. Le collecteur et sa vue restent en lecture seule ; ils ne deviennent pas des mandataires système pour commander la machine.
- Une projection publique éventuelle expose seulement les informations approuvées. Les réglages de maintenance, détails d’audit et informations de comptes sont exclus de cette proposition de lecture anonyme.
- Partager les règles ne signifie pas exposer toutes les fonctions sur tous les protocoles : aucune API d’administration des comptes, licences ou profils via OPC UA n’est ajoutée implicitement au périmètre.

## 6. Admission et révocation

Proposition : résolution des permissions effectives à partir des autorités communes au moment d’une nouvelle demande, avec contrôle de l’identité, de la portée machine, de la configuration, de la licence, de la disponibilité de l’audit et des préconditions métier.

Retirer un rôle, désactiver un compte ou fermer une session doit empêcher ses nouvelles demandes protégées. Une confirmation déjà ouverte ne conserve pas des droits révoqués. Les projections peuvent afficher une disponibilité, mais elles ne remplacent pas le contrôle autoritatif.

Les opérations déjà admises continuent suivant leurs règles machine, même si leur initiateur se déconnecte. Leur consultation ultérieure dépend des permissions actuelles. Les autres clients autorisés doivent pouvoir suivre l’état machine sans récupérer tous les détails personnels de la demande.

L’expiration de licence et une panne d’audit durable bloquent les nouvelles modifications et commandes selon les décisions du projet. Les chemins de récupération autorisés (notamment installation d’une licence et rétablissement de l’audit) doivent être définis explicitement pour éviter un blocage sans issue ; cette liste d’exceptions n’est pas encore validée et ne doit pas devenir une exemption générale d’administration.

## 7. Audit et qualification à prévoir

Événements proposés : connexion/refus, activation/désactivation de compte, attribution/retrait de rôle, modification de politique, demande d’action, admission/refus et résultat. Inclure identité, client, cible, corrélations et changements pertinents sans secrets d’authentification.

Vérifications nécessaires avant mise en service :

1. Même demande web et OPC UA : même décision pour mêmes identité, cible et conditions.
2. Administrateur sans rôle d’exploitation : refus de Prepare/Load et des écritures outil.
3. Édition en broche : permission supplémentaire exigée, y compris après changement de position entre sélection et admission.
4. Retrait de droit ou compte désactivé : nouvelle demande refusée sans effet PLC ; opération antérieure admise conservée.
5. Identité Service limitée à une machine : aucune action ni lecture protégée sur une autre cible.
6. Compte avec droits les plus élevés : aucune écriture de maintenance V1, aucun accès brut aux structures de commande.
7. Consultation publique : aucun accès aux champs privés ; absence de mutation.
8. Audit indisponible ou licence expirée : règles identiques sur tous les chemins ; récupération exercée selon politique explicite.
9. Absence de permission : aucun appel technologique produisant un effet.

Ces scénarios sont un plan de recette, pas des résultats de tests exécutés.

## 8. Décisions attendues

Priorité : valider les rôles et l’autorisation de modifier les données/correcteurs pour l’opérateur. Ensuite préciser le périmètre public, les identités de service, l’attribution des rôles et les exceptions de récupération. Les identifiants candidats doivent être consolidés avec le catalogue commun avant implémentation.

## 9. Références de code

PlateformeWM-Demo à la révision indiquée en tête :

- [Permissions outil](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Machine.Contracts/Operations/ToolHandling/ToolHandlingPermissions.cs)
- [Sujet et permission globale](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Foundation/Identity/SubjectContext.cs)
- [Types de sujets](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Foundation/Identity/SubjectKind.cs)
- [Autorisation](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.CrossCutting.Runtime/Identity/PermissionAuthorizationService.cs)
