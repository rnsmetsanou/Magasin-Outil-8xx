# T2.4-A — Secret temporaire de réinitialisation du mot de passe

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-4-audit-recovery`.
Branche plateforme : `pilot/t2-4-audit-recovery`.

## Objectif

T2.4-A introduit un chemin de récupération ciblé pour un utilisateur ayant oublié son mot de passe lorsqu’une autorité administrative normale existe encore.

Ce parcours n’est pas le mécanisme de récupération du **dernier administrateur**. Ce scénario exceptionnel appartient à T2.4-B et utilisera un artefact signé distinct.

## Autorité d’émission

L’émission et la révocation d’une autorisation temporaire exigent explicitement :

`identity.manage`

Un utilisateur ne possédant pas cette permission ne peut ni émettre ni révoquer une autorisation de réinitialisation.

Le résultat `TargetNotFound` n’est exposé que dans ce parcours d’administration de confiance ; T2.4-A ne crée pas un endpoint anonyme de type « mot de passe oublié » permettant l’énumération des comptes.

## Secret temporaire

Chaque autorisation est associée à un secret :

- généré avec `RandomNumberGenerator` ;
- **256 bits** d’aléa ;
- transmis au bénéficiaire sous forme Base64 URL-safe ;
- jamais persisté en clair ;
- persisté uniquement sous forme de `PasswordHashDescriptor` derrière `IPasswordHashingProvider` ;
- durée pilote initiale : **15 minutes** ;
- usage unique.

Dans la composition actuelle de qualification, `IPasswordHashingProvider` est l’adaptateur Argon2id déjà qualifié en T2.2. Les contrats et le runtime T2.4-A ne dépendentent toutefois pas du paquet Argon2id concret.

## Une seule autorisation active par sujet

Lorsqu’une nouvelle autorisation est émise pour le même sujet, toute autorisation précédente encore active est révoquée atomiquement avant l’insertion de la nouvelle.

Cela évite que plusieurs secrets de récupération restent simultanément valides pour le même compte.

## Liaison à la révision de sécurité du compte

L’autorisation mémorise la `SecurityRevision` du compte au moment de son émission.

Si le compte change ensuite avant consommation — par exemple permissions ou credential remplacés — le secret devient obsolète et ne peut plus écraser l’état plus récent.

## Consommation atomique

`SqliteTemporaryPasswordResetStore` partage la même base SQLite que `local_accounts` afin de pouvoir effectuer dans **une seule transaction** :

1. validation de l’autorisation et de sa révision ;
2. validation de la révision de sécurité du compte ;
3. remplacement du credential ;
4. incrément de la révision de sécurité du compte ;
5. marquage de l’autorisation comme consommée.

Une course entre deux consommations ne peut donc produire qu’un seul remplacement réussi.

## Sessions existantes

Un changement de mot de passe de récupération doit couper les sessions déjà émises pour le sujet.

Un contrat interne générique `ISubjectSessionRevoker` a été ajouté. `LocalIdentityAuthority` l’implémente en révoquant toutes les sessions non encore révoquées du sujet.

Après un reset réussi :

- les anciennes sessions du sujet sont révoquées ;
- les sessions des autres sujets ne sont pas affectées ;
- aucune nouvelle session n’est créée automatiquement ;
- l’utilisateur doit s’authentifier normalement avec son nouveau mot de passe.

## Permissions et état du compte

La réinitialisation :

- ne change pas les permissions ;
- ne donne aucune permission `*` ;
- ne donne aucune commande machine ;
- ne transforme pas le secret temporaire en session ;
- ne modifie pas implicitement l’état activé/désactivé du compte.

## Persistance

Nouveau store :

`Platform.Poc.Identity.Persistence.Sqlite.SqliteTemporaryPasswordResetStore`

Il introduit une table dédiée `temporary_password_resets` et un espace de migration `identity_recovery_schema_migrations`, séparé de la version du schéma nominal des comptes T2.2.

Les données persistées incluent l’identifiant de l’autorisation, le sujet cible, le sujet émetteur, la révision de sécurité attendue, le dérivé du secret, les dates d’émission/expiration/consommation/révocation et une révision optimiste.

## Recette T2.4-A

Commande globale :

```powershell
.\eng\Test-T24.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

La recette rejoue d’abord tout T0/T1 + T2.1 + T2.2 + T2.3, puis vérifie notamment :

- contrats/runtime indépendants de SQLite et du paquet Argon2id concret ;
- émission refusée sans `identity.manage` ;
- secret de 256 bits ;
- expiration à 15 minutes ;
- absence du secret en clair dans les artefacts SQLite ;
- nouvelle autorisation révoquant l’ancienne ;
- mauvais secret refusé ;
- révocation administrative ;
- expiration ;
- refus d’une autorisation devenue obsolète après changement de révision de sécurité ;
- remplacement réussi du mot de passe ;
- permissions inchangées ;
- ancien mot de passe refusé et nouveau accepté ;
- révocation de toutes les sessions existantes du sujet ;
- session d’un autre sujet conservée ;
- absence d’auto-login ;
- état consommé persistant après recréation du store ;
- deux consommations concurrentes donnant exactement un gagnant ;
- absence de secrets temporaires et mots de passe de test en clair dans SQLite.

## Limite de composition

T2.4-A qualifie le mécanisme et sa persistance, pas encore sa composition produit complète dans `MagasinOutil.CoreHost`. Cette composition appartient à T2.4-D.

La révocation de sessions est actuellement prouvée avec `LocalIdentityAuthority`. Une composition future utilisant une autre autorité de session devra conserver la même garantie `ISubjectSessionRevoker` ou une garantie plus forte.

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

Aucun PASS T2.4-A n’est déclaré avant réception d’un journal vert de `Test-T24.ps1` avec toutes les régressions T2.3 conservées.
