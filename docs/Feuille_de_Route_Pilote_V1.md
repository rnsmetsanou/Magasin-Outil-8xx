# Feuille de route du pilote Magasin 8xx

Date : 15 septembre 2026. Statut : feuille de route actualisée après clôture locale de T2.3 ; décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md).

## 1. Où en sommes-nous ?

Le socle **T0/T1** est **PASS LOCAL** sur Windows.

Le lot **T2.1 — autorités durables et stockage** est **PASS LOCAL et clôturé** : contrats fournisseur-indépendants, admission + audit atomiques avant effet technologique, déduplication concurrente, persistance/sauvegarde SQLite et composition du fournisseur côté CoreHost.

Le lot **T2.2 — identités locales, authentification et sessions** est **PASS LOCAL et clôturé** : comptes nominatifs durables, Argon2id, sessions opaques/révocables, permissions dynamiques, throttling durable, premier administrateur commissionné sans compte universel et stockage borné des faux identifiants.

Le lot **T2.3 — licences hors ligne signées et temps de confiance** est **PASS LOCAL et clôturé** :

- A : format signé et vérification cryptographique ;
- B : identité d’installation, anti-rollback, temps de confiance et récupération temporelle ;
- C : admission gouvernée par licence ;
- D : composition réelle de la licence durable dans `MagasinOutil.CoreHost`.

La prochaine tranche est **T2.4 — audit et chemins de récupération produit**.

La connexion Beckhoff réelle, la récupération produit complète, la qualification du PC industriel et la qualification produit restent hors des jalons déjà clôturés.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | Principales décisions V1 acquises | Questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats, transport, stockage et composition | T0/T1 + T2.1 + T2.2 + T2.3 vérifiés | Frontières communes qualifiées sans duplication des autorités |
| 3. Première tranche intégrée simulée | Identité réelle, opération commune, licence, audit, HMI/OPC UA/Fleet | En cours — T2.3 clôturé, T2.4 prochain | Une opération bout en bout avec refus gouvernés, audit durable et coupures exercés |
| 4. Couverture fonctionnelle V1 | Outils, correcteurs, usures, maintenance consultative, langues/unités, administration | À réaliser | Matrice V1 couverte et vérifiée en simulation |
| 5. Raccordement Beckhoff 8xx | Lectures puis écritures/opérations réelles, Secure ADS, synchronisation PLC/CNC | Sources partiellement analysées | Preuves sur banc cible pour chaque capacité annoncée |
| 6. Qualification produit et livraison pilote | Installation, profils, reprise, matériel réel, dossiers de preuve | À préparer | Critères de recette produit satisfaits et limites acceptées |

## 3. Décisions déjà acquises

- application autonome utilisant la plateforme ;
- runtime Machine/Application séparé de l’HMI ;
- HMI web locale comme direction cible ;
- quatre profils de composition ;
- OPC UA en exposition V1 et Fleet en lecture seule ;
- comptes locaux nominatifs utilisables hors ligne ;
- quatre rôles initiaux : Consultation, Opérateur, Régleur outils, Administrateur ;
- aucune consultation métier OPC UA anonyme ;
- sessions opaques liées au sujet, client et cible ;
- inactivité initiale : 30 min local interactif, 10 min distant interactif ; durée absolue 8 h ;
- authentification Argon2id avec paramètres versionnés et benchmark à refaire sur cible ;
- throttling progressif démarrant au cinquième échec ; courbe exacte configurable ;
- commissioning du premier administrateur par secret d’activation propre à l’installation, à usage unique ;
- licence locale signée, liée à une identité d’installation et sans clé privée d’émission sur la machine ;
- renouvellement de licence monotone et récupération du temps par artefact signé ;
- expiration/incohérence temporelle bloquant les nouvelles opérations licenciées, pas les opérations déjà admises ;
- `license.install` comme permission explicite d’import d’un artefact déjà signé ;
- composition concrète de licence uniquement côté CoreHost ;
- SQLite et gRPC remplaçables derrière des contrats ;
- maintenance V1 en consultation seule ;
- Opérateur autorisé à modifier uniquement les usures, y compris en broche sous conditions, et à Préparer/Charger ;
- formats 1024 × 768 et 1920 × 1080, tactile, thèmes WM, français requis, anglais souhaité, allemand si possible, mm/pouces.

## 4. T2.2 — clôturé

T2.2-A/B/C/D sont **PASS LOCAL** en simulation Windows. Les invariants de compte, authentification, session, permission, throttling et premier commissioning sont qualifiés.

Le secret temporaire de réinitialisation de mot de passe et la récupération signée du dernier administrateur sont volontairement reportés vers **T2.4**, car ils appartiennent aux chemins fermés de récupération et non au fonctionnement nominal de T2.2.

## 5. T2.3 — licences hors ligne signées et temps de confiance — clôturé

### T2.3-A — contrat de licence et vérification cryptographique : PASS LOCAL

Format versionné/canonique, ECDSA P-256 + SHA-256, clés publiques approuvées, liaisons émetteur/clé/produit/installation, capacités signées, période de validité et refus des altérations.

### T2.3-B — installation durable et temps de confiance : PASS LOCAL

Identité d’installation durable, persistance/revalidation, anti-rollback de `RenewalVersion`, borne UTC persistée, temps monotone, expiration, détection de recul d’horloge, récupération temporelle signée et absence de clés privées dans le store.

### T2.3-C — admission gouvernée par licence : PASS LOCAL

`license.install`, refus des nouvelles mutations lorsque la licence n’est pas autoritative, séparation permission/licence, opérations déjà admises non annulées rétroactivement, capacités côté Core et lectures hors du verrou de mutation.

### T2.3-D — composition réelle CoreHost : PASS LOCAL

`MagasinOutil.CoreHost` compose le store de licence, l’identité d’installation et l’autorité durable. `licensing.db` est réel, l’identité survit au redémarrage et le client de lecture reste indépendant des fournisseurs concrets. Le profil de simulation démarre avec zéro clé publique de production approuvée.

Voir le dossier consolidé `implementation/T2_3_Licences_Hors_Ligne_Validation_Windows_2026-09-15.md`.

**État : T2.3 clôturé en simulation Windows.**

Les durées commerciales, l’outil d’émission de production, les règles de transfert d’iPC, les clés publiques de production et un éventuel scellement TPM restent à définir/qualifier sans rouvrir les invariants techniques T2.3.

## 6. T2.4 — audit et chemins de récupération produit

Objectif : qualifier les chemins exceptionnels sans créer de bypass général des autorités nominales.

Décisions déjà acquises à implémenter :

- liste fermée des opérations de récupération ;
- aucun bypass général accordé au rôle Administrateur ;
- secret temporaire de réinitialisation de mot de passe, à usage unique, durée initiale **15 minutes** ;
- récupération signée du dernier administrateur, liée à l’installation, à usage unique, avec finalité et clé distinctes des licences ;
- licence expirée + audit disponible : consultation/suivi et renouvellement signé autorisés, aucune nouvelle commande métier ;
- audit principal indisponible : diagnostic, consultation vérifiable et récupération supervisée du stockage uniquement ;
- audit principal indisponible + licence expirée : restaurer d’abord l’audit, puis renouveler ;
- journal de secours local **borné** réservé aux événements de récupération ;
- ce journal de secours ne peut jamais admettre une commande métier ;
- si l’identité ou la trace de récupération ne peut pas être vérifiée/persistée, aucune mutation de récupération applicative n’est autorisée.

Découpage proposé :

### T2.4-A — secret temporaire de réinitialisation

- secret généré/provisionné pour une identité ciblée ;
- durée initiale 15 minutes ;
- stockage uniquement sous forme dérivée/opaque ;
- usage unique et consommation atomique ;
- aucun élargissement de permissions ;
- révocation/expiration explicites ;
- réinitialisation de mot de passe auditée.

### T2.4-B — récupération signée du dernier administrateur

- artefact signé versionné ;
- finalité cryptographique distincte de licence et récupération temporelle ;
- liaison à l’installation ;
- séquence/identifiant anti-replay ;
- usage unique ;
- création/rétablissement d’une autorité administrative explicite sans permission machine implicite ;
- refus si un administrateur récupérable existe encore selon la politique définie.

### T2.4-C — audit dégradé et journal de secours borné

- détection explicite d’indisponibilité de l’audit principal ;
- matrice fermée des actions encore permises ;
- journal de secours borné réservé aux événements de récupération ;
- aucune admission métier via ce journal ;
- récupération refusée si la trace de récupération ne peut pas être persistée ;
- restauration de l’audit avant retour au fonctionnement nominal.

### T2.4-D — composition pilote et non-régression

- composition réelle dans `MagasinOutil.CoreHost` ;
- preuve après redémarrage ;
- T0/T1 + T2.1 + T2.2 + T2.3 toujours verts ;
- aucun changement PLC requis.

## 7. Ensuite

Après T2.4 :

- T2.5 qualification globale et packaging ;
- extension de la première tranche à l’opération métier simulée complète, HMI web, OPC UA et Fleet ;
- raccordement Beckhoff réel par preuves successives.

## 8. Travaux transversaux

- **Conformité CRA** : chantier produit avec exigences et preuves dédiées ; aucune conformité n’est déclarée ici.
- **Sécurité produit** : identités, certificats, secrets, droits, reprise et diagnostic dès les contrats.
- **Déploiement** : installation hors ligne, démarrage système, sauvegarde/restauration, mise à jour et retour arrière.
- **Expérience opérateur** : tactile, clavier, perte de connexion, refus explicables, données périmées clairement signalées.
- **Documentation** : conserver séparément décisions, implémentations, preuves simulées et preuves sur cible.

## 9. Questions toujours ouvertes

- paramètres Argon2id finaux sur le PC industriel cible ;
- politiques commerciales de licence et autorités d’émission ;
- fournisseur matériel éventuel pour l’identité d’installation et disponibilité TPM ;
- tolérance produit exacte aux petits reculs de l’horloge ;
- destination externe et responsabilité opérationnelle des sauvegardes ;
- détails Fleet, certificats réseau et packaging ;
- mapping Beckhoff 8xx, unités/échelles, protocole et preuves de complétion ;
- qualification finale du renderer HMI web.

Aucun de ces points ne remet en cause les PASS LOCAL déjà obtenus pour T0/T1, T2.1, T2.2 ou T2.3.
