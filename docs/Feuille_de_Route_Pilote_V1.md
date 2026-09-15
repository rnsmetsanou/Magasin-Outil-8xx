# Feuille de route du pilote Magasin 8xx

Date : 15 septembre 2026. Statut : feuille de route actualisée après clôture locale de T2.2 ; décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md).

## 1. Où en sommes-nous ?

Le socle de lecture T0/T1 est **PASS LOCAL** sur Windows : cœur séparé, clients gRPC, même autorité Machine partagée et consommation de paquets communs.

Le lot **T2.1 — autorités durables et stockage** est **PASS LOCAL** : contrats provider-indépendants, admission + audit atomiques avant effet technologique, déduplication concurrente, persistance et sauvegarde/restauration SQLite, composition du provider uniquement côté CoreHost et régression T0/T1 verte.

Le lot **T2.2 — identités locales, authentification et sessions** est également **PASS LOCAL** :

- identités humaines nominatives et comptes durables hors ligne ;
- Argon2id derrière un adaptateur remplaçable ;
- réponse de login générique et travail cryptographique aussi pour les noms inconnus ;
- sessions opaques, révocables, liées au sujet, client et cible ;
- délais d’inactivité local/distant et durée absolue ;
- permissions réévaluées avant nouvelle admission ;
- throttling durable à partir de cinq échecs, politique de durée configurable ;
- commissioning atomique et à usage unique du premier administrateur, sans compte universel ;
- stockage borné des traces de throttling pour faux identifiants, sans éviction des vrais comptes.

La dernière exécution conserve T0/T1 et T2.1 verts. Voir les dossiers T2.2-A/B/C/D dans `docs/implementation`.

La prochaine tranche est **T2.3 — licences hors ligne signées et temps de confiance**. La connexion Beckhoff réelle, la récupération signée du dernier administrateur et les commandes métier complètes restent hors de ce jalon.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | Principales décisions V1 acquises | Questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats, transport, stockage et composition | T0/T1 + T2.1 + T2.2 vérifiés | Frontières communes qualifiées sans duplication des autorités |
| 3. Première tranche intégrée simulée | Identité réelle, opération commune, licence, audit, HMI/OPC UA/Fleet | En cours — prochaine sous-tranche T2.3 | Une opération bout en bout avec refus gouvernés, audit durable et coupures exercés |
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
- licence locale signée et audit durable ;
- SQLite et gRPC remplaçables derrière des contrats ;
- maintenance V1 en consultation seule ;
- Opérateur autorisé à modifier uniquement les usures, y compris en broche sous conditions, et à Préparer/Charger ;
- formats 1024 × 768 et 1920 × 1080, tactile, thèmes WM, français requis, anglais souhaité, allemand si possible, mm/pouces.

## 4. T2.2 — clôturé

T2.2-A/B/C/D sont **PASS LOCAL** en simulation Windows. Les invariants de compte, authentification, session, permission, throttling et premier commissioning sont donc considérés qualifiés pour la suite du pilote.

Le secret temporaire de réinitialisation de mot de passe et la récupération signée du dernier administrateur sont volontairement déplacés vers **T2.4 — audit/récupération produit**, car ils relèvent des chemins de récupération fermés et non du noyau d’authentification nominal.

## 5. T2.3 — licences hors ligne signées et temps de confiance

T2.3 sera construit par micro-tranches, sans placer la clé privée d’émission sur la machine.

### T2.3-A — contrat de licence et vérification cryptographique

Objectif : prouver qu’un fichier de licence peut être vérifié hors ligne à partir d’une clé publique approuvée, sans dépendance Internet ni secret d’émission dans le runtime.

Critères :

- format versionné et représentation canonique signée ;
- `LicenseId`, version de renouvellement, émetteur/identifiant de clé, produit, identité d’installation, capacités, début de validité et expiration optionnelle ;
- signature vérifiée avec une clé publique approuvée ;
- clés de test et de production séparables ;
- signature invalide, contenu altéré, mauvais produit, mauvaise installation et clé inconnue refusés ;
- contrats indépendants de l’algorithme/provider cryptographique concret ;
- aucune clé privée dans le Core, la HMI ou le dépôt pilote.

### T2.3-B — installation durable et temps de confiance

Objectif : conserver l’autorité de licence localement et résister aux incohérences d’horloge évidentes sans exiger de réseau.

Critères :

- installation/renouvellement seulement via `license.install` ;
- persistance de la dernière licence approuvée et de sa révision ;
- refus d’un rollback de renouvellement ;
- référence temporelle persistée permettant de détecter un recul significatif de l’horloge murale ;
- temps monotone utilisé pour les durées internes au processus, temps civil uniquement pour les périodes de licence ;
- renouvellement signé pouvant rétablir un état temporel cohérent ;
- révocation hors ligne uniquement lorsqu’une information signée approuvée est importée.

### T2.3-C — admission gouvernée par licence

Objectif : raccorder l’autorité de licence aux admissions sans casser les opérations déjà admises.

Critères :

- consultation autorisée selon droits lorsque la licence est expirée, sauf capacité explicitement licenciée autrement ;
- nouvelles mutations/commandes licenciées refusées si licence absente, invalide, expirée ou temporellement incohérente ;
- opérations déjà admises continuent selon leur état machine ;
- capacités licenciées évaluées côté Core, jamais déclarées par le client ;
- même décision pour HMI, API et OPC UA lorsqu’ils atteignent la même admission ;
- non-régression T0/T1 + T2.1 + T2.2.

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
- destination externe et responsabilité opérationnelle des sauvegardes ;
- détails Fleet, certificats réseau et packaging ;
- mapping Beckhoff 8xx, unités/échelles, protocole et preuves de complétion ;
- qualification finale du renderer HMI web.

Aucun de ces points ne remet en cause les PASS LOCAL déjà obtenus pour T0/T1, T2.1 ou T2.2.
