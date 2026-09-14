# Registre des décisions — Magasin 8xx

Actualisé le 14 septembre 2026. Source : validations explicites dans les échanges du projet. Ce registre actualise les analyses historiques sans en modifier les constats datés.

## Décisions retenues

| Sujet | Décision |
|---|---|
| Positionnement | Pilote autonome utilisant la plateforme, intégrable dans une autre application ultérieurement. |
| Interface | Direction HMI web commune, hébergée localement sur le PC industriel, sans dépendance Internet ; remplacement effectif d’Avalonia après qualification. |
| Isolation | Runtime Machine/Application séparé de l’hôte HMI dès la première version. |
| Clients | Plusieurs clients souhaités ; actions selon autorisations. La tablette est un exemple de client. |
| Profils | Local isolé, Intégration usine, Rattaché au parc et Intégration complète, suivant la matrice de l’analyse de déploiement. Aucun profil par défaut choisi. |
| OPC UA | Exposition des données et actions en V1 ; connexion à des équipements OPC UA ensuite. |
| Maintenance V1 | Consultation des réglages et états utiles dans la HMI et OPC UA. Modification des réglages, apprentissage des positions (teach), changements de mode et mouvements de maintenance hors exposition V1 ; restent dans la HMI Beckhoff. La gestion des données d’outils et les opérations Préparer/Charger restent dans leur périmètre distinct. |
| Fleet | Reprendre son rôle dans la plateforme et le raccordement commun, en supervision en lecture seule dans le périmètre examiné. |
| Comptes | Comptes locaux utilisables sans Microsoft ni réseau externe ; Microsoft est l’environnement de comptes existant chez WM. Actions et administration sous identités nominatives. |
| Licences | Mécanisme WM commun à construire ; licences temporaires dès V1, vérifiables localement. À expiration : nouvelles modifications et commandes bloquées, consultation selon droits et poursuite des opérations admises. |
| Audit | Durable, politique configurable. Panne d’audit durable : bloquer les nouvelles modifications et commandes ; préserver consultation, récupération et suivi des opérations admises. Les exceptions de récupération restent à définir. |
| Langues | Français requis, anglais souhaité en V1, allemand si possible. |
| Unités | Millimètres et pouces en V1 ; préférences utilisateur et extension par grandeur physique. |
| Affichage | 1024 × 768 et 1920 × 1080, tactile, thèmes clair/sombre, charte et logo WM. |
| CRA | Exigence de conformité produit ; aucune conformité démontrée par les analyses actuelles. |
| Documentation | Versionner les livrables structurants dans ce dépôt au fil du travail. |

## Propositions et questions ouvertes

- Contrat d’exposition V0.1 : matrice de données et d’actions à valider ; lecture d’une structure PLC ne vaut pas autorisation de l’exposer en écriture.
- Maintenance : périmètre lecture seule validé pour V1. Liste précise des réglages et états utiles, mapping, unités, fraîcheur et droits de consultation à préciser ; aucune écriture de maintenance autorisée par ce choix.
- Blazor Interactive Server et MudBlazor : candidats à qualifier, pas dépendances adoptées dans le code.
- Implantation du collecteur Fleet, transports internes, isolations additionnelles, démarrage système, certificats et packaging : à définir.
- Permissions détaillées, identités de service OPC UA, politique de lecture anonyme, binding et renouvellement des licences : à définir.
- Mapping Beckhoff 8xx : propriétaires PLC/CNC/application, échelles, encodage, protocole, atomicité et preuves de complétion à confirmer.
- Distribution et compatibilité des composants communs de plateforme : à définir ; ne pas copier leur code privé dans ce dépôt.

## État de réalisation

Le code actuel reste un prototype Avalonia simulé. Les décisions de cible ne signifient pas que les services plateforme, le serveur OPC UA, le raccordement Fleet ou le connecteur Beckhoff sécurisé sont déjà intégrés au pilote. Aucun nouveau test machine n’est associé à cette synchronisation documentaire.
