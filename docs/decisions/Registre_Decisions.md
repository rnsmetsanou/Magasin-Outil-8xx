# Registre des décisions — Magasin 8xx

Actualisé le 15 septembre 2026. Source : validations explicites dans les échanges du projet. Ce registre actualise les analyses historiques sans en modifier les constats datés.

## Décisions retenues

| Sujet | Décision |
|---|---|
| Positionnement | Pilote autonome utilisant la plateforme, intégrable dans une autre application ultérieurement. |
| Interface | Direction HMI web commune, hébergée localement sur le PC industriel, sans dépendance Internet ; remplacement effectif d’Avalonia après qualification. |
| Isolation | Runtime Machine/Application séparé de l’hôte HMI dès la première version. |
| Clients | Plusieurs clients souhaités ; actions selon autorisations. La tablette est un exemple de client. |
| Profils | Local isolé, Intégration usine, Rattaché au parc et Intégration complète. Aucun profil par défaut. Identité, licence, autorisation et audit restent obligatoires. |
| OPC UA | Exposition des données et actions en V1 ; pas de consultation métier anonyme. Une identité Service et une confiance de transport qualifiée sont requises. |
| Maintenance V1 | Consultation des réglages et états utiles dans HMI/OPC UA. Écritures de maintenance, teach, changements de mode et mouvements hors exposition V1. |
| Fleet | Publication/supervision en lecture seule dans le périmètre étudié ; pas de commande machine. |
| Comptes et sessions | Comptes locaux nominatifs hors ligne, sans compte partagé ni administrateur universel. Sessions opaques et révocables, liées au sujet/client/cible. Inactivité initiale : 30 min local, 10 min distant ; durée absolue 8 h. Réauthentification après redémarrage du Core. |
| Rôles | Consultation, Opérateur, Régleur outils, Administrateur. Pas de hiérarchie implicite. L’Administrateur ne reçoit pas de commande machine par défaut. |
| Consultation sans identité | Pas de consultation métier anonyme en V1. Un futur sujet `Guest` local lecture seule pourra être étudié explicitement. |
| Licences | Licence hors ligne signée, liée à une identité cryptographique d’installation ; clé privée uniquement dans l’outil d’émission WM. Expiration/incohérence : nouvelles mutations licenciées bloquées, lectures selon droits et poursuite des opérations déjà admises. |
| Admission et audit | Le Core est seul propriétaire des écritures. Intention, corrélation, représentation canonique et audit d’admission sont persistés atomiquement avant tout effet technologique. SQLite reste un adaptateur remplaçable. |
| Politique audit pilote | Rétention 365 jours, budget 1 Gio, alerte à 80 %, sauvegarde quotidienne et avant migration, dix sauvegardes quotidiennes conservées. Valeurs configurables à qualifier. |
| Récupération | Liste fermée d’actions de récupération ; aucun bypass Administrateur général. Secret temporaire ciblé à usage unique pour le mot de passe ; récupération du dernier administrateur par autorisation signée propre à l’installation et à usage unique. |
| Audit en panne | Diagnostic, consultation vérifiable et récupération supervisée uniquement ; aucune nouvelle commande métier ni modification courante des rôles. |
| Langues | Français requis, anglais souhaité en V1, allemand si possible. |
| Unités | Millimètres et pouces en V1 ; préférences utilisateur et extension par grandeur physique. |
| Affichage | 1024 × 768 et 1920 × 1080, tactile, thèmes clair/sombre, charte et logo WM. |
| CRA | Exigence de conformité produit ; aucune conformité démontrée par les preuves actuelles. |
| Édition Opérateur | Modification des seuls champs d’usure autorisés, y compris pour l’outil en broche sous conditions machine communes. |
| Intégration technique | Paquets NuGet versionnés, gRPC sur tubes nommés Windows, SQLite derrière les services du Core et hôte OPC UA séparé. Fournisseurs remplaçables derrière contrats. |
| Documentation | Versionner les livrables structurants dans ce dépôt au fil du travail. |

## Propositions et questions ouvertes

- Contrat d’exposition V0.1 : matrice détaillée des données/actions à consolider ; une donnée lisible PLC n’est pas automatiquement exposable en écriture.
- Maintenance : mapping, unités, fraîcheur et droits de consultation à préciser ; aucune écriture de maintenance autorisée par le choix V1.
- Blazor Interactive Server et MudBlazor : candidats à qualifier, pas dépendances produit adoptées.
- Fleet, transports réseau, certificats, démarrage système et packaging : à définir et qualifier.
- Catalogue détaillé des champs outils/correcteurs et préconditions machine : à consolider.
- Durées commerciales de licence, responsabilités d’émission/transfert, migration d’iPC et révocation hors ligne : décisions WM à définir.
- L’identité d’installation T2.3 est cryptographiquement aléatoire mais pas matériellement scellée ; disponibilité d’un Trusted Platform Module (TPM) ou équivalent à qualifier sur l’iPC cible.
- Tolérance produit exacte aux petits reculs d’horloge : à définir ; les valeurs de recette ne sont pas une politique finale.
- Destination externe des sauvegardes et responsabilité d’exploitation : à définir.
- Mapping Beckhoff 8xx : propriétaires PLC/CNC/application, échelles, encodage, protocole, atomicité et preuves de complétion à confirmer.
- Paramètres Argon2id finaux à benchmarker sur le PC industriel cible.
- Limitation par source/contexte réseau à ajouter pour une exposition réseau de l’authentification ; le throttling actuel reste centré compte/identifiant.
- T2.4-B : politique exacte définissant la perte du dernier administrateur récupérable à figer avec la recette signée.
- T2.4-C : taille/rétention exacte du journal de secours borné à qualifier comme politique produit.

## État de réalisation

Le prototype Avalonia reste autonome. Le socle de lecture T0/T1 est qualifié en simulation Windows. **T2.1, T2.2 et T2.3 sont clôturés en simulation Windows.**

**T2.4-A est implémenté et attend sa qualification locale.** T2.4-B/C/D restent à implémenter.

Le serveur OPC UA intégré, le raccordement Fleet, la qualification Beckhoff réelle et le packaging produit restent hors de ces preuves.

## T0/T1 — socle de lecture vérifié sur Windows

Le journal du 14 septembre 2026 confirme la frontière de processus, les clients séparés, la même autorité Machine, les contrôles WS-AT04/WS-AT11/P6.2-D et la consommation des paquets communs : **PASS LOCAL**.

## T2.1 — autorités durables et stockage : clôturé

T2.1-A/B/C sont **PASS LOCAL** avec régression T0/T1 verte : contrats indépendants des fournisseurs, admission + audit atomiques, déduplication, persistance, sauvegarde/restauration et composition SQLite côté CoreHost.

## T2.2 — identités locales, authentification et sessions : clôturé

T2.2-A/B/C/D sont **PASS LOCAL** : identités nominatives, comptes durables, Argon2id, sessions opaques/révocables, permissions dynamiques, throttling, commissioning du premier administrateur et stockage borné des faux identifiants.

## T2.3 — licences hors ligne signées et temps de confiance : clôturé

T2.3-A/B/C/D sont **PASS LOCAL** : format signé, identité d’installation durable, anti-rollback, temps de confiance, admission gouvernée et composition réelle dans `MagasinOutil.CoreHost`. Voir `../implementation/T2_3_Licences_Hors_Ligne_Validation_Windows_2026-09-15.md`.

**État : T2.3 clôturé en simulation Windows.** Aucune qualification Beckhoff réelle, TPM, clé de production ou conformité CRA n’en découle.

## T2.4-A — secret temporaire de réinitialisation

La micro-tranche A est implémentée avec les décisions suivantes :

- émission/révocation gouvernées par `identity.manage` ;
- secret de 256 bits généré cryptographiquement ;
- durée pilote initiale de 15 minutes ;
- secret persisté uniquement sous forme dérivée via `IPasswordHashingProvider` ;
- une seule autorisation active par sujet, la nouvelle révoquant l’ancienne ;
- liaison à la `SecurityRevision` du compte afin qu’un reset obsolète ne puisse écraser un changement plus récent ;
- remplacement du credential et consommation du secret dans une transaction SQLite commune ;
- permissions et état d’activation du compte non modifiés par le reset ;
- toutes les sessions courantes du sujet révoquées après succès via `ISubjectSessionRevoker` ;
- aucune création automatique de session ;
- replay et double consommation concurrente empêchés.

Voir `../implementation/T2_4_A_Secret_Temporaire_Reinitialisation_Mot_de_Passe.md`.

**État : IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.** Aucun PASS n’est déclaré avant un journal vert de `Test-T24.ps1`.

## Suite T2.4

- **T2.4-B** : récupération signée du dernier administrateur, liée à l’installation et anti-replay ;
- **T2.4-C** : audit dégradé et journal de secours borné réservé à la récupération ;
- **T2.4-D** : composition réelle dans `MagasinOutil.CoreHost` et non-régression globale.
