# Première tranche intégrée — plan d’implémentation V0.1

Date : 14 septembre 2026. Statut : plan proposé après validation des choix techniques. Aucun code modifié ; passage à l’implémentation à confirmer explicitement, conformément au cadrage actuel.

## 1. Objectif et périmètre de preuve

Livrer une application magasin autonome utilisant réellement les composants communs de plateforme : interface homme-machine (HMI) web locale, runtime séparé, utilisateur local, permissions, licence temporaire vérifiée, audit durable, modification d’usure et préparation d’un outil simulé ; mêmes services exposés via OPC UA et état publié dans Fleet.

La tranche prouve le chemin commun. Elle ne constitue pas toute la V1, ne qualifie pas le PLC Beckhoff 8xx et ne démontre pas la conformité CRA du produit.

Révisions de départ vérifiées : pilote 61dea98d974b51cbb625fb1714d31ad18bce841d ; plateforme bc0819d0774c67e920481a7e0312d12f55a291a8. Recontrôler les branches et instructions des dépôts au début de l’implémentation ; ne pas écraser les évolutions parallèles de l’Executive.

Choix validés : paquets NuGet versionnés, gRPC sur tubes nommés Windows, SQLite local derrière les services du Core, processus OPC UA séparé. Stockage et transport remplaçables.

## 2. Répartition des changements

| Dépôt | Travail attendu |
|---|---|
| PlateformeWM-Demo | Contrats communs, évolution du HMI Runtime, adaptateurs de transport et de persistance, autorités transversales, extension OPC UA/Fleet réutilisable, tests et paquets |
| Magasin-Outil-8xx | Module métier et composition 8xx, parcours HMI, configuration du simulateur et des profils, consommation des paquets, recette du pilote et documentation |
| Programme PLC du magasin | Aucune modification dans cette tranche simulée ; questions de mapping et protocole suivies séparément |

La frontière précise d’un contrat outil doit être revue avant création : les primitives de gestion d’outils réutilisables appartiennent à la plateforme ; les particularités de racks, d’emplacements et de règles 8xx restent dans le module du pilote. Les noms des futurs projets sont indicatifs, pas des décisions de multiplication des assemblages.

Les modifications de plateforme doivent être réalisées sur une branche dédiée issue de la révision courante compatible. Aucun changement de la branche Executive ni fusion automatique n’est prévu par ce plan. La préparation des branches de travail est comprise dans une future autorisation d’implémentation ; la publication de releases ou la fusion restent distinctes.

## 3. Ordre des lots

| Lot | Contenu | Dépendances | Critère de sortie |
|---|---|---|---|
| T0 — Référence et contrats | Fixer dépendances, contrats de lecture/édition/suivi, identité cliente, persistance et catalogue minimal de capacités ; préparer la consommation de paquets locaux | Choix architecturaux validés | Revue des contrats, aucune dépendance fournisseur dans le métier ; compilation et non-régressions ciblées |
| T1 — Frontière de processus | Core simulé autonome, client distant, adaptation HMI Runtime, gRPC/tubes nommés, compatibilité et démarrage contrôlé | T0 | Deux clients lisent le même Core ; perte/recréation d’un client sans nouvelle session Machine |
| T2 — Autorités durables | Comptes locaux, permissions actuelles, stockage SQLite, journal d’intentions/audit, vérification de licence temporaire | T0 ; s’intègre à T1 | Identité non falsifiable par paramètres clients ; état durable ; expiration et panne d’écriture explicites |
| T3 — Cas métier communs | Lire outils/racks, éditer usure avec révision, PrepareTool gouverné sur simulateur | T1 + T2 | Conflit et refus sans effet ; résultat observé ; usures en broche soumises aux permissions adaptées |
| T4 — HMI web et OPC UA | Parcours web, contexte par client, édition/confirmation/suivi ; méthodes OPC UA sur les mêmes services | T3 ; validation du rendu web avant intégration graphique complète | Décisions concordantes entre clients ; pas de second runtime ou d’écriture brute |
| T5 — Fleet et profils | Exporteur de projection, collecteur de qualification indépendant, activation des capacités et diagnostics | T3 ; vérification intégrée après T4 | Arrêt Fleet sans impact local ; changement de profil sans reconnexion ni rejeu |
| T6 — Recette de la tranche | Pannes, concurrence, redémarrages, restauration, preuves de dépendances, rendu et instructions de lancement | T1 à T5 | Dossier de résultats traçables, limites et défauts restants explicites |

T0/T1 préparent une frontière de lecture sans exposer des commandes non gouvernées. Les simulations internes nécessaires aux tests restent identifiées comme telles. T2 n’est pas remplacé par des utilisateurs ou licences fictifs présentés comme des services produit.

La préparation technique du navigateur, des contrats et des retours automatisme peut progresser sans attendre la fin des autres lots. Cette possibilité n’impose pas une délégation ou des agents parallèles.

## 4. Contrat minimal à figer dans T0

### Observations

- Identité machine, version du contrat, génération de session et incarnation du runtime.
- Outil et emplacement distincts, présence observée distincte de l’affectation, usures applicables.
- Valeurs typées, unités de référence, qualité, fraîcheur et horodatages disponibles.
- Révision de l’objet éditable et politique explicite de cohérence ; aucun lot de signaux supposé atomique.

### Demandes

- Identifiant d’intention stable, cible et client ; référence de session validée par les autorités communes.
- Édition explicite des seuls champs demandés, avec révision attendue.
- Même intention et même contenu : retrouver le traitement existant ; contenu différent : refus.
- Contrôle des permissions au moment d’admettre, y compris position courante de l’outil.
- Suivi par intention/opération autorisé après perte de réponse ; aucun renvoi automatique.

### Persistance et transport

- Transaction locale pour admission, corrélation et audit avant effet technologique.
- Une transaction SQLite ne couvre pas l’effet PLC : résultat incertain traité par réconciliation.
- Pas de SQLite, SQL, types gRPC générés ou objets de requête fournisseur dans les contrats métier.
- Suites de conformité réutilisables ; remplacement futur qualifié par un autre adaptateur.
- Durées de conservation des corrélations et comportement après expiration à définir avant revendication de reprise durable.

## 5. Décisions à traiter au bon moment

| Sujet ouvert | Proposition ou préparation possible | Ce qui reste bloqué |
|---|---|---|
| Mode Blazor et composants | Qualifier les candidats déjà proposés sur les deux tailles ; conserver le parcours Avalonia comme référence | Choix final du renderer et de la bibliothèque |
| Connexion locale | Décrire création du premier administrateur, verrouillage, récupération et sessions ; mécanisme de saisie à décider | Parcours d’authentification produit |
| Licences | Définir format signé, identité d’installation, droits et expiration ; clés de test séparées et émetteur de test hors hôte machine | Émission produit, rattachement matériel, temps fiable et renouvellement |
| Audit indisponible | Définir un ensemble explicite d’actions de récupération, avec permissions et traces possibles | Qualification produit tant que l’on peut rester bloqué sans moyen de récupération |
| Stockage | Contrats et tests, SQLite choisi ; mesurer budgets et latences | Valeurs de rétention, politique sauvegarde/restauration définitive |
| Distribution | Construire des paquets à versions figées et les consommer depuis un dossier local de développement | Choix du flux privé et publication produit |
| OPC UA et Fleet réseau | Préparer authentification et confiance, identités de service et limites | Qualification réseau usine ; un test en boucle locale ne la remplace pas |
| Mapping 8xx | Continuer analyse et questions techniques sans inventer les échelles | Écritures et opérations sur banc réel |

Aucune proposition de cette table n’accorde des droits à un compte réel ou n’installe une licence. Le plan peut avancer sur les parties indépendantes ; les choix ouverts sont regroupés avant le lot qui les exige.

## 6. Critères de recette de la tranche

| Vérification | Résultat attendu |
|---|---|
| Deux sessions clientes | Sélections et préférences indépendantes, mêmes observations autoritatives |
| Usure autorisée | Opérateur autorisé au magasin et en broche selon permissions et conditions du simulateur |
| Champ interdit ou requête mixte | Refus intégral avant effet, aucune modification nominale |
| Révision dépassée | Conflit explicite ; pas d’écrasement silencieux |
| Permission retirée | Nouvelle demande refusée même si confirmation déjà ouverte |
| Licence expirée | Nouvelles modifications/commandes refusées ; consultation selon droits ; opération admise poursuivie |
| Audit indisponible | Pas de nouvelle mutation ; suivi des opérations admises et diagnostic conservés |
| Préparation | Acceptation distincte de complétion ; preuve fraîche requise |
| Réponse perdue | Retrouver l’opération autorisée, sans nouvelle soumission |
| Web ou OPC UA arrêté | Core et opérations admises poursuivis ; restauration depuis état courant |
| Core redémarré | Nouvelle incarnation, reprise conservatrice, réacquisition et réconciliation ; pas d’ancien état présenté comme frais |
| Fleet indisponible | Parc signalé non actualisé ; fonctionnement local préservé |
| Profil isolé | HMI web locale disponible ; interfaces réseau non activées |
| Maintenance | Aucune écriture accessible, même pour un administrateur |
| Stockage/transport | Dépendances vérifiées ; conformité de l’adaptateur V1 testée, sans prétendre avoir qualifié un autre moteur |
| Affichage | 1024 × 768 et 1920 × 1080, clair/sombre, logo WM et saisie tactile qualifiés dans l’environnement déclaré |

Chaque résultat référence versions, environnement, commande ou scénario, résultat et limite. Les tests ciblent des comportements et frontières réels ; aucune multiplication de tests qui recopient l’implémentation.

## 7. Livrables concrets

- Paquets communs de développement versionnés et composition reproductible du pilote.
- Instructions de lancement des hôtes, configuration de simulation distincte des cibles physiques.
- Parcours web minimal et exposition OPC UA fonctionnels sur même autorité.
- Raccordement Fleet de qualification, séparé du besoin de déploiement usine.
- Documentation des comptes/permissions, licence de test, persistance, reprise et profils.
- Dossier de preuves, écarts et liste des fonctions V1 restant à couvrir.

Le programme installé doit fonctionner sans source NuGet accessible. Les secrets de test restent explicitement impropres à la production. Aucune clé privée d’émission produit n’est distribuée avec l’application machine.

## 8. Frontière avec les étapes suivantes

Après recette de cette tranche : compléter les champs et cas d’usage V1, qualifier les langues/unités et profils produits, puis intégrer Beckhoff 8xx progressivement. Les études du banc, du matériel et du déploiement peuvent commencer avant cette clôture.

La conformité CRA et le cycle de vie produit sont suivis dès le cadrage, mais aucune conformité n’est revendiquée par un test simulé. Les preuves réseau, matériel et opérationnelles restent séparées.

## 9. Prochaine action proposée

Après autorisation explicite du passage au code : commencer T0, puis T1, avec changements de plateforme isolés et consommation par le pilote. Présenter les choix restant nécessaires à T2 sous forme d’une proposition groupée avant leur implémentation. Ne pas commencer simultanément toutes les fonctionnalités.

Références : [architecture d’intégration](../architecture/Integration_Pilote_V1_Candidate.md), [permissions](../securite/Permissions_Roles_V0.1.md), [maintenance](../contrats/Maintenance_Lecture_V1.md), [contrat d’exposition](../analyse/Contrat_Exposition_Magasin_8xx_V0.1_2026-09-14.md).
