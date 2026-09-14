# HMI Avalonia — première tranche simulée

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

La fenêtre démarre avec Width=1024 et Height=768 ; elle est redimensionnable. Maximiser sur le FIP1000 Full HD 1920 × 1080. F11 active/quitte le plein écran. Les dimensions Avalonia sont logiques : vérifier aussi l’échelle Windows, la surface cliente et les décorations. Les panneaux peuvent défiler verticalement, les boutons de sélection restent tactiles. La fiche reste à droite ; aucune mise à l’échelle globale ne réduit les cibles au redimensionnement.

## Fonctions

- 140 positions déclarées, 137 places physiques nominales ; positions 1, 2, 96 interdites.
- Huit racks, sélection d’une place, accès broche/préparé et recherche paginée.
- Distinction entre outil, place fixe et position actuelle.
- Données/correcteurs, édition du nom et d’une usure fictive, pavé numérique, vérification, confirmation ou annulation.
- Brouillon conservé lors du changement de thème ; sélection suspendue pendant l’édition.
- Contrôle de l’identité et de la révision côté service, sans écriture complète de structure.
- Thème clair au démarrage, thème sombre sélectionnable ; logos WM originaux adaptés au fond.

La position « préparé » du simulateur ne prouve aucune position physique réelle. Les trois décimales et trente caractères sont des règles du jeu simulé, pas des facteurs d’échelle ou une validation d’encodage automate. Les boutons broche/préparé naviguent vers les outils : ils ne commandent aucun mouvement. Les blocages et occupations sont fictifs.

## Structure et migration

- `MagasinOutil.Core` : modèle métier immuable, contrat de service et simulateur protégé contre les modifications concurrentes.
- `MagasinOutil.Desktop` : fenêtre Avalonia et état de présentation local. La vue est construite en C# pour cette première tranche.
- `MagasinOutil.Checks` : contrôles exécutables du modèle, sans moteur graphique ni dépendance automate.

`MagasinOutil.Hmi.slnx` est indépendante de `Tool_Magazine_8xx.sln`. Aucune configuration TwinCAT n’est changée. Aucun fichier de propriétés MSBuild commun n’est ajouté à la racine pour éviter d’influencer les projets automate.

Référence examinée : PlateformeWM-Demo, branche p6-7-live-commissioning-impl, révision be8e3393bf1884dff8b4b1989ee46ee0b6f39012. Le renderer existant est essentiellement textuel. Aucun code privé de la plateforme n’est copié dans ce dépôt public. Les contrats locaux sont provisoires et n’amendent pas Architecture V1.3.

Le futur service live devra introduire observation asynchrone, qualité/fraîcheur/provenance, admission, autorisation, opérations corrélées et preuve métier. Ne pas adapter une réussite d’écriture ADS en `AppliedInSimulation` : cet état est exclusivement simulé. Le cycle de vie de la connexion et des opérations devra rester indépendant de la fenêtre. Les unités, symboles, propriétaires de données et acquittements Beckhoff/FANUC restent à préciser avant les écritures.

## Charte WM

Référence : Charte graphique v1.4 FR, pages 7–8, 12 et 15–18. Bleu WM #202945 et blanc principaux ; bleu noir #051C2C pour les surfaces sombres. Les couleurs de sélection sont des nuances dédiées, sans bleu motif réservé aux motifs. Logos PNG fournis par l’utilisateur, intégrés sans modification, déformation, filtre ni réduction d’opacité. Espace de protection ménagé dans l’en-tête. Les logos et la marque WM restent la propriété de Willemin-Macodel ; la licence du code ne concède pas de droit sur la marque.

Poppins Regular est prévue comme ressource locale avec sa licence OFL. Si l’actif n’est pas disponible, le code utilise explicitement la police système ; aucune requête de police n’a lieu au lancement.

## Vérification

Le workflow `.github/workflows/hmi.yml` compile uniquement la solution HMI puis exécute les contrôles métier. Il ne compile pas TwinCAT et ne se connecte à aucune machine. La compilation locale n’a pas pu être exécutée dans l’environnement initial faute de SDK .NET ; consulter le résultat réel du workflow, sans considérer son existence comme preuve de réussite.

Recette à effectuer sur Windows :

1. Ouvrir à 1024 × 768, puis maximiser à 1920 × 1080 ; vérifier absence de chevauchement et accès aux actions par défilement si nécessaire.
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
