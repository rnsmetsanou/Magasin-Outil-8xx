# T2.2-D — Stockage borné du throttling des identités inconnues

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche ferme un risque de déni de service sur la persistance de sécurité : un attaquant ne doit pas pouvoir faire croître sans limite la base SQLite en soumettant continuellement de nouveaux noms d’utilisateur inexistants.

Elle complète T2.2-C sans modifier la politique fonctionnelle de temporisation. Le seuil de cinq échecs reste acquis ; la courbe de délai demeure configurable et la fixture `1 s → 2 s → 4 s → 8 s` n’est pas une politique produit figée.

## Implémentation

`SqliteIdentitySecurityStore` applique désormais une capacité maximale aux lignes de throttling qui ne correspondent à aucun compte local durable.

- capacité par défaut de qualification : **256** identités inconnues actives ;
- valeur injectable dans le constructeur afin de permettre qualification et adaptation produit ;
- la purge ne cible que les lignes dont le nom normalisé ne correspond à aucun `local_accounts.normalized_user_name` ;
- les compteurs des comptes réels ne sont donc jamais évincés par un flood de faux identifiants ;
- lorsque la limite est dépassée, les plus anciennes lignes inconnues sont supprimées ;
- l’initialisation du store réapplique également la borne, de sorte qu’un redémarrage ne conserve pas un dépassement historique.

Cette limite de persistance ne remplace pas une limitation de volume au niveau du transport. Pour une exposition réseau, une défense supplémentaire par source ou contexte réseau doit être composée avec le throttling par compte ; elle relève du profil de transport et non du store d’identité.

## Recette T2.2-D

Nouveau projet de qualification :

`tests/Platform.Poc.Identity.ThrottleStorage.Tests`

La recette utilise une borne réduite à 32 pour rendre la preuve déterministe et rapide, puis :

1. crée un vrai compte local ;
2. établit un compteur durable de cinq échecs pour ce compte ;
3. injecte 512 noms inconnus directement dans le store, sans exécuter Argon2id 512 fois ;
4. vérifie que seulement 32 lignes inconnues subsistent ;
5. vérifie que le compteur du vrai compte est toujours présent et inchangé ;
6. vérifie que les anciennes lignes inconnues sont évincées et que les plus récentes restent disponibles ;
7. recrée le store et confirme que la borne reste appliquée ;
8. confirme que le throttling du vrai compte continue normalement après le flood.

La commande globale reste :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

## État

**IMPLÉMENTÉ — À QUALIFIER LOCALEMENT.**

T2.2 ne sera clôturé qu’après PASS de T2.2-D avec T0/T1, T2.1 et T2.2-A/B/C toujours verts. Le secret temporaire de réinitialisation et la récupération signée du dernier administrateur restent des capacités de récupération distinctes qui pourront être traitées dans une tranche ultérieure sans rouvrir les invariants de session/authentification déjà qualifiés.
