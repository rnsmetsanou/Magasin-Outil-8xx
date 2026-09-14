# Analyse de faisabilité du connecteur Beckhoff et de la gestion du magasin d’outils

Date : 11 septembre 2026

Statut : analyse et recommandations proposées. Ce document ne modifie pas la baseline de la plateforme et ne constitue pas une validation sur machine.

## 1. Avis et décision proposée

Le projet est réalisable à partir des principes et des composants réutilisables de la plateforme multi-technologie .NET. Il est pertinent de démarrer une application métier autonome de gestion des données d’outils, reliée au magasin Beckhoff, sans attendre la fin de la démonstration globale de la plateforme.

La cible de migration est de conserver l’interface et les règles métier, puis de remplacer l’implémentation des services machine derrière des contrats stables. La quantité de code directement réutilisable reste à vérifier dans le dépôt : cette analyse porte sur les documents transmis, sans inspection du code actuel ni exécution sur le matériel cible.

Les principaux risques sont la synchronisation Beckhoff–FANUC, les écritures concurrentes et la définition incomplète du contrat automate. Les structures fournies décrivent les données, mais ne prouvent ni leur exposition effective ni leur protocole de modification.

## 2. Sources et différences documentaires

| Référence | Source | Contenu et portée |
| --- | --- | --- |
| S1 | Architecture.pptx, 7 diapositives, date affichée 29.06.2026 | Échanges entre contrôleurs et quatre cas d’emploi, dont le quatrième est seulement énoncé |
| S2 | Cahier des charges Gestion table outil.docx | Révision 1.1 du 17.08.2026, ajout de la section 7 sur la configuration du magasin |
| S3 | Cahier des charges Gestion table outil.pdf, 6 pages | Sections 1 à 6 ; absence de la section 7 et de la table de révision présentes dans S2 |
| S4 | Sept fichiers ST_*.xml fournis | Déclarations de types exportées en XML PLCopen ; configurations d’instances vides |
| S5 | Structures.txt | Les sept déclarations présentes dans S4 et une déclaration supplémentaire ST_HMI_Manipulator_Breakage |
| S6 | Contexte_Continuite_P1_P6_5_Northbound_OPCUA_2026-09-11(2).md | Acquis et limites de la plateforme, baseline Architecture V1.3 et décisions jusqu’à ADR-035 |

S2 doit servir de source de travail pour les détails ajoutés, sous réserve de confirmation de la version contractuelle. Le planning de consultation mentionne le 21 août et le 16 juillet ; il ne fournit pas une échéance actuelle de mise en service.

Les déclarations textuelles des sept XML correspondent à celles de Structures.txt après normalisation des espaces. Cela ne prouve pas leur identité avec le programme chargé dans l’automate.

Le nom HMI_Magasin_Settings.xml cité en section 7 semble désigner ST_HMI_Magazine_Settings.xml, mais cette correspondance reste à confirmer.

## 3. Périmètre réel et répartition des responsabilités

S2 décrit un ordinateur industriel FANUC FIP 1000 de 24 pouces, Windows 10 IoT Enterprise LTSC 2019, 16 Go de mémoire et 120 Go de disque. Le magasin Beckhoff dispose de 137 places physiques et de deux axes. La zone d’usinage comporte une broche de fraisage et une position de changement d’outil, pilotées par une commande numérique FANUC F31iB5+.

| Partie | Fonctions décrites | Conséquence proposée |
| --- | --- | --- |
| Nouvelle interface homme-machine, ou HMI | Affichage, création et modification des données d’outils du magasin ; affichage et modification en broche | Cœur de la première application |
| HMI BECKi dans S1 | Table d’outils, outil en broche, outil en préparation | Inclure l’outil préparé dans le cadrage, absent du résumé fonctionnel de S2 |
| HMI Beckhoff | Apprentissage des positions, messages, alarmes et modes magasin | Conserver cette responsabilité pour la première livraison, à confirmer |
| Automate Beckhoff | Table d’outils et automatisme magasin | Point d’accès applicatif prévu via ADS, Automation Device Specification |
| Liaison Beckhoff–FANUC | PROFINET, commandes, données et quittances | Dépendance indispensable des fonctions concernant les valeurs effectives en commande numérique |
| Interface externe | Modification des correcteurs, S1 diapositive 6 | Besoin réel, mais protocole, client et priorité non définis |

La liste des structures inclut des réglages et des demandes d’ouverture/fermeture de pince. Cela ne signifie pas que toutes ces fonctions doivent être offertes dans la nouvelle HMI. Il faut distinguer couverture technique du connecteur et périmètre fonctionnel des écrans.

Le cahier des charges initial vise surtout une prestation de connecteur : code source, documentation, formation et accompagnement aux essais. La demande actuelle ajoute la HMI ; son périmètre et sa recette doivent être explicités.

## 4. Configuration du magasin

La section 7 de S2 permet de vérifier les capacités suivantes :

| Racks | Places théoriques | Places utilisables décrites | Hauteur maximale indiquée |
| --- | --- | --- | --- |
| Rack 1 | 19 | 17 | 100 mm |
| Racks 2 à 5 | 76 | 76 | 100 mm |
| Rack 6 | 15 | 14 | 120 mm |
| Racks 7 et 8 | 30 | 30 | 120 mm |
| Total | 140 | 137 | Selon rack |

Les positions absolues 1, 2 et 96 sont interdites. Les outils sont à place fixe, obligatoirement indexés, avec interfaces HSK-A ou HSK-T selon le document. La configuration du nombre de racks et de positions peut varier selon le client.

Recommandation : conserver les 140 identités de position et représenter explicitement les interdictions. Ne pas comprimer la numérotation en une liste de 137 indices, ce qui casserait la correspondance avec l’automate. La configuration active peut désactiver d’autres places ; la capacité effectivement disponible peut aussi diminuer avec les blocages de voisinage.

Distinguer l’identité de l’outil, sa place fixe affectée et sa localisation physique actuelle. Un outil en broche peut garder sa place fixe au magasin alors que cette place est physiquement vide. Les règles de voisinage pour les outils encombrants et la signification exacte des hauteurs restent à préciser.

## 5. Le point critique de synchronisation avec FANUC

S1 demande :

1. Modifier le correcteur d’usure de l’outil de fraisage en broche ; le cas tournage avec correcteurs dans des variables macro demeure une question ouverte.
2. Voir la longueur de l’outil pendant le cycle de mesure M8888.
3. Déclarer l’outil en broche défectueux ou en fin de vie.
4. Permettre à une interface externe de modifier des correcteurs.

La diapositive 4 écrit M8888 dans son titre et M888 dans le dessin : le nom exact du cycle doit être confirmé avant tout mapping.

Les diapositives 3 à 5 indiquent une mise à jour des valeurs dans Beckhoff lors du changement d’outil. Ce mécanisme seul ne démontre pas la disponibilité des valeurs pendant une mesure ni l’application immédiate d’une correction à l’outil en broche.

**Une lecture ADS récente d’une copie Beckhoff peut contenir une valeur FANUC ancienne.** La fraîcheur de la copie et celle de la source doivent être distinguées. Une perte PROFINET peut rendre les données en broche incertaines alors que la communication ADS et les données locales du magasin restent disponibles.

Il faut établir, par famille de données : la source de référence, l’auteur autorisé, la propagation vers l’autre contrôleur, le moment d’application, la preuve de fin et les conflits possibles.

Exemple proposé pour une correction d’usure : l’application soumet une demande ciblant un outil identifié, un correcteur, une valeur et la version observée. Le service vérifie droits et préconditions. Beckhoff transmet et suit la modification via le protocole convenu. La fin n’est annoncée qu’après une preuve liée à la demande et à l’outil concerné, confirmant l’application sur la source faisant autorité. Une écriture ADS réussie est seulement une étape technique.

Le mécanisme exact est à concevoir avec les équipes Beckhoff et FANUC. Les acquittements esquissés dans S1 ne sont pas un contrat complet. Si la synchronisation existe déjà, il faut la démontrer ; sinon, du travail automate et/ou commande numérique est nécessaire en plus du connecteur .NET.

Le chemin prévu passe par Beckhoff. Un accès FANUC direct depuis l’application n’est pas justifié par les pièces actuelles. Si une lacune l’impose plus tard, il devra rester derrière la couche machine et faire l’objet d’une décision d’architecture.

## 6. Analyse des structures et conséquences pour les écritures

| Structure | Ce qu’elle décrit | Attention principale |
| --- | --- | --- |
| ST_Tool_Location | Position, type, numéros, états, coordonnées, nom et paramètres imbriqués | Mélange d’état observé, configuration et données éditables |
| ST_Tool_Parameters | Identité, durée de vie, état, dimensions codées, options et correcteurs | Ordre déclaré important ; unités et significations partielles |
| ST_Tool_Name | Nom de longueur définie par une constante automate | Valeur de la constante et encodage non fournis |
| ST_Tool_Corrector_M | Cinq champs entiers par correcteur de fraisage | Unités, échelles, bornes et convention de signe à définir |
| ST_Tool_Corrector_T | Neuf champs entiers par correcteur de tournage | Correspondance FANUC/macros à préciser ; conserver le nom source LontZ dans le mapping |
| ST_HMI_Magazine_Settings | Géométrie, configuration de racks, temporisation et sauvegarde | Séparer données de réglage, demande de sauvegarde et état interne de déclenchement |
| ST_HMI_Manipulator_Gripper | Durées, contrôle de capteurs, demandes d’ouverture/fermeture | Commandes à effet physique et déclencheurs internes dans la même structure |
| ST_HMI_Manipulator_Breakage | Temporisation, décalage, tolérance et vitesse de contrôle | Présent uniquement dans Structures.txt ; périmètre et exposition à confirmer |

Les champs bEnable, bForbiden, bBlocked, bUsed et bToolPresence portent des significations distinctes. En particulier, la déclaration opérateur et la présence constatée doivent rester séparées ; une divergence doit être explicable dans l’interface.

Les correcteurs ont trois représentations déclarées : ListCorrecteur[0..44], ListCorrToolM[0..4] et ListCorrToolT[0..4]. Les exports les déclarent comme champs distincts, sans prouver une superposition mémoire. Il faut déterminer la représentation de référence, la conversion et qui synchronise les autres. Écrire les trois automatiquement pourrait écraser une modification valide.

Ne pas réécrire toute ST_Tool_Location ou ST_Tool_Parameters à partir d’une copie affichée. Une correction de nom ne doit pas restaurer une durée de vie périmée ou un ancien état. Privilégier les modifications ciblées ; si plusieurs champs doivent changer ensemble, prévoir une validation cohérente côté automate et une protection contre les éditions concurrentes. Une relecture après écriture ne suffit pas à elle seule à empêcher une concurrence.

Les instances rtSettingsSave, rtOpenManupulatorGripper et rtCloseManupulatorGripper de type R_Trig sont des détails de fonctionnement automate. Proposition : les exclure des écritures applicatives ; exposer seulement des demandes contrôlées dont le cycle demande/acquittement/remise à zéro est défini. Ne pas inventer une durée d’impulsion depuis le PC.

## 7. Informations manquantes pour un connecteur fiable

Les XML sont des définitions de types. Ils ne fournissent pas la liste des instances globales réellement publiées, leurs chemins de symboles ou le tableau global des emplacements.

À obtenir avant des écritures réelles :

- chemins de symboles, bornes du tableau d’emplacements et symboles de l’outil en broche/préparé ;
- valeur de GV_Tool_Magazine.iNumCharaToolName et règles d’encodage ;
- définitions numériques et représentation des ENUM_Position_Magazine et ENUM_Tool_Type ;
- codage de State, Size, FixedLocation, LifetimeType, UseFlags, Options, ModelFlags et des champs partiellement documentés ;
- unités, échelles, signes, bornes, arrondis et signification de PositionW, TipAngle, TailleMemByt et des paramètres utilisateur ;
- relations entre TIME et INT pour les temporisations ;
- droits lecture/écriture par champ, modes autorisés et règles de création/réaffectation d’un outil ;
- disposition mémoire réelle si accès binaire : tailles, décalages, alignement, longueur des chaînes, sous-structures et version ;
- versions TwinCAT et routeurs réellement installées, adresse de routage et port cible ;
- persistance automate : données conservées au redémarrage, commande de sauvegarde et procédure de restauration ;
- contrat de synchronisation et d’acquittement Beckhoff–FANUC.

La version 3.5.21.20 inscrite dans l’en-tête d’export ne permet pas à elle seule de déduire le build TwinCAT déployé. L’environnement P4 de la plateforme ne prouve pas non plus la configuration du nouveau magasin.

Les schémas exportés peuvent servir à préparer les mappings et les essais. Les octets réels et leur interprétation doivent être vérifiés avec les symboles de la cible et des valeurs connues. Aucun choix arbitraire d’alignement ou de facteur de conversion ne doit être appliqué.

## 8. Architecture de transition proposée

Le produit doit distinguer un connecteur ADS réutilisable et l’adaptation au magasin particulier. Le premier gère connexion, transport, lectures, écritures et diagnostic. La seconde connaît les symboles, structures et mécanismes du magasin. Les règles métier supérieures ne dépendent d’aucun symbole Beckhoff.

| Couche | Responsabilité | Réutilisation future visée |
| --- | --- | --- |
| HMI de gestion d’outils | Vue par racks, fiche outil, broche/préparation, états d’application des modifications | Écrans et modèles de présentation |
| Application de gestion d’outils | Parcours d’édition, validation métier, droits et suivi des demandes | Règles métier et tests |
| Contrats machine du magasin | États sémantiques, qualité, fraîcheur, identité et opérations | Contrats versionnés et scénarios de conformité |
| Intégration du magasin Beckhoff | Mapping des structures et suivi du protocole de synchronisation | Adaptation ou remplacement derrière les contrats |
| Connecteur Beckhoff ADS | Accès technique et cycle de connexion | Composant candidat à l’intégration dans la plateforme |

Exemples de services à spécifier, sans figer leurs signatures : observer le magasin, lire une fiche outil, créer une déclaration d’outil à une place fixe, modifier certains paramètres, modifier un correcteur, déclarer un état outil et consulter le résultat d’une demande. Les noms PrepareTool et LoadTool du démonstrateur ne doivent pas servir à masquer ces nouvelles opérations.

Le service métier doit réévaluer les autorisations et préconditions. Une interface externe éventuelle consommera ces mêmes services. L’exigence de « requêtes libres » peut être satisfaite par un accès technique partagé dans la couche d’intégration, avec délais et files bornés. Une surface brute de lecture/écriture de symboles accessible aux écrans créerait un couplage durable et contournerait les règles.

Pour l’hébergement, un service local indépendant de la HMI est un candidat pertinent lorsque le suivi doit continuer après fermeture de l’écran ou être partagé avec un autre client. Le protocole local et son authentification restent à choisir. Si une première composition dans un seul processus est retenue pour réduire l’effort, elle devra expliciter la perte de suivi au crash et sa reprise, sans revendiquer une isolation de processus.

La plateforme reste fondée sur Architecture V1.3. La frontière vers les consommateurs externes peut inspirer les contrats, mais sa candidate ne devient pas normative par cette application. La passerelle OPC UA, la gestion de flotte et les fonctions d’intelligence artificielle ne bloquent pas ce produit en l’absence d’exigence correspondante.

## 9. Acquis réutilisables et validations à ajouter

S6 décrit des preuves de concept, ou PoC, réussies sur Beckhoff : ADS réel, observation, préparation/chargement d’outils, reconnexion, état incertain, nouvelle génération de session, réconciliation et EventLogger. Ces acquis réduisent le risque architectural.

Ils ne prouvent pas encore :

- les structures imbriquées et le magasin complet de cette machine ;
- Secure ADS sur les deux équipements cibles ;
- les mises à jour de correcteurs en broche, le cycle M8888 ou les conflits de données ;
- la reprise après arrêt complet du service avec une demande en cours ;
- le maintien des données, les mises à jour logicielles et l’installation sur le PC industriel ;
- la compatibilité du composant avec l’hôte oCNC/BECKi à intégrer.

L’inventaire de code devra relever dépendances, contrats exposés, hypothèses de démonstration, tests disponibles et durcissements nécessaires. Utiliser une version identifiée des composants retenus, sans dépendre d’une branche de démonstration mouvante.

## 10. Secure ADS, système cible et exigences de cybersécurité

Secure ADS est une exigence explicite de S2/S3. Beckhoff le décrit comme un transport chiffré entre routeurs TwinCAT, disponible à partir de TwinCAT 3.1 build 4024.0. La configuration de route permet à une application ADS existante d’utiliser ce transport. Ce n’est pas simplement une fenêtre de connexion utilisateur dans la HMI. [Beckhoff, description Secure ADS](https://infosys.beckhoff.com/content/1033/secure_ads/6798091787.html)

Définir l’établissement de confiance, la gestion des certificats ou clés, leur renouvellement, les routes autorisées et le comportement en cas d’échec. L’authentification de la communication reste distincte des droits opérateur sur les correcteurs. [Beckhoff, échange des clés](https://infosys.beckhoff.com/content/1033/secure_ads/6801109899.html)

Pour une nouvelle application Windows, WPF (Windows Presentation Foundation) avec .NET moderne reste un candidat adapté à l’expérience de l’équipe. La matrice officielle .NET 10 inclut Windows 10 Enterprise 1809 ; la compatibilité exacte de l’édition IoT, des dépendances et de l’hôte FANUC doit néanmoins être vérifiée sur le PC réel. Ne pas déduire du système annoncé une obligation d’utiliser .NET Framework. [Microsoft, systèmes pris en charge par .NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)

Si l’hôte impose .NET Framework, un service .NET moderne séparé est une option à comparer à une adaptation du connecteur. Une bibliothèque .NET 10 ne s’intègre pas telle quelle dans un processus .NET Framework. La décision dépend du mode d’intégration oCNC/BECKi, encore absent des pièces.

L’édition Windows 10 IoT Enterprise LTSC 2019 possède son propre cycle de support, distinct de Windows 10 grand public. Prévoir le maintien de la pile logicielle sur la durée de vie du produit. [Microsoft, cycle de vie de l’édition cible](https://learn.microsoft.com/en-us/lifecycle/products/windows-10-iot-enterprise-ltsc-2019)

Le Cyber Resilience Act (CRA), règlement européen sur la cyberrésilience, est cité par le cahier des charges. Secure ADS ne constitue pas à lui seul une preuve de conformité : prévoir responsabilités de maintenance, gestion des vulnérabilités, inventaire des dépendances, droits, protection des secrets et mises à jour maîtrisées. L’applicabilité précise dépend du produit et de sa mise sur le marché. La Commission indique le 11 décembre 2027 pour les principales obligations et le 11 septembre 2026 pour les obligations de signalement. [Commission européenne, CRA](https://digital-strategy.ec.europa.eu/en/policies/cyber-resilience-act)

Les journaux devront distinguer diagnostic technique et traçabilité des changements : outil concerné, auteur, valeurs avant/après si approprié, demande et résultat. Prévoir rotation et rétention pour le disque local, sans journaliser de secrets.

## 11. Première livraison et ordre de réalisation proposés

Le périmètre urgent recommandé comprend la connexion sécurisée, les états de communication, les journaux, la représentation fidèle des positions, la consultation des fiches et les modifications métier nécessaires. L’édition en broche reste dans le besoin, mais dépend de la preuve de synchronisation.

Conserver provisoirement l’apprentissage et la commande manuelle de la pince sur la HMI Beckhoff. L’interface externe reste un besoin identifié, dont la priorité et le protocole doivent être définis avant engagement. Aucun report proposé ici ne supprime une exigence sans décision du projet.

| Étape | Travail | Preuve de sortie attendue |
| --- | --- | --- |
| 1 | Contrat automate et audit ciblé du code réutilisable | Symboles, unités, droits, autorités et dépendances recensés ; inconnues explicitement bloquantes |
| 2 | Connexion Secure ADS et observation | Lectures exactes sur la cible, 140 identités et configuration correcte, pertes et reprises observables |
| 3 | HMI de consultation | Vue racks, fiche outil, broche/préparation et distinction des états, sans commandes d’écriture |
| 4 | Une modification ciblée hors broche | Validation, écriture sans écrasement d’autres données, persistance et conflit contrôlés |
| 5 | Une correction en broche de bout en bout | Valeur réellement appliquée et confirmée dans FANUC, cas d’incertitude et changement d’outil maîtrisés |
| 6 | Complétion métier et industrialisation | Création, état outil, autres correcteurs, reprise, installation, documentation et essais accompagnés |

Les étapes de consultation et de modification hors broche peuvent avancer pendant la clarification de la liaison FANUC. La disponibilité d’une machine ou d’un banc représentatif conditionne les preuves finales. Aucun délai chiffré fiable n’est possible avant de connaître le protocole existant et l’échéance réelle.

## 12. Recette minimale proposée

| Essai | Résultat attendu |
| --- | --- |
| Valeurs connues et limites de types | Conversion exacte, tableaux et chaînes corrects, aucune unité supposée |
| Configuration nominale | 140 identités, positions 1/2/96 interdites, 137 places physiques au maximum nominal |
| Outil en broche à place fixe | Place affectée et localisation physique représentées séparément |
| Création et édition hors broche | Règles d’identité et d’affectation respectées ; champs non concernés préservés |
| Éditions concurrentes | Conflit détecté ou arbitrage explicite, aucune perte silencieuse de modification |
| Édition d’un outil pendant changement | Ancienne fiche jamais appliquée au nouvel outil arrivé en broche |
| Modification de correcteur | Preuve d’application sur la source autoritative et au bon outil |
| Mesure M8888 | Valeur observée conforme au niveau de suivi convenu, pendant ou à la fin de la mesure ; aucune fausse actualité |
| Fin de vie/défectueux | Cohérence automate–commande numérique, conservation après changement d’outil |
| ADS connecté, liaison FANUC perdue | Données FANUC identifiées indisponibles ou périmées malgré la disponibilité ADS |
| Coupure après demande avant réponse | Résultat incertain, aucun rejeu aveugle, réconciliation bornée |
| Redémarrage HMI puis service/PC | Relecture, réconciliation, aucune commande dupliquée ; limites de l’historique explicites |
| Redémarrage/modification du programme automate | Compatibilité réévaluée, références techniques recréées si nécessaire, données anciennes invalidées |
| Route sécurisée invalide | Échec explicite et absence de repli silencieux vers une route non sécurisée |
| Fonctionnement prolongé | Charge, réactivité, consommation mémoire et volume des journaux conformes à des seuils convenus |

Une demande persistée doit servir à comprendre et réconcilier une opération après redémarrage, pas à la rejouer automatiquement. Sans identifiant de demande et preuve conservée par le contrôleur, certaines situations resteront indécidables ; l’interface doit le reconnaître.

## 13. Migration vers la plateforme

1. Versionner les contrats métier, les schémas de configuration et les règles d’identité dès la première application.
2. Garder les données physiques sous l’autorité prévue côté machine. Toute persistance locale supplémentaire devra avoir une responsabilité explicite.
3. Réaliser ensuite une implémentation des mêmes services reposant sur la plateforme et exécuter les mêmes scénarios de conformité.
4. Comparer éventuellement les observations en lecture seule, après mesure de la charge supplémentaire.
5. Effectuer la bascule hors opération en cours avec un seul chemin d’écriture actif, bootstrap de l’état et validation de la compatibilité.
6. Prévoir un retour à la version précédente compatible avec les données et le programme automate. Ne jamais restaurer une ancienne image physique du magasin depuis une sauvegarde logicielle.

Les adaptateurs pourront évoluer et certains contrats nécessiter une version suivante. La séparation proposée limite la migration, sans promettre une substitution binaire automatique.

## 14. Questions à résoudre en priorité

| Priorité | Question | Interlocuteur principal |
| --- | --- | --- |
| Immédiate | Le programme Beckhoff et le protocole PROFINET sont-ils déjà implémentés et testés, notamment pour l’écriture en broche et M8888 ? | Beckhoff et FANUC |
| Immédiate | Quels symboles représentent la table, l’outil en broche, l’outil préparé et les demandes/quittances ? | Beckhoff |
| Immédiate | Qui fait autorité sur chaque famille de correcteurs et comment les éditions sont-elles arbitrées ? | Beckhoff et FANUC |
| Immédiate | Quels sont les codages, unités, tailles de chaînes et règles de persistance ? | Beckhoff et métier outil |
| Immédiate | Application autonome ou intégration à oCNC/BECKi ; quelles contraintes de runtime et d’installation ? | Équipe HMI |
| Immédiate | Quelle date pour la première démonstration machine, puis pour la mise en service ? | Responsable projet |
| Avant recette | La nouvelle HMI doit-elle aussi exposer réglages, pince, alarmes et modes, ou seulement les données d’outils ? | Responsable produit et automatisme |
| Avant recette | L’interface externe est-elle exigée à la première livraison et qui la consomme ? | Responsable produit |

Prochaine étape recommandée : figer le périmètre de la première version et le contrat d’échange automate, puis auditer le connecteur existant contre ce contrat. La chaîne prioritaire à prouver est une modification d’outil hors broche, suivie d’une correction en broche confirmée dans FANUC.
