# Pilote Magasin 8xx — composition, OPC UA et Fleet

Date : 14 septembre 2026. Analyse sans modification de code.

Ce document complète l’analyse d’alignement antérieure. Les décisions utilisateur récapitulées ici actualisent ses questions ouvertes ; les profils et contrats proposés ci-dessous restent à valider. Aucun test machine ou test d’exécution n’a été réalisé pour cette analyse. Les résultats historiques sont ceux consignés dans les sources, pas de nouvelles validations.

## 1. Révisions examinées

- Plateforme `rnsmetsanou/PlateformeWM-Demo`, branche `p6-7-live-commissioning-impl`, commit `bc0819d0774c67e920481a7e0312d12f55a291a8`, vérifié sur GitHub. La branche a avancé depuis la première analyse arrêtée à `3d00106`.
- Pilote `rnsmetsanou/Magasin-Outil-8xx`, branche `hmi/avalonia-magazine-v1`, commit `06ea743ca76368c04f69fd381fd7ea60a597635a`, inchangé lors de la vérification. Le projet Desktop référence Core et Avalonia ; il n’intègre pas encore les composants plateforme.
- Architecture V1.3, règles de composition P6.4, profils Executive V0.3, Mini Fleet P6.6, contrats northbound et implémentations actuelles OPC UA/Fleet.

## 2. Décisions utilisateur à préserver

| Sujet | Décision établie |
|---|---|
| Produit | Application magasin autonome consommant la plateforme ; intégration dans une autre application envisageable ensuite |
| Interface homme-machine (HMI) | Direction web commune sur PC industriel (IPC) et clients autorisés ; hébergement local, sans dépendance à Internet |
| Isolation | Runtime Machine/Application séparé de l’hôte HMI dès V1 |
| Clients | Plusieurs clients souhaités ; actions permises avec les autorisations nécessaires ; tablette comme exemple |
| OPC UA | Exposition des données et actions dès V1 ; connexion à des équipements OPC UA ensuite |
| Fleet | Partie intégrante de l’architecture à reprendre ; pas une extension à ignorer dans le pilote |
| Comptes | Comptes locaux permettant le fonctionnement autonome ; Microsoft est le système de comptes existant chez WM |
| Licences | Mécanisme WM commun à construire ; licences temporaires dès V1 ; vérification locale |
| Expiration | Consultation selon permissions ; nouvelles modifications et commandes bloquées ; opérations déjà admises poursuivies |
| Audit | Durable, politique configurable ; nouvelles modifications et commandes bloquées si l’audit durable est indisponible ; suivi des opérations admises conservé |
| Langues | Français requis ; anglais souhaité en V1 ; allemand si possible |
| Unités | Millimètres et pouces en V1 ; préférences par utilisateur ; conversions extensibles par grandeur physique |
| Visuel | Charte et logo WM, clair/sombre, 1024 × 768 et 1920 × 1080, usage tactile |
| Conformité | Exigence CRA, Cyber Resilience Act ; aucune conformité démontrée par cette analyse |

Blazor Interactive Server et MudBlazor restent des candidats. Le remplacement effectif d’Avalonia dépend d’une qualification. Les rôles détaillés, le stockage, les transports entre processus et le conditionnement des licences ne sont pas décidés ici.

## 3. Ce que dit réellement la plateforme

### 3.1 Les profils existent déjà

Le design Executive V0.3 définit quatre compositions : Local / Air-gapped, Factory Integration, Fleet-enabled et Full Connected. Il précise explicitement qu’un profil décrit une composition fonctionnelle, et ne fixe pas la topologie physique de production.

Cela corrige également ma formulation précédente : un profil ne doit pas se limiter à la répartition des processus. Il faut décrire séparément composition, implantation physique et politique d’accès.

Les dimensions suivantes doivent rester distinctes : configuré, installé, compatible, autorisé par licence, autorisé pour l’identité, disponible et réellement en fonctionnement. Sélectionner un profil n’accorde aucun droit et ne rend pas un composant automatiquement opérationnel.

### 3.2 Une frontière commune vers les consommateurs

`Platform.Poc.Northbound.Contracts` expose trois surfaces : lecture sémantique Machine, services Application et disponibilité produit. « Northbound » désigne ici l’exposition vers les applications et systèmes consommateurs, au-dessus des technologies machine.

Le contrat 1.0 actuel expose connexion, génération de session, outil préparé, outil courant, alarmes et capacités ; les services sont `PrepareTool` et `RefreshOperation`. Il n’expose pas encore le catalogue complet, les racks, les emplacements, les correcteurs, l’édition ou `LoadTool` au travers de cette interface.

La preuve P6.4-A établit dans son périmètre que la passerelle ne référence ni les technologies ni les runtimes concrets, et qu’un composant optionnel incompatible peut être refusé sans arrêter le cœur. Ce n’est pas une preuve générale de transport produit entre processus.

### 3.3 OPC UA est déjà une vraie exposition, mais partielle

Le serveur inspecté publie `PrepareTool` et `GetOperation`. Il construit le contexte d’appel à partir d’une session authentifiée puis utilise la surface Application commune. Il ne fait pas d’appel ADS ou FOCAS direct.

La preuve ED-4B2 consigne un succès local sur FANUC NC Guide : refus sans permission, une soumission admise, complétion établie par observation et conservation de la session après arrêt de la passerelle. Cette preuve ne qualifie pas le magasin Beckhoff 8xx.

Limites produit vérifiées dans le code :

- les comptes sont explicitement des comptes de preuve, pas un service de comptes produit ;
- les corrélations sont en mémoire et rattachées à une incarnation de session ; elles sont supprimées à sa fermeture ;
- `PrepareTool` crée son identifiant d’intention dans la passerelle ; le client ne fournit pas de clé de demande stable ;
- une réponse perdue puis une nouvelle session ne permettent donc pas, par ce seul contrat, une reprise complète et sûre du suivi ;
- le catalogue et les opérations magasin nécessitent des extensions versionnées.

Il faut compléter le contrat de recherche et de déduplication, sans promettre une exécution physique « exactement une fois ». Aucune reconnexion ne doit provoquer un renvoi automatique d’action.

### 3.4 Fleet supervise ; il ne commande pas

Le Mini Fleet actuel est une supervision de parc en lecture seule : une projection par cœur, un exporteur, un collecteur et une vue. Il consomme des instantanés existants et des métadonnées ; il ne déclenche pas de lecture technologique ni de commande machine.

OPC UA n’est pas un prérequis de Fleet. L’application locale ne doit dépendre ni de Fleet ni d’un cloud pour fonctionner.

Le collecteur distingue le dernier état machine reçu, l’âge du rapport et sa compatibilité. Un rapport récent ne rend pas fraîches ses données ; une absence de rapport ne prouve pas une déconnexion du PLC.

Le `FleetHttpHost` inspecté écoute sur la boucle locale et ne configure pas d’authentification dans cet hôte. Les contrôles d’identité déclarée, de séquence, de taille et de compatibilité ne constituent pas une authentification d’un émetteur réseau. Son passage au réseau usine nécessite une industrialisation explicite.

## 4. Profils proposés pour le pilote

Ces profils adaptent ceux de l’Executive ; ils ne sont pas encore approuvés. Ils décrivent la composition souhaitée, avant évaluation de licence, de permissions, de compatibilité et de sécurité.

| Profil | HMI web sur IPC | Web sur réseau usine | Serveur OPC UA | Publication Fleet |
|---|---|---|---|---|
| Local isolé | Oui | Non | Désactivé | Désactivée |
| Intégration usine | Oui | Configurable | Activé | Désactivée |
| Rattaché au parc | Oui | Configurable | Configurable | Activée |
| Intégration complète | Oui | Activé | Activé | Activée |

L’exposition OPC UA fait partie de la V1 livrée même si un profil isolé ne l’active pas. Sa présence dans le produit ne signifie pas qu’un port doit toujours écouter. La même distinction vaut pour la capacité de publication Fleet.

Le profil local de l’Executive indique « Web off ». Dans le pilote web, cela doit signifier « accès web réseau désactivé », jamais « hôte de la HMI locale arrêté ». Cette différence nécessite une adaptation documentée des capacités, pas une copie littérale des boutons de démonstration.

Aucun profil par défaut n’est choisi. Il reste à décider si chaque passerelle est obligatoire au démarrage pour une composition donnée, et quelle dégradation autoriser si elle ne démarre pas. L’absence de Fleet ne doit pas rendre la machine inutilisable.

## 5. Implantation proposée, distincte des profils

| Composant | Implantation proposée | Responsabilité |
|---|---|---|
| Cœur Machine/Application et autorités communes | IPC machine | Connexion Beckhoff, état, admission, suivi d’opérations, politiques communes |
| Hôte web et présentation HMI | Processus séparé sur IPC | Sessions, projections, formulaires et transport client |
| Passerelle OPC UA | Sur IPC ; isolation de processus à décider | Projection du contrat commun et adaptation des demandes |
| Projection et exporteur Fleet | Côté machine ; placement à décider | Publication bornée d’états déjà disponibles |
| Collecteur et vue Fleet | Service de parc ; hôte à valider | Agrégation de plusieurs machines en lecture seule |
| Navigateurs | IPC et autres terminaux autorisés | Affichage et interaction |

Je propose que le pilote fournisse son raccordement à Fleet sans installer un collecteur complet sur chaque machine. Un collecteur local de qualification reste utile. L’emplacement réel du service de parc et sa livraison avec le pilote restent à valider.

Le point de composition Executive possède actuellement un cœur Simulator et orchestre les passerelles de démonstration. Il ne doit pas devenir directement l’hôte produit du magasin. Les composants communs doivent être consommés ou extraits proprement ; les scénarios, identités et sélections Executive restent propres à la démo.

## 6. Contrats et services à compléter en commun

| Surface | Travail nécessaire |
|---|---|
| Lecture magasin | Outils, emplacements, racks, présence, état, correcteurs ; unités sémantiques, révisions et provenance |
| Commandes | Préparer, charger et modifier selon périmètre validé ; contexte authentifié, intention stable, préconditions et résultat structuré |
| Suivi | Reprise autorisée après perte de réponse ou changement de session, sans divulguer toutes les opérations aux autres utilisateurs |
| Disponibilité | Règles partagées avec raisons ; droits, licence, état machine et disponibilité de l’audit réévalués au moment d’admettre |
| Identité | Comptes locaux durables et mapping des identités OPC UA ; identité de service pour systèmes automatisés à définir avec WM |
| Licence | Émission et vérification locales des artefacts, temporalité et récupération ; mêmes décisions via web et OPC UA |
| Audit | Persistance, rétention, budgets et récupération ; l’audit inspecté est actuellement une liste en mémoire |
| Présentation | Langues et préférences par client, mm/pouces ; pas de variation des unités OPC UA au gré de l’utilisateur HMI |
| Composition | Manifeste versionné, diagnostic d’activation, séparation souhaité/effectif ; modification administrative auditée |
| Réseau | Confiance, certificats, authentification et limites pour les interfaces activées ; aucune confiance implicite liée au réseau local |

Les règles communes ne doivent pas être recodées dans une méthode OPC UA ou un composant Blazor. Les publications Fleet ne transportent ni clés de licence ni secrets de connexion. La politique exacte des données publiées reste à préciser.

La conformité CRA doit être traitée au niveau produit et cycle de vie. La réutilisation de la plateforme n’apporte pas à elle seule de preuve de conformité ; le plan produit doit prévoir son analyse dédiée et les preuves associées.

## 7. Plan proposé et critères de sortie

1. **Fixer le contrat produit.** Valider la matrice des profils, l’exposition OPC UA détaillée et le rôle de raccordement Fleet. Répertorier champs éditables, autorités PLC/CNC/application et identités autorisées, y compris les systèmes automatisés.
2. **Consolider les composants plateforme.** Identifier les projets réellement distribuables et leur versionnement. Étendre les contrats communs, compléter comptes/licence/audit et définir le transport entre processus. Aucune duplication de plateforme dans le dépôt pilote.
3. **Livrer une tranche simulée commune.** Une modification et une préparation gouvernées, via HMI web et OPC UA, publiées dans Fleet. Vérifier conflits, refus, audit et résultat sémantique. Distinguer cette preuve de la qualification matérielle.
4. **Exercer les profils.** Passer d’intégration complète à local isolé puis revenir ; vérifier session Machine conservée, aucune commande rejouée, HMI locale disponible et reconstruction depuis l’état courant. Tester aussi un refus d’activation et un échec partiel de transition.
5. **Qualifier les pannes et la reprise.** Couper client, hôte web, passerelle OPC UA et collecteur Fleet séparément ; poursuivre les opérations admises. Distinguer perte de transport, perte de processus et perte du runtime autoritatif. Tester réponse perdue, nouvelle session, concurrence web/OPC UA, expiration de licence et panne d’audit.
6. **Raccorder Beckhoff 8xx et qualifier le poste.** Valider symboles, échelles, protocole et critères physiques ; exercer les deux résolutions, thèmes et saisie tactile. Les acquis sur le banc PoC ne remplacent pas cette étape.

Les étapes 2 à 6 décrivent un plan futur, pas une autorisation implicite de modifier le code pendant cette analyse.

## 8. Décisions restant à prendre

- Valider ou ajuster les quatre profils adaptés et choisir la composition par défaut.
- Confirmer le raccordement Fleet en V1 et l’hôte du collecteur ; aucun contrôle machine depuis Fleet n’est proposé.
- Fixer les données et actions OPC UA de V1 et les règles des comptes de service.
- Choisir les transports internes, les isolations additionnelles et la politique en cas de passerelle obligatoire indisponible.
- Définir la distribution des composants communs et la matrice de compatibilité.

## 9. Sources primaires

Les liens sont figés à la révision analysée.

- [Architecture V1.3](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/architecture/Architecture_V1.3_Consolidation_P4_P5_Plateforme_NET_MultiTechnologies_2026-09-09.md).
- [Profils Executive V0.3](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/p6/P6.9_Executive_Demo_App_Design_V0.3_Deployment_Profiles_2026-09-12.md).
- [Composition P6.4 et limites de preuve](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/p6/P6.4_Deployment_Lifecycle_Northbound_Gateway_Design_2026-09-10.md).
- [Preuve P6.4-A](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/p6/P6.4-A_Northbound_Boundary_Deployment_Composition_Evidence_2026-09-10.md).
- [Contrats communs](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Northbound.Contracts/NorthboundPlatformContracts.cs).
- [Méthodes OPC UA](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Gateway.OpcUa/OpcUaNorthboundMethods.cs).
- [Sessions et comptes OPC UA de preuve](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Gateway.OpcUa/OpcUaCommandProfile.cs).
- [Preuve ED-4B2](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/p6/P6.9_ED-4B2_FANUC_Governed_OPCUA_Action_Gate_Decision_2026-09-14.md).
- [Conception Mini Fleet](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/docs/p6/P6.6_Mini_Fleet_Local_First_Design_2026-09-11.md).
- [Transport Fleet](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.Fleet.Http/FleetHttpHost.cs).
- [Composition Executive](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.ExecutiveDemo.Runtime/Live/ExecutiveDemoDeploymentRuntime.cs).
- [Audit actuel](https://github.com/rnsmetsanou/PlateformeWM-Demo/blob/bc0819d0774c67e920481a7e0312d12f55a291a8/src/Platform.Poc.CrossCutting.Runtime/Audit/AuditTrailRuntime.cs).
