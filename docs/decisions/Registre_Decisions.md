# Registre des décisions — Magasin 8xx

Actualisé le 15 septembre 2026. Source : validations explicites dans les échanges du projet. Ce registre actualise les analyses historiques sans en modifier les constats datés.

## Décisions retenues

| Sujet | Décision |
|---|---|
| Positionnement | Pilote autonome utilisant la plateforme, intégrable dans une autre application ultérieurement. |
| Interface | Direction HMI web commune, hébergée localement sur le PC industriel, sans dépendance Internet ; remplacement effectif d’Avalonia après qualification. |
| Isolation | Runtime Machine/Application séparé de l’hôte HMI dès la première version. |
| Clients | Plusieurs clients souhaités ; actions selon autorisations. La tablette est un exemple de client. |
| Profils | Local isolé, Intégration usine, Rattaché au parc et Intégration complète, suivant la matrice de l’analyse de déploiement. Aucun profil par défaut choisi. Identité, licence, autorisation et audit restent obligatoires dans tous les profils. |
| OPC UA | Exposition des données et actions en V1 ; connexion à des équipements OPC UA ensuite. Pas de consultation métier anonyme OPC UA. Une identité Service et une confiance de transport qualifiée sont requises pour l’exposition protégée. |
| Maintenance V1 | Consultation des réglages et états utiles dans la HMI et OPC UA. Modification des réglages, apprentissage des positions (teach), changements de mode et mouvements de maintenance hors exposition V1 ; restent dans la HMI Beckhoff. La gestion des données d’outils et les opérations Préparer/Charger restent dans leur périmètre distinct. |
| Fleet | Reprendre son rôle dans la plateforme et le raccordement commun, en supervision en lecture seule dans le périmètre examiné. |
| Comptes et sessions | Comptes locaux nominatifs utilisables hors ligne, sans compte partagé ni administrateur universel. Sessions opaques, révocables et liées côté Core au sujet, au client et à la cible. Inactivité : 30 min pour une session interactive locale, 10 min pour une session interactive distante ; durée absolue initiale 8 h. Réauthentification après redémarrage du Core. |
| Rôles | Quatre rôles initiaux : Consultation, Opérateur, Régleur outils, Administrateur. L’Opérateur peut modifier uniquement les usures, y compris en broche sous conditions, et dispose de Préparer/Charger. L’Administrateur ne reçoit pas implicitement des commandes machine. |
| Consultation sans identité | Pas de consultation métier anonyme en V1. Un futur sujet `Guest` local strictement lecture seule peut être étudié comme capacité explicite, sans contourner les autorités communes. |
| Licences | Licence hors ligne signée, liée à une identité cryptographique d’installation ; clés privées uniquement dans l’outil d’émission WM. Expiration ou incohérence temporelle : nouvelles modifications et commandes bloquées, consultation selon droits et poursuite des opérations déjà admises. |
| Admission et audit | Le Core est seul propriétaire des écritures. Intention, corrélation, représentation canonique et audit d’admission sont persistés avant tout effet technologique. SQLite est un adaptateur remplaçable. Politique pilote : rétention 365 jours, budget 1 Gio, alerte à 80 %, sauvegarde quotidienne et avant migration, dix sauvegardes quotidiennes conservées. |
| Récupération | Liste fermée d’actions de récupération ; aucune exemption générale Administrateur. Récupération du dernier administrateur via autorisation signée propre à l’installation et à usage unique. Un journal de secours ne peut pas servir à admettre des commandes métier. |
| Audit en panne | Une panne durable de l’audit bloque les nouvelles modifications et commandes ; consultation autorisée, diagnostic, récupération et suivi des opérations déjà admises restent possibles selon la politique fermée de récupération. |
| Langues | Français requis, anglais souhaité en V1, allemand si possible. |
| Unités | Millimètres et pouces en V1 ; préférences utilisateur et extension par grandeur physique. |
| Affichage | 1024 × 768 et 1920 × 1080, tactile, thèmes clair/sombre, charte et logo WM. |
| CRA | Exigence de conformité produit ; aucune conformité démontrée par les analyses actuelles. |
| Édition Opérateur | Modification des seuls champs d’usure des correcteurs ; autres données et correcteurs exclus. Autorisation également validée pour l’outil actuellement en broche, sous conditions machine et contrôles communs. |
| Intégration technique | Paquets NuGet versionnés, gRPC sur tubes nommés Windows, SQLite local derrière les services du Core et hôte OPC UA séparé : validés. SQLite et gRPC doivent être remplaçables derrière des contrats indépendants des fournisseurs, avec garanties et tests de conformité. |
| Documentation | Versionner les livrables structurants dans ce dépôt au fil du travail. |

## Propositions et questions ouvertes

- Contrat d’exposition V0.1 : matrice détaillée des données et actions encore à consolider ; lecture d’une structure PLC ne vaut pas autorisation de l’exposer en écriture.
- Maintenance : périmètre lecture seule validé pour V1. Liste précise des réglages et états utiles, mapping, unités, fraîcheur et droits de consultation à préciser ; aucune écriture de maintenance autorisée par ce choix.
- Blazor Interactive Server et MudBlazor : candidats à qualifier, pas dépendances adoptées dans le code.
- Implantation du collecteur Fleet, transports réseau, isolations additionnelles, démarrage système, certificats et packaging : à définir et qualifier.
- Catalogue détaillé des champs outils/correcteurs, bornes, unités et préconditions machine : à consolider ; les rôles et principes d’autorisation T2 sont désormais acquis.
- Durées commerciales de licence, responsabilités d’émission/transfert, disponibilité éventuelle d’un matériel de confiance et procédure opérationnelle de migration d’iPC : à définir par WM.
- Destination externe des sauvegardes, responsabilité d’exploitation et restauration produit complète : à définir ; les paramètres pilotes de fréquence/rétention sont acquis pour la qualification.
- Mapping Beckhoff 8xx : propriétaires PLC/CNC/application, échelles, encodage, protocole, atomicité et preuves de complétion à confirmer.
- Distribution et compatibilité des composants communs de plateforme : à définir ; ne pas copier leur code privé dans ce dépôt.
- T2.2 : le seuil de temporisation progressive après cinq échecs est acquis, mais la courbe/durée exacte reste à définir et qualifier. La fixture de test `1 s → 2 s → 4 s → 8 s` n’est pas une décision produit. Le secret temporaire de réinitialisation et la récupération signée restent à implémenter dans une tranche de récupération ultérieure.
- Pour une exposition réseau de l’authentification, ajouter une limitation indépendante par source/contexte réseau en complément du throttling par compte. Cette protection appartient au transport et ne doit pas être simulée comme une propriété du store d’identité.
- T2.3 : durées commerciales, autorités d’émission et règles de transfert restent à définir par WM ; ces questions ne bloquent pas la qualification technique du format signé, de l’identité d’installation et du comportement d’expiration.
- L’identité d’installation logicielle T2.3-B est cryptographiquement aléatoire mais pas encore matériellement scellée. La disponibilité et l’usage éventuel d’un Trusted Platform Module (TPM) ou d’un autre matériel de confiance restent à qualifier sur l’iPC cible.
- La tolérance exacte aux petits reculs d’horloge reste une politique produit à définir. La recette T2.3-B utilise deux minutes uniquement comme paramètre de qualification.

## État de réalisation

Le prototype Avalonia reste autonome. Le socle de lecture séparé utilisant les paquets plateforme est vérifié en simulation sur Windows (T0/T1). Le lot **T2.1 — autorités durables et stockage** est clôturé en simulation Windows. Le lot **T2.2 — identités locales, authentification et sessions** est également clôturé en simulation Windows. **T2.3-A et T2.3-B sont PASS LOCAL. T2.3-C est implémenté et attend sa qualification locale.** Le serveur OPC UA intégré, le raccordement Fleet et le connecteur Beckhoff sécurisé ne sont pas encore déclarés réalisés par ces preuves.

## Avancement — matrice maintenance

La [matrice de consultation](../contrats/Maintenance_Lecture_V1.md) identifie les sources PLC et les points à confirmer. Sa liste détaillée et sa présentation restent proposées ; le périmètre lecture seule est validé. L’analyse relève une affectation Y/Z suspecte, des compensations forcées à zéro et une différence d’usage du contrôle capteur entre manuel et automatique. Ces constats ne sont pas des défauts confirmés sur machine et n’ont entraîné aucune modification PLC.

## Permissions et rôles — base V1 validée

La [matrice V0.1](../securite/Permissions_Roles_V0.1.md) reste le document détaillé de travail. La validation T2 du 15 septembre 2026 fixe la base suivante : rôles Consultation, Opérateur, Régleur outils et Administrateur ; Opérateur limité à l’édition des usures pour les écritures de correcteurs, y compris en broche sous conditions ; `tool.prepare` et `tool.load` attribués à l’Opérateur ; Administrateur sans droit implicite de commande machine ; aucune consultation métier OPC UA anonyme. Les identités de service restent distinctes des identités humaines. Les détails de mapping, champs et préconditions machine ne sont pas déduits de cette validation.

## Architecture d’intégration — choix techniques validés

Le [dossier V1](../architecture/Integration_Pilote_V1_Candidate.md) propose des paquets NuGet versionnés, gRPC sur tubes nommés Windows, SQLite local derrière les services du Core et un hôte OPC UA séparé. Les quatre choix techniques sont validés ; le plan détaillé de première tranche est disponible. SQLite et gRPC sont explicitement remplaçables sans dépendance fournisseur dans les contrats métier. La migration des données et la qualification d’un nouvel adaptateur restent nécessaires. La référence directe Hmi.Runtime vers Application.Runtime doit être adaptée via des contrats et un client distant ; aucun second runtime machine n’est prévu dans l’hôte web.

## T0/T1 — socle de lecture vérifié sur Windows

Le journal reçu le 14 septembre 2026 confirme les contrôles de frontière de processus, la compilation de la plateforme, WS-AT04/WS-AT11/P6.2-D et la consommation des paquets par le pilote : **PASS LOCAL**. Le [dossier de preuve](../implementation/T0_T1_Validation_Windows_2026-09-14.md) précise la provenance et les limites.

## T2 — décisions A à F validées

La [proposition T2 V0.1](../plan/T2_Autorites_Durables_Proposition_V0.1.md) constitue le document de discussion initial. Les choix A à F ont été validés le 15 septembre 2026 avec deux ajustements explicités dans le présent registre : délai d’inactivité de 30 minutes pour l’interactif local et 10 minutes pour l’interactif distant ; aucune consultation métier OPC UA anonyme. Un éventuel sujet `Guest` local strictement lecture seule reste une capacité future à étudier, pas un contournement d’authentification. Les politiques commerciales de licence, la destination externe des sauvegardes et certains prérequis matériels restent ouverts sans bloquer l’avancement technique.

## T2.1 — autorités durables et stockage clôturés en simulation

Le journal reçu le 15 septembre 2026 confirme simultanément T2.1-A, T2.1-B, T2.1-C et la régression T0/T1 : **PASS LOCAL**. Le [dossier de preuve T2.1](../implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md) fait foi pour le périmètre simulé. Aucune qualification Beckhoff réelle n’en découle.

## T2.2 — identités locales, authentification et sessions : clôturé

T2.2-A/B/C/D sont **PASS LOCAL**. Ils qualifient les identités nominatives, comptes durables, Argon2id, sessions opaques, permissions dynamiques, throttling, commissioning du premier administrateur et stockage borné des faux identifiants. Voir les dossiers [A](../implementation/T2_2_A_Noyau_Autorite_Identites_Sessions.md), [B](../implementation/T2_2_B_Comptes_Durables_Authentification_Argon2id.md), [C](../implementation/T2_2_C_Throttling_Commissioning_Premier_Administrateur.md) et [D](../implementation/T2_2_D_Stockage_Borne_Throttling_Identites_Inconnues.md).

**État : T2.2 clôturé en simulation Windows.**

## T2.3-A — contrat et vérification de licence hors ligne signée : PASS LOCAL

Le journal reçu le 15 septembre 2026 confirme la vérification ECDSA P-256 + SHA-256 du payload canonique et versionné, les liaisons émetteur/clé/produit/installation, la période de validité et le refus des altérations ou représentations signées non canoniques. Les contrats/runtime ne dépendent pas de l’adaptateur ECDSA concret et aucune API de signature privée n’est exposée au runtime machine.

Voir le [dossier T2.3-A](../implementation/T2_3_A_Contrat_Verification_Licence_Signee.md). **État : PASS LOCAL.**

## T2.3-B — identité d’installation, renouvellement et temps de confiance : PASS LOCAL

Le journal reçu le 15 septembre 2026 confirme l’identité d’installation durable aléatoire, l’anti-rollback de `RenewalVersion`, l’idempotence, la persistance, l’expiration, la détection durable du recul d’horloge, la récupération temporelle signée et la séparation des clés d’émission/récupération. La plus haute version de renouvellement reste autoritative après récupération et aucune clé privée de signature n’est retrouvée dans SQLite.

Voir le [dossier T2.3-B](../implementation/T2_3_B_Identite_Installation_Renouvellement_Temps_Confiance.md). **État : PASS LOCAL.**

## T2.3-C — admission gouvernée par licence

La micro-tranche C est implémentée et décrite dans le [dossier T2.3-C](../implementation/T2_3_C_Admission_Gouvernee_Licence.md).

L’autorité de licence est évaluée à chaque nouvelle admission machine via un contrat générique asynchrone dans Foundation. `Machine.Runtime` ne dépend pas du runtime de licence, de SQLite ou de l’adaptateur cryptographique. `license.install` gouverne l’import d’un artefact déjà signé. Permissions humaines et licence produit restent indépendantes ; les capacités licenciées ne sont jamais fournies par le client.

Une expiration ou une incohérence temporelle doit refuser une **nouvelle** opération, sans réévaluer ni annuler rétroactivement une opération déjà admise. Les runtimes de lecture restent hors de ce verrou de mutation.

**État : implémenté/en qualification, pas PASS.** T2.3 ne sera pas clôturé avant réception du journal local T2.3-C avec les régressions précédentes toujours vertes.
