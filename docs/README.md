# Documentation du projet Magasin 8xx

## Règle de suivi documentaire

Les analyses, contrats, décisions et plans produits pour ce projet sont versionnés ici au fil des échanges. Chaque évolution structurante doit mettre à jour cet index et le registre des décisions. Une proposition n’est considérée comme acceptée qu’après validation utilisateur ; une validation documentaire ne constitue pas une preuve d’implémentation ou de qualification machine.

Ne pas réécrire les analyses historiques pour leur faire décrire les décisions ultérieures. Les compléter par un statut et une référence au document qui les actualise. Les documents de référence privés de la plateforme restent dans leur dépôt ; les liens ne donnent pas d’accès supplémentaire.

## À lire en premier

- [T2.4-A — secret temporaire de réinitialisation](implementation/T2_4_A_Secret_Temporaire_Reinitialisation_Mot_de_Passe.md) : **implémenté, qualification locale requise** ; secret 256 bits, durée initiale 15 minutes, usage unique, remplacement atomique du credential, permissions inchangées et révocation des sessions existantes du sujet.

- [T2.4 — plan audit et chemins de récupération produit](plan/T2_4_Audit_Recuperation_Produit_V0.1.md) : découpage A/B/C/D pour le secret temporaire, la récupération signée du dernier administrateur, l’audit dégradé avec journal de secours borné et la composition réelle dans CoreHost.

- [T2.3 — validation Windows consolidée](implementation/T2_3_Licences_Hors_Ligne_Validation_Windows_2026-09-15.md) : **T2.3-A/B/C/D PASS LOCAL — T2.3 clôturé** ; format signé, identité d’installation, anti-rollback, temps de confiance, admission gouvernée et composition réelle dans `MagasinOutil.CoreHost`.

- [T2.3-D — composition réelle de la licence durable dans CoreHost](implementation/T2_3_D_Composition_CoreHost_Licence_Durable.md) : **PASS LOCAL** ; `licensing.db`, identité d’installation durable et composants communs de licence réellement composés dans `MagasinOutil.CoreHost`, sans clé d’émetteur de production embarquée.

- [T2.3-C — admission gouvernée par licence](implementation/T2_3_C_Admission_Gouvernee_Licence.md) : **PASS LOCAL** ; licence évaluée à chaque nouvelle admission, permission `license.install`, lectures hors verrou et opérations déjà admises non annulées rétroactivement.

- [T2.3-B — identité d’installation, renouvellement et temps de confiance](implementation/T2_3_B_Identite_Installation_Renouvellement_Temps_Confiance.md) : **PASS LOCAL — révision courante requalifiée** ; identité d’installation durable, anti-rollback de renouvellement, borne temporelle persistée, statut d’expiration explicite et récupération du temps par artefact signé.

- [T2.3-A — contrat et vérification de licence hors ligne signée](implementation/T2_3_A_Contrat_Verification_Licence_Signee.md) : **PASS LOCAL** ; format canonique versionné, registre de clés publiques approuvées, vérification ECDSA P-256/SHA-256, refus des altérations et absence de clé privée d’émission dans le runtime.

- [T2.2-D — stockage borné du throttling des identités inconnues](implementation/T2_2_D_Stockage_Borne_Throttling_Identites_Inconnues.md) : **PASS LOCAL** ; borne des traces inconnues sans éviction des vrais comptes.

- [T2.2-C — limitation des tentatives et commissioning du premier administrateur](implementation/T2_2_C_Throttling_Commissioning_Premier_Administrateur.md) : **PASS LOCAL**.

- [T2.2-B — comptes durables et authentification Argon2id](implementation/T2_2_B_Comptes_Durables_Authentification_Argon2id.md) : **PASS LOCAL sur la révision durcie**.

- [T2.2-A — noyau d’autorité des identités et sessions](implementation/T2_2_A_Noyau_Autorite_Identites_Sessions.md) : **PASS LOCAL**.

- [T2.1 — validation Windows des autorités durables](implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md) : **T2.1-A/B/C PASS LOCAL** ; T2.1 clôturé.

- [T2 — décision sur les autorités durables](decisions/T2_Autorites_Durables_Decision_2026-09-15.md) : décisions A à F faisant autorité pour les tranches T2.

- [Décisions et points ouverts](decisions/Registre_Decisions.md) : état actuel des décisions et limites restantes.

- [Feuille de route et état d’avancement](Feuille_de_Route_Pilote_V1.md) : jalons, preuves et distinction simulation/cible.

- [Plan d’implémentation de la première tranche](plan/Premiere_Tranche_Integration_V0.1.md) et [Architecture d’intégration V1](architecture/Integration_Pilote_V1_Candidate.md).

- [Permissions et rôles V0.1](securite/Permissions_Roles_V0.1.md) et [Matrice maintenance V1](contrats/Maintenance_Lecture_V1.md).

## T2.2 — clôturé en simulation Windows

T2.2-A/B/C/D sont **PASS LOCAL**. Le lot qualifie les comptes locaux nominatifs, Argon2id, sessions opaques/révocables, permissions dynamiques, throttling, commissioning du premier administrateur et stockage borné des faux identifiants.

## T2.3 — licences hors ligne signées et temps de confiance — clôturé

**T2.3-A/B/C/D sont PASS LOCAL.**

Le lot qualifie : format signé/canonique/versionné ; vérification ECDSA P-256 + SHA-256 ; identité d’installation durable ; anti-rollback de `RenewalVersion` ; temps de confiance et récupération signée ; admission gouvernée ; `license.install` ; séparation permission/licence ; opérations déjà admises non annulées rétroactivement ; composition réelle dans `MagasinOutil.CoreHost` et indépendance du client de lecture.

Le profil T2.3-D contient volontairement zéro clé publique de production approuvée. Voir le [dossier consolidé](implementation/T2_3_Licences_Hors_Ligne_Validation_Windows_2026-09-15.md).

**État : T2.3 clôturé en simulation Windows.**

## T2.4 — audit et chemins de récupération produit

Le [plan T2.4](plan/T2_4_Audit_Recuperation_Produit_V0.1.md) fixe quatre micro-tranches.

### T2.4-A — secret temporaire de réinitialisation

**Implémenté — à qualifier localement.**

Le mécanisme :

- exige `identity.manage` pour émettre/révoquer ;
- génère 256 bits d’aléa cryptographique ;
- utilise une durée initiale de 15 minutes ;
- ne persiste jamais le secret en clair ;
- ne conserve qu’une autorisation active par sujet ;
- lie le reset à la révision de sécurité du compte ;
- remplace le credential et consomme le secret dans une transaction SQLite commune ;
- conserve exactement les permissions du compte ;
- révoque les sessions existantes du sujet après succès ;
- ne crée aucune session automatique ;
- refuse le replay et qualifie la concurrence avec un seul gagnant.

Recette :

```powershell
.\eng\Test-T24.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

### T2.4-B/C/D

- **B** : récupération signée du dernier administrateur ;
- **C** : audit dégradé et journal de secours borné incapable d’admettre une commande métier ;
- **D** : composition réelle dans `MagasinOutil.CoreHost` et non-régression globale.

Aucun code PLC n’est modifié par T2.4.

## Limites et travaux transversaux

L’identité d’installation n’est pas encore revendiquée comme matériellement scellée au Trusted Platform Module (TPM). Les politiques commerciales de licence, l’outil d’émission, les clés publiques de production, la destination externe des sauvegardes, les paramètres Argon2id finaux, Fleet, les certificats réseau, le packaging, le mapping Beckhoff 8xx et la qualification du PC industriel cible restent à définir ou qualifier.

Aucune conformité au Cyber Resilience Act (CRA) n’est déclarée par ces PASS locaux.

## Analyses historiques

- [Alignement plateforme](analyse/Analyse_Alignement_Pilote_Magasin_8xx_Plateforme_2026-09-14.md)
- [Analyse initiale Beckhoff](analyse/Analyse_Gestion_Magasin_Outils_Beckhoff_2026-09-11.md)
- [Premier écran et présentation](analyse/HMI_Magasin_Premier_Ecran_Modele_Presentation_V0_1.md)
- [Adaptation 1024 et P6.9](analyse/HMI_Magasin_Adaptation_1024_P69_2026-09-14.md)
- [T0/T1 — implémentation et lancement](implementation/T0_T1_Frontiere_Processus.md)
