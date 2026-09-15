# T2.3 — Licences hors ligne signées et temps de confiance — Validation Windows

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-3-offline-signed-licenses`.
Branche plateforme : `pilot/t2-3-offline-signed-licenses`.
Statut : **PASS LOCAL — SIMULATION WINDOWS**.

## Objet

Ce dossier consolide les preuves T2.3-A à T2.3-D. Il qualifie l’architecture technique de licence hors ligne du pilote sans dépendance Internet, sans clé privée d’émission dans la machine et sans prétendre constituer une qualification produit ou Beckhoff réelle.

## T2.3-A — format signé et vérification cryptographique

**PASS LOCAL.**

Qualifié :

- format de licence versionné et représentation canonique ;
- ECDSA P-256 + SHA-256, signature IEEE P1363 ;
- registre de clés publiques approuvées lié à l’émetteur ;
- vérification du produit, de l’installation, de la fenêtre de validité et des capacités ;
- refus des altérations de payload/signature, clé ou algorithme inconnus, rebinding de clé et représentation signée non canonique ;
- aucune opération de signature privée exposée au runtime machine.

Voir : `T2_3_A_Contrat_Verification_Licence_Signee.md`.

## T2.3-B — identité d’installation, renouvellement et temps de confiance

**PASS LOCAL.**

Qualifié :

- identité `inst1_...` créée avec 256 bits d’aléa cryptographiquement sûr ;
- identité durable et distincte entre installations ;
- persistance de l’enveloppe signée et de la plus haute `RenewalVersion` ;
- idempotence de la même enveloppe, conflit sur même version/contenu différent et refus du rollback ;
- expiration explicite ;
- borne UTC haute persistée et temps monotone pendant le processus ;
- retour arrière de l’horloge détecté et persistant ;
- récupération temporelle par artefact signé, lié à l’installation et non rejouable ;
- clé/émetteur de récupération distincts de la licence dans la composition de qualification ;
- aucune clé privée d’émission ou de récupération retrouvée dans les artefacts SQLite.

Voir : `T2_3_B_Identite_Installation_Renouvellement_Temps_Confiance.md`.

## T2.3-C — admission gouvernée par licence

**PASS LOCAL.**

Qualifié :

- `license.install` comme permission d’administration explicite pour importer un artefact déjà signé ;
- absence de licence refusée avant effet machine ;
- licence valide ne donnant jamais une permission humaine absente ;
- permission humaine ne contournant jamais la licence ;
- nouvelle opération refusée lorsque la licence est expirée ou le temps incohérent ;
- opération déjà admise pouvant se terminer après expiration sans réévaluation rétroactive de licence ;
- même décision autoritative pour les requêtes HMI, API et OPC UA lorsqu’elles atteignent le même point d’admission ;
- aucune capacité de licence déclarée par le client ;
- lectures hors du verrou de mutation ;
- `Machine.Runtime` indépendant du runtime de licence, de SQLite et de l’adaptateur cryptographique concret.

Voir : `T2_3_C_Admission_Gouvernee_Licence.md`.

## T2.3-D — composition réelle dans le pilote

**PASS LOCAL.**

Qualifié :

- `MagasinOutil.CoreHost` compose les paquets communs de licence ;
- `licensing.db` est créé par le processus réel du pilote ;
- l’identité d’installation est créée et conservée après redémarrage sur le même état ;
- le profil de simulation contient zéro clé publique de production approuvée ;
- `MagasinOutil.ReadClient` n’embarque ni autorité de licence concrète, ni cryptographie, ni SQLite ;
- `MagasinOutil.Platform` reste indépendant des fournisseurs concrets de licence et de persistance.

Voir : `T2_3_D_Composition_CoreHost_Licence_Durable.md`.

## Invariants consolidés

T2.3 qualifie les invariants suivants :

1. **pas de secret d’émission sur la machine** : seules des clés publiques approuvées pourront être configurées côté produit ;
2. **installation autoritative** : une licence est liée à une identité d’installation durable ;
3. **anti-rollback** : un ancien artefact signé ne récupère pas l’autorité après un renouvellement supérieur ;
4. **temps non réversible silencieusement** : un saut vers l’avant ne peut pas être annulé par un simple recul de l’horloge ;
5. **admission, pas arrêt d’urgence** : l’expiration bloque un nouvel effet mais n’annule pas une opération déjà admise ;
6. **séparation des autorités** : licence produit, identité, permission humaine et préconditions machine restent distinctes ;
7. **frontière de processus réelle** : l’autorité concrète de licence est composée côté CoreHost, pas côté client de lecture ;
8. **fournisseurs remplaçables** : les contrats restent indépendants de SQLite et de l’adaptateur cryptographique concret.

## Limites explicites

Ce PASS ne démontre pas :

- une conformité Cyber Resilience Act (CRA) ;
- une qualification Beckhoff réelle ;
- la sécurité finale du PC industriel cible ;
- l’usage d’un Trusted Platform Module (TPM) ;
- la politique commerciale de durée, transfert ou révocation ;
- l’outil d’émission de licence de production ;
- la configuration opérationnelle des clés publiques de production.

Ces sujets doivent rester tracés comme décisions produit ou qualifications ultérieures.

## Résultat

**T2.3-A/B/C/D : PASS LOCAL.**

**T2.3 est clôturé en simulation Windows.**

La prochaine tranche est **T2.4 — audit et chemins de récupération produit**.
