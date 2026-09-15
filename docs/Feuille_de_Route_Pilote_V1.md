# Feuille de route du pilote Magasin 8xx

Date : 15 septembre 2026. Statut : T2.3 clôturé ; T2.4-A implémenté et en attente de qualification locale. Décisions acquises référencées dans le [registre](decisions/Registre_Decisions.md).

## 1. Où en sommes-nous ?

Le socle **T0/T1**, **T2.1**, **T2.2** et **T2.3** sont **PASS LOCAL** et clôturés sur leur périmètre de simulation Windows.

**T2.4-A — secret temporaire de réinitialisation du mot de passe est implémenté et attend sa qualification locale.**

Il introduit un secret 256 bits, une durée initiale de 15 minutes, un usage unique, une consommation atomique avec le remplacement du credential, la conservation des permissions, la révocation des sessions existantes du sujet et l’absence d’auto-login.

T2.4-B/C/D restent à implémenter. La connexion Beckhoff réelle, le PC industriel cible et la qualification produit restent hors des jalons déjà clôturés.

## 2. Étapes et critères de sortie

| Étape | Résultat attendu | État | Critère de sortie |
|---|---|---|---|
| 0. Prototype et découverte | Parcours magasin simulé, contraintes WM, formats écran et premières sources PLC | Prototype réalisé | Référence de parcours disponible, limites de simulation explicites |
| 1. Contrat V1 | Périmètre métier, exposition OPC UA, rôles, maintenance et profils | Principales décisions V1 acquises | Questions restantes identifiées avec leur impact |
| 2. Architecture d’intégration | Répartition plateforme/pilote, contrats, transport, stockage et composition | T0/T1 + T2.1 + T2.2 + T2.3 vérifiés | Frontières communes qualifiées sans duplication des autorités |
| 3. Première tranche intégrée simulée | Identité réelle, opération commune, licence, audit, récupération, HMI/OPC UA/Fleet | En cours — T2.4-A en qualification | Une opération bout en bout avec refus gouvernés, audit durable et chemins de récupération exercés |
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
- rôles Consultation, Opérateur, Régleur outils et Administrateur sans hiérarchie implicite ;
- aucune consultation métier OPC UA anonyme ;
- sessions opaques liées au sujet, client et cible ;
- inactivité initiale : 30 min local, 10 min distant ; durée absolue 8 h ;
- Argon2id derrière un fournisseur remplaçable ; paramètres finaux à benchmarker sur cible ;
- throttling progressif à partir du cinquième échec ;
- commissioning du premier administrateur par secret propre à l’installation, à usage unique ;
- licence hors ligne signée et liée à l’installation, sans clé privée d’émission sur la machine ;
- renouvellement monotone et récupération du temps signée ;
- expiration/incohérence bloquant les nouvelles opérations licenciées mais pas les opérations déjà admises ;
- `license.install` comme permission explicite d’import ;
- composition concrète des fournisseurs uniquement côté CoreHost ;
- SQLite et gRPC remplaçables derrière des contrats ;
- maintenance V1 en consultation seule ;
- aucune modification PLC requise par T2.

## 4. Lots clôturés

### T2.1 — autorités durables et stockage

**PASS LOCAL — clôturé.** Admission + audit atomiques, persistance, déduplication, sauvegarde/restauration et composition SQLite côté CoreHost.

### T2.2 — identités locales, authentification et sessions

**PASS LOCAL — clôturé.** Comptes durables, Argon2id, sessions opaques/révocables, permissions dynamiques, throttling, premier administrateur et stockage borné des faux identifiants.

### T2.3 — licences hors ligne signées et temps de confiance

**T2.3-A/B/C/D PASS LOCAL — clôturé.** Format signé, identité d’installation, anti-rollback, temps de confiance, admission gouvernée et composition réelle dans `MagasinOutil.CoreHost`.

Voir `implementation/T2_3_Licences_Hors_Ligne_Validation_Windows_2026-09-15.md`.

## 5. T2.4 — audit et chemins de récupération produit

Objectif : qualifier les chemins exceptionnels sans créer de bypass général des autorités nominales.

Décisions communes : liste fermée des opérations de récupération, aucun bypass Administrateur général, trace durable obligatoire, journal de secours uniquement pour récupération et jamais pour admission métier.

### T2.4-A — secret temporaire de réinitialisation

**État : implémenté — à qualifier localement.**

Implémenté :

- émission/révocation gouvernées par `identity.manage` ;
- secret aléatoire 256 bits ;
- durée initiale 15 minutes ;
- stockage uniquement sous forme dérivée ;
- une seule autorisation active par sujet ;
- liaison à la révision de sécurité du compte ;
- remplacement du credential + consommation dans une transaction SQLite commune ;
- permissions conservées ;
- révocation de toutes les sessions existantes du sujet ;
- aucune session automatique ;
- expiration, révocation, replay et concurrence traités explicitement.

Recette :

```powershell
.\eng\Test-T24.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Critère de sortie : T2.4-A vert avec toute la régression T2.3 verte.

### T2.4-B — récupération signée du dernier administrateur

À implémenter après A. Artefact signé versionné, finalité cryptographique distincte, liaison installation, anti-replay, usage unique, aucune permission machine implicite.

### T2.4-C — audit dégradé et journal de secours borné

À implémenter. Matrice fermée des actions permises, aucune admission métier via le journal de secours, récupération refusée si la trace ne peut pas être persistée, restauration de l’audit avant retour au nominal.

### T2.4-D — composition pilote et non-régression

À implémenter. Composition réelle côté `MagasinOutil.CoreHost`, persistance après redémarrage, fournisseurs absents des clients, T0/T1 + T2.1 + T2.2 + T2.3 toujours verts.

## 6. Ensuite

Après T2.4 :

- T2.5 qualification globale et packaging ;
- extension de la première tranche à l’opération métier simulée complète, HMI web, OPC UA et Fleet ;
- raccordement Beckhoff réel par preuves successives.

## 7. Travaux transversaux et questions ouvertes

- conformité au Cyber Resilience Act (CRA) : chantier produit dédié, aucune conformité déclarée ici ;
- paramètres Argon2id finaux sur l’iPC cible ;
- politiques commerciales de licence, clés publiques et outil d’émission produit ;
- disponibilité/usage éventuel d’un Trusted Platform Module (TPM) ;
- tolérance produit exacte aux petits reculs d’horloge ;
- destination externe/responsabilité des sauvegardes ;
- politique exacte du journal de secours T2.4-C ;
- définition opérationnelle du scénario « dernier administrateur perdu » T2.4-B ;
- Fleet, certificats réseau, packaging et renderer HMI web ;
- mapping Beckhoff 8xx et preuves de complétion réelles.

Aucun de ces points ne remet en cause les PASS LOCAL déjà obtenus pour T0/T1, T2.1, T2.2 ou T2.3.
