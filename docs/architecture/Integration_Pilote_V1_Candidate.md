# Architecture d’intégration du pilote Magasin 8xx — V1 candidate

Date : 14 septembre 2026. Statut : **proposition à valider**, sans modification de code, de compte, de licence, de configuration ni du PLC. Ce document prépare les décisions ; il ne remplace pas les références normatives de la plateforme.

## 1. Références et état

Dépôts examinés : Magasin-Outil-8xx à 0d39a4084314c42bbb805addf35c24264e0a38d3 ; PlateformeWM-Demo à bc0819d0774c67e920481a7e0312d12f55a291a8. Les têtes des branches de travail ont été vérifiées pour cette étape.

Les décisions acquises figurent dans le [registre](../decisions/Registre_Decisions.md). Le [plan](../Feuille_de_Route_Pilote_V1.md) situe cette livraison entre contrat V1 et architecture d’intégration. Le code du pilote demeure Avalonia simulé ; les transports, le stockage et les paquets décrits ici ne sont pas implémentés.

Vérifications nouvelles :
- Northbound.Runtime référence les runtimes Machine/Application et adapte leur autorité au contrat public.
- Gateway.OpcUa référence Northbound.Contracts ; cette séparation est à préserver.
- Hmi.Runtime référence directement Application.Runtime : sa réutilisation dans un processus web séparé nécessite une adaptation explicite.
- Le pont de preuve WS-AT15 utilise un tube nommé et des messages JSON ; il se trouve dans les tests, s’appelle ExperimentalHmiRuntimeBridge et accepte une instance cliente. Il démontre une frontière, pas un transport produit multi-client prêt à distribuer.
- Les projets inspectés utilisent des références de projets ; leur distribution sous forme de paquets produit reste un travail à faire.

## 2. Décisions acquises et décisions proposées

Acquis : application autonome ; direction web locale ; runtime Machine/Application séparé de la HMI ; multi-client souhaité ; profils Local isolé, Intégration usine, Rattaché au parc et Intégration complète ; exposition OPC UA V1 ; Fleet consultatif ; comptes locaux ; licences temporaires ; audit durable ; maintenance consultative ; édition Opérateur limitée aux usures, y compris en broche.

Propositions de ce dossier :
1. Composants communs distribués en paquets NuGet versionnés, avec source locale possible pour développement hors ligne.
2. Communication interne par gRPC sur tubes nommés Windows, derrière les contrats communs.
3. SQLite local, accessible exclusivement par les services autoritatifs du cœur pour les données produit durables.
4. Passerelle OPC UA dans un processus séparé du cœur et de l’hôte web.
5. Contrats d’édition et reprise définis ci-dessous ; composition de la première tranche simulée.

Aucune de ces cinq propositions n’est considérée acceptée par la simple poursuite de l’analyse. Blazor Interactive Server reste le candidat de rendu ; MudBlazor reste à qualifier.

## 3. Répartition logique des responsabilités

| Propriétaire | Responsabilités | Dépendances interdites |
|---|---|---|
| Technology Beckhoff | ADS, symboles, conversions natives, notifications/acquisition, protocole PLC et gardes proches de l’effet | UI, rôles métier, décisions commerciales |
| Machine Runtime | Observations sémantiques, sessions, ressources et preuve physique des opérations | Renderer, serveur OPC UA concret, collecteur Fleet |
| Application magasin | Cas d’usage, édition de champs autorisés, coordination et conflits ; réutilisation du Tool Handling commun | Syntaxe PLC, composants graphiques |
| Services transversaux | Comptes, autorisation, licence, audit, configuration, temps, conversions, persistance et compatibilité | Logique graphique et branches par fournisseur |
| HMI Runtime et renderer web | Projections, sélection, brouillons, saisie, préférences et affichage de disponibilité | Instances locales des autorités Machine et licence |
| Passerelle OPC UA | Traduction des lectures et méthodes, association de session à une identité validée | SDK Beckhoff, moteur métier parallèle |
| Projection/exporteur Fleet | Rapport borné à partir d’états déjà acquis | Déclenchement d’action ou d’acquisition technologique |
| Composition produit | Assemblage, paramètres d’installation et cycle de vie | Modification de la vérité machine pour simuler une activation réussie |

Les noms de projets à créer ne sont pas figés. Les primitives partagées sont développées dans la plateforme ; les écrans et règles spécifiques du magasin restent dans son dépôt. Un mapping 8xx ne doit pas devenir une condition fournisseur dans un service applicatif générique.

## 4. Processus proposés

- **Core Host sur le PC industriel** : Machine/Application, adaptateur actif et services transversaux autoritatifs. Peut démarrer et rester disponible sans interface.
- **Web Host sur le même PC** : présentation HMI et sessions navigateur. Dispose d’un client des contrats communs, pas d’un deuxième Machine Runtime.
- **OPC UA Host sur le même PC** : passerelle existante adaptée à un client distant du contrat northbound. Isolation proposée pour limiter l’effet d’un arrêt ou d’une panne du serveur OPC UA.
- **Navigateurs** : écran local et autres postes autorisés.
- **Fleet** : projection/exporteur côté machine ; collecteur de parc indépendant. L’emplacement réel du collecteur reste à choisir. Pour les essais, un hôte local distinct suffit à préparer une preuve limitée.

Le nombre de processus n’est pas un profil de déploiement. Le profil active les capacités compatibles et autorisées. En profil isolé, le Web Host reste actif pour le navigateur local tandis que son accès réseau est désactivé.

L’export Fleet doit utiliser une exécution bornée sans file de rapports historiques dans le chemin machine. L’arrêt du collecteur ne bloque pas l’admission locale. Son placement précis côté machine reste à définir sans imposer un exécutable supplémentaire par principe.

Le redémarrage du Core est un cas différent du redémarrage d’un client : après perte du Core, les observations doivent être réacquises et les opérations incertaines réconciliées. La continuité physique n’est pas garantie par la seule persistance d’un identifiant.

## 5. Réutilisation du HMI Runtime

Proposition : extraire ou consolider les interfaces nécessaires dans les contrats applicatifs communs ; faire dépendre Hmi.Runtime de ces interfaces plutôt que du projet concret Application.Runtime. Le Core fournit l’implémentation autoritative, le Web Host consomme une implémentation cliente distante.

Ce travail doit conserver les projections et invariants de la plateforme ainsi que leurs tests. Il ne consiste pas à remplacer Hmi.Runtime par un second modèle autonome dans Blazor.

L’état de présentation est par client : sélection, brouillon, culture, unités. Une session de navigateur n’est ni une session Machine ni l’identité d’un utilisateur global. Les droits affichés sont une projection ; le contrôle effectif reste côté Core.

## 6. Transport interne proposé

**gRPC**, mécanisme d’appel de services à distance, sur tubes nommés Windows entre les hôtes locaux. Motivation : contrats typés, échanges d’observation possibles et contrôle d’accès du transport par le système Windows. La documentation Microsoft décrit ce transport avec .NET 8 ou ultérieur sous Windows. [Source Microsoft](https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess-namedpipes?view=aspnetcore-10.0)

Ce choix adapte le principe de tube nommé déjà exercé par WS-AT15 sans promouvoir son protocole expérimental tel quel. Il ajoute un adaptateur de transport, pas une dépendance gRPC dans les contrats métier.

Alternative : HTTPS sur boucle locale, préférable si une répartition des hôtes sur plusieurs machines devient un besoin immédiat. Elle demanderait la gestion des certificats de ce lien. Cette répartition n’est pas décidée pour V1.

Limites du choix proposé :
- transport Windows dans cette composition ; portabilité des couches métier préservée, transport à adapter pour un futur hôte différent ;
- navigateur connecté au Web Host par son protocole web, jamais directement au tube nommé ;
- contrôle d’accès du processus et identité métier distincts : le Core valide la session utilisateur ou service et ses droits courants ; une liste de permissions envoyée par le navigateur ne fait pas autorité ;
- identité du processus client, droits sur les tubes et comptes d’exécution à contractualiser ; ne pas autoriser tous les utilisateurs Windows par défaut ;
- délais, annulation avant admission, taille des messages et nombre de clients bornés ;
- une coupure ne déclenche pas de nouvelle soumission d’action.

Séparer les contrats d’observation, d’opérations et d’administration. OPC UA ne reçoit pas automatiquement l’accès à tous les services administratifs du Core.

## 7. Stockage proposé

**SQLite local**, derrière des interfaces de persistance plateforme. L’hôte web, OPC UA et Fleet accèdent aux données par les services ; ils n’ouvrent pas directement la base du Core.

| Donnée | Autorité et stockage proposés |
|---|---|
| Comptes, rôles, préférences et configuration produit | Services communs ; stockage local versionné |
| Admission, corrélations, audit et état de suivi durable | Core ; stockage permettant une transaction locale cohérente avant effet externe |
| Licence installée | Artefact vérifiable et métadonnées ; service licence commun |
| Clés privées et secrets | Protection dédiée du système, séparée des exports ordinaires ; mécanisme à choisir |
| État physique et données PLC/CNC | Autorités machine ; éventuel cache clairement identifié, jamais vérité fraîche après redémarrage |
| Table outil indépendante de la machine | Non décidée ; ne pas créer une seconde autorité de table par défaut |
| Audit central de parc | Hors décision ; l’audit local ne dépend pas de Fleet |

SQLite en mode journal WAL permet lectures et écriture concurrentes mais conserve un seul écrivain à un instant donné et ne convient pas à une base WAL sur partage réseau. Cela justifie ici l’accès centralisé et les transactions courtes. La durabilité dépend des réglages de synchronisation et du stockage ; le seul choix de SQLite ne la prouve pas. [Documentation SQLite](https://sqlite.org/wal.html)

Pour la première tranche, proposer une même base logique pour journal d’admission et audit afin de ne pas inventer une transaction atomique entre deux bases. Les budgets, la rétention et les interfaces restent séparés par responsabilité. Une transaction de base ne peut pas englober un effet PLC : cette frontière impose un état incertain et une réconciliation.

À qualifier : coupure électrique, disque plein, panne d’écriture, sauvegarde cohérente, restauration, migrations et retour arrière. Aucun budget de rétention ou paramètre de journalisation précis n’est fixé sans mesures. Ne pas promettre un journal infalsifiable grâce à SQLite seul.

## 8. Contrat d’une modification ou commande

Schéma fonctionnel proposé, sans signature de code définitive :

1. Demande avec identifiant d’intention stable, identité de session validable, client, machine cible, outil et champs explicitement visés.
2. Pour l’édition : révision attendue et valeurs sémantiques avec unités contractuelles. Aucun envoi arbitraire de structure PLC complète.
3. Contrôle des permissions courantes, de la licence, de l’audit, de la compatibilité et des préconditions ; contrôle des champs et de la position actuelle de l’outil.
4. Enregistrement durable de l’admission et de sa corrélation avant effet technologique. Les refus sont enregistrés lorsque le stockage le permet.
5. Soumission technologique puis observation ou acquittement selon le contrat de l’opération.
6. Résultat suivi par identifiant ; acceptation technique distincte de l’application observée.
7. Si le client perd la réponse, consultation par la même intention autorisée ; si la situation reste incertaine, aucune relance automatique.

Le même identifiant avec un contenu différent doit être refusé. La durée de conservation des corrélations et le comportement après expiration de cette durée restent à fixer ; aucune garantie universelle d’exécution physique « exactement une fois ».

Pour l’Opérateur : usures seulement, y compris en broche avec permission supplémentaire. Une demande mêlant usure et dimension nominale non autorisée est refusée intégralement. Les conditions PLC/CNC d’application en broche restent à confirmer.

La présence d’un audit indisponible après admission ne doit pas annuler l’opération physique. Le suivi continue avec une dégradation signalée ; la politique de consignation/récupération des résultats manquants doit être définie avant qualification produit.

## 9. Distribution des composants communs

Proposition : paquets NuGet versionnés produits depuis le dépôt plateforme, puis référencés par le pilote. Les nouveaux contrats et composants réutilisables y sont développés et vérifiés ; pas de copie de leur code privé dans le dépôt public du magasin.

Deux usages du même jeu de paquets :
- développement connecté : source de paquets privée autorisée ;
- développement ou compilation isolés : dossier local contenant les versions et dépendances nécessaires. Les sources NuGet locales sont prises en charge par l’outillage. [Documentation Microsoft](https://learn.microsoft.com/en-us/nuget/hosting-packages/local-feeds)

L’application installée reçoit ses binaires et ressources nécessaires ; elle ne contacte pas une source NuGet au démarrage. Le mécanisme d’installation et le mode de publication .NET restent à choisir.

Les versions des contrats, composants, schémas de données et profils sont suivies distinctement. Les versions exactes sont figées dans une composition de livraison. Une incompatibilité de passerelle doit être expliquée avant activation, sans contourner le gate pour faire démarrer la démo.

Le choix de l’hébergement du flux privé, les identifiants d’accès du développement et la chaîne de publication doivent être validés. Ne pas réintroduire implicitement les sources privées inaccessibles qui ont bloqué le démarrage initial du pilote.

## 10. Plan d’intégration par petits jalons

| Jalon | Travail | Preuve attendue |
|---|---|---|
| A. Contrats et composition | Dépendances communes, packages, transport et simulateur ; session cliente validée | Core indépendant, client compatible accepté, client incompatible refusé |
| B. Services durables | Comptes locaux, permissions, vérification de licence temporaire, audit et corrélations | Refus avant effet ; expiration ; panne audit ; reprise |
| C. Première fonction commune | Lecture outil, édition d’usure et PrepareTool sur simulateur | Même décision HMI/OPC UA ; conflit de révision ; suivi sans rejeu |
| D. Fleet et profils | Publication, activation/désactivation de consommateurs | Collecteur absent sans impact local ; pas de commande causée par un profil |
| E. Parcours opérateur | Deux résolutions, tactile, langues/unités, clair/sombre | Usage qualifié sur poste cible ; bascule depuis Avalonia après validation |
| F. Extension V1 puis réel | Contrats restants et raccordement Beckhoff | Matrice métier couverte, preuves PLC/CNC spécifiques |

Les jalons ne sont pas des promesses de délai. Les tests existants de la plateforme sont réutilisés comme non-régression ; les preuves propres au pilote sont ajoutées là où le risque change.

La conformité CRA reste un chantier produit transversal : exigences et responsabilités doivent être instruits dès maintenant ; ni ce document ni la sélection des composants ne démontrent la conformité.

## 11. Décisions groupées soumises à validation

| Décision | Proposition | Conséquence |
|---|---|---|
| Distribution | Paquets plateforme versionnés, source privée/local hors ligne | Frontière de réutilisation explicite ; chaîne de packages à créer |
| Transport | gRPC sur tubes nommés pour les hôtes locaux Windows | Adaptateur commun à développer ; contrôle du processus et du sujet métier |
| Persistance | SQLite local, accès par Core uniquement | Pas de serveur de base à installer ; durabilité et sauvegarde à qualifier |
| Isolation OPC UA | Hôte séparé du Core et du Web Host | Domaine de panne supplémentaire isolé ; cycle de vie à gérer |
| Première tranche | Comptes, licence temporaire, audit et contrats réels autour d’un simulateur | Validation précoce des services communs sans attendre le banc |

Ces décisions peuvent être discutées ensemble. Les prérequis automatisme ne sont pas résolus arbitrairement pour démarrer la simulation.

## 12. Références complémentaires de la plateforme

À la révision bc0819d0774c67e920481a7e0312d12f55a291a8 :
- [HMI Runtime et dépendances](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Hmi.Runtime/Platform.Poc.Hmi.Runtime.csproj)
- [Adaptateur northbound](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Northbound.Runtime/Platform.Poc.Northbound.Runtime.csproj)
- [Passerelle OPC UA](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Gateway.OpcUa/Platform.Poc.Gateway.OpcUa.csproj)
- [Pont expérimental WS-AT15](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/tests/Platform.Poc.WalkingSkeleton.Tests/Scenarios/WS_AT15_HmiProcessLossRecovery/ExperimentalHmiRuntimeBridge.cs)

Les identités, les permissions, les profils et la maintenance sont détaillés dans les documents liés depuis l’index. Aucun résultat d’exécution nouveau n’est revendiqué.
