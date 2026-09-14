# T2 — Décision sur les autorités durables

Date : 15 septembre 2026. Statut : **validé pour l'implémentation T2**.

Ce document actualise la proposition `../plan/T2_Autorites_Durables_Proposition_V0.1.md`. Il ne réécrit pas le document historique : il fixe les arbitrages explicitement acceptés avant le démarrage de T2.1.

## Décisions validées

### A — Comptes locaux et sessions

- Comptes locaux nominatifs utilisables hors ligne, sans dépendance Microsoft ni Internet pour exploiter l'application.
- Aucun compte partagé, mot de passe universel ou administrateur produit livré actif.
- Premier administrateur créé par un parcours local de mise en service protégé par un secret propre à l'installation.
- Mots de passe stockés uniquement sous forme de dérivés salés avec un algorithme éprouvé ; aucune cryptographie maison.
- Sessions opaques, révocables, liées côté Core au sujet, au client enregistré et à la cible. Un identifiant client ou une référence de session fournis par un appelant n'accordent aucun droit par eux-mêmes.
- Redémarrage du Core : réauthentification requise. Une opération déjà admise continue indépendamment de la session qui l'a initiée.
- Valeurs initiales du pilote : 30 minutes d'inactivité humaine pour une HMI locale, 10 minutes pour un client interactif distant, durée absolue de session de 8 heures, secret temporaire de 15 minutes et temporisation progressive à partir de 5 échecs rapprochés. Ces valeurs restent configurables et doivent être qualifiées.
- Récupération du dernier administrateur par autorisation signée à usage unique, liée à l'installation, sans mot de passe maître permanent. La finalité et la clé de récupération sont distinctes de celles des licences.

### B — Permissions, rôles et consultation

- Quatre rôles initiaux : Consultation, Opérateur, Régleur outils et Administrateur. Ils restent configurables, sans hiérarchie implicite ni permission universelle `*` pour les comptes humains ou les services.
- L'Opérateur peut modifier uniquement les usures autorisées, y compris celles de l'outil en broche sous les conditions machine communes déjà décidées.
- `tool.prepare` et `tool.load` sont attribués à l'Opérateur et au Régleur outils dans le modèle initial.
- L'Administrateur ne reçoit aucune commande machine par défaut ; administration et exploitation restent séparées par les rôles.
- Pas de lecture métier anonyme via OPC UA. Les opérations OPC UA utilisent une identité authentifiée et les mêmes autorités que les autres clients.
- Une future identité locale `Guest`/Consultation strictement lecture seule pourra être introduite explicitement pour `magazine.read`. Elle sera une identité configurée et limitée, pas un contournement de l'authentification ni une autorisation anonyme OPC UA.
- Web, OPC UA et Fleet utilisent des identités Service distinctes. Fleet reste en publication/supervision et ne reçoit pas de commande machine.

### C — Licences temporaires hors ligne

- Fichier de licence signé avec format versionné, identité de licence, émetteur/clé, installation cible, produit, capacités, début de validité, expiration éventuelle et version de renouvellement.
- Clé privée uniquement dans l'outil d'émission WM séparé ; seules les clés publiques approuvées sont embarquées côté machine. Clés de test et de production distinctes.
- Rattachement à une identité cryptographique d'installation, sans dépendance rigide à une adresse réseau ou à un disque particulier.
- Une expiration ou une incohérence temporelle bloque les nouvelles mutations soumises à licence, sans interrompre une opération déjà admise.
- Renouvellement et récupération temporelle par document signé et journalisé. Une machine totalement isolée ne peut connaître une révocation qu'après import approuvé.

La durée commerciale des licences, les personnes habilitées à les émettre et les règles commerciales de transfert restent des décisions WM hors de ce choix d'architecture.

### D — Admission, stockage et audit

- Le Core est l'unique propriétaire des écritures durables. Les interfaces et clients n'accèdent jamais directement à SQLite.
- Session, permissions, portée, licence et conditions sont vérifiées avant admission.
- Intention, corrélation, représentation canonique versionnée et audit d'admission sont persistés atomiquement avant tout effet technologique.
- Une transaction de stockage n'englobe jamais l'effet PLC, CNC ou robot. En cas de résultat ou de commit incertain : lookup, observation et réconciliation ; aucun rejeu automatique de commande physique.
- SQLite est l'adaptateur V1 choisi, mais les contrats restent indépendants du fournisseur et un remplacement exige migration et tests de conformité.
- Paramètres initiaux du pilote : rétention d'audit de 365 jours, budget de 1 Gio, alerte à 80 %, sauvegarde quotidienne et avant migration, dix sauvegardes quotidiennes conservées. Ce sont des valeurs produit configurables à qualifier, pas des exigences légales ou CRA.
- Une opération non résolue ne peut pas être supprimée pour libérer de l'espace.

La destination externe des sauvegardes et la responsabilité d'exploitation restent à définir.

### E — Récupération

- Liste fermée d'actions de récupération ; aucun bypass général lié au rôle Administrateur.
- Licence expirée avec audit disponible : consultation autorisée selon droits, suivi des opérations admises et import d'un renouvellement signé ; aucune nouvelle commande métier.
- Audit indisponible : diagnostic, consultation lorsque l'identité et les données restent vérifiables, et récupération supervisée du stockage ; aucune nouvelle commande métier ni modification courante des rôles.
- Audit indisponible et licence expirée : rétablir l'audit avant le renouvellement.
- Journal de secours local borné réservé aux événements de récupération. Il ne peut jamais servir à admettre une commande métier.
- Si l'identité ou la trace de récupération ne peut être vérifiée/persistée, aucune mutation de récupération applicative n'est autorisée.

### F — Profils et services communs

- Identité, licence, autorisation et audit restent obligatoires dans les quatre profils de déploiement et ne sont pas des modules désactivables.
- Le tube nommé Windows reste une option de déploiement local. Des comptes système séparés nécessitent des règles d'accès explicites.
- Toute exposition réseau ajoute authentification de service, chiffrement et confiance qualifiés ; la réussite de T0/T1 en local ne vaut pas qualification réseau.

## Répartition d'architecture confirmée

Les autorités génériques — sessions, identité, autorisation, licences, admission durable, audit et contrats de stockage — appartiennent à `PlateformeWM-Demo`. `Magasin-Outil-8xx` consomme ces capacités et conserve les règles propres au magasin 8xx, aux racks, emplacements et parcours produit.

## Démarrage de T2.1

Une branche dédiée `pilot/t2-1-durable-authorities-storage` est utilisée dans les deux dépôts. T2.1 commence par les contrats fournisseur-indépendants et les invariants d'autorité, avant l'adaptateur SQLite. Les premiers contrats plateforme couvrent la résolution de session autoritative et l'admission durable avec représentation canonique versionnée.

Aucune modification du programme PLC n'est autorisée ou nécessaire pour cette tranche simulée.
