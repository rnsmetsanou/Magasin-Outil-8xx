# Premier écran de gestion du magasin et modèle de présentation

Date : 11 septembre 2026

Statut : proposition V0.1 à examiner. Cette définition couvre le premier écran et ses interactions. Elle ne valide ni le renderer produit ni les écritures sur machine.

## 1. Objectif de la première étape

Présenter dès la première étape une interface homme-machine (HMI) moderne et utilisable au doigt : localiser un outil, comprendre son état et préparer une modification sans confondre sa place affectée avec sa position physique actuelle.

Le premier écran fonctionne sur des données simulées. Il prépare une implémentation Avalonia, candidat principal discuté, mais la maquette interactive en conversation ne constitue pas une implémentation Avalonia ni une preuve de ses performances. Le choix définitif dépend du test sur le PC FANUC FIP 1000 et du mode d’intégration avec oCNC/BECKi.

Le travail reste compatible avec Architecture V1.3 de la plateforme. Aucun nouveau contrat proposé ici n’est implicitement promu dans la baseline normative.

## 2. Données sources intégrées

Sources : cahier des charges Word révision 1.1, Architecture.pptx, les structures ST_*.xml, Structures.txt, GV_Tool_Magazine.xml et les quatre nouveaux exports ENUM_*.xml.

| Information | Base retenue |
| --- | --- |
| Tableau principal | GV_Tool_Magazine.aLocation, déclaration ARRAY[1..iMaxAbsLocation] OF ST_Tool_Location |
| Capacité maximale déclarée | 5 × 19 + 3 × 15 = 140 positions |
| Places physiques nominales | 137, d’après le cahier des charges |
| Places interdites | 1, 2, 96, d’après le cahier des charges ; initialisation correspondante non fournie |
| Partie supérieure | Racks 1 à 5, 19 positions théoriques par rack |
| Partie inférieure | Racks 6 à 8, 15 positions théoriques par rack |
| Hauteurs maximales indiquées | 100 mm en haut ; 120 mm en bas ; sens métier exact à confirmer |
| Nom d’outil | Constante iNumCharaToolName = 30 ; encodage restant à confirmer |
| Catégorie locale eToolType | Unknow=0, Small=1, Big=2, GPS70=3 |
| Position magasin | Unknow=0, Top=1, Bottom=2 |
| Tableau de contrôle | aCheckLocation, distinct du tableau principal ; cycle de fonctionnement à préciser |
| Persistance déclarée | aLocation et aCheckLocation dans VAR_GLOBAL PERSISTENT ; conservation effective à tester |

Les fichiers ENUM_Gripper_Mode.xml et ENUM_Tunnel_Mode2.xml contiennent plusieurs déclarations. Le second contient ENUM_Tunnel_Mode, sans nouveau type ENUM_Tunnel_Mode2. Les valeurs répétées concordent. ENUM_Position_Axis est aussi disponible : Unknow=0, Work1..Work5=10..50 par pas de 10, CNC=80, Ref=90, Home=100.

Le gabarit, la famille d’usinage et l’interface porte-outil sont des dimensions différentes. Les valeurs Small/Big/GPS70 ne déterminent pas à elles seules fraisage/tournage ou HSK-A/HSK-T.

## 3. Composition de l’écran

### Bandeau

Titre Gestion des outils, identification du contexte et statut de communication. Dans la maquette, la mention de démonstration et l’absence de liaison machine restent visibles. En production, distinguer disponibilité du service, communication avec le magasin et validité des données provenant de la commande numérique.

Aucun compteur de production ou indicateur sans utilité pour cette tâche n’est ajouté.

### Broche et préparation

Deux accès permanents affichent l’outil en broche et l’outil préparé, leur nom et leur place fixe. Une sélection ouvre leur fiche. La localisation physique du préparé ne doit pas être déduite du seul fait qu’il est préparé ; elle exige une observation correspondante.

### Vue du magasin

La vue d’ensemble représente les huit racks. Pour la configuration nominale, le haut affiche les racks 5 à 1 de haut en bas ; le bas affiche les racks 6 à 8. Cette disposition reprend le dessin fourni. Le sens croissant dans chaque rack est conservé, à confirmer par la configuration et les numéros effectivement observés.

La sélection tactile porte sur un rack entier. Ses emplacements apparaissent ensuite dans une zone agrandie et sélectionnable. Cette zone de détail peut se répartir sur plusieurs lignes ; elle est une liste des positions du rack, pas un dessin mécanique à l’échelle.

Ce choix évite de comprimer 19 boutons tactiles et une fiche outil dans une largeur insuffisante. Sur le PC de 24 pouces, une sélection directe dans la vue d’ensemble pourra être ajoutée si la résolution, le facteur d’échelle et la taille physique des cibles le permettent.

Aux petites largeurs, les miniatures sont remplacées par les plages de numéros des racks. La zone des emplacements agrandis reste accessible. La fiche passe sous le magasin.

### Fiche de la sélection

La fiche indique d’abord la place sélectionnée et le rack, puis l’identité de l’outil si disponible. Elle distingue : place fixe affectée, position actuelle, présence à cette place, état métier de l’outil.

Deux vues : Données et Correcteurs. Les coordonnées d’apprentissage, états internes de déclenchement et variables de transport ne sont pas exposés à l’opérateur sur ce premier écran.

Une position interdite ou bloquée reste consultable, avec son motif. Une place vide indique clairement l’absence d’affectation connue. La création d’une affectation sera ajoutée après définition de ses règles ; elle ne doit jamais simuler une présence physique.

## 4. Parcours de la maquette

Les interactions réalisées sont : sélectionner un rack, sélectionner une position, rechercher un outil par nom/numéro ou une place par numéro, ouvrir les fiches de broche et de préparation, consulter les données/correcteurs, modifier le nom et un exemple d’usure longueur, vérifier puis confirmer ou annuler.

Le pavé numérique agit sur le champ d’usure uniquement. La saisie du nom utilise le clavier du poste ; le comportement du clavier tactile système reste à tester dans l’application cible.

Pendant l’édition, les autres sélections sont suspendues. L’opérateur peut terminer ou annuler sans perdre silencieusement sa saisie. La confirmation rappelle l’identité de l’outil et les valeurs modifiées. Les changements de la maquette sont locaux et disparaissent au rechargement.

Les valeurs de longueur, d’usure, les identités d’outils, les états d’occupation, les positions de blocage et les emplacements des outils en broche/préparation sont des exemples inventés pour l’examen de l’interface. Les seuls emplacements interdits normatifs de ce jeu nominal sont ceux provenant du cahier des charges.

Le jeu présente notamment T12 affecté à la place 12 mais en broche, T34 affecté à la place 34 et situé dans une position de préparation fictive, ainsi que T127 affecté à la place 27. Ce dernier exemple montre explicitement que numéro d’outil et numéro de place ne sont pas interchangeables.

Les blocages illustrés ne constituent pas un algorithme de voisinage. Les trois décimales d’affichage et de saisie sont un choix de maquette, sans facteur de conversion vers les entiers automate. Aucun seuil mécanique n’est inventé pour valider l’usure.

## 5. Modèle de présentation proposé

Les noms suivants désignent des modèles à spécifier, sans imposer des signatures C# définitives. Ils sont indépendants des classes Beckhoff et de leurs types exportés.

| Modèle | Contenu principal | Autorité |
| --- | --- | --- |
| ToolMagazineScreenPresentation | État du service, disponibilité des sources, broche/préparation, racks, fiche, opération affichée | Projection du service et état de navigation local |
| MagazineLayoutPresentation | Capacités maximale et active, ordre des racks, numéros absolus, étiquettes | Configuration sémantique validée |
| RackPresentation | Identité, partie haute/basse, ordre visuel, places, contraintes exposables | Configuration et observation |
| LocationPresentation | Identité absolue, numéro relatif, rack, activé/interdit/bloqué, motif, affectation déclarée, présence observée | Observation sémantique |
| ToolSummaryPresentation | Identité, numéro affiché, nom, catégorie, état outil | Observation sémantique |
| ToolDetailPresentation | Affectation fixe, position actuelle, paramètres, famille de correcteurs, champs éditables | Observation et disponibilité des actions |
| ToolEditDraft | Outil ciblé, version attendue, valeurs initiales, valeurs modifiées, erreurs de saisie | Brouillon local, jamais vérité machine |
| OperationPresentation | Identifiant, cible, état, motif et confirmation disponible | Service applicatif |
| ObservationPresentation | Qualité, fraîcheur, disponibilité de la source, génération pertinente, date source facultative | Projection de l’observation, sans date inventée |

Le modèle peut comporter plusieurs observations ayant des niveaux de fraîcheur différents. Un unique drapeau Fresh sur toute la fiche serait insuffisant si certaines valeurs viennent de FANUC via Beckhoff.

### Règles d’identité

- SelectedLocationId et SelectedToolId sont distincts.
- AssignedLocationId décrit la place fixe, PhysicalLocation décrit la position observée.
- Le numéro métier d’outil ne doit pas être supposé unique et immuable sans contrat. Définir les règles de réutilisation du numéro et, si nécessaire, une identité d’instance.
- Une demande d’édition capture la cible et sa révision. Elle ne doit pas se transformer en demande sur le nouvel outil en broche si un changement survient.

### États d’emplacement

Enabled, Forbidden, Blocked, DeclaredUsed et Presence restent séparés. La présentation peut choisir un style principal par priorité, mais elle conserve les valeurs sources et affiche les désaccords dans la fiche. Une présence indéterminée n’est pas assimilée à une absence.

Couleurs accompagnées de textes ou symboles : présence, outil hors magasin, défaut/fin de vie, vide, interdit/bloqué. La sélection possède son propre indicateur et ne change pas la signification de la couleur métier.

## 6. Contrats vers l’application

| Surface proposée | Rôle |
| --- | --- |
| Observation du magasin | Fournir la configuration et les états sans exposer les symboles ADS, Automation Device Specification |
| Lecture d’une fiche outil | Fournir les paramètres sémantiques, leurs unités et leur actualité |
| Disponibilité d’une modification | Expliquer si elle est possible et pourquoi elle est indisponible |
| Soumission d’une modification ciblée | Recevoir identité, révision attendue et uniquement les champs modifiés |
| Suivi/résolution d’une demande | Retrouver son état après perte de transport ou redémarrage de la HMI |

Les règles de droits, d’admission et de validité machine sont réévaluées dans le service. La HMI peut aider à la saisie mais n’autorise pas elle-même une écriture.

Les contrats ne contiennent ni AdsClient, ni chemins GV_*, ni tableaux d’octets, ni instances R_Trig. L’adaptation technique assure les correspondances et conversions après validation de leurs unités et dispositions mémoire.

## 7. Édition et situations dégradées en production

| Situation | Comportement requis |
| --- | --- |
| Consultation normale | État observé et actions disponibles visibles |
| Brouillon local | Valeurs sources conservées séparément ; aucun appel d’écriture |
| Données modifiées ailleurs | Conflit expliqué ; aucune réécriture silencieuse d’une fiche périmée |
| Demande acceptée | Suivi en cours ; l’interface n’affiche pas déjà la valeur comme confirmée |
| Modification confirmée | Projection actualisée depuis une preuve sémantique liée à la bonne cible |
| Refus | Motif compréhensible ; aucune tentative supplémentaire implicite |
| Résultat incertain | Aucune relance automatique ; consultation/réconciliation de la demande |
| Connexion au magasin perdue | Dernières valeurs identifiées comme anciennes, éditions concernées indisponibles |
| Magasin joignable, FANUC indisponible | Distinguer données locales encore utilisables et valeurs de broche non garanties |
| Retour de communication | Relecture et réconciliation avant de conclure à la validité des données |
| Changement d’outil pendant édition | Conserver l’identité ciblée et contrôler les préconditions avant soumission |

La maquette réalise seulement l’édition locale avec vérification et confirmation. Elle n’exerce pas ces pannes, les droits, le protocole distribué, la persistance ou les reprises. Ces cas appartiennent aux essais de l’application et du service réels.

## 8. Composants à réaliser ou réutiliser

| Composant | Approche pour l’implémentation candidate Avalonia |
| --- | --- |
| Fenêtre, boutons, champs, dialogues | Contrôles existants avec thème cohérent |
| Vue globale des racks | Composition métier réutilisable |
| Sélecteur agrandi des emplacements | Composition de contrôles standards avec modèles de données |
| Résumé broche/préparation | Composant métier partagé |
| Fiche outil et correcteurs | Formulaires composés, sans dépendance à une grille commerciale |
| Pavé numérique avec unités | Composant tactile métier |
| Disponibilité et résultat d’une demande | Composants communs aux écrans machine |

La compatibilité des versions Avalonia et d’une éventuelle bibliothèque complémentaire reste à valider. Aucun composant commercial n’est requis par cette définition d’écran. Le document ne remplace pas l’inventaire de licences des dépendances finalement retenues.

## 9. Questions encore ouvertes et limites

Les déclarations ST_Manipulator_Head, ST_Changing_Arm et ST_Miscellaneous restent manquantes. Les symboles effectifs des outils en broche/préparation et le contrat Beckhoff–FANUC restent à établir. Ne pas en déduire des propriétés internes à partir du seul nom des structures.

Le codage State/Size/LifetimeType, les correspondances entre les trois représentations de correcteurs, les unités des entiers, les règles de création et la sauvegarde effective restent ouverts. Les limites de réglage présentes dans GV_Tool_Magazine ne constituent pas des limites de correction d’usinage.

Les valeurs initiales de configuration égales à zéro ne sont pas utilisées comme configuration active. Les constantes de capacité sont distinctes des variables relatives déterminant la configuration client. La déclaration de persistance ne vaut pas preuve de sauvegarde sur coupure brutale.

## 10. Suite immédiate et critères d’examen

Examiner d’abord le parcours sélection d’un rack → sélection d’une place → lecture de la fiche → édition → vérification → confirmation. Vérifier que la différence entre place et identité d’outil est évidente, que les états sont lisibles et que la fiche ne surcharge pas l’opérateur.

Puis implémenter ce même écran en Avalonia avec une source simulée derrière les contrats proposés. Sur le PC cible, tester résolution et échelle d’affichage réelles, tailles physiques des cibles, clavier tactile, séparateur décimal, saisie des valeurs négatives, réactivité et comportement au redémarrage de la fenêtre. Ces essais permettront de décider du renderer produit sans attendre le connecteur complet.

Le branchement sur Beckhoff intervient ensuite par remplacement de la source simulée, après validation du mapping et des contrats. L’intégration future à la plateforme vise la même frontière ; les modèles de présentation restent propres à la HMI et ne deviennent pas le contrat universel de la plateforme.
