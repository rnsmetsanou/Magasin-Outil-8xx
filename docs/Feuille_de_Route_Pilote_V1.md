# Feuille de route du pilote Magasin 8xx

Date : 15 septembre 2026. Statut : feuille de route actualisée après clôture locale de T2.2 et qualification de T2.3-A/B ; décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md).

## 1. Où en sommes-nous ?

Le socle de lecture T0/T1 est **PASS LOCAL** sur Windows.

Le lot **T2.1 — autorités durables et stockage** est **PASS LOCAL** : contrats fournisseur-indépendants, admission + audit atomiques avant effet technologique, déduplication concurrente, persistance/sauvegarde SQLite et composition du fournisseur côté CoreHost.

Le lot **T2.2 — identités locales, authentification et sessions** est **PASS LOCAL** : comptes nominatifs durables, Argon2id, sessions opaques/révocables, permissions dynamiques, throttling durable, premier administrateur commissionné sans compte universel et stockage borné des faux identifiants.

**T2.3-A est PASS LOCAL** : format de licence canonique versionné et vérification cryptographique hors ligne.

**T2.3-B est PASS LOCAL** : identité d’installation durable, renouvellement monotone, temps de confiance et récupération temporelle signée.

**T2.3-C est implémenté et attend sa qualification locale** : admission des nouvelles opérations gouvernée par licence, permission `license.install`, lectures hors verrou et poursuite des opérations déjà admises.

La connexion Beckhoff réelle, la récupération signée du dernier administrateur et la qualification produit restent hors de ce jalon.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | Principales décisions V1 acquises | Questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats, transport, stockage et composition | T0/T1 + T2.1 + T2.2 vérifiés | Frontières communes qualifiées sans duplication des autorités |
| 3. Première tranche intégrée simulée | Identité réelle, opération commune, licence, audit, HMI/OPC UA/Fleet | En cours — T2.3-A/B PASS, T2.3-C en qualification | Une opération bout en bout avec refus gouvernés, audit durable et coupures exercés |
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
- SQLite et gRPC remplaçables derrière des contrats ;
- maintenance V1 en consultation seule ;
- Opérateur autorisé à modifier uniquement les usures, y compris en broche sous conditions, et à Préparer/Charger ;
- formats 1024 × 768 et 1920 × 1080, tactile, thèmes WM, français requis, anglais souhaité, allemand si possible, mm/pouces.

## 4. T2.2 — clôturé

T2.2-A/B/C/D sont **PASS LOCAL** en simulation Windows. Les invariants de compte, authentification, session, permission, throttling et premier commissioning sont qualifiés pour la suite du pilote.

Le secret temporaire de réinitialisation de mot de passe et la récupération signée du dernier administrateur sont déplacés vers **T2.4 — audit/récupération produit**.

## 5. T2.3 — licences hors ligne signées et temps de confiance

T2.3 est construit par micro-tranches, sans placer la clé privée d’émission sur la machine.

### T2.3-A — contrat de licence et vérification cryptographique : PASS LOCAL

Critères qualifiés : format versionné/canonique, signature ECDSA P-256 + SHA-256, clés publiques approuvées, liaisons émetteur/clé/produit/installation, capacités signées, période de validité et refus des altérations. Les contrats restent indépendants de l’adaptateur cryptographique concret.

### T2.3-B — installation durable et temps de confiance : PASS LOCAL

Critères qualifiés :

- identité d’installation durable issue d’un aléa cryptographique, sans MAC ni numéro de disque ;
- persistance/revalidation de la licence signée ;
- anti-rollback de `RenewalVersion` ;
- borne UTC haute persistée et temps monotone pendant le processus ;
- expiration non contournable par recul d’horloge ;
- récupération temporelle signée liée à l’installation et à séquence croissante ;
- renouvellement possible comme chemin de récupération sans supprimer à lui seul l’incohérence temporelle ;
- absence de clés privées dans le store machine.

L’identité logicielle V1 n’est pas déclarée matériellement non clonable. Un fournisseur adossé à un Trusted Platform Module (TPM) ou autre matériel de confiance pourra être qualifié ultérieurement derrière les mêmes contrats.

### T2.3-C — admission gouvernée par licence

Objectif : raccorder l’autorité de licence aux nouvelles admissions sans casser les opérations déjà admises.

Critères de qualification :

- installation/renouvellement via une autorité exigeant `license.install` ;
- nouvelle mutation/commande refusée si licence absente, invalide, expirée, temporellement incohérente ou sans capacité requise ;
- une licence valide ne confère aucun droit utilisateur absent ;
- une permission utilisateur ne contourne pas la licence ;
- opération déjà admise poursuivie jusqu’à sa conclusion observée sans nouvelle décision de licence ;
- capacités licenciées évaluées côté Core, jamais déclarées par le client ;
- même décision pour HMI, API et OPC UA lorsqu’ils atteignent la même admission ;
- runtimes de lecture hors du verrou de licence des mutations ;
- `Machine.Runtime` indépendant du runtime de licence, de SQLite et de la cryptographie concrète ;
- non-régression T0/T1 + T2.1 + T2.2 + T2.3-A/B.

**État : implémenté — à qualifier localement.**

Les durées commerciales, personnes autorisées à émettre et règles de transfert d’iPC restent des décisions WM ouvertes ; elles ne bloquent pas la qualification de l’architecture technique.

## 6. Ensuite

Après T2.3 :

- T2.4 audit/récupération produit, incluant secret temporaire de réinitialisation et récupération signée du dernier administrateur ;
- T2.5 qualification globale et packaging ;
- puis extension de la première tranche à l’opération métier simulée complète, HMI web, OPC UA et Fleet ;
- enfin raccordement Beckhoff réel par preuves successives.

## 7. Travaux transversaux

- **Conformité CRA** : chantier produit à mener avec exigences et preuves dédiées ; aucune conformité n’est déclarée ici.
- **Sécurité produit** : identités, certificats, secrets, droits, reprise et diagnostic dès les contrats.
- **Déploiement** : installation hors ligne, démarrage système, sauvegarde/restauration, mise à jour et retour arrière.
- **Expérience opérateur** : tactile, clavier, perte de connexion, refus explicables, données périmées clairement signalées.
- **Documentation** : conserver séparément décisions, implémentations, preuves simulées et preuves sur cible.

## 8. Questions toujours ouvertes

- paramètres Argon2id finaux sur le PC industriel cible ;
- politiques commerciales de licence et autorités d’émission ;
- fournisseur matériel éventuel pour l’identité d’installation et disponibilité TPM sur l’iPC cible ;
- tolérance produit exacte aux petits reculs de l’horloge ;
- destination externe et responsabilité opérationnelle des sauvegardes ;
- détails Fleet, certificats réseau et packaging ;
- mapping Beckhoff 8xx, unités/échelles, protocole et preuves de complétion ;
- qualification finale du renderer HMI web.

Aucun de ces points ne remet en cause les PASS LOCAL déjà obtenus pour T0/T1, T2.1, T2.2 ou T2.3-A/B.
