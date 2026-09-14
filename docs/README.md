# Documentation du projet Magasin 8xx

## Règle de suivi documentaire

Les analyses, contrats, décisions et plans produits pour ce projet sont versionnés ici au fil des échanges. Chaque évolution structurante doit mettre à jour cet index et le registre des décisions. Une proposition n’est considérée comme acceptée qu’après validation utilisateur ; une validation documentaire ne constitue pas une preuve d’implémentation ou de qualification machine.

Ne pas réécrire les analyses historiques pour leur faire décrire les décisions ultérieures. Les compléter par un statut et une référence au document qui les actualise. Les documents de référence privés de la plateforme restent dans leur dépôt ; les liens ne donnent pas d’accès supplémentaire.

## À lire en premier

- [Plan d’implémentation de la première tranche](plan/Premiere_Tranche_Integration_V0.1.md) : lots T0–T6, répartition des dépôts, dépendances, décisions restantes et recette ; passage au code non encore engagé.

- [Architecture d’intégration V1](architecture/Integration_Pilote_V1_Candidate.md) : composants communs, processus, contrats, stockage et quatre choix techniques validés ; stockage et transport remplaçables, plan détaillé à préparer.

- [Feuille de route et état d’avancement](Feuille_de_Route_Pilote_V1.md) : étapes proposées, jalons et distinction entre prototype, intégration et qualification.

- [Permissions et rôles V0.1](securite/Permissions_Roles_V0.1.md) : rôles configurables et droits communs HMI/OPC UA ; édition Opérateur limitée aux usures validée, autres attributions à confirmer.

- [Matrice maintenance V1](contrats/Maintenance_Lecture_V1.md) : sources PLC examinées, unités établies ou à confirmer, valeurs configurées/appliquées et questions automatisme.

- [Décisions et points ouverts](decisions/Registre_Decisions.md).
- [Contrat d’exposition V0.1](analyse/Contrat_Exposition_Magasin_8xx_V0.1_2026-09-14.md) : proposition détaillée OPC UA ; maintenance V1 validée en consultation seule, autres détails fonctionnels à valider.
- [Déploiement, OPC UA et Fleet](analyse/Analyse_Deploiement_OPCUA_Fleet_Magasin_8xx_2026-09-14.md) : analyse de la plateforme ; quatre profils acceptés dans l’échange suivant, autres choix ouverts détaillés dans le registre.
- [État du prototype Avalonia](hmi/README.md) : documentation du code actuellement présent, distincte de la cible web.

## Analyses historiques

| Document | Statut et articulation |
|---|---|
| [Alignement plateforme](analyse/Analyse_Alignement_Pilote_Magasin_8xx_Plateforme_2026-09-14.md) | Diagnostic initial ; plusieurs questions ont depuis été tranchées. Consulter le registre actuel. |
| [Analyse initiale Beckhoff](analyse/Analyse_Gestion_Magasin_Outils_Beckhoff_2026-09-11.md) | Historique du besoin et des premières orientations ; ne fixe pas les choix actuels. |
| [Premier écran et présentation](analyse/HMI_Magasin_Premier_Ecran_Modele_Presentation_V0_1.md) | Proposition graphique historique ; ne prouve pas l’implémentation de tous les parcours. |
| [Adaptation 1024 et P6.9](analyse/HMI_Magasin_Adaptation_1024_P69_2026-09-14.md) | Proposition historique ; dimensions et cible graphique actualisées par les échanges et le registre. |

La synchronisation de ces documents ne modifie aucun code applicatif ou PLC.

- [T0/T1 — implémentation et lancement](implementation/T0_T1_Frontiere_Processus.md)
