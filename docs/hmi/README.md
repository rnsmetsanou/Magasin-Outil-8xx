# HMI Avalonia — première tranche simulée

> Ce document décrit le prototype actuellement implémenté. Pour la cible web et l’intégration plateforme, consulter [l’index documentaire](../README.md) et le [registre des décisions](../decisions/Registre_Decisions.md).

## État

Première implémentation .NET 10 / Avalonia 12.1.1. Aucune liaison machine, aucune écriture ADS, aucune commande de mouvement. Les données et confirmations sont fictives et perdues à la fermeture. Cette tranche ne revendique pas une qualification industrielle.

## Lancement

Depuis la racine du dépôt, avec le SDK .NET 10 :

```powershell
git fetch origin
git switch hmi/avalonia-magazine-v1
dotnet build .\MagasinOutil.Hmi.slnx
dotnet run --project .\tests\MagasinOutil.Checks\MagasinOutil.Checks.csproj
dotnet run --project .\src\MagasinOutil.Desktop\MagasinOutil.Desktop.csproj
```

La fenêtre démarre avec Width=1024 et Height=768 ; elle est redimensionnable. Maximiser sur le FIP1000 Full HD 1920 × 1080. F11 active/quitte le plein écran. Les dimensions Avalonia sont logiques : vérifier aussi l’échelle Windows, la surface cliente et les décorations. Les panneaux magasin et outil ne défilent plus : la grille du rack tient dans le panneau, les actions restent en bas et l’édition utilise une page dédiée. Les boutons restent tactiles. La fiche reste à droite ; aucune mise à l’échelle globale ne réduit les cibles au redimensionnement.

## Fonctions

- 140 positions déclarées, 137 places physiques nominales ; positions 1, 2, 96 interdites.
- Huit racks, sélection d’une place, accès broche/préparé et recherche paginée.
- Distinction entre outil, place fixe et position actuelle.
- Données/correcteurs, édition du nom et d’une usure fictive, pavé numérique, vérification, confirmation ou annulation.
- Brouillon conservé lors du changement de thème ; sélection suspendue pendant l’édition.
- Contrôle de l’identité et de la révision côté service, sans écriture complète de structure.
- Thème clair au démarrage, thème sombre sélectionnable ; logos WM originaux adaptés au fond.

La position « préparé » du simulateur ne prouve aucune position physique réelle. Les trois décimales et trente caractères sont des règles du jeu simulé, pas des facteurs d’échelle ou une validation d’encodage automate. Les boutons du bandeau broche/préparé consultent les occupants actuels, sans identifiants codés en dur. Les actions de la fiche changent uniquement le simulateur après confirmation ; elles ne commandent aucun mouvement machine. Les blocages et occupations sont fictifs.

## Structure et migration

- `MagasinOutil.Core` : modèle métier immuable, contrat de service et simulateur protégé contre les modifications concurrentes.
- `MagasinOutil.Desktop` : fenêtre Avalonia et état de présentation local. La vue est construite en C# pour cette première tranche.
- `MagasinOutil.Checks` : contrôles exécutables du modèle, sans dépendance automate, avec une vérification du placement Avalonia sans fenêtre visible.

`MagasinOutil.Hmi.slnx` est indépendante de `Tool_Magazine_8xx.sln`. Aucune configuration TwinCAT n’est changée. Aucun fichier de propriétés MSBuild commun n’est ajouté à la racine pour éviter d’influencer les projets automate.

Référence examinée : PlateformeWM-Demo, branche p6-7-live-commissioning-impl, révision be8e3393bf1884dff8b4b1989ee46ee0b6f39012. Le renderer existant est essentiellement textuel. Aucun code privé de la plateforme n’est copié dans ce dépôt public. Les contrats locaux sont provisoires et n’amendent pas Architecture V1.3.

Le futur service live devra introduire observation asynchrone, qualité/fraîcheur/provenance, admission, autorisation, opérations corrélées et preuve métier. Ne pas adapter une réussite d’écriture ADS en `AppliedInSimulation` : cet état est exclusivement simulé. Le cycle de vie de la connexion et des opérations devra rester indépendant de la fenêtre. Les unités, symboles, propriétaires de données et acquittements Beckhoff/FANUC restent à préciser avant les écritures.

## Charte WM

Référence : Charte graphique v1.4 FR, pages 7–8, 12 et 15–18. Bleu WM #202945 et blanc principaux ; bleu noir #051C2C pour les surfaces sombres. Les couleurs de sélection sont des nuances dédiées, sans bleu motif réservé aux motifs. Logos PNG fournis par l’utilisateur, intégrés sans modification, déformation, filtre ni réduction d’opacité. Espace de protection ménagé dans l’en-tête. Les logos et la marque WM restent la propriété de Willemin-Macodel ; la licence du code ne concède pas de droit sur la marque.

Poppins Regular est prévue comme ressource locale avec sa licence OFL. Si l’actif n’est pas disponible, le code utilise explicitement la police système ; aucune requête de police n’a lieu au lancement.

## Vérification

Le workflow `.github/workflows/hmi.yml` compile uniquement la solution HMI puis exécute les contrôles métier. Il ne compile pas TwinCAT et ne se connecte à aucune machine. La compilation locale n’a pas pu être exécutée dans l’environnement initial faute de SDK .NET ; consulter le résultat réel du workflow, sans considérer son existence comme preuve de réussite.

Recette à effectuer sur Windows :

1. Ouvrir à 1024 × 768, puis maximiser à 1920 × 1080 ; vérifier absence de chevauchement et visibilité de toutes les places, de la légende et des actions sans défilement.
2. Comparer Clair/Sombre : contraste, logo, champs, sélection et contrôles natifs.
3. Rechercher « Fraise », parcourir les pages et ouvrir T12 : place fixe 12, présence absente.
4. Modifier une usure avec le pavé ; changer le thème ; vérifier le brouillon, annuler puis recommencer et confirmer.
5. Saisir un nom invalide ou une usure non numérique ; vérifier le refus.
6. Vérifier le clavier tactile système pour le nom, les gants éventuels et les facteurs d’échelle Windows.
7. Fermer et relancer : données fictives réinitialisées, thème clair initial, aucun replay.

La persistance des préférences, la création d’outil, la localisation multilingue, les droits utilisateurs et les alarmes réelles sont des étapes suivantes. Aucun statut de validation visuelle ou machine n’est revendiqué ici.

## Résultat exécuté le 14 septembre 2026

La [vérification GitHub Actions 34857510339](https://github.com/rnsmetsanou/Magasin-Outil-8xx/actions/runs/34857510339) a réussi sur le commit `e7636cacadbdeae3c919909a7ea3fce146b2846d` : compilation Release .NET 10, zéro avertissement de compilation, zéro erreur, onze contrôles métier réussis. L’environnement était Ubuntu ; ce résultat ne valide pas le rendu Windows, le tactile ni la communication Beckhoff. Deux premiers essais ont permis de corriger les conflits de noms et une propriété obsolète Avalonia 12, sans désactiver les avertissements traités en erreurs.

Poppins Regular et sa licence OFL sont effectivement embarquées, ainsi que les deux logos fournis. L’application peut charger ces ressources sans réseau.

## Correction du lancement : sources NuGet héritées

Le journal Windows du 14 septembre montre NU1301 / HTTP 401 sur les sources privées héritées, avant compilation et démarrage. La HMI ne dépend actuellement que de packages publics. Chaque projet de la solution HMI sélectionne donc explicitement `config/hmi/NuGet.Config` par `RestoreConfigFile`. Cette configuration déclare NuGet.org pour les packages et leur audit ; l'audit de vulnérabilités reste actif.

Cette correction ne modifie aucune configuration utilisateur ou machine et n'affecte pas les projets TwinCAT. Ne pas désactiver les sources privées globalement : elles restent utiles aux autres projets. Lors de l'ajout futur de packages privés de plateforme, réviser cette configuration explicitement avec leur authentification.

Après récupération de la branche, la commande ordinaire `dotnet run --project .\src\MagasinOutil.Desktop\MagasinOutil.Desktop.csproj` utilise cette configuration. Le workflow ajoute une source héritée volontairement inaccessible, puis restaure sans option `--source` ni `--configfile`, afin de vérifier le comportement du projet. Le résultat de ce contrôle doit être consulté dans GitHub Actions ; cette configuration ne constitue pas une preuve de démarrage graphique Windows.

## Révision ergonomique — 14 septembre 2026

- Logo original affiché sur 156 × 62 unités logiques avec interpolation haute qualité pour la réduction, sans modifier le PNG source.
- Chaque rack indique sa plage de **places physiques**, distincte des identifiants T des outils.
- États : vert présent, bleu hors magasin, rouge défectueux, ambre fin de vie, gris vide/indisponible. Les symboles, libellés et la légende complètent les couleurs, en clair comme en sombre. Une bordure indique la sélection indépendamment de l’état.
- Fiche : identité et état, place affectée et position, onglets vue d’ensemble/correcteurs, puis actions. Les champs d’édition restent dans un parcours séparé.
- « Préparer » et « Charger en broche » ouvrent une confirmation sur l’outil choisi. L’occupant précédent est annoncé et rendu à sa place fixe dans le simulateur. La sélection est suspendue pendant cette confirmation.
- Le service vérifie l’identité/révision de l’outil et de l’occupant de destination avant une mutation atomique ; un conflit exige une nouvelle confirmation. Les outils défectueux/en fin de vie sont refusés.

Cette règle de remplacement instantané est propre au simulateur. Elle ne définit ni le séquencement Beckhoff, ni les interverrouillages, ni l’acquittement d’un mouvement réel. Le contrat `SimulatedTransfer` reste provisoire : l’intégration plateforme nécessitera les opérations asynchrones et preuves métier décrites plus haut.

Recette Windows complémentaire : préparer un outil du magasin, vérifier le retour du précédent et le bandeau ; charger cet outil, vérifier la broche et la préparation libérée ; annuler une autre confirmation ; vérifier les mêmes parcours et la légende dans les deux thèmes aux deux résolutions. La compilation et les contrôles métier ne valident pas le rendu graphique Windows.

## Correction du placement et de l’édition — 14 septembre 2026

Les huit boutons rack mesurent 88 × 52 unités logiques. Les plages sans préfixe redondant ne passent plus à la ligne ; « places » figure dans le titre de chaque groupe. La grille comporte six colonnes, avec une bordure réservée même hors sélection. Les panneaux magasin/outil n’utilisent plus de ScrollViewer ; les résultats de recherche remplacent temporairement la grille et restent paginés.

L’édition occupe la largeur des deux panneaux : nom, longueur et usure à gauche, pavé numérique à droite, Annuler/Vérifier en bas. La confirmation présente les trois valeurs avant/après. Les brouillons restent conservés au changement de thème. Une longueur négative ou à plus de trois décimales est refusée dans le simulateur, sans modifier les données.

La limitation initiale au nom/usure était un choix d’implémentation provisoire, pas une exigence du cahier des charges. Celui-ci demande la gestion des données d’outils et cite ST_Tool_Parameters ainsi que les correcteurs M et T. L’édition actuelle nom/longueur/usure **ne couvre pas encore** les durées de vie, outils jumeaux, vitesse maximale, géométrie, paramètres technologiques et jeux complets de correcteurs. Leurs unités, échelles et règles d’écriture PLC ne sont pas supposées validées. Cette correction ne revendique pas la couverture complète du cahier des charges.

Les contrôles de placement exécutent le véritable arbre Avalonia avec Skia sur une plateforme sans affichage : deux tailles, deux thèmes, tous les racks, recherche, onglets, édition et confirmation. Ils vérifient le maintien des commandes dans les panneaux et l’absence de chevauchement avec la zone d’actions. Ils ne remplacent pas la recette tactile et le contrôle DPI Windows sur le FIP1000.
