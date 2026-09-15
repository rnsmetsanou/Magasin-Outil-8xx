# T2.2-B — Comptes locaux durables et authentification Argon2id

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche prépare une authentification locale hors ligne et durable sans introduire de dépendance fournisseur dans les contrats ou le runtime d’identité.

Elle ne constitue pas encore le commissioning complet du premier administrateur ni la récupération produit.

## Découpage plateforme

Quatre composants sont distingués :

- `Platform.Poc.Identity.Contracts` : contrats de compte local, empreinte de mot de passe, store et fournisseur de dérivation ; aucune dépendance SQLite ou Argon2 concrète ;
- `Platform.Poc.Identity.Runtime` : service de création/authentification et règles de normalisation ; dépend seulement des contrats ;
- `Platform.Poc.Identity.PasswordHashing.Argon2` : adaptateur Argon2id remplaçable ;
- `Platform.Poc.Identity.Persistence.Sqlite` : adaptateur durable SQLite remplaçable.

## Choix Argon2id pour la qualification pilote

Bibliothèque candidate : `Konscious.Security.Cryptography.Argon2` version `1.3.1`, sous licence MIT et compatible avec .NET 10 via ses cibles .NET 6/.NET Standard.

Politique pilote initiale :

- algorithme : Argon2id ;
- mémoire : 19 MiB (`19456 KiB`) ;
- itérations : 2 ;
- parallélisme : 1 ;
- sel aléatoire : 16 octets par credential ;
- sortie dérivée : 32 octets ;
- format versionné et paramètres stockés avec l’empreinte afin de permettre un rehash futur.

Ces valeurs sont des paramètres de qualification, pas une politique produit immuable. Elles suivent le minimum Argon2id actuellement recommandé par OWASP ; le temps de dérivation réel est mesuré par la recette sur le poste pilote.

## Règles couvertes

- aucun mot de passe en clair dans le contrat de compte ;
- sel unique aléatoire par credential ;
- comparaison du hash en temps constant ;
- même statut `InvalidCredentials` pour utilisateur inconnu et mauvais mot de passe ;
- compte désactivé refusé même avec le bon mot de passe ;
- unicité du nom utilisateur normalisé ;
- identifiant de sujet non réaffectable silencieusement ;
- droits humains sans permission générique `*` ;
- révision de sécurité optimiste pour changement de mot de passe, droits ou activation ;
- détection de credential nécessitant un rehash lorsque la politique évolue ;
- stockage durable SQLite sans dépendance Internet.

## Recette préparée

La commande reste :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Après T2.2-A, la recette exécute T2.2-B et vérifie notamment :

- indépendance des contrats/runtime vis-à-vis de SQLite et de la bibliothèque Argon2 ;
- paramètres Argon2id et salts distincts ;
- bon/mauvais mot de passe ;
- signal de rehash futur ;
- création durable d’un compte nommé ;
- conflits de nom et sujet ;
- même résultat pour utilisateur inconnu/mauvais mot de passe ;
- authentification après recréation du store ;
- persistance des droits et révisions ;
- refus d’une écriture avec révision obsolète ;
- changement de mot de passe et invalidation immédiate de l’ancien ;
- désactivation persistée ;
- absence des mots de passe de test en clair dans les artefacts SQLite ;
- refus de `*` pour un humain.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.2-B n’est déclaré avant réception du journal d’exécution local. Le délai progressif après cinq échecs, le commissioning du premier administrateur, le secret temporaire et la récupération signée restent hors de cette micro-tranche.
