# Documentation du projet Magasin 8xx

## Règle de suivi documentaire

Les analyses, contrats, décisions et plans produits pour ce projet sont versionnés ici au fil des échanges. Chaque évolution structurante doit mettre à jour cet index et le registre des décisions. Une proposition n’est considérée comme acceptée qu’après validation utilisateur ; une validation documentaire ne constitue pas une preuve d’implémentation ou de qualification machine.

Ne pas réécrire les analyses historiques pour leur faire décrire les décisions ultérieures. Les compléter par un statut et une référence au document qui les actualise. Les documents de référence privés de la plateforme restent dans leur dépôt ; les liens ne donnent pas d’accès supplémentaire.

## À lire en premier

- [T2.3-C — admission gouvernée par licence](implementation/T2_3_C_Admission_Gouvernee_Licence.md) : **implémenté, qualification locale requise** ; décision de licence évaluée à chaque nouvelle admission, permission `license.install`, lectures hors verrou et opérations déjà admises non annulées rétroactivement.

- [T2.3-B — identité d’installation, renouvellement et temps de confiance](implementation/T2_3_B_Identite_Installation_Renouvellement_Temps_Confiance.md) : **PASS LOCAL historique ; révision courante à requalifier** après introduction des statuts explicites `Expired` / `NotYetValid` ; aucun affaiblissement du mécanisme d’expiration.

- [T2.3-A — contrat et vérification de licence hors ligne signée](implementation/T2_3_A_Contrat_Verification_Licence_Signee.md) : **PASS LOCAL** ; format canonique versionné, registre de clés publiques approuvées, vérification ECDSA P-256/SHA-256, refus des altérations et absence de clé privée d’émission dans le runtime.

- [T2.2-D — stockage borné du throttling des identités inconnues](implementation/T2_2_D_Stockage_Borne_Throttling_Identites_Inconnues.md) : **PASS LOCAL** ; borne par défaut de 256 traces inconnues, éviction limitée aux identités sans compte durable et protection des compteurs de vrais comptes.

- [T2.2-C — limitation des tentatives et commissioning du premier administrateur](implementation/T2_2_C_Throttling_Commissioning_Premier_Administrateur.md) : **PASS LOCAL** ; throttling durable à partir de cinq échecs, remise à zéro après succès et commissioning d’installation à usage unique sans administrateur universel.

- [T2.2-B — comptes durables et authentification Argon2id](implementation/T2_2_B_Comptes_Durables_Authentification_Argon2id.md) : **PASS LOCAL sur la révision durcie** ; travail Argon2id également pour un utilisateur inconnu, réponse générique pour compte désactivé et paramètres de coût mesurés localement.

- [T2.2-A — noyau d’autorité des identités et sessions](implementation/T2_2_A_Noyau_Autorite_Identites_Sessions.md) : **PASS LOCAL** ; deux identités nominatives, sessions opaques, liaison client/cible, expiration, activité humaine, révocation, désactivation et permissions dynamiques.

- [T2.1 — validation Windows des autorités durables](implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md) : **T2.1-A, T2.1-B et T2.1-C PASS LOCAL** avec régression T0/T1 verte ; T2.1 est clôturé en simulation Windows.

- [T2 — décision sur les autorités durables du 15 septembre 2026](decisions/T2_Autorites_Durables_Decision_2026-09-15.md) : choix A à F validés, délais de session local/distant, rôles, licences, admission durable, audit et récupération.

- [Décisions et points ouverts](decisions/Registre_Decisions.md) : état actuel des décisions et limites restantes.

- [Feuille de route et état d’avancement](Feuille_de_Route_Pilote_V1.md) : jalons, preuves et distinction entre simulation et qualification sur cible.

- [T2 — proposition groupée historique](plan/T2_Autorites_Durables_Proposition_V0.1.md) : proposition qui a servi de base à la décision T2 ; conserver ce document pour la traçabilité des options examinées.

- [Plan d’implémentation de la première tranche](plan/Premiere_Tranche_Integration_V0.1.md) : lots T0–T6, répartition des dépôts, dépendances et recette.

- [Architecture d’intégration V1](architecture/Integration_Pilote_V1_Candidate.md) : composants communs, processus, contrats, stockage et choix techniques validés.

- [Permissions et rôles V0.1](securite/Permissions_Roles_V0.1.md) : matrice qui a préparé les rôles ; la décision T2 du 15 septembre fait désormais autorité pour les attributions validées.

- [Matrice maintenance V1](contrats/Maintenance_Lecture_V1.md) : sources PLC examinées, unités établies ou à confirmer, valeurs configurées/appliquées et questions automatisme.

- [Contrat d’exposition V0.1](analyse/Contrat_Exposition_Magasin_8xx_V0.1_2026-09-14.md) : proposition détaillée OPC UA ; maintenance V1 validée en consultation seule, autres détails fonctionnels à consolider.
- [Déploiement, OPC UA et Fleet](analyse/Analyse_Deploiement_OPCUA_Fleet_Magasin_8xx_2026-09-14.md) : analyse de la plateforme et profils de déploiement.
- [État du prototype Avalonia](hmi/README.md) : documentation du code actuellement présent, distincte de la cible web.

## T2.2 — clôturé en simulation Windows

T2.2-A, T2.2-B durci, T2.2-C et T2.2-D sont **PASS LOCAL** avec régression T0/T1 + T2.1 verte.

Le lot qualifie notamment : comptes locaux nominatifs durables, authentification Argon2id hors ligne, sessions opaques et révocables, liaison client/cible, permissions dynamiques, protection anti-énumération, temporisation durable à partir de cinq échecs, commissioning atomique du premier administrateur sans compte universel et persistance bornée des faux identifiants.

**État : T2.2 clôturé.**

## T2.3 — licences hors ligne signées et temps de confiance

**T2.3-A est PASS LOCAL.** La vérification hors ligne du format signé, de la canonicalité, de l’émetteur, de la clé, du produit, de l’installation et de la période est qualifiée sans clé privée dans le runtime machine.

**T2.3-B possède un PASS LOCAL historique, mais la révision courante doit être requalifiée.** Le raffinement des statuts d’évaluation a remplacé l’ancien résultat métier générique `VerificationFailed` par les états explicites `Expired` et `NotYetValid`. Le premier rerun s’est arrêté parce que l’assertion B attendait encore l’ancien contrat ; le runtime avait bien détecté l’expiration. Le test est corrigé et doit repasser avant de restaurer le statut PASS courant.

**T2.3-C est implémenté et attend sa qualification locale.** Il raccorde l’autorité de licence aux nouvelles admissions machine tout en gardant les permissions humaines indépendantes. `license.install` gouverne l’import d’un artefact signé. Une opération déjà admise n’est pas annulée lors d’une expiration ultérieure, et les lectures restent hors du verrou de mutation.

L’identité V1 est cryptographiquement aléatoire mais n’est pas encore revendiquée comme matériellement scellée au Trusted Platform Module (TPM). Un fournisseur matériel pourra être qualifié ultérieurement derrière les mêmes contrats.

Les limites restent explicites : simulation Windows seulement, aucune qualification Beckhoff réelle ni validation finale sur le PC industriel cible.

## Analyses historiques

| Document | Statut et articulation |
|---|---|
| [Alignement plateforme](analyse/Analyse_Alignement_Pilote_Magasin_8xx_Plateforme_2026-09-14.md) | Diagnostic initial ; plusieurs questions ont depuis été tranchées. Consulter le registre actuel. |
| [Analyse initiale Beckhoff](analyse/Analyse_Gestion_Magasin_Outils_Beckhoff_2026-09-11.md) | Historique du besoin et des premières orientations ; ne fixe pas les choix actuels. |
| [Premier écran et présentation](analyse/HMI_Magasin_Premier_Ecran_Modele_Presentation_V0_1.md) | Proposition graphique historique ; ne prouve pas l’implémentation de tous les parcours. |
| [Adaptation 1024 et P6.9](analyse/HMI_Magasin_Adaptation_1024_P69_2026-09-14.md) | Proposition historique ; dimensions et cible graphique actualisées par les échanges et le registre. |

La synchronisation de ces documents ne modifie aucun code applicatif ou PLC.

- [T0/T1 — implémentation et lancement](implementation/T0_T1_Frontiere_Processus.md)
