# Analyse d’alignement du pilote Magasin d’outils 8xx avec la plateforme WM

Date : 14 septembre 2026. Statut : **analyse et plan proposés, à valider par Ernest ; aucune décision nouvelle adoptée**.

Aucun code, paramètre ou document des deux dépôts n’a été modifié pour cette analyse. Aucun test machine n’a été exécuté. Les constats de code ne constituent pas une qualification produit.

## 1. Conclusion et compréhension du besoin

Le magasin d’outils doit devenir un pilote de la plateforme à part entière, avec le même niveau d’attention architecturale que l’Executive PoC (preuve de concept exécutive). Son interface opérateur et son raccordement Beckhoff doivent exercer les services communs, puis pouvoir s’intégrer dans l’application cible avec une réécriture limitée.

La direction proposée est de faire du pilote un **consommateur des composants communs de plateforme**, accompagné d’un module métier magasin et d’un renderer remplaçable. Il ne suffit pas de reproduire les mêmes noms de couches dans une seconde implémentation autonome.

Aujourd’hui, cette intégration n’existe pas encore. Le pilote fournit une interface Avalonia fonctionnelle sur un simulateur local, mais son contrat `IMagazineService` ne représente pas les frontières complètes de la plateforme. La plateforme possède des mécanismes réutilisables, mais plusieurs services transversaux restent des réalisations de PoC à compléter et à qualifier.

Avalonia n’est pas, sur les éléments vérifiés, un obstacle intrinsèque à l’autorisation, au licensing, à la traduction ou à l’audit : ces services doivent vivre hors du renderer. Son adéquation au matériel, au support et aux composants tactiles doit néanmoins être réévaluée avant de figer le choix.

## 2. Références et niveau de certitude

### Sources inspectées

- Pilote : `rnsmetsanou/Magasin-Outil-8xx`, branche `hmi/avalonia-magazine-v1`, commit `06ea743ca76368c04f69fd381fd7ea60a597635a`.
- Plateforme : `rnsmetsanou/PlateformeWM-Demo`, branche `p6-7-live-commissioning-impl`, commit `3d00106ac271f6541ad12e697a0e73ad90a30952`.
- Architecture V1.3, héritant des dispositions V1.2 non amendées ; décisions d’architecture ADR V0.2 ; cahier des charges V0.2 héritant de V0.1.
- Documents ED-5 et ED-6, code des services transversaux, frontières HMI/Application, opérations gouvernées, adaptateur Beckhoff et renderer Avalonia.
- Cahier des charges du magasin, structures XML/TwinCAT et contexte Executive fourni.
- Documentation officielle Avalonia et Microsoft, consultée pour les dépendances commerciales et plateformes supportées.

Les liens de référence figurent en fin de document. L’analyse est arrêtée à ces révisions ; elle ne suppose pas que la branche restera immobile pendant l’implémentation.

### Ce qui est déjà demandé ou établi

- .NET 10 et respect de la plateforme existante.
- Intégration des droits utilisateurs, licences, traductions, audit et autres services transversaux.
- Fonctionnement local selon les principes de la plateforme.
- Interface moderne, tactile, thèmes clair/sombre, logo et charte WM.
- Formats 1024 × 768 et 1920 × 1080 ; validation de l’échelle Windows nécessaire.
- Avalonia est le choix actuel, explicitement réexaminable ; Blazor local est une possibilité, pas une migration décidée.
- Les opérations ne doivent pas déduire leur succès physique d’une écriture ADS réussie.
- Cette étape porte sur l’analyse et le plan, sans modification de code.

### Ce qui n’est pas décidé

Ni le fournisseur d’identité, ni les rôles et permissions du produit, ni le profil de licence, ni le stockage durable, ni la topologie des processus, ni le protocole entre HMI et runtime, ni le mode de distribution des composants communs ne sont considérés comme approuvés.

« Tous les services transversaux » est pris comme une demande d’intégration complète à inventorier. Cela ne vaut pas décision d’activer automatiquement Fleet, OPC UA, un annuaire externe ou chaque option d’infrastructure. Leur applicabilité au pilote doit être explicitée et validée, sans retrait silencieux du périmètre.

## 3. Diagnostic du pilote actuel

| Sujet | Constat vérifié | Conséquence |
|---|---|---|
| Dépendances | Desktop référence uniquement MagasinOutil.Core ; aucune référence plateforme | Alignement de principes partiel, intégration réelle absente |
| Composition | App construit directement SimulatedMagazine puis MainWindow | Durée de vie du service attachée à l’application graphique |
| Modèle | Core rassemble données, service et simulateur | Frontières Machine/Application/Technology non matérialisées |
| Opérations | Apply/Transfer synchrones avec révisions locales | Pas d’IntentId/OperationId, admission commune ou résultat physique observé |
| Observations | Liste instantanée de données fictives | Pas de qualité/fraîcheur/provenance/génération de session |
| Présence | Calculée à partir de la position simulée | Ne remplace pas le signal PLC bToolPresence |
| Gouvernance | Aucune identité, permission, licence ou feature resolution plateforme | Boutons disponibles selon règles locales uniquement |
| Localisation | Libellés français et culture fr-CH dans la vue ; messages français dans Core | Extraction des textes, codes métier et saisies nécessaire |
| Persistance/audit | Données volatiles, pas de journal d’audit produit | Redémarrage et traçabilité non couverts |
| Présentation | MainWindow porte sélection, brouillon, saisie, confirmation, couleurs et appels service | Remplacer Avalonia aujourd’hui exigerait de réécrire une part de la présentation fonctionnelle |
| Vérification | Contrôles de simulation et de placement automatisés | Acquis utiles, mais pas une preuve de conformité complète à la plateforme |

Le pilote ne doit donc pas être présenté comme respectant déjà toutes les couches. Les itérations graphiques ont établi un parcours opérateur ; elles ne constituent pas le socle d’intégration final.

## 4. Ce que la plateforme apporte réellement

| Service ou frontière | Existant inspecté | Limite à traiter pour le pilote |
|---|---|---|
| Identité et autorisation | SubjectContext, PermissionId, IAuthorizationService, PermissionAuthorizationService ; contrôle avant opérations | Le choix de profil Executive crée un sujet authentifié de démonstration ; ce n’est pas un système complet de connexion et gestion de comptes |
| Droits commerciaux | EntitlementSnapshot, provider local, EntitledToolHandlingOperations | Le provider remplace un snapshot ; il ne valide pas lui-même un fichier de licence signé ni son rattachement à une installation |
| Disponibilité produit | FeatureResolver, raisons structurées et présentation Enabled/Disabled/Hidden | Catalogue et règles du magasin à définir, sans doubler les règles de l’admission |
| Langues et unités | PresentationContextRuntime, ILocalizationRuntime, PocLocalizationRuntime, PhysicalValuePresentationProjector | Catalogue inspecté très limité ; projection physique centrée sur l’axe X, pas encore bibliothèque complète des paramètres d’outil |
| Temps | Horloges monotone/civile et outils de test | Autorité temporelle d’une licence expirante et comportement produit à définir |
| Opérations | ToolHandlingApplicationRuntime, registre d’intentions, contrôle contexte, PrepareTool/LoadTool, observation et réconciliation | Pas de contrat complet de création/édition du magasin. Les chemins de compatibilité sans contexte doivent être exclus de la composition protégée du pilote |
| HMI | PlatformHmiRuntime, navigation, projections sémantiques | Vue magasin, brouillons et formulaires à ajouter à cette frontière |
| Renderer Avalonia | Consomme les projections partagées | Implémentation inspectée essentiellement textuelle dans un TextBlock ; pas une bibliothèque opérateur prête à reprendre telle quelle |
| Audit | AuditTrailRuntime, IAuditTrailReader, événements typés ; intégration Executive et page Activity | En mémoire, non durable. Liste sans borne dans le code inspecté. Capture liée aux runtimes Executive, pas encore instrumentation générale de tous les services autoritatifs |
| Diagnostics | Contrats et corrélations structurées | Politique de stockage, rotation, filtrage et support du produit à définir |
| Compatibilité | Manifests, CompatibilityResolver et activation gates | Versions et composition livrées au magasin à contractualiser |
| Beckhoff | Canal ADS, polling/notifications, EventLogger, soumission et contrôle de session | Protocole PoCFixture, catalogue de symboles de banc, outils de 1 à 99 dans l’exécution ToolHandling inspectée : pas le profil 8xx |

Deux distinctions conditionnent le plan :

1. Réutiliser le contrôle d’entitlements ne suffit pas à livrer un gestionnaire de licences produit.
2. Réutiliser SubjectContext ne suffit pas à livrer une authentification utilisateur.

L’Executive démontre les frontières et les décisions. Le pilote doit permettre de transformer les services manquants en composants communs, sans importer les sélecteurs de démonstration comme autorités de production.

### État documentaire ED-5 / ED-6

Le document de gate ED-5 consulté conserve ED-5 global « REOPENED » et ED-5B « IMPLEMENTED / LOCAL RUN + VISUAL CHECK REQUIRED ». ED-6 a depuis été implémenté et son test étendu aux actions Machine Experience, mais sa readiness réclame encore une exécution locale et un contrôle visuel.

Il existe un décalage entre le design ED-6 initial et le code : le design envisage des contrats bas niveau et une rétention bornée ; le code inspecté place les types dans CrossCutting.Runtime et conserve une liste en mémoire sans borne. Ce point doit être consolidé avant réutilisation industrielle. Aucune validation ED-6 supplémentaire n’est présumée ici.

## 5. Répartition architecturale proposée

Les noms suivants sont des responsabilités logiques ; ils ne fixent pas encore les noms ni le nombre de projets .csproj.

| Responsabilité | Contenu cible | Ne doit pas posséder |
|---|---|---|
| Technology Beckhoff | ADS, symboles, sérialisation PLC, échelles natives, protocole de commande, contraintes SDK | Droits utilisateurs, textes d’écran, règles commerciales |
| Machine | Observations sémantiques du magasin, état outil, qualité/fraîcheur, ressources et opérations, preuve de résultat | Objets Avalonia/Blazor, saisies localisées |
| Application magasin | Cas d’usage consulter/créer/modifier/préparer/charger ; contexte de demande ; coordination et conflits | Symboles PLC, contournement de l’admission |
| HMI Runtime | Projections magasin/fiche, sélection, navigation, brouillons, validation de saisie, disponibilité expliquée, langue/unités | Vérité Machine, autorité de licence ou de permission |
| Renderer | Contrôles, mise en page, focus, clavier, accessibilité, thèmes | Opérations métier, validation de licence, persistance autoritative |
| Services transversaux | Identité, autorisation, licence, features, traduction, unités, temps, audit, diagnostics, configuration, persistance, compatibilité | Une seconde logique machine ou une dépendance à un framework graphique |
| Host et composition | Assemblage, démarrage/arrêt, secrets, identité d’installation, stockage concret, services du système | Règles métier dépendant du renderer |

Parcours d’une demande protégée : saisie HMI → intention et contexte client authentifié → application → contrôles de droits commerciaux et d’autorisation → admission et ressources Machine → mécanisme Technology → observations → résultat sémantique. L’ordre concret des contrôles suit la composition de plateforme approuvée ; le point essentiel est leur exécution avant tout effet.

L’audit enregistre les décisions et résultats de leurs propriétaires. La HMI lit la projection de cet audit. Elle ne fabrique ni une autorisation, ni un événement de succès à partir du clic utilisateur.

### Ce qui serait conservé lors d’un remplacement Avalonia → Blazor

Contrats Machine et Application, adaptateurs, opérations, gouvernance, stockage, audit, catalogue de features, traductions et sémantique des unités doivent rester partagés. Le modèle de présentation et les brouillons indépendants du renderer doivent également pouvoir être repris.

La mise en page, les contrôles, le focus, le clavier, les bindings et certaines intégrations au système seront réimplémentés. La promesse réaliste est une migration circonscrite à ces éléments ; une conversion automatique sans travail n’est pas garantie.

## 6. Intégration de tous les services transversaux

| Domaine | Intégration attendue | Décision encore requise |
|---|---|---|
| Authentification | Connexion, déconnexion, verrouillage, expiration ; subject lié à chaque requête | Comptes locaux, identité Windows/annuaire, badge ou autre ; dispositif existant à reprendre ? |
| Autorisation | Permissions sémantiques pour commandes et lectures sensibles ; rôles configurables ; motifs de refus | Matrice actions × permissions × rôles ; administration et récupération |
| Licences WM | Validation locale d’artefacts vérifiables, publication cohérente des entitlements ; installation/remplacement audités | Licence perpétuelle, temporaire ou autre ; émetteur, périmètre, binding, renouvellement, changement d’IPC |
| Feature resolution | Croisement support technique, installation/configuration, licence, droits et état Machine | Catalogue produit, visibilité des fonctions indisponibles |
| Traduction | Catalogues versionnés, codes et paramètres structurés, fallback, changement à chaud | Langues livrées, fallback, terminologie validée, gestion des traductions client |
| Culture/unités | Formatage et conversion par client ; valeur sémantique avant admission ; brouillons sans réinterprétation silencieuse | Unités proposées, précision, arrondi, règles des champs et des correcteurs |
| Temps | Durées monotones ; instants non ambigus pour audit ; provenance des timestamps | Fuseau installation, synchronisation, politique de licence temporelle |
| Audit | Demandes/refus/actions, modifications avant/après selon politique, droits/licences/configuration ; corrélations | Durabilité, rétention, export, droit de lecture, protection, disque plein et panne d’écriture |
| Diagnostics | Connexion, fraîcheur, opérations, ressources, dégradation ; collecte pour support sans secrets | Rétention, accès support, export, budget disque |
| Configuration | Séparation technologie/machine/application/HMI ; validation de schéma et compatibilité | Source de la configuration, modifications autorisées, procédure d’application et migrations |
| Persistance | Séparer configuration, données applicatives, historiques et secrets ; reprise explicitement définie | Moteur de stockage, sauvegarde/restauration, propriété de chaque donnée entre PLC/CNC/application |
| Secrets et confiance | Credentials/certificats hors configuration exportable ; protection des échanges | Secure ADS, routage, certificats, provisioning et contraintes informatiques réelles |
| Host et identité installation | Démarrage/arrêt, accès fichiers système, clavier, identité stable, maintenance | Droits d’installation, compte d’exécution, remplacement matériel, mode kiosque |
| Déploiement et isolation | HMI remplaçable ; état des opérations hors vue ; connectivité HMI/runtime distincte du lien machine | Processus unique ou runtime séparé ; intégration dans l’application existante |
| Compatibilité et mises à jour | Contrats/schémas/catalogues versionnés ; activation validée ; compatibilité des données au retour de version | Distribution, cadence, support des versions, installation et rollback |
| Ressources et dégradation | Files bornées, diagnostics de saturation, politique de client lent | Budgets à mesurer et comportement de chaque service indisponible |
| Sécurité et gouvernance livraison | Inventaire des dépendances et licences, contrôle des accès et secrets, traçabilité des versions | Exigences produit/entreprise applicables et critères d’acceptation ; aucune conformité réglementaire automatique revendiquée |
| Northbound/Fleet | Consommateurs optionnels des surfaces approuvées, sans autorité machine | Nécessaires au pilote initial, seulement compatibles, ou différés explicitement ? |

La panne d’identité ou de licence ne doit pas devenir « tout autoriser ». La perte d’un droit, la fermeture de la HMI ou le retrait d’une licence ne doit pas annuler implicitement une opération physique déjà admise. La réponse à une panne d’audit doit être décidée par classe d’action : la simple politique observatrice du PoC n’est pas présumée suffisante pour le produit.

Les rôles Operator/Maintenance/Administrator et leur association FR/mm ou EN/inch dans la démo sont des scénarios de preuve, pas une configuration approuvée du magasin. Langue, culture, unités, permissions et licence restent des dimensions distinctes.

## 7. Écart métier et Beckhoff à fermer

Le besoin magasin dépasse PrepareTool/LoadTool : lecture, création et modification des données, correcteurs M/T, outils en broche, configuration des emplacements. Une extension du domaine commun est nécessaire ; il faut décider quels concepts sont génériques et lesquels appartiennent au module 8xx.

Le cahier des charges décrit les données de l’outil en broche échangées PLC/CNC par Profinet. Il ne permet pas de conclure que la HMI doit envoyer directement des commandes FANUC, ni qu’un seul PLC est maître de toutes les données.

Avant écriture live, produire une matrice par champ et par action :

- propriétaire autoritatif et autres écrivains possibles ;
- symbole, type, encodage, unité, échelle, bornes et résolution ;
- droit de lecture/écriture et conditions machine ;
- édition ciblée ou transaction, détection de modification concurrente ;
- acquittement et preuve de prise en compte ;
- politique en cas de rupture de communication ou résultat inconnu ;
- persistance au redémarrage et sauvegarde ;
- événement d’audit associé.

Les structures seules ne valident pas les facteurs d’échelle des DINT, les codes State/Size/flags, ni les transactions. Une relecture égale à une valeur demandée ne prouve pas automatiquement quel acteur l’a écrite si plusieurs écrivains existent. Le protocole réel doit fournir les garanties nécessaires ou la limite doit être rendue explicite.

Ne pas transposer les règles du simulateur : présence déduite de la position, retour immédiat du précédent outil et unicité instantanée de certaines positions sont des simplifications actuelles. La plateforme précise notamment que LoadTool peut être terminé sur CurrentTool frais, sans imposer la remise à zéro simultanée de PreparedTool.

L’exigence Secure ADS figure dans le cahier des charges. Le canal et la configuration inspectés ne démontrent pas sa mise en service sur l’installation 8xx. Réemploi du transport et validation de la sécurité sont deux travaux distincts.

## 8. Avalonia, composants payants et alternative Blazor

### Constat

Le socle Avalonia reste sous licence MIT, utilisable commercialement sans licence payante du framework. L’éditeur distingue ce socle de ses outils et composants commerciaux. Les services métier de droits, licence WM, traduction et audit n’exigent donc pas intrinsèquement un abonnement Avalonia. [Source officielle Avalonia](https://avaloniaui.net/blog/building-a-sustainable-future-for-avalonia).

En revanche, le clavier virtuel officiel est présenté comme composant Pro/Enterprise. Il ne faut pas confondre le pavé numérique élémentaire du pilote avec un clavier complet multilingue. [Clavier officiel](https://avaloniaui.net/on-screen-keyboard/).

Le coût à examiner comprend les composants, l’outillage de développement, le support, la maintenance interne et le déploiement. Aucune éligibilité commerciale de WM à une offre Community n’est présumée. La licence commerciale de WM pour son produit est indépendante de celle des composants graphiques achetés.

### Point matériel à vérifier avant choix final

Le cahier des charges indique un FIP1000 sous Windows 10 IoT Enterprise LTSC 2019. L’édition réellement installée et son build doivent être confirmés.

La documentation Avalonia actuelle classe Windows 10 22H2 en Tier 2 et les builds antérieurs en Tier 3, avec support commercial uniquement. Cela ne prouve pas que l’application ne fonctionnera pas et n’impose pas l’achat d’une licence d’exécution ; cela limite la promesse de support communautaire sur cette cible. [Matrice Avalonia](https://docs.avaloniaui.net/docs/supported-platforms).

La matrice .NET 10 inclut Windows 10 1809 Enterprise, sous les conditions de support système correspondantes. La politique .NET ne remplace donc pas celle du renderer. Le cycle de vie officiel IoT LTSC 2019 va jusqu’en janvier 2029 pour le support étendu. [Matrice .NET](https://raw.githubusercontent.com/dotnet/core/refs/heads/main/release-notes/10.0/supported-os.md), [cycle Microsoft](https://learn.microsoft.com/en-us/lifecycle/products/windows-10-iot-enterprise-ltsc-2019).

### Évaluation proposée, sans changement de renderer décidé

Comparer un même parcours opérateur : connexion, disponibilité expliquée, modification d’outil, traduction/unités, audit, reconnexion, saisie tactile aux deux tailles.

| Critère | Avalonia | Blazor local |
|---|---|---|
| Métier et gouvernance | Composants .NET partagés hors vue | Mêmes composants côté autoritatif |
| Capital actuel | Écran magasin existant à refactorer | Executive existant, mais écran magasin à produire |
| Contrôles et clavier | Vérifier licence, maintenance et alternatives pour chaque contrôle | Même inventaire nécessaire ; navigateur/clavier tactile à qualifier |
| Exploitation locale | Host natif à qualifier sur le FIP1000 | Hôte web local et navigateur/version à qualifier |
| Isolation | Dépend de la composition choisie | Dépend aussi du mode d’hébergement ; Blazor ne garantit pas seul l’isolation du runtime |
| Remplacement futur | Possible si frontière sémantique respectée | Même condition |
| Décision | À conserver comme candidat ; pas de validation produit à ce stade | Alternative à évaluer, sans supposer qu’elle élimine tous les coûts ou contraintes |

Blazor côté serveur conserve une liaison entre navigateur et serveur ; local peut signifier serveur sur le même IPC, sans cloud. Mode Server, WebAssembly ou Hybrid ne doit pas être choisi implicitement. [Hébergement Blazor côté serveur](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server/?view=aspnetcore-10.0).

## 9. Comment partager la plateforme

Constat d’accès : le dépôt pilote est public ; le dépôt plateforme est privé. Aucun code privé n’a été copié pour cette analyse. La stratégie de distribution et la visibilité doivent être validées avant extraction ou référence de composants privés.

| Option proposée | Intérêt | Coût/contrainte |
|---|---|---|
| Packages internes versionnés issus de la plateforme | Dépendances explicites, adoption contrôlée, indépendance des livraisons | Extraction des surfaces stables, pipeline de publication, authentification des feeds, compatibilité |
| Composition dans un espace source privé commun | Refactorings coordonnés et références directes au début | Couplage des sources/livraisons et organisation à décider |
| Copie autonome des services dans le pilote | Démarrage immédiat apparent | Divergence durable ; proposition déconseillée pour l’objectif exprimé |

Ma préférence proposée est un socle commun versionné consommé par les deux applications, avec une étape de maturation si le packaging n’est pas prêt. Ce n’est pas une décision adoptée. Aucun choix de registre NuGet, sous-module Git ou dépôt supplémentaire n’est effectué ici.

Le correctif NuGet actuel du pilote limite explicitement les sources aux packages publics. Si des packages privés sont retenus, les sources et l’authentification devront être configurées intentionnellement, sans réintroduire le blocage de restauration rencontré ni désactiver l’audit de dépendances.

## 10. Plan d’exécution proposé

Les phases sont des lots avec critères de sortie, sans engagement de délai tant que périmètre, équipe, accès matériel et choix structurants ne sont pas validés. Les services transversaux font partie du parcours de bout en bout ; ils ne sont pas reportés collectivement après le connecteur.

| Lot | Travail | Livrable et condition de sortie |
|---|---|---|
| A — Cadrage approuvé | Définir l’application d’accueil, le niveau attendu du pilote, les fonctionnalités initiales, l’OS réel, les politiques transversales et le mode de partage | Registre de décisions approuvé ; matrice du périmètre ; aucun choix critique implicite |
| B — Baseline commune | Stabiliser la révision de référence, compléter la traçabilité architecture/exigences/code, distinguer contrats et compositions Executive | Carte des dépendances, composants réutilisés/adaptés/manquants, stratégie de compatibilité et distribution validée |
| C — Frontières du pilote | Séparer composition, Machine, Application magasin, HMI Runtime et renderer ; définir observations et contrats d’édition | Simulation utilisant le même chemin gouverné que le futur live ; tests de dépendances ; interface préservée fonctionnellement |
| D — Services transversaux opérationnels | Implémenter/intégrer identité, droits, licence, features, traduction, unités, temps, audit, diagnostics, stockage/secrets/configuration selon les décisions | Scénario complet avec vraie identité et licence vérifiable du profil choisi ; audit durable selon politique ; aucune autorité dans la vue |
| E — Qualification renderer/host | Tester Avalonia et, si nécessaire, un parcours Blazor équivalent ; budgets composants/support ; tactile, thèmes, texte long, installation hors ligne | Choix renderer et topologie motivé par résultats ; pas de dépendance commerciale critique découverte après gel |
| F — Contrats et lecture Beckhoff 8xx | Valider dictionnaire PLC, sécurité, configuration de magasin et ownership ; raccorder les observations | Lecture réelle qualifiée avec provenance, qualité/fraîcheur, absence d’effet à l’ouverture de la HMI |
| G — Écriture et opérations réelles | Édition ciblée, création selon périmètre, puis préparation/chargement après validation de leur protocole réel | Droits/licence/admission/audit actifs ; preuve de résultat, gestion des conflits et perte de liaison sans replay |
| H — Qualification du pilote | Reprise, perte HMI, panne des services, mises à jour, performances et tests sur IPC | Dossier de preuve, limitations, installation et exploitation documentées ; retour des améliorations communes vers l’Executive |

L’étude matériel/composants du lot E commence dès A/B pour éviter d’investir dans un renderer incompatible. Son gate fonctionnel utilise les services et parcours des lots C/D. La collecte de contrats PLC du lot F peut aussi commencer tôt ; aucune écriture live ne précède la validation de ses règles.

### Premier parcours de référence proposé

Un utilisateur authentifié ouvre une fiche issue d’une observation ; la HMI explique la disponibilité ; il prépare une modification ; les valeurs sont converties et validées ; la demande traverse droits/licence/admission ; le résultat n’est affiché comme confirmé que selon la preuve définie ; l’audit conserve acteur, contexte, valeurs pertinentes et corrélations. Le changement de langue ou la recréation de la vue ne redéclenche aucune écriture.

Ce parcours doit d’abord être prouvé avec le simulateur derrière les mêmes contrats. Son champ initial exact reste à valider après la matrice des données ; nom/longueur/usure ne deviennent pas par défaut le périmètre final du produit.

## 11. Critères d’acceptation à proposer à la validation

1. Renderer et application métier sans référence ADS/FOCAS ; contrats sémantiques sans types graphiques.
2. Appel direct d’une action sans permission ou sans entitlement : refus et zéro soumission Technology, même en contournant la vue.
3. Fermeture/déconnexion utilisateur : pas d’abandon implicite d’une opération admise ; auteur et corrélations conservés.
4. Changement de licence : aucun changement artificiel de capacité ou de vérité Machine ; politique des nouvelles actions et de récupération respectée.
5. Changement de langue/culture/unités : aucune mutation Machine ; pas de réinterprétation du brouillon ; textes absents diagnostiqués.
6. Données anciennes ou de session précédente : aucune fausse fraîcheur ni preuve de complétion.
7. Écriture acceptée puis perte de liaison : résultat incertain/inconnu correctement exposé, sans replay aveugle.
8. Audit : événements structurés et corrélés ; pas de secrets ; reprise/rétention/panne conformes à la politique approuvée.
9. Deux demandes concurrentes et données modifiées par un autre acteur : conflit explicite, pas d’écrasement silencieux.
10. Reconstruction HMI sans nouvelle commande ni reconnexion Machine ; perte de processus testée si le profil isolé est retenu.
11. Configuration, catalogue ou données persistées incompatibles : diagnostic explicite ; aucune remise à zéro silencieuse.
12. Installation, redémarrage et utilisation sans Internet conformes au profil retenu.
13. Contrôles tactiles, texte long, clavier, clair/sombre, deux résolutions et échelles réelles : parcours utilisables.
14. Budgets mémoire/disque/files bornés ; l’audit et les consommateurs lents ne créent pas de croissance illimitée.
15. Parcours équivalent via un consommateur indépendant du renderer, et preuve Blazor limitée si retenue pour démontrer le remplacement.
16. Chaque résultat distingue simulation, test hors ligne, banc réel et support produit.

## 12. Décisions à prendre avec Ernest

| ID | Question ouverte | Effet sur le plan |
|---|---|---|
| D01 | Quelle est précisément l’application d’accueil : oCNC cité au cahier, autre HMI existante, Executive Demo ou produit autonome utilisant la plateforme ? Intégration de module, écran embarqué ou application séparée ? | Frontière de composition et livraison |
| D02 | Quel est le premier niveau de livraison : démonstrateur intégré ou mise en service sur une machine, et avec quel périmètre fonctionnel obligatoire ? | Gates, dépendances et estimation |
| D03 | Le Windows IoT LTSC 2019 du cahier est-il toujours exact ? Build, architecture CPU, échelle écran, possibilité de mise à jour et contraintes d’installation ? | Renderer, runtime, clavier, support |
| D04 | Quel système d’identités existe déjà et faut-il le reprendre ? Quels comptes, rôles, actions sensibles et règles de verrouillage ? | Identité/autorisation |
| D05 | Quel profil et périmètre de licence WM veut-on livrer ? Quel mécanisme existe déjà ? Qui émet, installe, renouvelle ou remplace la licence ? | Licence vérifiable et identité installation |
| D06 | Quelles langues, cultures et unités sont exigées dès la première livraison ? | Catalogues, saisies et recette |
| D07 | Quelles exigences d’audit durable : rétention, export, droit de consultation, protection et comportement si l’enregistrement est impossible ? | Stockage et admission |
| D08 | Quels services/configurations/stockages existants de WM doivent être intégrés plutôt que recréés ? | Réutilisation et migrations |
| D09 | Quel mode de partage des composants et quelle visibilité des dépôts sont autorisés ? | Packages privés, accès et livraison |
| D10 | Le runtime doit-il survivre dès la première livraison à un crash du processus HMI ? Y aura-t-il plusieurs clients simultanés ? | Profil compact ou isolé et frontière de confiance |
| D11 | Quels paramètres et commandes PLC sont réellement autorisés et acquittés, et par quel contrôleur ? Qui valide la matrice avec l’équipe automatisme ? | Étendue du connecteur et écritures |
| D12 | Une dépense ciblée pour composants/support est-elle acceptable ? Quels critères feraient préférer Blazor ? | Comparatif renderer sans supposer budget nul ou ouvert |
| D13 | OPC UA, Fleet et accès distant doivent-ils être activés dans le pilote initial ou seulement rendus compatibles ? | Périmètre d’intégration externe |

Les invariants déjà acceptés de la plateforme ne sont pas remis au vote. Les questions ci-dessus concernent leur application au produit et les choix restés ouverts. Le plan ne suppose aucune réponse implicite.

## 13. Références traçables

### Dépôts et code

- [Pilote, révision inspectée](https://github.com/rnsmetsanou/Magasin-Outil-8xx/tree/06ea743ca76368c04f69fd381fd7ea60a597635a).
- [Plateforme, révision inspectée — accès privé requis](https://github.com/rnsmetsanou/PlateformeWM-Demo/tree/3d00106ac271f6541ad12e697a0e73ad90a30952).
- [Architecture V1.3](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/docs/architecture/Architecture_V1.3_Consolidation_P4_P5_Plateforme_NET_MultiTechnologies_2026-09-09.md).
- [Architecture V1.2 héritée](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/docs/architecture/Architecture_V1.2_Plateforme_NET_MultiTechnologies_2026-08-28.md).
- [Exigences transversales V0.1 héritées](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/docs/requirements/Cahier_des_Charges_V0.1_Plateforme_NET_MultiTechnologies_2026-08-28.md).
- [Contrôle d’autorisation](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/src/Platform.Poc.CrossCutting.Runtime/Identity/PermissionAuthorizationService.cs).
- [Provider d’entitlements](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/src/Platform.Poc.CrossCutting.Runtime/Licensing/LocalEntitlementSnapshotProvider.cs).
- [Contexte de présentation et localisation](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/src/Platform.Poc.Hmi.Runtime/Presentation/PresentationContextRuntime.cs).
- [Audit runtime](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/src/Platform.Poc.CrossCutting.Runtime/Audit/AuditTrailRuntime.cs).
- [Readiness ED-6](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/docs/p6/P6.9_ED-6_Audit_Activity_Readiness_2026-09-14.md).
- [Gate ED-5](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/docs/p6/P6.9_ED-5_Gate_Decision_2026-09-14.md).
- [Exécution Beckhoff sur fixture](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/3d00106ac271f6541ad12e697a0e73ad90a30952/src/Platform.Poc.Technology.Beckhoff/ToolHandling/BeckhoffToolHandlingExecution.cs).

Les pièces jointes du cahier des charges et des structures PLC complètent ces sources. Ce document ne remplace ni les exigences normatives ni leur validation métier ; il prépare les décisions et la traçabilité du pilote.
