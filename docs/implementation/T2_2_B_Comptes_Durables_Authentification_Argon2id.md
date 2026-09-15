# T2.2-B — Comptes locaux durables et authentification Argon2id

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche qualifie une authentification locale hors ligne et durable sans introduire de dépendance fournisseur dans les contrats ou le runtime d’identité.

## Découpage plateforme

Quatre composants sont distingués :

- `Platform.Poc.Identity.Contracts` : contrats fournisseur-indépendants ;
- `Platform.Poc.Identity.Runtime` : orchestration d’authentification ;
- `Platform.Poc.Identity.PasswordHashing.Argon2` : adaptateur Argon2id remplaçable ;
- `Platform.Poc.Identity.Persistence.Sqlite` : adaptateur durable SQLite remplaçable.

## Choix Argon2id pour la qualification pilote

Bibliothèque candidate : `Konscious.Security.Cryptography.Argon2` version `1.3.1`.

Politique pilote :

- Argon2id ;
- mémoire : 19 MiB (`19456 KiB`) ;
- 2 itérations ;
- parallélisme 1 ;
- sel aléatoire de 16 octets ;
- sortie dérivée de 32 octets ;
- format et paramètres persistés avec le credential afin de permettre un rehash futur.

Ces paramètres restent une politique de qualification et doivent être remesurés sur le PC industriel cible avant décision produit.

## Validation locale — révision durcie

Commande :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Les journaux du 15 septembre 2026 qualifient la révision durcie. Ils confirment notamment :

- contrats et runtime indépendants de SQLite et de l’implémentation Argon2 ;
- sels distincts pour deux mots de passe identiques ;
- vérification correcte et refus d’un mauvais mot de passe ;
- signal de rehash lors d’une évolution de politique ;
- création et authentification durable hors ligne ;
- noms normalisés uniques et identifiants de sujet non réaffectables silencieusement ;
- utilisateur inconnu et mauvais mot de passe exposant le même résultat ;
- preuve instrumentée qu’un mauvais mot de passe d’un compte connu exécute `VerifyAsync` ;
- preuve instrumentée qu’un utilisateur inconnu effectue lui aussi une dérivation Argon2id au lieu d’un retour rapide ;
- compte désactivé renvoyant le même résultat générique de login afin de ne pas divulguer son état ;
- révisions de sécurité optimistes ;
- remplacement durable des permissions ;
- changement de mot de passe invalidant immédiatement l’ancien ;
- absence des mots de passe de test en clair dans les artefacts SQLite ;
- refus de la permission générique `*` pour un compte humain.

La dernière exécution T2.2 complète a mesuré **192 ms** pour un hash Argon2id sur le poste Windows de qualification. Des exécutions antérieures avaient mesuré environ **309 ms** puis **360 ms** avec les mêmes paramètres. Cette variation renforce la décision de ne pas figer le coût produit à partir du poste de développement : un benchmark reproductible sur le PC industriel cible reste nécessaire.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

Ce résultat ne constitue ni une certification cryptographique ni une qualification du coût Argon2id sur le PC industriel cible. La limitation durable des tentatives et le commissioning du premier administrateur sont qualifiés séparément dans T2.2-C ; le stockage borné des faux identifiants est qualifié dans T2.2-D.
