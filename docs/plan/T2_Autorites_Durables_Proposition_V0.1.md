# T2 — Proposition groupée des autorités durables V0.1

Date : 14 septembre 2026. **Proposition à valider ; aucune implémentation T2 engagée.**
Le socle de lecture T0/T1 est PASS LOCAL. Cette proposition ne transforme pas les modèles de rôles ou paramètres candidats en décisions acquises.

## 1. Objectif et acquis

Donner au Core des autorités communes pour l'identité, les permissions, les licences et l'admission durable. HMI locale, navigateur distant et OPC UA devront appeler ces mêmes services. Les particularités de racks restent dans le pilote.

Acquis : application autonome ; plusieurs clients autorisés ; comptes locaux hors ligne ; licences WM incluant des licences temporaires ; audit durable configurable ; SQLite et gRPC remplaçables ; usures seules pour l'Opérateur, y compris en broche sous conditions ; maintenance V1 consultative ; OPC UA et Fleet dans l'architecture. T2 prépare les autorités ; T3 raccorde les opérations, T4 les interfaces web/OPC UA et T5 Fleet.

## 2. État technique vérifié

Références de lecture : plateforme `127181d13f5a712c96793ce82bd8a105c9530bab`, pilote `5329a06bad7e82f5d54261abb94c3fe78e7cd8e1`.

| Élément existant | Réutilisation et travail nécessaire |
|---|---|
| SubjectContext, SubjectKind, permissions | Conserver les types ; produire le contexte côté autorité après validation d'une session. Ne jamais accepter une liste de permissions déclarée par un client. |
| PermissionAuthorizationService | Conserver le contrôle de permission ; ajouter résolution des rôles, portée machine, révocation et version de politique. |
| LocalEntitlementSnapshotProvider | Point de consommation existant ; ne constitue pas une vérification de signature ou un système de licence. L'alimenter uniquement après validation autoritative. |
| TemporalEntitlementValidity | Réutiliser début inclusif, fin exclusive et autorité temporelle explicite. |
| AuditTrailRuntime | Projection historique en mémoire ; ne pas la présenter comme un journal durable. Préserver la projection et fournir une persistance séparée. |
| IAdmissionJournal et MutationContext | Contrats préparatoires T0 ; compléter résultat incertain, récupération, canonicalisation et conservation avant qualification. |
| Tube CurrentUserOnly et ClientId T0/T1 | Limite au compte Windows courant, sans comptes applicatifs. ClientId reste un libellé ; ne lui donner aucun pouvoir d'authentification. |

## 3. Choix recommandés, à valider

### A — Comptes locaux et sessions

- Première version : identifiant et mot de passe nominatifs locaux. La connexion Microsoft reste une extension ; aucune dépendance Microsoft ou Internet pour démarrer et exploiter l'application.
- Aucun compte partagé, mot de passe universel ou compte administrateur livré actif. Création du premier administrateur par un parcours local de mise en service, protégé par un secret d'activation propre à l'installation remis par un canal distinct. L'absence de compte ne doit pas ouvrir un endpoint de création accessible à tous. La procédure de remise du secret sera décrite avant déploiement.
- Stockage de mots de passe sous forme de dérivés salés avec un algorithme éprouvé ; Argon2id candidat, bibliothèque et coût à qualifier sur l'iPC. Aucun secret en clair dans SQLite, l'audit ou les journaux.
- Sessions opaques, révocables et indépendantes par client. Le Core lie session, sujet, client enregistré et portée ; le client ne choisit pas librement cette liaison. Les secrets de session ne figurent pas dans l'audit.
- Redémarrage du Core : réauthentification ; les opérations déjà admises restent suivies indépendamment des sessions. Plusieurs connexions d'une même personne sont permises et identifiées distinctement.
- Réinitialisation d'un mot de passe par un administrateur habilité : secret temporaire à usage unique, changement obligatoire et révocation des sessions. Pour le dernier administrateur perdu : procédure locale avec autorisation de récupération signée propre à l'installation, à usage unique, sans mot de passe de secours universel. La signature de récupération a une finalité et une clé distinctes de l'émission des licences ; une licence ne peut jamais réinitialiser un compte.

Paramètres de départ **proposés**, tous versionnés et configurables dans les limites qualifiées : verrouillage après 10 minutes sans activité humaine ; durée absolue de session de 8 heures ; secret temporaire valable 15 minutes ; temporisation progressive à partir de 5 échecs rapprochés. Les rafraîchissements automatiques ne sont pas une activité humaine. Déconnexion et verrouillage n'arrêtent jamais une opération admise. Le comptage des échecs combine compte et origine pour limiter les blocages provoqués par un tiers ; les seuils exacts doivent être testés.

### B — Permissions, rôles et consultation

Adopter comme modèles initiaux les quatre rôles de la [matrice V0.1](../securite/Permissions_Roles_V0.1.md) : Consultation, Opérateur, Régleur outils, Administrateur. Les rôles sont configurables ; aucun rôle implicite universel et aucune permission `*` pour les clients.

Les seuls droits Opérateur déjà validés sont l'édition des usures, y compris en broche. Dans le modèle proposé, Préparer et Charger sont également attribués à l'Opérateur : **cette attribution fait partie de la validation demandée ici**. Administrateur ne reçoit aucune commande machine par défaut. Toute affectation de rôle est auditée ; proposer une réauthentification pour les changements sensibles. L'auto-attribution d'un rôle par un administrateur reste possible dans ce modèle, explicitement auditée ; aucune double validation organisationnelle n'est présumée.

Proposition : aucune consultation anonyme des données métier dans la première version des autorités. Un diagnostic minimal de disponibilité peut rester public sans données outil, compte ou licence. Un éventuel écran public d'atelier sera une capacité séparée à décider. Le client de diagnostic T0/T1 devra alors être authentifié ou rester limité à un profil de test explicitement distinct.

Les identités techniques web, OPC UA et exporteur Fleet sont enregistrées séparément des personnes. Un service ne peut agir au nom d'un humain par simple paramètre. La preuve de délégation est validée côté Core. L'OPC UA associe confiance du client et identité autorisée ; Fleet ne reçoit pas de commande machine. Les mécanismes de confiance seront adaptés au transport, derrière des contrats communs.

### C — Licences temporaires hors ligne

Proposer un fichier de licence signé contenant au minimum : identifiant, version de format, émetteur et identifiant de clé, installation cible, produit, capacités, début de validité, expiration éventuelle et version de renouvellement.

- Clé privée de signature uniquement dans un outil d'émission WM séparé ; le logiciel machine embarque les clés publiques approuvées. Clés de test distinctes des clés de production.
- Rattachement recommandé à une identité cryptographique d'installation, protégée par le système d'exploitation. Pas d'attachement rigide à une adresse réseau ou à un disque. Remplacement d'iPC : réémission autorisée, avec procédure de migration.
- Sans matériel de confiance qualifié, ce rattachement ne garantit pas une résistance absolue à un administrateur du système ou à une copie complète de son état. La disponibilité d'un module matériel de confiance sur l'iPC n'est pas supposée.
- Expiration : blocage des nouvelles modifications et commandes ; consultation selon droits et poursuite des opérations admises, comme déjà validé. Pas de prolongation silencieuse.
- Temps : horloge monotone pour les durées du processus, horloge civile et dernier repère persisté pour la période de licence. Recul incohérent détecté : état explicite et refus des nouvelles mutations soumises à licence. Une restauration complète de machine peut contourner des repères purement logiciels ; aucune inviolabilité hors ligne n'est revendiquée.
- Renouvellement et récupération temporelle par un document signé, lié à l'installation et journalisé. La simple modification de l'horloge ne réactive pas automatiquement les droits.
- Une révocation émise par WM ne peut pas être instantanément connue d'une machine totalement isolée : elle arrive avec un import approuvé. Cette limite est inhérente au fonctionnement hors ligne.

La durée commerciale des licences, les personnes habilitées à les émettre et les conditions de transfert restent des décisions WM. T2 peut les représenter sans inventer leur politique commerciale.

### D — Admission, stockage et audit

Un seul propriétaire d'écriture : services du Core. Les interfaces ne lisent pas SQLite directement. Les contrats utilisent intentions, identités, versions et résultats métier, sans connexion SQL ni transaction fournisseur exposée.

Pour une nouvelle demande : valider la session, résoudre les droits actuels et la portée, contrôler licence et conditions, puis enregistrer atomiquement intention, corrélation et audit d'admission **avant effet technologique**. Les changements concurrents de droits/politique sont ordonnés par rapport à l'admission. Une révocation postérieure n'annule pas implicitement une opération déjà admise.

Même intention, même propriétaire, même cible et même contenu canonique : retrouver le résultat autorisé. Autre contenu ou propriétaire : refus, sans divulguer le traitement d'autrui. La révision attendue et les champs effectivement demandés font partie du contenu canonique. Conserver également une représentation versionnée permettant la reprise, pas seulement une empreinte.

La transaction SQLite ne comprend jamais l'effet PLC. Après un commit ou un effet incertain : consulter, observer et réconcilier ; ne pas rejouer automatiquement une commande. Documenter les points de crash avant commit, après commit, après soumission et avant enregistrement de la réponse.

L'audit conserve acteur, client, cible, action, décisions, horodatages disponibles, corrélations et modifications utiles, sans mots de passe, jetons ou clés. Les événements d'admission et de résultat restent distincts. Un refus impossible à écrire n'est pas présenté comme durablement audité.

Politique de qualification proposée : conservation de l'audit 365 jours, budget 1 Gio, alerte à 80 %. Ce sont des paramètres pilotes à valider, **pas une exigence CRA ni une durée légale**. Nettoyage uniquement selon rétention autorisée ; jamais de suppression d'une opération non résolue pour libérer de l'espace. Si aucun nettoyage admissible ne suffit, bloquer les nouvelles mutations. Proposer de conserver les identifiants minimaux de déduplication pendant la vie de l'installation ; le budget et leur export/migration doivent être suivis. Les volumes réels peuvent imposer d'ajuster ces paramètres.

Sauvegarde cohérente par le service de stockage, pas copie brute d'un fichier ouvert. Proposition : sauvegarde quotidienne et avant migration, dix sauvegardes quotidiennes conservées. Une sauvegarde sur le même disque ne protège pas de la perte du disque ; destination externe et responsabilité d'exploitation restent à définir.

Remplacer SQLite impose migration, sauvegarde/restauration et tests de conformité sur atomicité, concurrence, commit incertain et reprise. Aucune substitution à chaud n'est promise.

### E — Récupération lorsque l'audit ou la licence bloque

Valider une liste fermée, pas une exemption générale d'administrateur :

| Situation | Autorisé sous droits et contrôle spécifiques | Toujours exclu |
|---|---|---|
| Licence expirée, audit disponible | Lire, suivre les opérations admises, importer un renouvellement signé | Nouvelle écriture outil, préparation ou chargement |
| Audit indisponible | Diagnostic, consultation autorisée possible, récupération supervisée du stockage | Nouvelle commande métier ou changement courant de rôles |
| Audit indisponible et licence expirée | Rétablir d'abord la capacité d'audit, puis renouveler la licence | Import silencieux ou commande avec traces seulement en mémoire |
| Dernier administrateur perdu | Récupération locale signée et à usage unique | Compte maître permanent ou secret universel |

Pour rétablir le stockage, proposer un journal de secours local réservé aux événements de récupération, protégé et borné, distinct du magasin de données principal. Il doit accepter une trace avant changement ; les traces sont ensuite réintégrées avec conservation de leur provenance. Ce journal ne sert jamais à admettre des commandes métier pendant la panne.

Si même ce journal ne peut écrire ou si l'identité ne peut être vérifiée, l'application n'effectue pas de mutation de récupération : intervention locale hors application selon procédure de maintenance avec conservation externe des preuves. Pas de privilège automatique obtenu en simulant une panne. La procédure doit être exercée sur corruption, disque plein et restauration d'une ancienne sauvegarde, y compris le risque de réintroduire des comptes ou droits révoqués.

### F — Profils et services communs

Les quatre profils restent acquis et sans défaut choisi. Identités, licences, autorisation et audit sont communs aux profils, pas des modules désactivables pour contourner les contrôles. Les profils pilotent les interfaces web distantes, OPC UA et Fleet ainsi que leur configuration.

Le tube nommé reste une option de déploiement locale. Des comptes système séparés nécessiteront des règles d'accès explicites au tube ; `CurrentUserOnly` n'est pas une preuve que ce déploiement est déjà disponible. L'activation réseau devra ajouter authentification du service, chiffrement et confiance qualifiés ; elle ne découle pas automatiquement du succès local T0/T1.

## 4. Répartition et ordre de réalisation proposés

| Sous-lot | Plateforme | Pilote et critère de sortie |
|---|---|---|
| T2.1 — contrats et stockage | Compléter admission, sessions et ports de stockage ; adaptateur SQLite et migrations | Composition locale ; tests de transaction, concurrence et restauration |
| T2.2 — identités | Comptes, mise en service, sessions et révocation ; résolution des permissions | Deux clients nominatifs ; droits retirés effectifs avant nouvelle admission |
| T2.3 — licences | Validation signée et temporelle ; outil d'émission de test séparé | Expiration/renouvellement/recul d'horloge exercés hors ligne |
| T2.4 — audit et récupération | Admission durable, projections filtrées, sauvegarde et voie de secours | Disque plein, stockage indisponible, crash et récupération sans contournement |
| T2.5 — qualification | Suites de conformité et nouveaux paquets versionnés | Régression T0/T1 et preuves locales Windows consignées |

Préparer un chemin de validation nominatif pour le client de diagnostic ; ne pas attendre le choix du renderer pour tester les autorités. Les comptes, licences et droits ne sont pas codés dans Avalonia ou Blazor. Le vrai parcours graphique d'administration attend T4.

## 5. Critères de sortie

- Impossible de gagner des permissions en fabriquant ClientId, SubjectContext ou une référence de session.
- Révocation, expiration et concurrence d'admission exercées, sans annuler les opérations déjà admises.
- Mot de passe et jeton absents des fichiers et événements de diagnostic.
- Signature invalide, mauvaise installation, renouvellement ancien et horloge incohérente refusés explicitement.
- Crash à chaque point de la transaction/commande : ni perte de corrélation admise ni rejeu aveugle.
- Panne d'audit : nouveaux effets bloqués ; consultation disponible uniquement si identité et données nécessaires restent vérifiables.
- Voie de récupération réellement utilisable, mais incapable d'admettre une commande métier.
- Sauvegarde/restauration et remplacement d'adaptateur documentés ; pas de validation d'un autre moteur sans test.
- Compilation et tests en local, sans budget GitHub Actions requis. Versions et empreintes des paquets ajoutées aux futures preuves.

## 6. CRA et limites produit

Le Cyber Resilience Act (CRA) ne se résume pas aux comptes ou aux licences. T2 doit contribuer à l'analyse de risque, aux valeurs par défaut sûres, à la protection des secrets et à la traçabilité. Le projet doit aussi préparer gestion des vulnérabilités, inventaire des dépendances, mises à jour de sécurité, période de support et responsabilités de traitement des signalements.

La Commission indique l'application des obligations de signalement à partir du 11 septembre 2026 et des obligations principales à partir du 11 décembre 2027. L'applicabilité précise au produit WM et les responsabilités du fabricant doivent être établies ; ce dossier ne constitue pas une déclaration de conformité. Une licence commerciale expirée ne doit pas être utilisée pour couper les correctifs de sécurité dus pendant la période de support applicable.

Sources consultées le 14 septembre 2026 :
- [Commission européenne — CRA](https://digital-strategy.ec.europa.eu/en/policies/cyber-resilience-act).
- [OWASP — stockage des mots de passe](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

## 7. Validation demandée

Valider ou corriger les choix A à F et leurs paramètres de départ. Les paramètres candidats ne sont pas des politiques WM existantes. La validation du modèle n'invente pas la durée commerciale des licences, la destination des sauvegardes ou la disponibilité de matériel de confiance.

Les points qui nécessitent une décision avant leur sous-lot sont : attribution Préparer/Charger à l'Opérateur, absence de lecture métier anonyme, modèle de récupération signé, règles de session, rattachement de licence à l'installation et politique de conservation. Les bibliothèques cryptographiques exactes et les budgets de calcul seront qualifiés techniquement ; aucune cryptographie maison.

Aucun code T2, compte, secret, licence ou configuration de machine n'est créé par ce dossier.
