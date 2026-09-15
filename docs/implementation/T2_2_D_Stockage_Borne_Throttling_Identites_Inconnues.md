# T2.2-D — Stockage borné du throttling des identités inconnues

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-2-local-identities-sessions`.
Branche plateforme : `pilot/t2-2-local-identities-sessions`.

## Objectif

Cette micro-tranche ferme un risque de déni de service sur la persistance de sécurité : un attaquant ne doit pas pouvoir faire croître sans limite la base SQLite en soumettant continuellement de nouveaux noms d’utilisateur inexistants.

Elle complète T2.2-C sans modifier la politique fonctionnelle de temporisation. Le seuil de cinq échecs reste acquis ; la courbe de délai demeure configurable et la fixture `1 s → 2 s → 4 s → 8 s` n’est pas une politique produit figée.

## Implémentation

`SqliteIdentitySecurityStore` applique une capacité maximale aux lignes de throttling qui ne correspondent à aucun compte local durable.

- capacité par défaut de qualification : **256** identités inconnues actives ;
- valeur injectable dans le constructeur afin de permettre qualification et adaptation produit ;
- la purge ne cible que les lignes dont le nom normalisé ne correspond à aucun `local_accounts.normalized_user_name` ;
- les compteurs des comptes réels ne sont donc jamais évincés par un flood de faux identifiants ;
- lorsque la limite est dépassée, les plus anciennes lignes inconnues sont supprimées ;
- l’initialisation du store réapplique également la borne, de sorte qu’un redémarrage ne conserve pas un dépassement historique.

Cette limite de persistance ne remplace pas une limitation de volume au niveau du transport. Pour une exposition réseau, une défense supplémentaire par source ou contexte réseau doit être composée avec le throttling par compte ; elle relève du profil de transport et non du store d’identité.

## Qualification locale reçue

Commande exécutée :

```powershell
.\eng\Test-T22.ps1 -PilotRepository D:/Projets/Magasin-Outil-8xx
```

Environnement observé : Windows 10.0.22631, `win-x64`, .NET SDK 10.0.400.

La recette T2.2-D a utilisé une borne réduite à 32 pour une preuve déterministe et rapide, puis a :

1. créé un vrai compte local ;
2. établi un compteur durable de cinq échecs pour ce compte ;
3. injecté 512 noms inconnus directement dans le store ;
4. confirmé que seulement 32 lignes inconnues subsistent ;
5. confirmé que le compteur du vrai compte reste présent et inchangé ;
6. confirmé que les anciennes lignes inconnues sont évincées et que les plus récentes restent disponibles ;
7. recréé le store et confirmé que la borne reste appliquée ;
8. confirmé que le throttling du vrai compte continue normalement après le flood.

Le même journal conserve verts T0/T1, T2.1, T2.2-A, T2.2-B et T2.2-C.

## État

**PASS LOCAL — SIMULATION WINDOWS.**

T2.2-D est clôturé. Avec A, B et C également PASS LOCAL, le lot **T2.2 — identités locales, authentification et sessions** est officiellement clôturé en simulation Windows.

Cette preuve ne qualifie pas une exposition réseau réelle ni le matériel industriel cible. Le secret temporaire de réinitialisation et la récupération signée du dernier administrateur restent des capacités de récupération distinctes prévues dans la suite du chantier de sécurité ; elles ne rouvrent pas les invariants de session et d’authentification déjà qualifiés.

## Suite

Le prochain lot est **T2.3 — licences hors ligne signées et temps de confiance**.