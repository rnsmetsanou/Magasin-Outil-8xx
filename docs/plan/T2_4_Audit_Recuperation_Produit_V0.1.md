# T2.4 — Audit et chemins de récupération produit — Plan V0.1

Date : 15 septembre 2026.
Branche pilote : `pilot/t2-4-audit-recovery`.
Branche plateforme : `pilot/t2-4-audit-recovery`.
Statut : **plan d’implémentation après clôture T2.3**.

## 1. Objectif

T2.4 qualifie les parcours exceptionnels de récupération sans introduire de bypass général des autorités nominales.

Le principe central reste :

> une action de récupération est une capacité explicite, fermée, traçable et plus limitée qu’un fonctionnement normal ; elle ne devient jamais une autorité implicite pour les commandes métier.

T2.4 ne modifie aucun programme PLC.

## 2. Décisions déjà acquises

Les décisions T2 imposent :

- liste fermée d’actions de récupération ;
- aucun bypass général accordé au rôle Administrateur ;
- secret temporaire de réinitialisation de mot de passe, durée initiale 15 minutes ;
- récupération signée du dernier administrateur, liée à l’installation et à usage unique ;
- finalité et clé de récupération distinctes des licences ;
- licence expirée + audit disponible : consultation/suivi et renouvellement signé possibles, aucune nouvelle commande métier ;
- audit indisponible : diagnostic, consultation vérifiable et récupération supervisée du stockage uniquement ;
- audit indisponible + licence expirée : audit restauré avant renouvellement ;
- journal de secours local borné réservé aux événements de récupération ;
- journal de secours incapable d’admettre une commande métier ;
- absence de mutation de récupération si l’identité ou la trace de récupération ne peut pas être vérifiée/persistée.

## 3. Principes de conception

### 3.1 Autorités distinctes

T2.4 ne fusionne pas :

- authentification nominale ;
- permissions administratives ;
- licence produit ;
- récupération de compte ;
- récupération du dernier administrateur ;
- récupération du stockage/audit.

Chaque parcours possède un contrat et une finalité explicites.

### 3.2 Pas de secret maître permanent

Aucun mot de passe universel, code usine réutilisable ou compte caché n’est introduit.

Les secrets temporaires de récupération sont :

- générés avec un générateur cryptographiquement sûr ;
- liés à une identité/cible précise ;
- limités dans le temps ;
- à usage unique ;
- stockés uniquement sous une représentation non réversible adaptée à leur entropie ;
- consommés atomiquement avant ou avec la mutation qu’ils autorisent.

### 3.3 Récupération ≠ session normale

Présenter un secret temporaire ou un artefact signé de récupération ne crée pas automatiquement une session utilisateur générale.

L’autorisation obtenue est limitée à l’action de récupération prévue.

### 3.4 Audit obligatoire ou repli borné

Une mutation de récupération doit laisser une trace durable.

Si l’audit principal est indisponible, seules les actions explicitement autorisées par la matrice de récupération peuvent utiliser un journal de secours borné. Ce journal n’est jamais une seconde base d’admission métier.

## 4. Découpage

## T2.4-A — secret temporaire de réinitialisation de mot de passe

### Objectif

Permettre la réinitialisation d’un mot de passe oublié lorsqu’une autre autorité administrative valide peut encore gouverner le compte cible.

Ce parcours ne traite **pas** le cas où aucun administrateur exploitable ne subsiste ; ce cas appartient à T2.4-B.

### Contrat proposé

Une autorisation temporaire contient au minimum :

- identifiant d’autorisation ;
- identifiant du sujet cible ;
- instant de création ;
- expiration ;
- représentation dérivée du secret ;
- état `Active`, `Consumed`, `Revoked` ou `Expired` dérivé ;
- révision optimiste ;
- contexte d’émission/audit.

Le secret présenté au bénéficiaire n’est jamais persisté en clair.

### Règles

- durée pilote initiale : **15 minutes** ;
- usage unique ;
- une consommation réussie remplace le mot de passe et invalide l’autorisation dans une même frontière atomique ;
- un secret expiré/révoqué/consommé ne peut plus agir ;
- la réponse ne doit pas permettre d’énumérer inutilement les comptes ;
- le nouveau mot de passe suit exactement la même politique de dérivation que l’authentification normale ;
- la récupération ne modifie pas les rôles/permissions du compte ;
- le bénéficiaire repasse ensuite par l’authentification normale ; aucune session générale automatique n’est créée.

### Autorité d’émission

L’émission nominale doit exiger une autorité administrative explicite, candidate : `identity.manage`.

Cette permission permet d’émettre/révoquer une autorisation de réinitialisation, mais pas de lire le secret d’un utilisateur ni de contourner les autres contrôles.

### Preuves A

- contrat indépendant de SQLite et de l’implémentation Argon2id ;
- secret 256 bits ou entropie équivalente généré cryptographiquement ;
- aucun secret en clair dans SQLite ;
- expiration à 15 minutes dans la fixture ;
- mauvais secret refusé ;
- usage unique ;
- concurrence : un seul consommateur gagne ;
- mot de passe remplacé durablement ;
- permissions inchangées ;
- ancien mot de passe refusé, nouveau accepté ;
- redémarrage du store conserve l’état consommé ;
- aucun login/session générale automatique après reset.

## T2.4-B — récupération signée du dernier administrateur

### Objectif

Récupérer une installation où l’autorité administrative humaine est perdue sans créer de compte maître permanent.

### Artefact signé proposé

Payload versionné contenant au minimum :

- identifiant de récupération ;
- séquence anti-replay ;
- finalité explicite `last-administrator-recovery` ;
- émetteur ;
- identifiant de clé ;
- identité d’installation ;
- fenêtre de validité ;
- action autorisée strictement bornée.

### Règles

- clé/finalité distinctes de l’émission de licence et de la récupération temporelle ;
- signature vérifiée hors ligne ;
- liaison stricte à l’installation ;
- usage unique / séquence monotone ;
- impossible à transformer en commande machine ;
- l’identité administrative restaurée reçoit uniquement les droits administratifs explicitement définis, jamais des commandes machine implicites ;
- refus lorsque le scénario ne correspond pas réellement à une perte de l’autorité administrative selon la politique qualifiée ;
- trace durable obligatoire.

### Preuves B

- mauvaise installation, mauvaise clé, mauvaise finalité ou signature modifiée refusées ;
- replay refusé ;
- artefact de licence inutilisable comme récupération administrateur ;
- artefact de récupération temporelle inutilisable ;
- récupération atomique et durable ;
- aucune permission machine implicite ;
- état survivant au redémarrage.

## T2.4-C — audit dégradé et journal de secours borné

### Objectif

Définir le comportement lorsque l’audit principal n’est plus disponible sans rendre l’application impossible à diagnostiquer ni ouvrir une voie secondaire d’admission métier.

### Matrice fermée initiale

Lorsque l’audit principal est indisponible :

Autorisé sous conditions :

- diagnostic ;
- consultation de données dont l’autorité reste vérifiable ;
- consultation de l’état de licence/identité ;
- restauration/sauvegarde supervisée de l’audit ;
- événements de récupération explicitement inscrits au journal de secours.

Refusé :

- nouvelle commande machine ;
- mutation métier ;
- changement courant de rôle/permission ;
- utilisation du journal de secours comme substitut de l’admission durable ;
- récupération applicative si la trace de récupération ne peut pas être persistée.

### Journal de secours

Le journal de secours est :

- local ;
- borné ;
- append-only au niveau du contrat logique ;
- réservé à une liste fermée de types d’événement de récupération ;
- distinct du store d’admission métier ;
- incapable de produire un `OperationId` admis ou une preuve d’admission métier.

La politique exacte de taille/rétention sera qualifiée comme paramètre produit et ne sera pas inventée silencieusement dans le contrat.

### Preuves C

- audit principal disponible : journal de secours non utilisé pour le nominal ;
- audit indisponible : commande/mutation refusée ;
- diagnostic/consultation autorisés selon matrice ;
- récupération autorisée seulement si sa trace peut être persistée ;
- saturation du journal de secours ne permet pas de supprimer une trace non résolue ni d’autoriser une mutation sans trace ;
- restauration de l’audit nécessaire avant retour au nominal ;
- audit indisponible + licence expirée : renouvellement refusé jusqu’au rétablissement de l’audit conformément à la décision T2.

## T2.4-D — composition pilote et clôture

### Objectif

Prouver que les mécanismes A/B/C sont réellement composés dans le processus pilote sans fuite vers les clients.

### Preuves D

- composition côté `MagasinOutil.CoreHost` ;
- stores concrets uniquement côté Core ;
- clients HMI/API/OPC UA sans accès direct SQLite ;
- persistance et anti-replay après redémarrage ;
- T0/T1 + T2.1 + T2.2 + T2.3 toujours verts ;
- aucune modification PLC.

## 5. Ordre d’implémentation

1. T2.4-A — secret temporaire de réinitialisation ;
2. T2.4-B — dernier administrateur signé ;
3. T2.4-C — audit dégradé et journal de secours ;
4. T2.4-D — composition pilote et clôture.

Cet ordre permet de qualifier d’abord une récupération d’identité ciblée et simple, puis le scénario exceptionnel de perte totale d’autorité administrative, avant d’aborder la dégradation de l’audit qui affecte plusieurs autorités simultanément.

## 6. Points à ne pas confondre

- le secret temporaire A n’est pas une licence ;
- la récupération B n’est pas un mot de passe maître ;
- le journal de secours C n’est pas une base d’admission secondaire ;
- le rôle Administrateur ne constitue pas un bypass de récupération ;
- le renouvellement de licence ne répare pas une panne d’audit ;
- un PASS Windows simulé ne constitue pas une qualification produit ou CRA.

## 7. Prochaine action

Commencer T2.4-A par les contrats fournisseur-indépendants et la recette d’invariants, puis seulement ajouter l’adaptateur SQLite et la composition.
