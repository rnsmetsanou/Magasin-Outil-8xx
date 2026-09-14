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
- T2.2-B : bibliothèque de dérivation de mot de passe Argon2id et paramètres de coût à qualifier avant stockage de mots de passe produit ; ne pas inventer de cryptographie maison.

## État de réalisation

Le prototype Avalonia reste autonome. Le socle de lecture séparé utilisant les paquets plateforme est vérifié en simulation sur Windows (T0/T1). Le lot **T2.1 — autorités durables et stockage** est clôturé en simulation Windows : T2.1-A, T2.1-B et T2.1-C sont PASS LOCAL, avec régression T0/T1 verte. **T2.2-A est maintenant implémenté et en attente de qualification locale** ; aucun PASS n’est déclaré avant exécution de `Test-T22.ps1`. Les mots de passe, licences produit, serveur OPC UA intégré, raccordement Fleet et connecteur Beckhoff sécurisé ne sont pas encore déclarés réalisés par ces preuves.

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

Le journal reçu le 15 septembre 2026 confirme simultanément :

- **T2.1-C PASS LOCAL** : composition réelle de `Platform.Poc.Persistence.Sqlite` par `MagasinOutil.CoreHost`, base durable créée, module métier et client de lecture non couplés à SQLite ;
- **T2.1-A PASS LOCAL** : invariants de sessions et d’admission provider-indépendants ;
- **T2.1-B PASS LOCAL** : migration, atomicité admission + audit, rollback, déduplication concurrente, persistance et sauvegarde/restauration ;
- régression **T0/T1 PASS LOCAL**.

Le [dossier de preuve T2.1](../implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md) fait foi pour le périmètre simulé. T2.1 est clôturé ; aucune qualification Beckhoff réelle n’en découle.

## T2.2-A — noyau d’autorité implémenté, qualification locale requise

La première micro-tranche T2.2 est implémentée dans `Platform.Poc.Identity.Runtime` et décrite dans le [dossier T2.2-A](../implementation/T2_2_A_Noyau_Autorite_Identites_Sessions.md). Elle introduit deux identités nominatives possibles, références de session opaques, liaison sujet/client/cible, délais d’inactivité local/distant, activité humaine explicite, révocation, désactivation et reconstruction des permissions à chaque résolution.

La recette `eng/Test-T22.ps1` rejoue T0/T1 + T2.1 avant les contrôles T2.2-A. **État : implémenté/en qualification, pas PASS.**

Le stockage des mots de passe, le commissioning du premier administrateur, les secrets temporaires et la récupération signée restent volontairement hors de T2.2-A et appartiennent à T2.2-B après qualification Argon2id.