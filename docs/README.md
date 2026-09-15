# Documentation du projet Magasin 8xx

## Règle de suivi documentaire

Les analyses, contrats, décisions et plans produits pour ce projet sont versionnés ici au fil des échanges. Chaque évolution structurante doit mettre à jour cet index et le registre des décisions. Une proposition n’est considérée comme acceptée qu’après validation utilisateur ; une validation documentaire ne constitue pas une preuve d’implémentation ou de qualification machine.

Ne pas réécrire les analyses historiques pour leur faire décrire les décisions ultérieures. Les compléter par un statut et une référence au document qui les actualise. Les documents de référence privés de la plateforme restent dans leur dépôt ; les liens ne donnent pas d’accès supplémentaire.

## À lire en premier

- [T2.2-C — limitation des tentatives et commissioning du premier administrateur](implementation/T2_2_C_Throttling_Commissioning_Premier_Administrateur.md) : implémenté, **qualification locale requise** ; throttling durable à partir de cinq échecs et commissioning d’installation à usage unique sans administrateur universel.

- [T2.2-B — comptes durables et authentification Argon2id](implementation/T2_2_B_Comptes_Durables_Authentification_Argon2id.md) : **PASS LOCAL** ; contrats fournisseur-indépendants, adaptateurs Argon2id et SQLite séparés, authentification hors ligne, révisions de sécurité optimistes et mesure pilote de 309 ms par hash.

- [T2.2-A — noyau d’autorité des identités et sessions](implementation/T2_2_A_Noyau_Autorite_Identites_Sessions.md) : **PASS LOCAL** ; deux identités nominatives, sessions opaques, liaison client/cible, expiration, activité humaine, révocation, désactivation et permissions dynamiques.

- [T2.1 — validation Windows des autorités durables](implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md) : **T2.1-A, T2.1-B et T2.1-C PASS LOCAL** avec régression T0/T1 verte ; T2.1 est clôturé en simulation Windows.

- [T2 — décision sur les autorités durables du 15 septembre 2026](decisions/T2_Autorites_Durables_Decision_2026-09-15.md) : choix A à F validés, délais de session local/distant, rôles, licences, admission durable, audit et récupération.

- [Décisions et points ouverts](decisions/Registre_Decisions.md) : état actuel des décisions et limites de T2.2.

- [Feuille de route et état d’avancement](Feuille_de_Route_Pilote_V1.md) : jalons, preuves et distinction entre simulation et qualification sur cible.

- [T2 — proposition groupée historique](plan/T2_Autorites_Durables_Proposition_V0.1.md) : proposition qui a servi de base à la décision T2 ; conserver ce document pour la traçabilité des options examinées.

- [Plan d’implémentation de la première tranche](plan/Premiere_Tranche_Integration_V0.1.md) : lots T0–T6, répartition des dépôts, dépendances et recette.

- [Architecture d’intégration V1](architecture/Integration_Pilote_V1_Candidate.md) : composants communs, processus, contrats, stockage et choix techniques validés.

- [Permissions et rôles V0.1](securite/Permissions_Roles_V0.1.md) : matrice qui a préparé les rôles ; la décision T2 du 15 septembre fait désormais autorité pour les attributions validées.

- [Matrice maintenance V1](contrats/Maintenance_Lecture_V1.md) : sources PLC examinées, unités établies ou à confirmer, valeurs configurées/appliquées et questions automatisme.

- [Contrat d’exposition V0.1](analyse/Contrat_Exposition_Magasin_8xx_V0.1_2026-09-14.md) : proposition détaillée OPC UA ; maintenance V1 validée en consultation seule, autres détails fonctionnels à consolider.
- [Déploiement, OPC UA et Fleet](analyse/Analyse_Deploiement_OPCUA_Fleet_Magasin_8xx_2026-09-14.md) : analyse de la plateforme et profils de déploiement.
- [État du prototype Avalonia](hmi/README.md) : documentation du code actuellement présent, distincte de la cible web.

## T2.2-C — état

Le sous-jalon couvre le commissioning à usage unique du premier administrateur et la limitation des tentatives d’authentification répétées. Le seuil de départ de la temporisation est fixé à cinq échecs ; la courbe exacte reste configurable et n’est pas figée silencieusement comme politique produit. La recette utilise `1 s → 2 s → 4 s → 8 s` uniquement comme fixture de test. Le secret d’activation du commissioning est propre à l’installation, fourni par un canal séparé et ne doit jamais être stocké en clair. Aucun administrateur universel ni mot de passe par défaut n’est introduit.

**État : implémenté — à qualifier localement.**

## Analyses historiques

| Document | Statut et articulation |
|---|---|
| [Alignement plateforme](analyse/Analyse_Alignement_Pilote_Magasin_8xx_Plateforme_2026-09-14.md) | Diagnostic initial ; plusieurs questions ont depuis été tranchées. Consulter le registre actuel. |
| [Analyse initiale Beckhoff](analyse/Analyse_Gestion_Magasin_Outils_Beckhoff_2026-09-11.md) | Historique du besoin et des premières orientations ; ne fixe pas les choix actuels. |
| [Premier écran et présentation](analyse/HMI_Magasin_Premier_Ecran_Modele_Presentation_V0_1.md) | Proposition graphique historique ; ne prouve pas l’implémentation de tous les parcours. |
| [Adaptation 1024 et P6.9](analyse/HMI_Magasin_Adaptation_1024_P69_2026-09-14.md) | Proposition historique ; dimensions et cible graphique actualisées par les échanges et le registre. |

La synchronisation de ces documents ne modifie aucun code applicatif ou PLC.

- [T0/T1 — implémentation et lancement](implementation/T0_T1_Frontiere_Processus.md)
