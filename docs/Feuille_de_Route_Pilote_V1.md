# Feuille de route du pilote Magasin 8xx

Date : 15 septembre 2026. Statut : feuille de route actualisée après clôture locale de T2.1 ; décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md).

## 1. Où en sommes-nous ?

Le socle de lecture T0/T1 est **PASS LOCAL** sur Windows : cœur séparé, clients gRPC, même autorité Machine partagée et consommation de paquets communs.

Le lot **T2.1 — autorités durables et stockage** est également **PASS LOCAL** :

- contrats de session et d’admission provider-indépendants ;
- admission + audit persistés atomiquement avant effet technologique ;
- déduplication concurrente et absence de divulgation inter-propriétaire ;
- persistance et sauvegarde/restauration SQLite qualifiées en simulation ;
- `Platform.Poc.Persistence.Sqlite` composé uniquement par le CoreHost du pilote ;
- régression T0/T1 toujours verte.

Voir le [dossier de preuve T2.1](implementation/T2_1_Autorites_Durables_Validation_Windows_2026-09-15.md).

La prochaine tranche est **T2.2 — identités locales, authentification, sessions révocables et résolution des permissions**. La connexion Beckhoff réelle, la licence produit et les commandes métier complètes restent hors de ce jalon.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | Principales décisions V1 acquises | Questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats, transport, stockage et composition | T0/T1 + T2.1 vérifiés | Frontières communes qualifiées sans duplication des autorités |
| 3. Première tranche intégrée simulée | Identité réelle, opération commune, licence, audit, HMI/OPC UA/Fleet | En cours — prochaine sous-tranche T2.2 | Une opération bout en bout avec refus gouvernés, audit durable et coupures exercés |
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
- licence locale signée et audit durable ;
- SQLite et gRPC remplaçables derrière des contrats ;
- maintenance V1 en consultation seule ;
- Opérateur autorisé à modifier uniquement les usures, y compris en broche sous conditions, et à Préparer/Charger ;
- formats 1024 × 768 et 1920 × 1080, tactile, thèmes WM, français requis, anglais souhaité, allemand si possible, mm/pouces.

## 4. T2.2 — identités locales et sessions

Le lot T2.2 doit être construit par micro-tranches et sans cryptographie maison.

### T2.2-A — noyau d’autorité

Objectif : prouver les règles de session et permission indépendamment du stockage de mot de passe.

Critères :

- deux identités nominatives distinctes ;
- aucune identité partagée ni Administrateur universel ;
- émission d’une référence de session opaque par le Core ;
- liaison autoritative au `SubjectId`, `ClientId`, `TargetId` et type d’accès ;
- refus d’un changement de client ou de cible ;
- expiration absolue et par inactivité ;
- révocation explicite de session ;
- activité automatique ne prolongeant pas une session humaine ;
- permissions résolues côté Core et réévaluées avant chaque nouvelle admission ;
- retrait d’un droit immédiatement effectif pour une nouvelle admission, sans altérer une opération déjà admise.

### T2.2-B — comptes locaux durables et authentification

Objectif : persister les comptes et vérifier les secrets localement hors ligne.

Prérequis : qualifier une bibliothèque Argon2id maintenue et des paramètres de coût adaptés au matériel cible. Tant que cette qualification n’est pas faite, aucun format de mot de passe produit n’est figé.

Critères :

- nom de compte unique et normalisé ;
- secret jamais stocké en clair ;
- sel propre au compte ;
- dérivation par bibliothèque éprouvée ;
- désactivation de compte et invalidation des nouvelles sessions ;
- secret temporaire à usage limité, durée initiale 15 min ;
- délai progressif après cinq échecs regroupés ;
- redémarrage Core exigeant une réauthentification.

### T2.2-C — composition pilote

Objectif : faire consommer l’autorité commune par le CoreHost du Magasin 8xx sans introduire l’implémentation dans le métier 8xx.

Critères : deux utilisateurs nominatifs, permissions différentes, révocation visible depuis deux clients distincts et non-régression T0/T1 + T2.1.

## 5. Ensuite

Après T2.2 :

- T2.3 licences hors ligne signées et temps de confiance ;
- T2.4 audit/récupération produit ;
- T2.5 qualification globale et packaging ;
- puis extension de la première tranche à l’opération métier simulée complète, HMI web, OPC UA et Fleet ;
- enfin raccordement Beckhoff réel par preuves successives.

## 6. Travaux transversaux

- **Conformité CRA** : chantier produit à mener avec exigences et preuves dédiées ; aucune conformité n’est déclarée ici.
- **Sécurité produit** : identités, certificats, secrets, droits, reprise et diagnostic dès les contrats.
- **Déploiement** : installation hors ligne, démarrage système, sauvegarde/restauration, mise à jour et retour arrière.
- **Expérience opérateur** : tactile, clavier, perte de connexion, refus explicables, données périmées clairement signalées.
- **Documentation** : conserver séparément décisions, implémentations, preuves simulées et preuves sur cible.

## 7. Questions toujours ouvertes

- bibliothèque Argon2id et paramètres de coût ;
- politiques commerciales de licence et autorités d’émission ;
- destination externe et responsabilité opérationnelle des sauvegardes ;
- détails Fleet, certificats réseau et packaging ;
- mapping Beckhoff 8xx, unités/échelles, protocole et preuves de complétion ;
- qualification finale du renderer HMI web.

Aucun de ces points ne remet en cause le PASS LOCAL de T0/T1 ou T2.1.