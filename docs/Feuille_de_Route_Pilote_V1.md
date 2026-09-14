# Feuille de route du pilote Magasin 8xx

Date : 14 septembre 2026. Statut : plan proposé ; décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md). Aucun changement de code dans cette étape.

## 1. Où en sommes-nous ?

Nous sommes dans le cadrage de l’intégration plateforme, après la réalisation d’un prototype graphique Avalonia simulé. Le parcours opérateur et plusieurs contraintes visuelles ont été exercés ; la cible web et les services produit ne sont pas encore implémentés dans le pilote.

Les analyses des profils, d’OPC UA, de Fleet, de la maintenance et des permissions sont versionnées. Le diagnostic architectural est établi sur les révisions examinées. Le périmètre principal est décidé, mais le contrat détaillé et les choix d’industrialisation restent ouverts. Aucun pourcentage global n’est donné : l’effort restant dépend notamment du protocole PLC et de la réutilisation réelle des composants communs.

Base pilote vérifiée : b550f944b1153a88ccece7226c18c47436c0a987. Plateforme examinée : bc0819d0774c67e920481a7e0312d12f55a291a8. Les preuves de la plateforme et le programme PLC existant ne sont pas une qualification de l’application magasin intégrée.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé ; diagnostic initial établi | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | En cours — étape actuelle | Décisions nécessaires à la première tranche prises ; questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats et versions, transport entre processus, stockage et composition | Analysée partiellement ; à formaliser | Plan de dépendances et de livraison validé, sans duplication des autorités |
| 3. Première tranche intégrée simulée | HMI web locale, runtime séparé, opération commune HMI/OPC UA, état publié dans Fleet | À réaliser | Une opération bout en bout, refus gouvernés, audit durable et coupures exercés |
| 4. Couverture fonctionnelle V1 | Gestion des outils, correcteurs, usures opérateur, maintenance consultative, langues/unités et administration | À réaliser | Matrice V1 couverte et exigences vérifiées en simulation |
| 5. Raccordement Beckhoff 8xx | Lectures puis écritures/opérations réelles, Secure ADS, synchronisation PLC/CNC | Sources partiellement analysées ; intégration non réalisée | Preuves sur le banc cible pour chaque capacité annoncée |
| 6. Qualification produit et livraison pilote | Installation, profils, reprise, matériel réel, dossiers de preuve et exploitation | À préparer dès maintenant ; validation finale ultérieure | Critères de recette produit satisfaits et limites acceptées |

Les étapes indiquent des jalons, pas une obligation d’attendre la fin de chaque lot pour commencer toute autre activité. L’examen du PLC, la qualification technique du navigateur et la préparation du déploiement peuvent avancer pendant l’intégration simulée.

## 3. Finir le cadrage sans attendre toutes les réponses

Décisions déjà acquises :
- application autonome utilisant la plateforme, direction HMI web hébergée localement ;
- runtime Machine/Application séparé de l’hôte HMI ;
- quatre profils de composition, OPC UA en exposition V1 et raccordement Fleet en lecture seule ;
- comptes locaux, licences temporaires, audit durable et politiques de dégradation ;
- maintenance en consultation seule ;
- Opérateur autorisé à modifier uniquement les usures, y compris en broche ;
- formats 1024 × 768 et 1920 × 1080, thèmes WM, français requis, anglais souhaité, allemand si possible, mm/pouces.

À regrouper dans une prochaine proposition cohérente :
1. Compléter les rôles et les lectures publiques, les identités de service et les exceptions de récupération.
2. Préciser les contrats d’édition : champs, révisions, résultat partiel et suivi après perte de réponse.
3. Proposer stockage durable, transport local et distribution des composants plateforme.
4. Identifier les choix qui bloquent seulement le raccordement réel : échelles, propriétaires PLC/CNC, protocole et conditions d’écriture en broche.

L’objectif est de soumettre des décisions groupées et motivées plutôt que d’enchaîner des questions isolées. Un choix non validé reste explicitement proposé.

## 4. Première tranche intégrée proposée

Une petite tranche doit éprouver l’architecture complète avant d’étendre tous les écrans :

- même machine simulée et même service d’opérations pour la HMI et OPC UA ;
- connexion d’un utilisateur local et permissions réelles ;
- lecture d’un rack et d’un outil ;
- modification d’une usure avec révision attendue ;
- préparation d’un outil avec distinction acceptation/résultat observé ;
- vérification d’une licence temporaire locale ;
- audit durable des décisions et résultats ;
- publication de l’état courant dans Fleet ;
- redémarrage du client ou indisponibilité de Fleet sans arrêt du runtime ni rejeu de commande.

Les détails de la bibliothèque graphique restent à qualifier. Les fonctions de démonstration Executive ne doivent pas servir d’autorité de comptes ou de licences produit.

La première tranche ne constitue pas la livraison V1 : elle démontre le chemin commun, avec des preuves limitées à la simulation. Les scénarios comprennent expiration de licence, audit indisponible, refus de permission, conflit de révision et réponse perdue.

## 5. Compléter la V1 et raccorder le réel

La couverture fonctionnelle inclut les données d’outils prévues au contrat, les correcteurs applicables, la consultation maintenance, les comptes/rôles, la gestion des licences, l’audit, les langues et unités, les profils et leurs diagnostics. Aucune réduction silencieuse à la seule usure n’est permise pour les utilisateurs autorisés à gérer davantage de données.

Le raccordement Beckhoff procède par preuves : connexion sécurisée et lecture ; vérification du mapping ; écriture contrôlée ; opérations physiques et réconciliation. Les gardes PLC et la coexistence avec la HMI Beckhoff/CNC doivent être vérifiées sur le chemin réel. Un succès sur le simulateur ne dispense pas de ces contrôles.

Les questions automatisme identifiées dans la matrice maintenance restent ouvertes, dont l’affectation Y/Z suspecte et les valeurs remplacées avant utilisation. Aucun correctif PLC n’est présumé nécessaire ou autorisé par le présent plan.

## 6. Travail transversal dès les premières étapes

- **Conformité CRA** : chantier produit à mener dès le cadrage, avec périmètre, responsabilités, exigences et preuves à établir. Cette feuille de route ne constitue pas une évaluation réglementaire ni une déclaration de conformité.
- **Sécurité produit** : identités, certificats, protection des secrets, droits, reprise et diagnostic dès les contrats ; ne pas reporter ces sujets à la dernière étape.
- **Déploiement et maintenance** : installation hors ligne, démarrage, sauvegarde/restauration, mise à jour et retour arrière à définir puis qualifier.
- **Expérience opérateur** : tactile, clavier, perte de connexion, explications de refus et affichage clair de données périmées.
- **Documentation et traçabilité** : décisions, exigences, versions et résultats réels tenus à jour dans Git.

## 7. Prochaine livraison documentaire

Un dossier d’architecture d’intégration V1 qui regroupe :
- composants conservés et extensions communes ;
- répartition plateforme/pilote et processus ;
- proposition de transport, stockage et distribution ;
- contrats de la première tranche et matrice de preuves ;
- décisions ouvertes regroupées, avec recommandation et impact.

La rédaction de ce dossier reste dans le périmètre actuel sans modification de code. Le passage à l’implémentation doit être explicite après revue de ce dossier.

## 8. Pilotage des progrès

Suivre chaque capacité avec quatre états distincts : spécifiée, implémentée, vérifiée en simulation, vérifiée sur cible. Conserver les exigences de livraison et les preuves produit séparément. Aucun statut « terminé » ne doit regrouper une simple maquette, un test simulé et une qualification réelle.

Les livrables restent liés depuis [l’index](README.md). Le [registre](decisions/Registre_Decisions.md) fait autorité sur les validations utilisateur ; les documents historiques conservent leur date et leurs limites.
