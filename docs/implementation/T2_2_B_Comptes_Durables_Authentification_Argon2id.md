# T2.2-B — Comptes locaux durables et authentification Argon2id

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche qualifie une authentification locale hors ligne et durable sans introduire de dépendance fournisseur dans les contrats ou le runtime d’identité.

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

Ces valeurs sont des paramètres de qualification, pas une politique produit immuable. La recette locale a mesuré **309 ms** pour un hash pilote sur le poste Windows utilisé pour la validation. Cette mesure devra être reprise sur le PC industriel cible avant de figer les paramètres produit.

## Preuve locale reçue — révision initiale de T2.2-B

Commande exécutée :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Le journal reçu le 15 septembre 2026 confirme pour la révision alors exécutée :

- contrats d’identité indépendants de SQLite et de l’implémentation Argon2 ;
- runtime d’identité indépendant de SQLite et de l’implémentation Argon2 ;
- paramètres Argon2id et métadonnées de credential persistés ;
- sels aléatoires distincts pour deux mots de passe identiques ;
- bon mot de passe accepté et mauvais mot de passe refusé ;
- signal de rehash lorsque la politique évolue ;
- création durable d’un compte local nommé ;
- aucun mot de passe en clair exposé dans le record ;
- unicité du nom normalisé et absence de réattribution silencieuse d’un identifiant de sujet ;
- même résultat d’authentification pour utilisateur inconnu et mauvais mot de passe ;
- authentification hors ligne après recréation du store ;
- révision de sécurité initiale à 1 ;
- remplacement des permissions avec contrôle de révision optimiste ;
- refus d’une écriture basée sur une révision obsolète ;
- changement de mot de passe durable et invalidation immédiate de l’ancien ;
- désactivation persistée et refus d’authentification du compte désactivé ;
- absence des deux mots de passe de test en clair dans les artefacts SQLite ;
- refus de la permission générique `*` avant persistance d’un compte humain.

Le même journal rejoue et conserve verts T0/T1, T2.1 et T2.2-A.

État de cette révision : **PASS LOCAL — SIMULATION WINDOWS**.

## Durcissement postérieur au PASS reçu

Après analyse sécurité, la branche a été durcie sur deux points qui n’étaient pas couverts par le journal précédent :

1. un nom utilisateur inconnu exécute désormais lui aussi un travail Argon2id avant de renvoyer `InvalidCredentials`, afin d’éviter un raccourci temporel permettant d’inférer l’existence d’un compte ;
2. un compte désactivé renvoie également `InvalidCredentials` via le chemin de connexion afin de ne pas divulguer son état ; cet état reste disponible aux surfaces d’administration de confiance.

La recette T2.2-B contient maintenant des preuves instrumentées :

- le mauvais mot de passe d’un compte connu déclenche `VerifyAsync` ;
- un utilisateur inconnu déclenche une dérivation Argon2id au lieu d’un retour rapide ;
- utilisateur inconnu, mauvais mot de passe et compte désactivé ne divulguent pas leur différence par le statut de connexion.

**La révision durcie est implémentée mais doit être requalifiée localement.** Le PASS reçu ci-dessus reste une preuve historique valide de la révision antérieure ; il ne doit pas être étendu silencieusement au nouveau code.

## Limites

La qualification actuelle ne constitue ni une certification cryptographique, ni une validation sur le PC industriel cible. Le commissioning du premier administrateur et la limitation durable des tentatives sont traités dans T2.2-C. Le secret temporaire de réinitialisation et la récupération signée du dernier administrateur restent des sous-jalons ultérieurs.
